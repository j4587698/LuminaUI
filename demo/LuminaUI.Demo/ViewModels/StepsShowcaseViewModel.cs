using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LuminaUI.Controls;

namespace LuminaUI.Demo.ViewModels;

public partial class StepsShowcaseViewModel : ObservableObject
{
    [ObservableProperty]
    private int _currentStep;

    [ObservableProperty]
    private int _currentVerticalStep;

    [ObservableProperty]
    private int _currentInteractiveStep;

    public ObservableCollection<StepItemViewModel> Steps { get; } =
    [
        new("Create account", "Fill in your details.", LuminaStepStatus.Finish),
        new("Verify email", "Check your inbox for the code.", LuminaStepStatus.Finish),
        new("Set up workspace", "Customize your environment.", LuminaStepStatus.Process),
        new("Invite team", "Add collaborators to your project.", LuminaStepStatus.Wait)
    ];

    public ObservableCollection<StepItemViewModel> ErrorSteps { get; } =
    [
        new("Upload file", "Select a file to upload.", LuminaStepStatus.Finish),
        new("Validate data", "Checking file integrity.", LuminaStepStatus.Error),
        new("Process", "Apply changes to the system.", LuminaStepStatus.Wait),
        new("Complete", "Review and confirm.", LuminaStepStatus.Wait)
    ];

    public StepsShowcaseViewModel()
    {
        CurrentStep = 2;
        CurrentVerticalStep = 1;
        CurrentInteractiveStep = 0;
    }

    [RelayCommand]
    private void NextStep()
    {
        if (CurrentInteractiveStep < 3)
        {
            CurrentInteractiveStep++;
        }
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentInteractiveStep > 0)
        {
            CurrentInteractiveStep--;
        }
    }
}

public sealed record StepItemViewModel(
    string Title,
    string Description,
    LuminaStepStatus Status);
