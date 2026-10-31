using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LuminaUI.Controls;
using Xunit;

namespace LuminaUI.Tests;

public class PopConfirmTests
{
    [AvaloniaFact]
    public void DropdownPopupHasOnlyCancelAndConfirmButtons()
    {
        // The default template used to carry a top-right close button whose icon was an open
        // stroke path. PathIcon only fills, so it rendered as an empty clickable square that
        // duplicated Cancel. The popup now exposes exactly the two action buttons.
        WithOpenPopup(confirm => { }, (popupChild, _) =>
        {
            string?[] buttons =
            [
                .. popupChild.GetLogicalDescendants()
                    .OfType<Button>()
                    .Select(button => button.Name)
                    .Order(StringComparer.Ordinal),
            ];

            Assert.Equal(
                [LuminaPopConfirm.PART_CancelButton, LuminaPopConfirm.PART_ConfirmButton],
                buttons);
        });
    }

    [AvaloniaFact]
    public void DefaultIconIsCentredOnTheTextBlock()
    {
        WithOpenPopup(
            confirm => confirm.PopupContent = "This removes the record and everything attached to it.",
            (popupChild, _) =>
            {
                Border badge = FindNamed<Border>(popupChild, "PART_IconBadge");
                Control text = (Control)popupChild.GetVisualDescendants()
                    .OfType<StackPanel>()
                    .First(panel => Grid.GetColumn(panel) == 1 && Grid.GetRow(panel) == 0);

                double badgeCentre = CentreY(badge, popupChild);
                double textCentre = CentreY(text, popupChild);

                Assert.True(
                    Math.Abs(badgeCentre - textCentre) <= 1,
                    $"Icon badge centre {badgeCentre:F1} is not aligned with the text block centre {textCentre:F1}.");
            });
    }

    [AvaloniaFact]
    public void DefaultIconShowsAnExclamationMark()
    {
        // The old glyph drew the "!" as two zero-area line segments. PathIcon only fills, so the
        // mark vanished and a solid triangle was left. The dot of the "!" must be a hole: the
        // pixel there shows the badge, not the icon colour, while the ring above it is inked.
        WithOpenPopup(confirm => confirm.Classes.Add("Danger"), (popupChild, frame) =>
        {
            PathIcon icon = FindNamed<PathIcon>(popupChild, "PART_DefaultIcon");
            Color ink = Assert.IsAssignableFrom<ISolidColorBrush>(icon.Foreground).Color;
            TopLevel topLevel = TopLevel.GetTopLevel(icon)!;
            Point origin = icon.TranslatePoint(default, topLevel)!.Value;

            // The geometry spans 2..22 on a 24 grid and is stretched onto the icon box, so a
            // geometry point g maps to (g - 2) / 20 * size.
            double scale = icon.Bounds.Width / 20;
            Point ring = origin + new Point(10 * scale, 1.5 * scale);
            Point dot = origin + new Point(10 * scale, 14.6 * scale);

            Assert.True(IsNear(Sample(frame, ring), ink), $"Ring pixel {Sample(frame, ring)} should be the icon colour {ink}.");
            Assert.False(IsNear(Sample(frame, dot), ink), $"Exclamation dot pixel {Sample(frame, dot)} should be cut out of the icon, not filled with {ink}.");
        });
    }

    private static void WithOpenPopup(Action<LuminaPopConfirm> setup, Action<Control, WriteableBitmap> assert)
    {
        LuminaPopConfirm confirm = new()
        {
            PopupType = LuminaPopupType.Dropdown,
            PopupHeader = "Delete?",
            Content = new Button { Content = "Delete" },
        };
        setup(confirm);

        Window window = new() { Width = 420, Height = 320, Content = confirm };
        window.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            confirm.IsOpen = true;
            Dispatcher.UIThread.RunJobs();

            Popup popup = confirm.GetVisualDescendants().OfType<Popup>().Single();
            Assert.True(popup.IsOpen);
            Control child = Assert.IsAssignableFrom<Control>(popup.Child);

            using WriteableBitmap frame = TopLevel.GetTopLevel(child)!.CaptureRenderedFrame()
                ?? throw new InvalidOperationException("No frame was rendered.");
            assert(child, frame);
        }
        finally
        {
            window.Close();
        }
    }

    private static T FindNamed<T>(Control root, string name) where T : Control =>
        root.GetVisualDescendants().OfType<T>().Single(control => control.Name == name);

    private static double CentreY(Control control, Visual relativeTo) =>
        control.TranslatePoint(new Point(0, control.Bounds.Height / 2), relativeTo)!.Value.Y;

    private static Color Sample(WriteableBitmap frame, Point point)
    {
        using ILockedFramebuffer buffer = frame.Lock();
        bool bgra = buffer.Format == PixelFormat.Bgra8888;
        int offset = ((int)point.Y * buffer.RowBytes) + ((int)point.X * 4);
        byte c0 = Marshal.ReadByte(buffer.Address, offset);
        byte g = Marshal.ReadByte(buffer.Address, offset + 1);
        byte c2 = Marshal.ReadByte(buffer.Address, offset + 2);
        return bgra ? Color.FromRgb(c2, g, c0) : Color.FromRgb(c0, g, c2);
    }

    private static bool IsNear(Color a, Color b) =>
        Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B) < 60;
}
