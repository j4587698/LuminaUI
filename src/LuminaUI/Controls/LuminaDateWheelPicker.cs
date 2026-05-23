using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using LuminaUI.Localization;

namespace LuminaUI.Controls;

internal sealed class LuminaDateWheelPicker : Grid
{
	private readonly LuminaWheelColumn? _yearColumn;

	private readonly LuminaWheelColumn? _monthColumn;

	private readonly LuminaWheelColumn? _dayColumn;

	private readonly TextBlock _summaryText;

	private readonly DateTime _minDate;

	private readonly DateTime _maxDate;

	private int _year;

	private int _month;

	private int _day;

	private bool _isUpdating;

	public DateTime SelectedDate => ClampDate(new DateTime(_year, _month, _day));

	public LuminaDateWheelPicker(DateTime selectedDate, DateTime minDate, DateTime maxDate, bool yearVisible = true, bool monthVisible = true, bool dayVisible = true)
	{
		_minDate = ((minDate.Date <= maxDate.Date) ? minDate.Date : maxDate.Date);
		_maxDate = ((maxDate.Date >= minDate.Date) ? maxDate.Date : minDate.Date);
		DateTime clampedDate = ClampDate(selectedDate.Date);
		_year = clampedDate.Year;
		_month = clampedDate.Month;
		_day = clampedDate.Day;
		base.RowDefinitions = new RowDefinitions("Auto,Auto");
		base.RowSpacing = 14.0;
		base.HorizontalAlignment = HorizontalAlignment.Stretch;
		_summaryText = new TextBlock
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			FontSize = 15.0,
			FontWeight = FontWeight.DemiBold
		};
		_summaryText.Classes.Add("LuminaWheelSummaryText");
		Border summary = new Border
		{
			Padding = new Thickness(14.0, 10.0),
			CornerRadius = new CornerRadius(999.0),
			HorizontalAlignment = HorizontalAlignment.Center,
			Child = _summaryText
		};
		summary.Background = LuminaPickerResources.Brush("LuminaPrimaryBgBrush", Brushes.Transparent);
		_summaryText.Foreground = LuminaPickerResources.Brush("LuminaTextPrimaryBrush", Brushes.White);
		summary.Classes.Add("LuminaWheelSummary");
		base.Children.Add(summary);
		Grid wheelGrid = new Grid
		{
			ColumnSpacing = 8.0,
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		Grid.SetRow(wheelGrid, 1);
		base.Children.Add(wheelGrid);
		AddColumn(wheelGrid, yearVisible, LuminaLocalization.Get("Lumina.Picker.Year"), out _yearColumn);
		AddColumn(wheelGrid, monthVisible, LuminaLocalization.Get("Lumina.Picker.Month"), out _monthColumn);
		AddColumn(wheelGrid, dayVisible, LuminaLocalization.Get("Lumina.Picker.Day"), out _dayColumn);
		if (_yearColumn != null)
		{
			_yearColumn.ValueChanged += delegate(int value)
			{
				if (!_isUpdating)
				{
					_year = value;
					SyncMonthAndDay();
				}
			};
		}
		if (_monthColumn != null)
		{
			_monthColumn.ValueChanged += delegate(int value)
			{
				if (!_isUpdating)
				{
					_month = value;
					SyncDay();
				}
			};
		}
		if (_dayColumn != null)
		{
			_dayColumn.ValueChanged += delegate(int value)
			{
				if (!_isUpdating)
				{
					_day = value;
					UpdateSummary();
				}
			};
		}
		SyncAll();
	}

	private static void AddColumn(Grid grid, bool isVisible, string label, out LuminaWheelColumn? column)
	{
		column = null;
		if (isVisible)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			column = new LuminaWheelColumn(label);
			Grid.SetColumn(column, grid.ColumnDefinitions.Count - 1);
			grid.Children.Add(column);
		}
	}

	private void SyncAll()
	{
		_isUpdating = true;
		_yearColumn?.SetOptions(CreateRange(_minDate.Year, _maxDate.Year, (int value) => value.ToString(CultureInfo.CurrentCulture)), _year);
		SyncMonthOptions();
		SyncDayOptions();
		_isUpdating = false;
		UpdateSummary();
	}

	private void SyncMonthAndDay()
	{
		_isUpdating = true;
		SyncMonthOptions();
		SyncDayOptions();
		_isUpdating = false;
		UpdateSummary();
	}

	private void SyncDay()
	{
		_isUpdating = true;
		SyncDayOptions();
		_isUpdating = false;
		UpdateSummary();
	}

	private void SyncMonthOptions()
	{
		int minMonth = ((_year != _minDate.Year) ? 1 : _minDate.Month);
		int maxMonth = ((_year == _maxDate.Year) ? _maxDate.Month : 12);
		_monthColumn?.SetOptions(CreateRange(minMonth, maxMonth, FormatMonth), _month);
		_month = _monthColumn?.SelectedValue ?? Math.Clamp(_month, minMonth, maxMonth);
	}

	private void SyncDayOptions()
	{
		int maxDayInMonth = DateTime.DaysInMonth(_year, _month);
		int minDay = ((_year != _minDate.Year || _month != _minDate.Month) ? 1 : _minDate.Day);
		int maxDay = ((_year == _maxDate.Year && _month == _maxDate.Month) ? _maxDate.Day : maxDayInMonth);
		_dayColumn?.SetOptions(CreateRange(minDay, maxDay, (int value) => value.ToString("00", CultureInfo.CurrentCulture)), _day);
		_day = _dayColumn?.SelectedValue ?? Math.Clamp(_day, minDay, maxDay);
	}

	private void UpdateSummary()
	{
		_summaryText.Text = SelectedDate.ToString(LuminaLocalization.Get("Lumina.Picker.DateSummaryFormat"), LuminaLocalization.CurrentCulture);
	}

	private DateTime ClampDate(DateTime date)
	{
		if (date < _minDate)
		{
			return _minDate;
		}
		if (date > _maxDate)
		{
			return _maxDate;
		}
		return date;
	}

	private static string FormatMonth(int month)
	{
		return new DateTime(2000, month, 1).ToString("MMM", CultureInfo.CurrentCulture);
	}

	private static IReadOnlyList<LuminaWheelOption> CreateRange(int start, int end, Func<int, string> format)
	{
		List<LuminaWheelOption> options = new List<LuminaWheelOption>();
		for (int value = start; value <= end; value++)
		{
			options.Add(new LuminaWheelOption(value, format(value)));
		}
		return options;
	}
}
