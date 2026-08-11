using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using NexusControl.Agent.Services;

namespace NexusControl.Agent.Windows;

internal sealed class WindowsController
{
    private readonly WindowsAudioService _audio;
    private readonly WindowsMediaSessionService _mediaSessions;

    private static readonly HashSet<string> ProtectedProcesses = new(
        [
            "system",
            "idle",
            "registry",
            "smss",
            "csrss",
            "wininit",
            "services",
            "lsass",
            "svchost",
            "winlogon",
            "fontdrvhost",
            "nexuscontrolagent",
        ],
        StringComparer.OrdinalIgnoreCase);

    public WindowsController(
        WindowsAudioService audio,
        WindowsMediaSessionService mediaSessions)
    {
        _audio = audio;
        _mediaSessions = mediaSessions;
    }

    internal static bool IsProtectedProcessName(string processName) =>
        ProtectedProcesses.Contains(processName);

    public bool IsPowerCommand(string command) =>
        command is "system.sleep" or "system.restart" or "system.shutdown";

    public string Execute(string command, JsonElement parameters)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new InvalidOperationException("Dieser Befehl benötigt Windows.");
        }

        return command switch
        {
            "system.wake" => "Der PC ist bereits eingeschaltet.",
            "session.lock" => LockSession(),
            "system.sleep" => Sleep(),
            "system.restart" => RunShutdown("/r /t 0", "Neustart wurde gestartet."),
            "system.shutdown" => RunShutdown("/s /t 0", "Herunterfahren wurde gestartet."),
            "media.playPause" => PressVirtualKey(0xB3, "Wiedergabe umgeschaltet."),
            "media.next" => PressVirtualKey(0xB0, "Nächster Titel."),
            "media.previous" => PressVirtualKey(0xB1, "Vorheriger Titel."),
            "media.session.playPause" => ControlMediaSession(
                ReadString(parameters, "sessionId"),
                "playPause"),
            "media.session.next" => ControlMediaSession(
                ReadString(parameters, "sessionId"),
                "next"),
            "media.session.previous" => ControlMediaSession(
                ReadString(parameters, "sessionId"),
                "previous"),
            "media.session.setVolume" => _mediaSessions.SetVolume(
                ReadString(parameters, "sessionId"),
                ReadInt(parameters, "value", 0, 100)),
            "media.session.toggleMute" => _mediaSessions.ToggleMute(
                ReadString(parameters, "sessionId")),
            "audio.toggleMute" => _audio.ToggleMute(),
            "audio.setVolume" => _audio.SetVolume(
                ReadInt(parameters, "value", 0, 100)),
            "input.pointerMove" => MovePointer(
                ReadInt(parameters, "deltaX", -250, 250),
                ReadInt(parameters, "deltaY", -250, 250)),
            "input.pointerButton" => ClickPointer(
                ReadString(parameters, "button"),
                ReadOptionalString(parameters, "action", "click")),
            "input.pointerScroll" => ScrollPointer(
                ReadInt(parameters, "delta", -1200, 1200)),
            "input.keyboardText" => TypeText(ReadString(parameters, "text")),
            "process.terminate" => TerminateProcess(
                ReadInt(parameters, "processId", 1, int.MaxValue)),
            _ => throw new InvalidOperationException(
                $"Befehl „{command}“ ist nicht freigegeben."),
        };
    }

    private static string LockSession()
    {
        if (!LockWorkStation())
        {
            throw new InvalidOperationException("Windows konnte nicht gesperrt werden.");
        }
        return "Windows wurde gesperrt.";
    }

    private static string Sleep()
    {
        if (!SetSuspendState(false, false, false))
        {
            throw new InvalidOperationException(
                "Standby wurde von Windows oder einem Gerätetreiber abgelehnt.");
        }
        return "Standby wurde gestartet.";
    }

    private static string RunShutdown(string arguments, string message)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "shutdown.exe",
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
        });
        return message;
    }

    private static string MovePointer(int deltaX, int deltaY)
    {
        SendMouseInput(deltaX, deltaY, MouseEventMove);
        return "Mauszeiger bewegt.";
    }

    private static string ScrollPointer(int delta)
    {
        SendMouseInput(
            0,
            0,
            MouseEventWheel,
            unchecked((uint)delta));
        return "Mausrad bewegt.";
    }

    private static string ClickPointer(string button, string action)
    {
        var (down, up) = button.ToLowerInvariant() switch
        {
            "left" => (MouseEventLeftDown, MouseEventLeftUp),
            "right" => (MouseEventRightDown, MouseEventRightUp),
            _ => throw new InvalidOperationException("Unbekannte Maustaste."),
        };

        var normalizedAction = action.ToLowerInvariant();
        switch (normalizedAction)
        {
            case "click":
                SendMouseSequence(down, up);
                break;
            case "doubleclick":
                SendMouseSequence(down, up, down, up);
                break;
            case "down":
                SendMouseSequence(down);
                break;
            case "up":
                SendMouseSequence(up);
                break;
            default:
                throw new InvalidOperationException(
                    "Unbekannte Maustasten-Aktion.");
        }

        var buttonName = button.Equals(
            "right",
            StringComparison.OrdinalIgnoreCase)
            ? "Rechte"
            : "Linke";
        return $"{buttonName} Maustaste: {normalizedAction}.";
    }

    private string ControlMediaSession(string sessionId, string action)
    {
        _mediaSessions.ControlAsync(sessionId, action).GetAwaiter().GetResult();
        return "Mediensitzung wurde gesteuert.";
    }

    private static string TypeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Es wurde kein Text übermittelt.");
        }

        text = text[..Math.Min(text.Length, 4000)];
        foreach (var character in text)
        {
            var inputs = new[]
            {
                CreateUnicodeInput(character, false),
                CreateUnicodeInput(character, true),
            };
            if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) == 0)
            {
                throw new InvalidOperationException(
                    "Windows hat die Tastatureingabe abgelehnt.");
            }
        }
        return "Text wurde eingegeben.";
    }

    private static string TerminateProcess(int processId)
    {
        if (processId == Environment.ProcessId)
        {
            throw new InvalidOperationException("Der Nexus Agent ist geschützt.");
        }

        using var process = Process.GetProcessById(processId);
        var processName = process.ProcessName;
        if (IsProtectedProcessName(processName))
        {
            throw new InvalidOperationException(
                "Dieser Windows-Systemprozess ist geschützt.");
        }

        process.Kill(true);
        return $"{processName} wurde beendet.";
    }

    private static string PressVirtualKey(byte key, string message)
    {
        PressKey(key);
        return message;
    }

    private static void PressKey(byte key)
    {
        KeybdEvent(key, 0, 0, UIntPtr.Zero);
        KeybdEvent(key, 0, KeyEventKeyUp, UIntPtr.Zero);
    }

    private static void SendMouseInput(
        int deltaX,
        int deltaY,
        uint flags,
        uint mouseData = 0)
    {
        SendMouseInputs(
            new Input
            {
                Type = InputMouse,
                Data = new InputUnion
                {
                    Mouse = new MouseInput
                    {
                        DeltaX = deltaX,
                        DeltaY = deltaY,
                        MouseData = mouseData,
                        Flags = flags,
                    },
                },
            });
    }

    private static void SendMouseSequence(params uint[] flags)
    {
        SendMouseInputs(flags.Select(flag => new Input
        {
            Type = InputMouse,
            Data = new InputUnion
            {
                Mouse = new MouseInput
                {
                    Flags = flag,
                },
            },
        }).ToArray());
    }

    private static void SendMouseInputs(params Input[] inputs)
    {
        if (
            inputs.Length == 0 ||
            SendInput(
                (uint)inputs.Length,
                inputs,
                Marshal.SizeOf<Input>()) != (uint)inputs.Length
        )
        {
            throw new InvalidOperationException("Windows hat die Mauseingabe abgelehnt.");
        }
    }

    private static Input CreateUnicodeInput(char character, bool keyUp) =>
        new()
        {
            Type = InputKeyboard,
            Data = new InputUnion
            {
                Keyboard = new KeyboardInput
                {
                    Scan = (ushort)character,
                    Flags = KeyEventUnicode | (keyUp ? KeyEventKeyUp : 0),
                },
            },
        };

    private static int ReadInt(
        JsonElement parameters,
        string property,
        int minimum,
        int maximum)
    {
        if (parameters.ValueKind != JsonValueKind.Object
            || !parameters.TryGetProperty(property, out var element)
            || !element.TryGetInt32(out var value))
        {
            throw new InvalidOperationException($"Parameter „{property}“ fehlt.");
        }
        return Math.Clamp(value, minimum, maximum);
    }

    private static string ReadString(JsonElement parameters, string property)
    {
        if (parameters.ValueKind != JsonValueKind.Object
            || !parameters.TryGetProperty(property, out var element))
        {
            throw new InvalidOperationException($"Parameter „{property}“ fehlt.");
        }
        return element.GetString() ?? "";
    }

    private static string ReadOptionalString(
        JsonElement parameters,
        string property,
        string fallback)
    {
        if (parameters.ValueKind != JsonValueKind.Object
            || !parameters.TryGetProperty(property, out var element))
        {
            return fallback;
        }
        return element.GetString() ?? fallback;
    }

    private const uint InputMouse = 0;
    private const uint InputKeyboard = 1;
    private const uint MouseEventMove = 0x0001;
    private const uint MouseEventLeftDown = 0x0002;
    private const uint MouseEventLeftUp = 0x0004;
    private const uint MouseEventRightDown = 0x0008;
    private const uint MouseEventRightUp = 0x0010;
    private const uint MouseEventWheel = 0x0800;
    private const uint KeyEventKeyUp = 0x0002;
    private const uint KeyEventUnicode = 0x0004;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LockWorkStation();

    [DllImport("powrprof.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetSuspendState(
        [MarshalAs(UnmanagedType.Bool)] bool hibernate,
        [MarshalAs(UnmanagedType.Bool)] bool forceCritical,
        [MarshalAs(UnmanagedType.Bool)] bool disableWakeEvent);

    [DllImport("user32.dll", EntryPoint = "keybd_event")]
    private static extern void KeybdEvent(
        byte virtualKey,
        byte scanCode,
        uint flags,
        UIntPtr extraInfo);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(
        uint inputCount,
        Input[] inputs,
        int inputSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MouseInput Mouse;

        [FieldOffset(0)]
        public KeyboardInput Keyboard;

        [FieldOffset(0)]
        public HardwareInput Hardware;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int DeltaX;
        public int DeltaY;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort Scan;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HardwareInput
    {
        public uint Message;
        public ushort ParameterLow;
        public ushort ParameterHigh;
    }
}
