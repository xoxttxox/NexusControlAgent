using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace NexusControl.Agent.UI;

/// <summary>
/// Normale WinForms-GroupBox mit einem zum Dark-Theme passenden Rahmen.
/// Die Standard-GroupBox zeichnet ihren Rahmen immer mit einer Windows-Systemfarbe.
/// </summary>
internal sealed class DarkGroupBox : GroupBox
{
    private Color _borderColor = WinFormsTheme.Border;

    public DarkGroupBox()
    {
        SetStyle(
            ControlStyles.UserPaint
            | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw,
            true);
    }

    // Die Farbe wird ausschließlich durch WinFormsTheme gesetzt und soll nicht
    // vom Designer in InitializeComponent serialisiert werden. Die explizite
    // Angabe ist seit dem neuen WinForms-Analyzer (WFO1000) erforderlich.
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor
    {
        get => _borderColor;
        set
        {
            if (_borderColor == value)
            {
                return;
            }

            _borderColor = value;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        var bounds = ClientRectangle;
        eventArgs.Graphics.Clear(BackColor);

        if (bounds.Width <= 1 || bounds.Height <= 1)
        {
            return;
        }

        var borderTop = Math.Max(1, Font.Height / 2);
        var borderBounds = new Rectangle(
            0,
            borderTop,
            bounds.Width - 1,
            bounds.Height - borderTop - 1);

        using (var borderPen = new Pen(BorderColor))
        {
            eventArgs.Graphics.DrawRectangle(borderPen, borderBounds);
        }

        if (string.IsNullOrEmpty(Text))
        {
            return;
        }

        const TextFormatFlags textFlags =
            TextFormatFlags.Left
            | TextFormatFlags.NoPadding
            | TextFormatFlags.NoPrefix
            | TextFormatFlags.SingleLine;
        var textSize = TextRenderer.MeasureText(
            eventArgs.Graphics,
            Text,
            Font,
            Size.Empty,
            textFlags);
        var textLeft = Padding.Left;
        var titleBackground = new Rectangle(
            Math.Max(0, textLeft - 3),
            0,
            Math.Min(textSize.Width + 6, bounds.Width - textLeft + 3),
            borderTop + 1);

        using (var backgroundBrush = new SolidBrush(BackColor))
        {
            eventArgs.Graphics.FillRectangle(backgroundBrush, titleBackground);
        }

        TextRenderer.DrawText(
            eventArgs.Graphics,
            Text,
            Font,
            new Point(textLeft, 0),
            Enabled ? ForeColor : SystemColors.GrayText,
            BackColor,
            textFlags);
    }
}
