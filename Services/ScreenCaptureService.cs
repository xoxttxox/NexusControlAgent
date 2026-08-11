using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace NexusControl.Agent.Services;

internal sealed class ScreenCaptureService
{
    private const int MaximumJpegBytes = 150 * 1024;
    private const int SmoothMaximumJpegBytes = 110 * 1024;
    private static readonly int[] CandidateWidths = [960, 800, 720, 640];
    private static readonly long[] CandidateQualities = [52L, 44L, 36L, 28L];
    private static readonly int[] SmoothCandidateWidths = [800, 720, 640, 560];
    private static readonly long[] SmoothCandidateQualities = [46L, 38L, 30L, 24L];
    private static readonly ImageCodecInfo JpegEncoder = ImageCodecInfo
        .GetImageEncoders()
        .First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);

    public ScreenFrame Capture(int displayIndex = 0)
    {
        var frame = CaptureJpeg(displayIndex: displayIndex);
        return new ScreenFrame(
            $"data:image/jpeg;base64,{Convert.ToBase64String(frame.Bytes)}",
            frame.Width,
            frame.Height,
            frame.CapturedAt);
    }

    public IReadOnlyList<ScreenDisplay> GetDisplays()
    {
        EnsureCaptureIsAvailable();
        return EnumerateDisplays();
    }

    public ScreenJpegFrame CaptureJpeg(
        bool smooth = false,
        int displayIndex = 0)
    {
        EnsureCaptureIsAvailable();

        var displays = EnumerateDisplays();
        if (displayIndex < 0 || displayIndex >= displays.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(displayIndex),
                "Der ausgewählte Bildschirm ist nicht mehr verfügbar.");
        }

        var display = displays[displayIndex];
        var bounds = display.Bounds;
        using var source = new Bitmap(
            bounds.Width,
            bounds.Height,
            PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(source))
        {
            graphics.CopyFromScreen(
                bounds.X,
                bounds.Y,
                0,
                0,
                bounds.Size,
                CopyPixelOperation.SourceCopy);
        }

        ScreenJpegFrame? smallestFrame = null;
        var maximumBytes = smooth
            ? SmoothMaximumJpegBytes
            : MaximumJpegBytes;
        var candidateWidths = smooth
            ? SmoothCandidateWidths
            : CandidateWidths;
        var candidateQualities = smooth
            ? SmoothCandidateQualities
            : CandidateQualities;

        foreach (var candidateWidth in candidateWidths)
        {
            var targetWidth = Math.Min(bounds.Width, candidateWidth);
            var targetHeight = Math.Max(
                1,
                (int)Math.Round(
                    bounds.Height * targetWidth / (double)bounds.Width));
            using var target = Resize(source, targetWidth, targetHeight);

            foreach (var quality in candidateQualities)
            {
                var bytes = EncodeJpeg(target, quality);
                smallestFrame = new ScreenJpegFrame(
                    bytes,
                    targetWidth,
                    targetHeight,
                    DateTimeOffset.UtcNow);
                if (bytes.Length <= maximumBytes)
                {
                    return smallestFrame;
                }
            }

            if (targetWidth == bounds.Width)
            {
                break;
            }
        }

        return smallestFrame
            ?? throw new InvalidOperationException(
                "Windows konnte kein Bildschirmbild erstellen.");
    }

    public ScreenJpegFrame CaptureStreamJpeg(
        int displayIndex,
        ScreenStreamProfile profile)
    {
        EnsureCaptureIsAvailable();

        var displays = EnumerateDisplays();
        if (displayIndex < 0 || displayIndex >= displays.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(displayIndex),
                "Der ausgewählte Bildschirm ist nicht mehr verfügbar.");
        }

        return CaptureStreamJpeg(displays[displayIndex], profile);
    }

    public ScreenJpegFrame CaptureStreamJpeg(
        ScreenDisplay display,
        ScreenStreamProfile profile)
    {
        var bounds = display.Bounds;
        var targetWidth = Math.Min(bounds.Width, profile.MaximumWidth);
        var targetHeight = Math.Max(
            1,
            (int)Math.Round(
                bounds.Height * targetWidth / (double)bounds.Width));
        using var target = CaptureScaled(bounds, targetWidth, targetHeight);
        return new ScreenJpegFrame(
            EncodeJpeg(target, profile.JpegQuality),
            targetWidth,
            targetHeight,
            DateTimeOffset.UtcNow);
    }

    private static void EnsureCaptureIsAvailable()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new InvalidOperationException(
                "Bildschirmübertragung benötigt Windows.");
        }
        if (!Environment.UserInteractive)
        {
            throw new InvalidOperationException(
                "Der Agent muss im angemeldeten Windows-Benutzerkonto laufen.");
        }
    }

    private static Bitmap Resize(Bitmap source, int width, int height)
    {
        var target = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(target);
        graphics.CompositingQuality = CompositingQuality.HighSpeed;
        graphics.InterpolationMode = InterpolationMode.Bilinear;
        graphics.SmoothingMode = SmoothingMode.HighSpeed;
        graphics.DrawImage(source, new Rectangle(0, 0, width, height));
        return target;
    }

    private static Bitmap CaptureScaled(
        Rectangle sourceBounds,
        int targetWidth,
        int targetHeight)
    {
        var target = new Bitmap(
            targetWidth,
            targetHeight,
            PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(target);
        var targetDeviceContext = graphics.GetHdc();
        var sourceDeviceContext = GetDC(IntPtr.Zero);
        try
        {
            try
            {
                if (sourceDeviceContext == IntPtr.Zero)
                {
                    throw new Win32Exception(
                        Marshal.GetLastWin32Error(),
                        "Windows konnte den Desktop nicht öffnen.");
                }

                _ = SetStretchBltMode(targetDeviceContext, 3);
                if (!StretchBlt(
                        targetDeviceContext,
                        0,
                        0,
                        targetWidth,
                        targetHeight,
                        sourceDeviceContext,
                        sourceBounds.X,
                        sourceBounds.Y,
                        sourceBounds.Width,
                        sourceBounds.Height,
                        0x00CC0020))
                {
                    throw new Win32Exception(
                        Marshal.GetLastWin32Error(),
                        "Windows konnte den Bildschirm nicht skalieren.");
                }
            }
            finally
            {
                if (sourceDeviceContext != IntPtr.Zero)
                {
                    _ = ReleaseDC(IntPtr.Zero, sourceDeviceContext);
                }
                graphics.ReleaseHdc(targetDeviceContext);
            }
        }
        catch
        {
            target.Dispose();
            throw;
        }

        return target;
    }

    private static byte[] EncodeJpeg(Bitmap bitmap, long quality)
    {
        using var output = new MemoryStream();
        using var encoderParameters = new EncoderParameters(1);
        encoderParameters.Param[0] = new EncoderParameter(
            System.Drawing.Imaging.Encoder.Quality,
            quality);
        bitmap.Save(output, JpegEncoder, encoderParameters);
        return output.ToArray();
    }

    private static IReadOnlyList<ScreenDisplay> EnumerateDisplays()
    {
        var monitors = new List<(Rectangle Bounds, bool IsPrimary, string Name)>();
        _ = EnumDisplayMonitors(
            IntPtr.Zero,
            IntPtr.Zero,
            (monitor, _, _, _) =>
            {
                var info = new MonitorInfoEx
                {
                    Size = Marshal.SizeOf<MonitorInfoEx>(),
                };
                if (GetMonitorInfo(monitor, ref info))
                {
                    var bounds = Rectangle.FromLTRB(
                        info.Monitor.Left,
                        info.Monitor.Top,
                        info.Monitor.Right,
                        info.Monitor.Bottom);
                    if (bounds.Width > 0 && bounds.Height > 0)
                    {
                        monitors.Add((
                            bounds,
                            (info.Flags & 1) != 0,
                            info.Device?.TrimEnd('\0') ?? string.Empty));
                    }
                }
                return true;
            },
            IntPtr.Zero);

        if (monitors.Count == 0)
        {
            throw new InvalidOperationException(
                "Windows hat keine aktive Anzeige erkannt.");
        }

        return monitors
            .OrderByDescending(display => display.IsPrimary)
            .ThenBy(display => display.Bounds.X)
            .ThenBy(display => display.Bounds.Y)
            .Select((display, index) => new ScreenDisplay(
                index,
                string.IsNullOrWhiteSpace(display.Name)
                    ? $"Bildschirm {index + 1}"
                    : $"Bildschirm {index + 1}",
                display.Bounds.Width,
                display.Bounds.Height,
                display.IsPrimary,
                display.Bounds))
            .ToArray();
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(
        IntPtr deviceContext,
        IntPtr clipRectangle,
        MonitorEnumProc callback,
        IntPtr data);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr window);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(
        IntPtr window,
        IntPtr deviceContext);

    [DllImport("gdi32.dll")]
    private static extern int SetStretchBltMode(
        IntPtr deviceContext,
        int stretchMode);

    [DllImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool StretchBlt(
        IntPtr destinationDeviceContext,
        int destinationX,
        int destinationY,
        int destinationWidth,
        int destinationHeight,
        IntPtr sourceDeviceContext,
        int sourceX,
        int sourceY,
        int sourceWidth,
        int sourceHeight,
        int rasterOperation);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(
        IntPtr monitor,
        ref MonitorInfoEx monitorInfo);

    private delegate bool MonitorEnumProc(
        IntPtr monitor,
        IntPtr deviceContext,
        IntPtr monitorRectangle,
        IntPtr data);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfoEx
    {
        public int Size;
        public NativeRectangle Monitor;
        public NativeRectangle WorkArea;
        public int Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Device;
    }
}

internal sealed record ScreenDisplay(
    int Id,
    string Name,
    int Width,
    int Height,
    bool IsPrimary,
    Rectangle Bounds);

internal sealed record ScreenStreamProfile(
    string Name,
    int MaximumWidth,
    long JpegQuality)
{
    public ScreenStreamProfile Degrade()
    {
        if (MaximumWidth > 1280)
        {
            return new($"{Name}-adaptive", 1280, 70);
        }
        if (MaximumWidth > 960)
        {
            return new($"{Name}-adaptive", 960, 62);
        }
        return this;
    }

    public static ScreenStreamProfile Resolve(string? name) =>
        name?.Trim().ToLowerInvariant() switch
        {
            "quality" => new("quality", 1600, 78),
            "balanced" => new("balanced", 1280, 70),
            _ => new("performance", 960, 60),
        };
}

internal sealed record ScreenJpegFrame(
    byte[] Bytes,
    int Width,
    int Height,
    DateTimeOffset CapturedAt);

internal sealed record ScreenFrame(
    string DataUrl,
    int Width,
    int Height,
    DateTimeOffset CapturedAt);
