using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using LuminaUI.Extensions;

namespace LuminaUI.Controls;

public class LuminaInputOtp : TemplatedControl
{
	private readonly List<TextBox> _boxes = new List<TextBox>();

	private StackPanel? _itemsHost;

	private bool _isSyncing;

	private bool _isComplete;

	private string? _lastCompletedText;

	public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<LuminaInputOtp, string>("Text", string.Empty, inherits: false, BindingMode.TwoWay);

	public static readonly StyledProperty<int> LengthProperty = AvaloniaProperty.Register<LuminaInputOtp, int>("Length", 6);

	public static readonly StyledProperty<bool> IsMaskedProperty = AvaloniaProperty.Register<LuminaInputOtp, bool>("IsMasked", defaultValue: false);

	public static readonly StyledProperty<double> BoxSizeProperty = AvaloniaProperty.Register<LuminaInputOtp, double>("BoxSize", 44.0);

	public static readonly StyledProperty<double> BoxFontSizeProperty = AvaloniaProperty.Register<LuminaInputOtp, double>("BoxFontSize", 18.0);

	public static readonly StyledProperty<bool> AcceptsOnlyDigitsProperty = AvaloniaProperty.Register<LuminaInputOtp, bool>("AcceptsOnlyDigits", defaultValue: true);

	public static readonly StyledProperty<double> ItemSpacingProperty = AvaloniaProperty.Register<LuminaInputOtp, double>("ItemSpacing", 8.0);

	public static readonly StyledProperty<ICommand?> CompletedCommandProperty = AvaloniaProperty.Register<LuminaInputOtp, ICommand?>("CompletedCommand");

	public static readonly DirectProperty<LuminaInputOtp, bool> IsCompleteProperty = AvaloniaProperty.RegisterDirect<LuminaInputOtp, bool>("IsComplete", (LuminaInputOtp input) => input.IsComplete, null, unsetValue: false);

	public string Text
	{
		get
		{
			return GetValue(TextProperty);
		}
		set
		{
			SetValue(TextProperty, value);
		}
	}

	public int Length
	{
		get
		{
			return GetValue(LengthProperty);
		}
		set
		{
			SetValue(LengthProperty, value);
		}
	}

	public bool IsMasked
	{
		get
		{
			return GetValue(IsMaskedProperty);
		}
		set
		{
			SetValue(IsMaskedProperty, value);
		}
	}

	public double BoxSize
	{
		get
		{
			return GetValue(BoxSizeProperty);
		}
		set
		{
			SetValue(BoxSizeProperty, value);
		}
	}

	public double BoxFontSize
	{
		get
		{
			return GetValue(BoxFontSizeProperty);
		}
		set
		{
			SetValue(BoxFontSizeProperty, value);
		}
	}

	public bool AcceptsOnlyDigits
	{
		get
		{
			return GetValue(AcceptsOnlyDigitsProperty);
		}
		set
		{
			SetValue(AcceptsOnlyDigitsProperty, value);
		}
	}

	public double ItemSpacing
	{
		get
		{
			return GetValue(ItemSpacingProperty);
		}
		set
		{
			SetValue(ItemSpacingProperty, value);
		}
	}

	public ICommand? CompletedCommand
	{
		get
		{
			return GetValue(CompletedCommandProperty);
		}
		set
		{
			SetValue(CompletedCommandProperty, value);
		}
	}

	public bool IsComplete
	{
		get
		{
			return _isComplete;
		}
		private set
		{
			SetAndRaise(IsCompleteProperty, ref _isComplete, value);
		}
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		_itemsHost = e.NameScope.FindRequired<StackPanel>("PART_ItemsHost");
		RebuildBoxes();
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == LengthProperty)
		{
			RebuildBoxes();
		}
		else if (change.Property == TextProperty || change.Property == IsMaskedProperty)
		{
			SyncBoxesFromText();
		}
		else if (change.Property == AcceptsOnlyDigitsProperty)
		{
			Text = NormalizeText(Text);
		}
		else if (change.Property == ItemSpacingProperty && _itemsHost != null)
		{
			_itemsHost.Spacing = ItemSpacing;
		}
	}

	public void Clear()
	{
		Text = string.Empty;
		_boxes.FirstOrDefault()?.Focus();
	}

	private void RebuildBoxes()
	{
		if (_itemsHost == null)
		{
			return;
		}
		foreach (TextBox box in _boxes)
		{
			box.TextChanged -= OnBoxTextChanged;
			box.KeyDown -= OnBoxKeyDown;
		}
		_boxes.Clear();
		_itemsHost.Children.Clear();
		_itemsHost.Spacing = ItemSpacing;
		int count = Math.Clamp(Length, 1, 12);
		for (int index = 0; index < count; index++)
		{
			TextBox box2 = new TextBox
			{
				Width = BoxSize,
				Height = BoxSize,
				MinHeight = BoxSize,
				MaxLength = count,
				Padding = new Thickness(0.0),
				FontSize = BoxFontSize,
				FontWeight = FontWeight.DemiBold,
				PasswordChar = (IsMasked ? '*' : '\0'),
				TextAlignment = TextAlignment.Center,
				HorizontalContentAlignment = HorizontalAlignment.Center,
				VerticalContentAlignment = VerticalAlignment.Center
			};
			box2.Classes.Add("LuminaOtpBox");
			box2.Tag = index;
			box2.TextChanged += OnBoxTextChanged;
			box2.KeyDown += OnBoxKeyDown;
			_boxes.Add(box2);
			_itemsHost.Children.Add(box2);
		}
		SyncBoxesFromText();
	}

	private void SyncBoxesFromText()
	{
		if (_boxes.Count == 0)
		{
			return;
		}
		string normalized = NormalizeText(Text);
		if (Text != normalized)
		{
			Text = normalized;
			return;
		}
		_isSyncing = true;
		for (int index = 0; index < _boxes.Count; index++)
		{
			_boxes[index].PasswordChar = (IsMasked ? '*' : '\0');
			_boxes[index].Text = ((index < normalized.Length) ? normalized[index].ToString() : string.Empty);
		}
		_isSyncing = false;
		UpdateCompletion();
	}

	private void OnBoxTextChanged(object? sender, TextChangedEventArgs e)
	{
		if (_isSyncing || !(sender is TextBox box))
		{
			return;
		}
		object? tag = box.Tag;
		int index = default(int);
		int num;
		if (tag is int)
		{
			index = (int)tag;
			num = 1;
		}
		else
		{
			num = 0;
		}
		if (num == 0)
		{
			return;
		}
		string text = NormalizeText(box.Text ?? string.Empty);
		if (text.Length > 1)
		{
			FillFrom(index, text);
			return;
		}
		box.Text = text;
		Text = string.Concat(_boxes.Select((TextBox current) => current.Text ?? string.Empty));
		if (text.Length == 1 && index < _boxes.Count - 1)
		{
			_boxes[index + 1].Focus();
		}
	}

	private void OnBoxKeyDown(object? sender, KeyEventArgs e)
	{
		if (!(sender is TextBox box))
		{
			return;
		}
		object? tag = box.Tag;
		int index = default(int);
		int num;
		if (tag is int)
		{
			index = (int)tag;
			num = 1;
		}
		else
		{
			num = 0;
		}
		if (num != 0)
		{
			if (e.Key == Key.Back && string.IsNullOrEmpty(box.Text) && index > 0)
			{
				_boxes[index - 1].Text = string.Empty;
				_boxes[index - 1].Focus();
				e.Handled = true;
			}
			else if (e.Key == Key.Left && index > 0)
			{
				_boxes[index - 1].Focus();
				e.Handled = true;
			}
			else if (e.Key == Key.Right && index < _boxes.Count - 1)
			{
				_boxes[index + 1].Focus();
				e.Handled = true;
			}
		}
	}

	private void FillFrom(int startIndex, string value)
	{
		_isSyncing = true;
		int target = startIndex;
		foreach (char character in value)
		{
			if (target >= _boxes.Count)
			{
				break;
			}
			_boxes[target].Text = character.ToString();
			target++;
		}
		_isSyncing = false;
		Text = string.Concat(_boxes.Select((TextBox box) => box.Text ?? string.Empty));
		_boxes[Math.Min(target, _boxes.Count - 1)].Focus();
	}

	private string NormalizeText(string? value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}
		IEnumerable<char> filtered = (AcceptsOnlyDigits ? value.Where(char.IsDigit) : value.Where((char character) => !char.IsControl(character)));
		string normalized = string.Concat(filtered).ToUpperInvariant();
		int maxLength = Math.Clamp(Length, 1, 12);
		return normalized.Substring(0, Math.Min(maxLength, normalized.Length));
	}

	private void UpdateCompletion()
	{
		IsComplete = Text.Length == Math.Clamp(Length, 1, 12);
		if (IsComplete && !(Text == _lastCompletedText))
		{
			_lastCompletedText = Text;
			ICommand? completedCommand = CompletedCommand;
			if (completedCommand != null && completedCommand.CanExecute(Text))
			{
				completedCommand.Execute(Text);
			}
		}
	}
}
