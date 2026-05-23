using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using LuminaUI.Extensions;

namespace LuminaUI.Controls;

public class LuminaTagInput : TemplatedControl
{
	private TextBox? _textBox;

	private INotifyCollectionChanged? _tagsNotifier;

	private bool _isSyncingText;

	public static readonly StyledProperty<IList?> TagsProperty;

	public static readonly StyledProperty<string?> PendingTextProperty;

	public static readonly StyledProperty<string?> WatermarkProperty;

	public static readonly StyledProperty<string> SeparatorProperty;

	public static readonly StyledProperty<int> MaxCountProperty;

	public static readonly StyledProperty<bool> AllowDuplicatesProperty;

	public static readonly StyledProperty<bool> CommitOnLostFocusProperty;

	public static readonly StyledProperty<bool> CanRemoveTagsProperty;

	public static readonly StyledProperty<bool> CanInputProperty;

	public static readonly StyledProperty<ICommand?> TagAddedCommandProperty;

	public static readonly StyledProperty<ICommand?> TagRemovedCommandProperty;

	public static readonly DirectProperty<LuminaTagInput, bool> HasTagsProperty;

	private bool _hasTags;

	public static readonly DirectProperty<LuminaTagInput, bool> CanAddMoreProperty;

	private bool _canAddMore = true;

	public IList? Tags
	{
		get
		{
			return GetValue(TagsProperty);
		}
		set
		{
			SetValue(TagsProperty, value);
		}
	}

	public string? PendingText
	{
		get
		{
			return GetValue(PendingTextProperty);
		}
		set
		{
			SetValue(PendingTextProperty, value);
		}
	}

	public string? Watermark
	{
		get
		{
			return GetValue(WatermarkProperty);
		}
		set
		{
			SetValue(WatermarkProperty, value);
		}
	}

	public string Separator
	{
		get
		{
			return GetValue(SeparatorProperty);
		}
		set
		{
			SetValue(SeparatorProperty, value);
		}
	}

	public int MaxCount
	{
		get
		{
			return GetValue(MaxCountProperty);
		}
		set
		{
			SetValue(MaxCountProperty, value);
		}
	}

	public bool AllowDuplicates
	{
		get
		{
			return GetValue(AllowDuplicatesProperty);
		}
		set
		{
			SetValue(AllowDuplicatesProperty, value);
		}
	}

	public bool CommitOnLostFocus
	{
		get
		{
			return GetValue(CommitOnLostFocusProperty);
		}
		set
		{
			SetValue(CommitOnLostFocusProperty, value);
		}
	}

	public bool CanRemoveTags
	{
		get
		{
			return GetValue(CanRemoveTagsProperty);
		}
		set
		{
			SetValue(CanRemoveTagsProperty, value);
		}
	}

	public bool CanInput
	{
		get
		{
			return GetValue(CanInputProperty);
		}
		set
		{
			SetValue(CanInputProperty, value);
		}
	}

	public ICommand? TagAddedCommand
	{
		get
		{
			return GetValue(TagAddedCommandProperty);
		}
		set
		{
			SetValue(TagAddedCommandProperty, value);
		}
	}

	public ICommand? TagRemovedCommand
	{
		get
		{
			return GetValue(TagRemovedCommandProperty);
		}
		set
		{
			SetValue(TagRemovedCommandProperty, value);
		}
	}

	public bool HasTags
	{
		get
		{
			return _hasTags;
		}
		private set
		{
			SetAndRaise(HasTagsProperty, ref _hasTags, value);
		}
	}

	public bool CanAddMore
	{
		get
		{
			return _canAddMore;
		}
		private set
		{
			SetAndRaise(CanAddMoreProperty, ref _canAddMore, value);
		}
	}

	static LuminaTagInput()
	{
		TagsProperty = AvaloniaProperty.Register<LuminaTagInput, IList?>("Tags", null, inherits: false, BindingMode.TwoWay);
		PendingTextProperty = AvaloniaProperty.Register<LuminaTagInput, string?>("PendingText", null, inherits: false, BindingMode.TwoWay);
		WatermarkProperty = AvaloniaProperty.Register<LuminaTagInput, string?>("Watermark");
		SeparatorProperty = AvaloniaProperty.Register<LuminaTagInput, string>("Separator", ",");
		MaxCountProperty = AvaloniaProperty.Register<LuminaTagInput, int>("MaxCount", int.MaxValue);
		AllowDuplicatesProperty = AvaloniaProperty.Register<LuminaTagInput, bool>("AllowDuplicates", defaultValue: false);
		CommitOnLostFocusProperty = AvaloniaProperty.Register<LuminaTagInput, bool>("CommitOnLostFocus", defaultValue: true);
		CanRemoveTagsProperty = AvaloniaProperty.Register<LuminaTagInput, bool>("CanRemoveTags", defaultValue: true);
		CanInputProperty = AvaloniaProperty.Register<LuminaTagInput, bool>("CanInput", defaultValue: true);
		TagAddedCommandProperty = AvaloniaProperty.Register<LuminaTagInput, ICommand?>("TagAddedCommand");
		TagRemovedCommandProperty = AvaloniaProperty.Register<LuminaTagInput, ICommand?>("TagRemovedCommand");
		HasTagsProperty = AvaloniaProperty.RegisterDirect<LuminaTagInput, bool>("HasTags", (LuminaTagInput input) => input.HasTags, null, unsetValue: false);
		CanAddMoreProperty = AvaloniaProperty.RegisterDirect<LuminaTagInput, bool>("CanAddMore", (LuminaTagInput input) => input.CanAddMore, null, unsetValue: false);
		TagsProperty.Changed.AddClassHandler(delegate(LuminaTagInput input, AvaloniaPropertyChangedEventArgs<IList?> args)
		{
			input.OnTagsChanged(args.OldValue.Value, args.NewValue.Value);
		});
		PendingTextProperty.Changed.AddClassHandler(delegate(LuminaTagInput input, AvaloniaPropertyChangedEventArgs _)
		{
			input.SyncTextBox();
		});
		MaxCountProperty.Changed.AddClassHandler(delegate(LuminaTagInput input, AvaloniaPropertyChangedEventArgs _)
		{
			input.UpdateState();
		});
		CanInputProperty.Changed.AddClassHandler(delegate(LuminaTagInput input, AvaloniaPropertyChangedEventArgs _)
		{
			input.UpdateState();
		});
	}

	public LuminaTagInput()
	{
		AllowDuplicates = true;
		Tags = new AvaloniaList<string>();
		UpdateState();
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		if (_textBox != null)
		{
			_textBox.TextChanged -= OnTextBoxTextChanged;
			_textBox.KeyDown -= OnTextBoxKeyDown;
			_textBox.LostFocus -= OnTextBoxLostFocus;
		}
		_textBox = e.NameScope.FindRequired<TextBox>("PART_TextBox");
		if (_textBox != null)
		{
			_textBox.TextChanged += OnTextBoxTextChanged;
			_textBox.KeyDown += OnTextBoxKeyDown;
			_textBox.LostFocus += OnTextBoxLostFocus;
			SyncTextBox();
		}
		AddHandler(Button.ClickEvent, OnButtonClick, RoutingStrategies.Bubble);
		UpdateState();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		SubscribeToTags(null);
		RemoveHandler(Button.ClickEvent, OnButtonClick);
		base.OnDetachedFromVisualTree(e);
	}

	public bool TryAddTag(string? text)
	{
		string tag = NormalizeTag(text);
		if (!CanInput || string.IsNullOrEmpty(tag) || !CanAddMore)
		{
			return false;
		}
		IList tags = EnsureTags();
		if (!AllowDuplicates && tags.Cast<object>().Any((object item) => string.Equals(item?.ToString(), tag, StringComparison.OrdinalIgnoreCase)))
		{
			return false;
		}
		tags.Add(tag);
		PendingText = string.Empty;
		UpdateState();
		ICommand? command = TagAddedCommand;
		if (command != null && command.CanExecute(tag))
		{
			command.Execute(tag);
		}
		return true;
	}

	public bool RemoveTag(object? tag)
	{
		IList? tags = Tags;
		if (!CanRemoveTags || tags == null || tag == null || !tags.Contains(tag))
		{
			return false;
		}
		tags.Remove(tag);
		UpdateState();
		ICommand? command = TagRemovedCommand;
		if (command != null && command.CanExecute(tag))
		{
			command.Execute(tag);
		}
		return true;
	}

	private void OnTagsChanged(IList? oldValue, IList? newValue)
	{
		SubscribeToTags(newValue);
		UpdateState();
	}

	private void SubscribeToTags(IList? tags)
	{
		if (_tagsNotifier != null)
		{
			_tagsNotifier.CollectionChanged -= OnTagsCollectionChanged;
		}
		_tagsNotifier = tags as INotifyCollectionChanged;
		if (_tagsNotifier != null)
		{
			_tagsNotifier.CollectionChanged += OnTagsCollectionChanged;
		}
	}

	private void OnTagsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		UpdateState();
	}

	private IList EnsureTags()
	{
		if (Tags != null)
		{
			return Tags;
		}
		return Tags = new AvaloniaList<string>();
	}

	private void OnTextBoxTextChanged(object? sender, TextChangedEventArgs e)
	{
		if (!_isSyncingText && _textBox != null)
		{
			PendingText = _textBox.Text;
			TryCommitSeparatedText();
		}
	}

	private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
	{
		Key key = e.Key;
		if ((key == Key.Tab || key == Key.Return) ? true : false)
		{
			if (TryAddTag(PendingText))
			{
				e.Handled = true;
			}
		}
		else if (CanRemoveTags && e.Key == Key.Back && string.IsNullOrEmpty(PendingText))
		{
			IList? tags = Tags;
			if (tags != null && tags.Count > 0)
			{
				RemoveTag(tags[tags.Count - 1]);
				e.Handled = true;
			}
		}
	}

	private void OnTextBoxLostFocus(object? sender, RoutedEventArgs e)
	{
		if (CommitOnLostFocus)
		{
			TryAddTag(PendingText);
		}
	}

	private void OnButtonClick(object? sender, RoutedEventArgs e)
	{
		if (CanRemoveTags && e.Source is Button { Name: "PART_RemoveTagButton" } button)
		{
			object? tag = button.DataContext;
			if (tag != null)
			{
				RemoveTag(tag);
				e.Handled = true;
			}
		}
	}

	private void TryCommitSeparatedText()
	{
		if (!string.IsNullOrEmpty(Separator) && !string.IsNullOrEmpty(PendingText) && PendingText.Contains(Separator, StringComparison.Ordinal))
		{
			string[] parts = PendingText.Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			string[] array = parts;
			foreach (string part in array)
			{
				TryAddTag(part);
			}
		}
	}

	private void SyncTextBox()
	{
		if (_textBox != null && !(_textBox.Text == PendingText))
		{
			_isSyncingText = true;
			_textBox.Text = PendingText;
			_isSyncingText = false;
		}
	}

	private void UpdateState()
	{
		int count = Tags?.Count ?? 0;
		HasTags = count > 0;
		CanAddMore = count < MaxCount;
		if (_textBox != null)
		{
			_textBox.IsVisible = CanInput;
			_textBox.IsEnabled = CanInput && CanAddMore && base.IsEnabled;
		}
	}

	private static string NormalizeTag(string? text)
	{
		return string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
	}
}
