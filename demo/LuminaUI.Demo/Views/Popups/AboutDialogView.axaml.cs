using System.Reflection;
using Avalonia.Controls;

namespace LuminaUI.Demo.Views.Popups;

public partial class AboutDialogView : UserControl
{
    public string AppVersion => $"版本 {Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "未知"}";

    public AboutDialogView()
    {
        InitializeComponent();
    }
}