using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NexusControl.Agent.UI;

/// <summary>
/// Kleine, zentrale Farb- und Native-Theme-Hilfe für normale WinForms-Controls.
/// Nur Rahmen, die WinForms nicht einfärben kann, werden passend nachgezeichnet.
/// </summary>
internal static class WinFormsTheme
{
    public static readonly Color Background = Color.FromArgb(21, 23, 26);
    public static readonly Color Surface = Color.FromArgb(28, 31, 36);
    public static readonly Color Input = Color.FromArgb(36, 39, 45);
    public static readonly Color Border = Color.FromArgb(52, 56, 64);
    public static readonly Color TextPrimary = Color.FromArgb(242, 242, 242);
    public static readonly Color TextMuted = Color.FromArgb(160, 165, 174);
    public static readonly Color Accent = Color.FromArgb(45, 125, 219);
    public static readonly Color AccentHover = Color.FromArgb(59, 140, 232);
    public static readonly Color Success = Color.FromArgb(66, 201, 135);

    private const int ImmersiveDarkMode = 20;
    private const int ImmersiveDarkModeLegacy = 19;

    public static void Apply(Form form)
    {
        form.BackColor = Background;
        form.ForeColor = TextPrimary;
        ApplyToControlTree(form);
        form.HandleCreated += (_, _) => ApplyNativeTitleBar(form);
        if (form.IsHandleCreated)
        {
            ApplyNativeTitleBar(form);
        }
    }

    public static ToolStripRenderer CreateToolStripRenderer() =>
        new ToolStripProfessionalRenderer(new DarkToolStripColorTable());

    private static void ApplyToControlTree(Control root)
    {
        StyleControl(root);
        foreach (Control child in root.Controls)
        {
            ApplyToControlTree(child);
        }
    }

    private static void StyleControl(Control control)
    {
        switch (control)
        {
            case Form form:
                form.BackColor = Background;
                form.ForeColor = TextPrimary;
                break;

            case GroupBox groupBox:
                groupBox.BackColor = Surface;
                groupBox.ForeColor = TextPrimary;
                if (groupBox is DarkGroupBox darkGroupBox)
                {
                    darkGroupBox.BorderColor = Border;
                }

                break;

            case TextBox textBox:
                var plainText = string.Equals(
                    textBox.Tag as string,
                    "plain",
                    StringComparison.OrdinalIgnoreCase);
                textBox.BackColor = plainText ? Background : Input;
                textBox.ForeColor = TextPrimary;
                textBox.BorderStyle = plainText
                    ? BorderStyle.None
                    : BorderStyle.FixedSingle;
                break;

            case ListBox listBox:
                listBox.BackColor = Input;
                listBox.ForeColor = TextPrimary;
                listBox.BorderStyle = string.Equals(
                    listBox.Tag as string,
                    "borderless",
                    StringComparison.OrdinalIgnoreCase)
                    ? BorderStyle.None
                    : BorderStyle.FixedSingle;
                break;

            case Panel panel when string.Equals(
                panel.Tag as string,
                "border",
                StringComparison.OrdinalIgnoreCase):
                panel.BackColor = Border;
                break;

            case Button button:
                var primary = string.Equals(
                    button.Tag as string,
                    "primary",
                    StringComparison.OrdinalIgnoreCase);
                button.UseVisualStyleBackColor = false;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = primary ? AccentHover : Border;
                button.FlatAppearance.MouseOverBackColor = primary
                    ? AccentHover
                    : Color.FromArgb(45, 49, 57);
                button.FlatAppearance.MouseDownBackColor = primary
                    ? Color.FromArgb(35, 105, 190)
                    : Input;
                button.BackColor = primary ? Accent : Input;
                button.ForeColor = TextPrimary;
                break;

            case CheckBox checkBox:
                checkBox.BackColor = checkBox.Parent?.BackColor ?? Surface;
                checkBox.ForeColor = TextPrimary;
                checkBox.UseVisualStyleBackColor = false;
                break;

            case Label label:
                label.BackColor = Color.Transparent;
                label.ForeColor = (label.Tag as string) switch
                {
                    "muted" => TextMuted,
                    "success" => Success,
                    _ => TextPrimary,
                };
                break;

            case StatusStrip statusStrip:
                statusStrip.BackColor = Surface;
                statusStrip.ForeColor = TextMuted;
                statusStrip.Renderer = CreateToolStripRenderer();
                foreach (ToolStripItem item in statusStrip.Items)
                {
                    item.ForeColor = TextMuted;
                }
                break;
        }
    }

    private static void ApplyNativeTitleBar(Form form)
    {
        if (!OperatingSystem.IsWindows() || !form.IsHandleCreated)
        {
            return;
        }

        try
        {
            var enabled = 1;
            var result = DwmSetWindowAttribute(
                form.Handle,
                ImmersiveDarkMode,
                ref enabled,
                sizeof(int));
            if (result != 0)
            {
                _ = DwmSetWindowAttribute(
                    form.Handle,
                    ImmersiveDarkModeLegacy,
                    ref enabled,
                    sizeof(int));
            }
        }
        catch (DllNotFoundException)
        {
            // Ältere Windows-Versionen verwenden ihre Standard-Titelleiste.
        }
        catch (EntryPointNotFoundException)
        {
            // Ältere Windows-Versionen verwenden ihre Standard-Titelleiste.
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int attribute,
        ref int attributeValue,
        int attributeSize);

    private sealed class DarkToolStripColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Surface;
        public override Color ImageMarginGradientBegin => Surface;
        public override Color ImageMarginGradientMiddle => Surface;
        public override Color ImageMarginGradientEnd => Surface;
        public override Color MenuBorder => Border;
        public override Color MenuItemBorder => Border;
        public override Color MenuItemSelected => Input;
        public override Color MenuItemSelectedGradientBegin => Input;
        public override Color MenuItemSelectedGradientEnd => Input;
        public override Color SeparatorDark => Border;
        public override Color SeparatorLight => Border;
        public override Color CheckBackground => Input;
        public override Color CheckSelectedBackground => Accent;
        public override Color CheckPressedBackground => AccentHover;
        public override Color StatusStripGradientBegin => Surface;
        public override Color StatusStripGradientEnd => Surface;
    }
}
