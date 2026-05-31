using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using LuminaUI.Extensions;

namespace LuminaUI.Controls;

public class LuminaPagination : TemplatedControl
{
    private StackPanel? _itemsHost;

    private TextBox? _quickJumpBox;

    private string _pageInfoText = string.Empty;

    private string _totalInfoText = string.Empty;

    public static readonly StyledProperty<int> CurrentPageProperty = AvaloniaProperty.Register<LuminaPagination, int>(nameof(CurrentPage), 1, inherits: false, BindingMode.TwoWay);

    public static readonly StyledProperty<int> PageCountProperty = AvaloniaProperty.Register<LuminaPagination, int>(nameof(PageCount), 1);

    public static readonly StyledProperty<int> TotalCountProperty = AvaloniaProperty.Register<LuminaPagination, int>(nameof(TotalCount), 0);

    public static readonly StyledProperty<int> PageSizeProperty = AvaloniaProperty.Register<LuminaPagination, int>(nameof(PageSize), 10, inherits: false, BindingMode.TwoWay);

    public static readonly StyledProperty<AvaloniaList<int>?> PageSizeOptionsProperty = AvaloniaProperty.Register<LuminaPagination, AvaloniaList<int>?>("PageSizeOptions");

    public static readonly StyledProperty<int> MaxVisiblePagesProperty = AvaloniaProperty.Register<LuminaPagination, int>(nameof(MaxVisiblePages), 7);

    public static readonly StyledProperty<bool> ShowEdgeButtonsProperty = AvaloniaProperty.Register<LuminaPagination, bool>(nameof(ShowEdgeButtons), defaultValue: true);

    public static readonly StyledProperty<bool> ShowPreviousNextButtonsProperty = AvaloniaProperty.Register<LuminaPagination, bool>(nameof(ShowPreviousNextButtons), defaultValue: true);

    public static readonly StyledProperty<bool> ShowFastJumpProperty = AvaloniaProperty.Register<LuminaPagination, bool>(nameof(ShowFastJump), defaultValue: true);

    public static readonly StyledProperty<bool> ShowPageInfoProperty = AvaloniaProperty.Register<LuminaPagination, bool>(nameof(ShowPageInfo), defaultValue: true);

    public static readonly StyledProperty<bool> ShowTotalProperty = AvaloniaProperty.Register<LuminaPagination, bool>(nameof(ShowTotal), defaultValue: false);

    public static readonly StyledProperty<bool> ShowQuickJumpProperty = AvaloniaProperty.Register<LuminaPagination, bool>(nameof(ShowQuickJump), defaultValue: false);

    public static readonly StyledProperty<bool> ShowPageSizeSelectorProperty = AvaloniaProperty.Register<LuminaPagination, bool>(nameof(ShowPageSizeSelector), defaultValue: false);

    public static readonly StyledProperty<bool> DisplayCurrentPageInQuickJumperProperty = AvaloniaProperty.Register<LuminaPagination, bool>(nameof(DisplayCurrentPageInQuickJumper), defaultValue: false);

    public static readonly StyledProperty<string> QuickJumpTextProperty = AvaloniaProperty.Register<LuminaPagination, string>(nameof(QuickJumpText), "Go to");

    public static readonly StyledProperty<string> PageTextProperty = AvaloniaProperty.Register<LuminaPagination, string>(nameof(PageText), "page");

    public static readonly StyledProperty<string> PageInfoFormatProperty = AvaloniaProperty.Register<LuminaPagination, string>(nameof(PageInfoFormat), "{0} / {1}");

    public static readonly StyledProperty<string> TotalTextFormatProperty = AvaloniaProperty.Register<LuminaPagination, string>(nameof(TotalTextFormat), "{0} items");

    public static readonly StyledProperty<ICommand?> PageChangedCommandProperty = AvaloniaProperty.Register<LuminaPagination, ICommand?>(nameof(PageChangedCommand));

    public static readonly DirectProperty<LuminaPagination, string> PageInfoTextProperty = AvaloniaProperty.RegisterDirect<LuminaPagination, string>(nameof(PageInfoText), (LuminaPagination pagination) => pagination.PageInfoText);

    public static readonly DirectProperty<LuminaPagination, string> TotalInfoTextProperty = AvaloniaProperty.RegisterDirect<LuminaPagination, string>(nameof(TotalInfoText), (LuminaPagination pagination) => pagination.TotalInfoText);

    public int CurrentPage
    {
        get => GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }

    public int PageCount
    {
        get => GetValue(PageCountProperty);
        set => SetValue(PageCountProperty, value);
    }

    public int TotalCount
    {
        get => GetValue(TotalCountProperty);
        set => SetValue(TotalCountProperty, value);
    }

    public int PageSize
    {
        get => GetValue(PageSizeProperty);
        set => SetValue(PageSizeProperty, value);
    }

    public AvaloniaList<int>? PageSizeOptions
    {
        get => GetValue(PageSizeOptionsProperty);
        set => SetValue(PageSizeOptionsProperty, value);
    }

    public int MaxVisiblePages
    {
        get => GetValue(MaxVisiblePagesProperty);
        set => SetValue(MaxVisiblePagesProperty, value);
    }

    public bool ShowEdgeButtons
    {
        get => GetValue(ShowEdgeButtonsProperty);
        set => SetValue(ShowEdgeButtonsProperty, value);
    }

    public bool ShowPreviousNextButtons
    {
        get => GetValue(ShowPreviousNextButtonsProperty);
        set => SetValue(ShowPreviousNextButtonsProperty, value);
    }

    public bool ShowFastJump
    {
        get => GetValue(ShowFastJumpProperty);
        set => SetValue(ShowFastJumpProperty, value);
    }

    public bool ShowPageInfo
    {
        get => GetValue(ShowPageInfoProperty);
        set => SetValue(ShowPageInfoProperty, value);
    }

    public bool ShowTotal
    {
        get => GetValue(ShowTotalProperty);
        set => SetValue(ShowTotalProperty, value);
    }

    public bool ShowQuickJump
    {
        get => GetValue(ShowQuickJumpProperty);
        set => SetValue(ShowQuickJumpProperty, value);
    }

    public bool ShowPageSizeSelector
    {
        get => GetValue(ShowPageSizeSelectorProperty);
        set => SetValue(ShowPageSizeSelectorProperty, value);
    }

    public bool DisplayCurrentPageInQuickJumper
    {
        get => GetValue(DisplayCurrentPageInQuickJumperProperty);
        set => SetValue(DisplayCurrentPageInQuickJumperProperty, value);
    }

    public string QuickJumpText
    {
        get => GetValue(QuickJumpTextProperty);
        set => SetValue(QuickJumpTextProperty, value);
    }

    public string PageText
    {
        get => GetValue(PageTextProperty);
        set => SetValue(PageTextProperty, value);
    }

    public string PageInfoFormat
    {
        get => GetValue(PageInfoFormatProperty);
        set => SetValue(PageInfoFormatProperty, value);
    }

    public string TotalTextFormat
    {
        get => GetValue(TotalTextFormatProperty);
        set => SetValue(TotalTextFormatProperty, value);
    }

    public ICommand? PageChangedCommand
    {
        get => GetValue(PageChangedCommandProperty);
        set => SetValue(PageChangedCommandProperty, value);
    }

    public string PageInfoText
    {
        get
        {
            return _pageInfoText;
        }
        private set
        {
            SetAndRaise(PageInfoTextProperty, ref _pageInfoText, value);
        }
    }

    public string TotalInfoText
    {
        get
        {
            return _totalInfoText;
        }
        private set
        {
            SetAndRaise(TotalInfoTextProperty, ref _totalInfoText, value);
        }
    }

    public LuminaPagination()
    {
        SetCurrentValue(PageSizeOptionsProperty, new AvaloniaList<int> { 10, 20, 50, 100 });
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_quickJumpBox != null)
        {
            _quickJumpBox.KeyDown -= OnQuickJumpKeyDown;
            _quickJumpBox.LostFocus -= OnQuickJumpLostFocus;
        }
        _itemsHost = e.NameScope.FindRequired<StackPanel>("PART_ItemsHost");
        _quickJumpBox = e.NameScope.Find<TextBox>("PART_QuickJumpBox");
        if (_quickJumpBox != null)
        {
            _quickJumpBox.KeyDown += OnQuickJumpKeyDown;
            _quickJumpBox.LostFocus += OnQuickJumpLostFocus;
        }
        Rebuild();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PageCountProperty || change.Property == TotalCountProperty || change.Property == PageSizeProperty)
        {
            CoercePage();
            Rebuild();
        }
        else if (change.Property == CurrentPageProperty || change.Property == MaxVisiblePagesProperty || change.Property == ShowEdgeButtonsProperty || change.Property == ShowPreviousNextButtonsProperty || change.Property == ShowFastJumpProperty || change.Property == PageInfoFormatProperty || change.Property == TotalTextFormatProperty || change.Property == DisplayCurrentPageInQuickJumperProperty)
        {
            Rebuild();
        }
    }

    private void Rebuild()
    {
        int pageCount = GetEffectivePageCount();
        int currentPage = GetEffectiveCurrentPage(pageCount);
        PageInfoText = Format(PageInfoFormat, currentPage, pageCount);
        TotalInfoText = Format(TotalTextFormat, Math.Max(0, TotalCount));
        UpdateQuickJumpText();
        if (_itemsHost == null)
        {
            return;
        }
        _itemsHost.Children.Clear();
        if (ShowEdgeButtons)
        {
            _itemsHost.Children.Add(CreateNavigationButton("«", 1, currentPage > 1, "Edge"));
        }
        if (ShowPreviousNextButtons)
        {
            _itemsHost.Children.Add(CreateNavigationButton("‹", currentPage - 1, currentPage > 1, "Navigation"));
        }
        foreach (int slot in CreatePageSlots(pageCount, currentPage))
        {
            if (slot <= 0)
            {
                switch (slot)
                {
                case -1:
                    _itemsHost.Children.Add(CreateEllipsis(currentPage, -1));
                    break;
                case -2:
                    _itemsHost.Children.Add(CreateEllipsis(currentPage, 1));
                    break;
                }
            }
            else
            {
                _itemsHost.Children.Add(CreatePageButton(slot));
            }
        }
        if (ShowPreviousNextButtons)
        {
            _itemsHost.Children.Add(CreateNavigationButton("›", currentPage + 1, currentPage < pageCount, "Navigation"));
        }
        if (ShowEdgeButtons)
        {
            _itemsHost.Children.Add(CreateNavigationButton("»", pageCount, currentPage < pageCount, "Edge"));
        }
    }

    private Button CreatePageButton(int page)
    {
        int currentPage = GetEffectiveCurrentPage();
        Button button = new Button
        {
            Content = page.ToString(),
            IsEnabled = (page != currentPage),
            Theme = ResolveButtonTheme()
        };
        button.Classes.Add("PaginationItem");
        SetClass(button, "Selected", page == currentPage);
        button.Click += (_, _) => {
            NavigateTo(page);
        };
        return button;
    }

    private Button CreateNavigationButton(string text, int targetPage, bool isEnabled, string className)
    {
        Button button = new Button
        {
            Content = text,
            IsEnabled = isEnabled,
            Theme = ResolveButtonTheme()
        };
        button.Classes.Add("PaginationItem");
        button.Classes.Add(className);
        button.Click += (_, _) => {
            NavigateTo(targetPage);
        };
        return button;
    }

    private Control CreateEllipsis(int currentPage, int direction)
    {
        if (!ShowFastJump)
        {
            return new TextBlock
            {
                Text = "...",
                Width = 32.0,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
        int pageCount = GetEffectivePageCount();
        int targetPage = Math.Clamp(currentPage + direction * Math.Max(3, MaxVisiblePages), 1, pageCount);
        Button button = CreateNavigationButton("...", targetPage, targetPage != currentPage, "Ellipsis");
        button.Classes.Add((direction < 0) ? "FastBackward" : "FastForward");
        return button;
    }

    private IEnumerable<int> CreatePageSlots(int pageCount, int currentPage)
    {
        int maxVisible = Math.Clamp(MaxVisiblePages, 3, 9);
        if (pageCount <= maxVisible)
        {
            return Enumerable.Range(1, pageCount);
        }
        int sideCount = maxVisible - 2;
        int start = Math.Max(2, currentPage - sideCount / 2);
        int end = Math.Min(pageCount - 1, start + sideCount - 1);
        start = Math.Max(2, end - sideCount + 1);
        List<int> pages = new List<int> { 1 };
        if (start > 2)
        {
            pages.Add(-1);
        }
        pages.AddRange(Enumerable.Range(start, end - start + 1));
        if (end < pageCount - 1)
        {
            pages.Add(-2);
        }
        pages.Add(pageCount);
        return pages;
    }

    private void NavigateTo(int page)
    {
        int nextPage = Math.Clamp(page, 1, GetEffectivePageCount());
        if (nextPage != CurrentPage)
        {
            CurrentPage = nextPage;
            UpdateQuickJumpText();
            ICommand? pageChangedCommand = PageChangedCommand;
            if (pageChangedCommand != null && pageChangedCommand.CanExecute(nextPage))
            {
                pageChangedCommand.Execute(nextPage);
            }
        }
    }

    private void CoercePage()
    {
        int page = GetEffectiveCurrentPage();
        if (page != CurrentPage)
        {
            CurrentPage = page;
        }
    }

    private int GetEffectiveCurrentPage()
    {
        return GetEffectiveCurrentPage(GetEffectivePageCount());
    }

    private int GetEffectiveCurrentPage(int pageCount)
    {
        return Math.Clamp(CurrentPage, 1, pageCount);
    }

    private int GetEffectivePageCount()
    {
        if (TotalCount > 0)
        {
            return Math.Max(1, (int)Math.Ceiling((double)TotalCount / (double)Math.Max(1, PageSize)));
        }
        return Math.Max(1, PageCount);
    }

    private void OnQuickJumpKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return)
        {
            SyncQuickJump();
            e.Handled = true;
        }
    }

    private void OnQuickJumpLostFocus(object? sender, RoutedEventArgs e)
    {
        SyncQuickJump();
    }

    private void SyncQuickJump()
    {
        if (_quickJumpBox != null && !string.IsNullOrWhiteSpace(_quickJumpBox.Text))
        {
            if (int.TryParse(_quickJumpBox.Text, out var page))
            {
                NavigateTo(page);
            }
            UpdateQuickJumpText();
        }
    }

    private void UpdateQuickJumpText()
    {
        if (_quickJumpBox != null)
        {
            _quickJumpBox.Text = DisplayCurrentPageInQuickJumper ? GetEffectiveCurrentPage().ToString() : string.Empty;
        }
    }

    private static void SetClass(Control control, string className, bool isEnabled)
    {
        if (isEnabled)
        {
            if (!control.Classes.Contains(className))
            {
                control.Classes.Add(className);
            }
        }
        else
        {
            control.Classes.Remove(className);
        }
    }

    private static string Format(string format, params object[] args)
    {
        try
        {
            return string.Format(format, args);
        }
        catch (FormatException)
        {
            return string.Join(" / ", args);
        }
    }

    private static ControlTheme? ResolveButtonTheme()
    {
        Application? current = Application.Current;
        object? resource;
        return (current != null && current.TryFindResource("LuminaPaginationButtonTheme", out resource)) ? (resource as ControlTheme) : null;
    }
}
