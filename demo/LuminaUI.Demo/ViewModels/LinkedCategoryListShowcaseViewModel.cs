using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LuminaUI.Demo.ViewModels;

public class LinkedCategoryListShowcaseViewModel : ObservableObject
{
    public IReadOnlyList<LinkedCategorySectionViewModel> MenuSections { get; } =
    [
        new("Popular",
        [
            new ListItemViewModel("Signature beef bowl", "Slow cooked beef, soft egg, house sauce.", "B", "$12"),
            new ListItemViewModel("Crispy chicken rice", "Chicken thigh, pickled cucumber, chili mayo.", "C", "$10")
        ]),
        new("Noodles",
        [
            new ListItemViewModel("Tonkotsu ramen", "Pork broth, chashu, bamboo shoots.", "R", "$13"),
            new ListItemViewModel("Cold sesame noodles", "Sesame dressing, cucumber, scallion.", "N", "$9")
        ]),
        new("Snacks",
        [
            new ListItemViewModel("Truffle fries", "Parmesan, parsley, black truffle oil.", "F", "$7"),
            new ListItemViewModel("Gyoza", "Pan fried pork dumplings, vinegar soy.", "G", "$8")
        ]),
        new("Drinks",
        [
            new ListItemViewModel("Jasmine sparkling tea", "Cold brewed tea with citrus soda.", "T", "$5"),
            new ListItemViewModel("Yuzu lemonade", "Yuzu peel, lemon, light syrup.", "Y", "$5")
        ])
    ];
}

public sealed record LinkedCategorySectionViewModel(
    string Header,
    IReadOnlyList<ListItemViewModel> Items);
