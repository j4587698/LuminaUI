using System;

namespace LuminaUI.Controls;

public sealed class LuminaBackRequestedEventArgs : EventArgs
{
    public bool Handled { get; set; }
}
