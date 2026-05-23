using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LuminaUI.Controls;

namespace LuminaUI.Demo.ViewModels;

public partial class TimelineShowcaseViewModel : ObservableObject
{
    public ObservableCollection<TimelineEventViewModel> Events { get; } =
    [
        new("Create request", "The customer submitted an enterprise onboarding request.", DateTime.Now.AddHours(-7), LuminaTimelineItemStatus.Success),
        new("Review contract", "Legal review is still in progress.", DateTime.Now.AddHours(-4), LuminaTimelineItemStatus.Ongoing),
        new("Provision workspace", "A workspace will be created after approval.", DateTime.Now.AddHours(-2), LuminaTimelineItemStatus.Info),
        new("Invite members", "Send initial administrator invitations.", DateTime.Now.AddHours(1), LuminaTimelineItemStatus.Default)
    ];
}

public sealed record TimelineEventViewModel(
    string Header,
    string Description,
    DateTime Time,
    LuminaTimelineItemStatus Status);
