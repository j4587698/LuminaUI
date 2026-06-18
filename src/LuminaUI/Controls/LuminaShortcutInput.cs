using System;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using LuminaUI.Localization;

namespace LuminaUI.Controls;

[PseudoClasses(PseudoClassEmpty)]
public class LuminaShortcutInput : TemplatedControl
{
    private const string PseudoClassEmpty = ":empty";

    public static readonly StyledProperty<KeyGesture?> GestureProperty =
        AvaloniaProperty.Register<LuminaShortcutInput, KeyGesture?>(nameof(Gesture), defaultValue: null, inherits: false, BindingMode.TwoWay);

    public static readonly StyledProperty<IList<Key>?> AcceptableKeysProperty =
        AvaloniaProperty.Register<LuminaShortcutInput, IList<Key>?>(nameof(AcceptableKeys));

    public static readonly StyledProperty<bool> ConsiderKeyModifiersProperty =
        AvaloniaProperty.Register<LuminaShortcutInput, bool>(nameof(ConsiderKeyModifiers), defaultValue: true);

    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<LuminaShortcutInput, string?>(nameof(PlaceholderText));

    public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
        ContentControl.HorizontalContentAlignmentProperty.AddOwner<LuminaShortcutInput>(
            new StyledPropertyMetadata<HorizontalAlignment>(HorizontalAlignment.Center));

    public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
        ContentControl.VerticalContentAlignmentProperty.AddOwner<LuminaShortcutInput>(
            new StyledPropertyMetadata<VerticalAlignment>(VerticalAlignment.Center));

    static LuminaShortcutInput()
    {
        FocusableProperty.OverrideDefaultValue<LuminaShortcutInput>(true);
        GestureProperty.Changed.AddClassHandler<LuminaShortcutInput>((input, _) => input.UpdatePseudoClasses());
    }

    public LuminaShortcutInput()
    {
        ClearCommand = new LuminaRelayCommand(_ => Clear());
        UpdateDefaultPlaceholderText();
    }

    public KeyGesture? Gesture
    {
        get => GetValue(GestureProperty);
        set => SetValue(GestureProperty, value);
    }

    public IList<Key>? AcceptableKeys
    {
        get => GetValue(AcceptableKeysProperty);
        set => SetValue(AcceptableKeysProperty, value);
    }

    public bool ConsiderKeyModifiers
    {
        get => GetValue(ConsiderKeyModifiersProperty);
        set => SetValue(ConsiderKeyModifiersProperty, value);
    }

    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public HorizontalAlignment HorizontalContentAlignment
    {
        get => GetValue(HorizontalContentAlignmentProperty);
        set => SetValue(HorizontalContentAlignmentProperty, value);
    }

    public VerticalAlignment VerticalContentAlignment
    {
        get => GetValue(VerticalContentAlignmentProperty);
        set => SetValue(VerticalContentAlignmentProperty, value);
    }

    public ICommand ClearCommand { get; }

    public void Clear()
    {
        SetCurrentValue(GestureProperty, null);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        LuminaLocalization.LanguageChanged += OnLanguageChanged;
        UpdateDefaultPlaceholderText();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        LuminaLocalization.LanguageChanged -= OnLanguageChanged;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        UpdatePseudoClasses();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (!LuminaKeyGestureDetector.TryCreateGesture(e, AcceptableKeys, ConsiderKeyModifiers, out KeyGesture? gesture))
        {
            return;
        }

        SetCurrentValue(GestureProperty, gesture);
        e.Handled = true;
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(PseudoClassEmpty, Gesture == null);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateDefaultPlaceholderText();
    }

    private void UpdateDefaultPlaceholderText()
    {
        if (!IsSet(PlaceholderTextProperty))
        {
            SetCurrentValue(PlaceholderTextProperty, LuminaLocalization.Get(LuminaLocalizationKeys.ShortcutInputPlaceholder));
        }
    }
}
