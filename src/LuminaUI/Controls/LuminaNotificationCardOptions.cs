using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace LuminaUI.Controls;

public class LuminaNotificationCardOptions : AvaloniaObject
{
	private static readonly HashSet<NotificationCard> CloseCompletionCards;

	public static readonly AttachedProperty<bool> CloseOnClickProperty;

	public static readonly AttachedProperty<bool> CompletesNotificationCloseProperty;

	public static readonly AttachedProperty<bool> ClosesNotificationProperty;

	static LuminaNotificationCardOptions()
	{
		CloseCompletionCards = new HashSet<NotificationCard>();
		CloseOnClickProperty = AvaloniaProperty.RegisterAttached<LuminaNotificationCardOptions, NotificationCard, bool>("CloseOnClick", defaultValue: false);
		CompletesNotificationCloseProperty = AvaloniaProperty.RegisterAttached<LuminaNotificationCardOptions, WindowNotificationManager, bool>("CompletesNotificationClose", defaultValue: false);
		ClosesNotificationProperty = AvaloniaProperty.RegisterAttached<LuminaNotificationCardOptions, Button, bool>("ClosesNotification", defaultValue: false);
		CloseOnClickProperty.Changed.AddClassHandler<NotificationCard>(OnCloseOnClickChanged);
		CompletesNotificationCloseProperty.Changed.AddClassHandler<WindowNotificationManager>(OnCompletesNotificationCloseChanged);
		ClosesNotificationProperty.Changed.AddClassHandler<Button>(OnClosesNotificationChanged);
	}

	public static bool GetCloseOnClick(NotificationCard element)
	{
		return element.GetValue(CloseOnClickProperty);
	}

	public static void SetCloseOnClick(NotificationCard element, bool value)
	{
		element.SetValue(CloseOnClickProperty, value);
	}

	public static bool GetCompletesNotificationClose(WindowNotificationManager element)
	{
		return element.GetValue(CompletesNotificationCloseProperty);
	}

	public static void SetCompletesNotificationClose(WindowNotificationManager element, bool value)
	{
		element.SetValue(CompletesNotificationCloseProperty, value);
	}

	public static bool GetClosesNotification(Button element)
	{
		return element.GetValue(ClosesNotificationProperty);
	}

	public static void SetClosesNotification(Button element, bool value)
	{
		element.SetValue(ClosesNotificationProperty, value);
	}

	public static void AttachCloseCompletion(WindowNotificationManager manager)
	{
		foreach (NotificationCard notificationCard in manager.GetVisualDescendants().OfType<NotificationCard>())
		{
			AttachCloseCompletion(notificationCard);
		}
	}

	private static void OnCloseOnClickChanged(NotificationCard notificationCard, AvaloniaPropertyChangedEventArgs args)
	{
		notificationCard.PointerReleased -= OnNotificationCardPointerReleased;
		if (args.NewValue is int num && num != 0)
		{
			notificationCard.PointerReleased += OnNotificationCardPointerReleased;
		}
	}

	private static void OnCompletesNotificationCloseChanged(WindowNotificationManager manager, AvaloniaPropertyChangedEventArgs args)
	{
		manager.LayoutUpdated -= OnNotificationManagerLayoutUpdated;
		if (args.NewValue is int num && num != 0)
		{
			manager.LayoutUpdated += OnNotificationManagerLayoutUpdated;
		}
	}

	private static void OnClosesNotificationChanged(Button button, AvaloniaPropertyChangedEventArgs args)
	{
		button.Click -= OnCloseButtonClick;
		if (args.NewValue is int num && num != 0)
		{
			button.Click += OnCloseButtonClick;
		}
	}

	private static void OnNotificationCardPointerReleased(object? sender, PointerReleasedEventArgs e)
	{
		if (sender is NotificationCard notificationCard && !e.Handled)
		{
			notificationCard.Close();
			notificationCard.SetValue(NotificationCard.IsClosedProperty, value: true);
			e.Handled = true;
		}
	}

	private static void OnNotificationManagerLayoutUpdated(object? sender, EventArgs e)
	{
		if (!(sender is WindowNotificationManager manager))
		{
			return;
		}
		foreach (NotificationCard notificationCard in manager.GetVisualDescendants().OfType<NotificationCard>())
		{
			AttachCloseCompletion(notificationCard);
		}
	}

	private static void AttachCloseCompletion(NotificationCard notificationCard)
	{
		if (CloseCompletionCards.Add(notificationCard))
		{
			notificationCard.DetachedFromVisualTree += OnNotificationCardDetachedFromVisualTree;
			notificationCard.PropertyChanged += OnNotificationCardPropertyChanged;
		}
	}

	private static void OnNotificationCardDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
	{
		if (sender is NotificationCard notificationCard)
		{
			CloseCompletionCards.Remove(notificationCard);
			notificationCard.DetachedFromVisualTree -= OnNotificationCardDetachedFromVisualTree;
			notificationCard.PropertyChanged -= OnNotificationCardPropertyChanged;
		}
	}

	private static void OnNotificationCardPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
	{
		NotificationCard? notificationCard = sender as NotificationCard;
		if (notificationCard == null || e.Property != NotificationCard.IsClosingProperty || !e.GetNewValue<bool>())
		{
			return;
		}
		DispatcherTimer.RunOnce(delegate
		{
			if (notificationCard.IsClosing && !notificationCard.IsClosed)
			{
				notificationCard.SetValue(NotificationCard.IsClosedProperty, value: true);
			}
		}, TimeSpan.FromMilliseconds(240L));
	}

	private static void OnCloseButtonClick(object? sender, RoutedEventArgs e)
	{
		if (sender is Button button)
		{
			NotificationCard? notificationCard = button.FindAncestorOfType<NotificationCard>();
			if (notificationCard != null)
			{
				notificationCard.Close();
				notificationCard.SetValue(NotificationCard.IsClosedProperty, value: true);
			}
			e.Handled = true;
		}
	}
}
