using System.Reflection;
using Avalonia.Controls;

namespace LuminaUI.Demo.Views.Popups;

public partial class AboutDialogView : UserControl
{
    public string AppVersion => $"Version {Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "Unknown"}";

    public AboutDialogView()
    {
        InitializeComponent();
    }
}