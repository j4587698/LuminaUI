using System;
using System.IO;
using Avalonia.Platform;
using LuminaUI.Controls;
using LuminaUI.Demo.ViewModels;

namespace LuminaUI.Demo.Views;

public partial class MarkdownShowcasePage : LuminaPage
{
    private static readonly Uri DocumentUri = new("avares://LuminaUI.Demo/Assets/markdown-showcase.md");

    public MarkdownShowcasePage()
    {
        DataContext = new StaticShowcaseViewModel();
        InitializeComponent();

        using Stream stream = AssetLoader.Open(DocumentUri);
        using StreamReader reader = new(stream);
        Document.Text = reader.ReadToEnd();
    }
}
