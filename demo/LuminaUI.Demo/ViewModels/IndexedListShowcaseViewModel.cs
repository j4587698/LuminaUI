using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LuminaUI.Localization;

namespace LuminaUI.Demo.ViewModels;

public partial class IndexedListShowcaseViewModel : ObservableObject
{
    private static readonly ContactRecord[] Contacts =
    [
        new("Alice", "alice@example.com"),
        new("Aiden", "aiden@example.com"),
        new("Bob", "bob@example.com"),
        new("Charlie", "charlie@example.com"),
        new("David", "david@example.com"),
        new("Eva", "eva@example.com"),
        new("Frank", "frank@example.com"),
        new("Grace", "grace@example.com"),
        new("Henry", "henry@example.com"),
        new("Ivy", "ivy@example.com"),
        new("Jack", "jack@example.com"),
        new("Kevin", "kevin@example.com"),
        new("Lily", "lily@example.com"),
        new("Mia", "mina@example.com"),
        new("Nancy", "nancy@example.com"),
        new("Oliver", "oliver@example.com"),
        new("Paul", "paul@example.com"),
        new("Queen", "queen@example.com"),
        new("Rose", "rose@example.com"),
        new("Sam", "sam@example.com"),
        new("Tom", "tom@example.com"),
        new("Ursula", "ursula@example.com"),
        new("Victor", "victor@example.com"),
        new("Wendy", "wendy@example.com"),
        new("Xavier", "xavier@example.com"),
        new("Yvonne", "yvonne@example.com"),
        new("Zack", "zack@example.com")
    ];

    public IndexedListShowcaseViewModel()
    {
        BuildDynamicSections();
        LuminaLocalization.LanguageChanged += OnLanguageChanged;
    }

    [ObservableProperty]
    private IReadOnlyList<IndexedSectionViewModel> _dynamicContactSections = [];

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        BuildDynamicSections();
    }

    private void BuildDynamicSections()
    {
        DynamicContactSections = Contacts
            .GroupBy(contact => contact.Name[..1].ToUpperInvariant())
            .OrderBy(group => group.Key)
            .Select(group => new IndexedSectionViewModel(
                group.Key,
                LuminaLocalization.Format(SandboxLocalization.IndexedListGroupFormat, group.Key),
                group.Select(contact => new ListItemViewModel(contact.Name, contact.Email)).ToArray()))
            .ToArray();
    }

    private sealed record ContactRecord(string Name, string Email);
}

public sealed record IndexedSectionViewModel(
    string Key,
    string Header,
    IReadOnlyList<ListItemViewModel> Items);

public sealed record ListItemViewModel(
    string Header,
    string Description,
    string? Icon = null,
    string? Value = null);
