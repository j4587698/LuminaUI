using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace LuminaUI.Controls;

public class LuminaFloatingWindow : Window
{
    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;

    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_FRAMECHANGED = 0x0020;

    [System.Runtime.InteropServices.DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern IntPtr sel_registerName(string name);

    [System.Runtime.InteropServices.DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern void objc_msgSend(IntPtr receiver, IntPtr selector, byte arg);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct XRectangle
    {
        public short x, y;
        public ushort width, height;
    }

    [System.Runtime.InteropServices.DllImport("libX11.so.6")]
    private static extern IntPtr XOpenDisplay(IntPtr display);

    [System.Runtime.InteropServices.DllImport("libX11.so.6")]
    private static extern int XCloseDisplay(IntPtr display);

    [System.Runtime.InteropServices.DllImport("libXext.so.6")]
    private static extern void XShapeCombineRectangles(IntPtr display, IntPtr dest, int dest_kind, int x_off, int y_off, ref XRectangle rectangles, int n_rects, int op, int ordering);

    [System.Runtime.InteropServices.DllImport("libXext.so.6", EntryPoint = "XShapeCombineRectangles")]
    private static extern void XShapeCombineRectanglesEmpty(IntPtr display, IntPtr dest, int dest_kind, int x_off, int y_off, IntPtr rectangles, int n_rects, int op, int ordering);

    private const int ShapeInput = 2;
    private const int ShapeSet = 0;

    public static readonly StyledProperty<bool> IsLockedProperty =
        AvaloniaProperty.Register<LuminaFloatingWindow, bool>(nameof(IsLocked));

    public static readonly StyledProperty<bool> IsHoveredProperty =
        AvaloniaProperty.Register<LuminaFloatingWindow, bool>(nameof(IsHovered));

    public bool IsLocked
    {
        get => GetValue(IsLockedProperty);
        set => SetValue(IsLockedProperty, value);
    }

    public bool IsHovered
    {
        get => GetValue(IsHoveredProperty);
        set => SetValue(IsHoveredProperty, value);
    }

    static LuminaFloatingWindow()
    {
        IsLockedProperty.Changed.AddClassHandler<LuminaFloatingWindow>((w, e) => w.OnIsLockedChanged());
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (!IsLocked)
        {
            IsHovered = true;
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        IsHovered = false;
    }

    protected void BeginDrag(PointerPressedEventArgs e)
    {
        if (!IsLocked)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnIsLockedChanged()
    {
        if (IsLocked)
        {
            IsHovered = false;
        }

        UpdatePassthroughState();
    }

    public void UpdatePassthroughState()
    {
        var handleInfo = TryGetPlatformHandle();
        if (handleInfo == null) return;

        if (OperatingSystem.IsWindows() && handleInfo.HandleDescriptor == "HWND")
        {
            var hwnd = handleInfo.Handle;
            var style = GetWindowLong(hwnd, GWL_EXSTYLE);
            if (IsLocked)
                style |= WS_EX_TRANSPARENT | WS_EX_LAYERED;
            else
                style &= ~WS_EX_TRANSPARENT;

            SetWindowLong(hwnd, GWL_EXSTYLE, style);
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
        }
        else if (OperatingSystem.IsMacOS() && handleInfo.HandleDescriptor == "NSWindow")
        {
            try
            {
                var nsWindow = handleInfo.Handle;
                var sel = sel_registerName("setIgnoresMouseEvents:");
                objc_msgSend(nsWindow, sel, IsLocked ? (byte)1 : (byte)0);
            }
            catch { }
        }
        else if (OperatingSystem.IsLinux() && handleInfo.HandleDescriptor == "XID")
        {
            try
            {
                var xid = handleInfo.Handle;
                var display = XOpenDisplay(IntPtr.Zero);
                if (display != IntPtr.Zero)
                {
                    if (IsLocked)
                    {
                        XShapeCombineRectanglesEmpty(display, xid, ShapeInput, 0, 0, IntPtr.Zero, 0, ShapeSet, 0);
                    }
                    else
                    {
                        var rect = new XRectangle { x = 0, y = 0, width = (ushort)Bounds.Width, height = (ushort)Bounds.Height };
                        XShapeCombineRectangles(display, xid, ShapeInput, 0, 0, ref rect, 1, ShapeSet, 0);
                    }
                    XCloseDisplay(display);
                }
            }
            catch { }
        }
    }

    public void PositionAtScreenBottom(double offset = 80)
    {
        if (Screens.Primary is { } primaryScreen)
        {
            var workingArea = primaryScreen.WorkingArea;
            var x = workingArea.X + (workingArea.Width - Width) / 2;
            var y = workingArea.Bottom - Height - offset;
            Position = new PixelPoint((int)x, (int)y);
        }
    }

    public void PositionAtScreenCenter()
    {
        if (Screens.Primary is { } primaryScreen)
        {
            var workingArea = primaryScreen.WorkingArea;
            var x = workingArea.X + (workingArea.Width - Width) / 2;
            var y = workingArea.Y + (workingArea.Height - Height) / 2;
            Position = new PixelPoint((int)x, (int)y);
        }
    }
}
