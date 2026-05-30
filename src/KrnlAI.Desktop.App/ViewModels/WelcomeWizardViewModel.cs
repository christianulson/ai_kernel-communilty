using System.Windows.Input;

namespace KrnlAI.Desktop.App.ViewModels;

public sealed class WelcomeWizardViewModel : ViewModelBase
{
    private int _currentStep = 1;
    private const int TotalSteps = 3;

    public WelcomeWizardViewModel()
    {
        NextStepCommand = new RelayCommand(Next);
        PreviousStepCommand = new RelayCommand(Previous);
        SkipCommand = new RelayCommand(Skip);
    }

    public int CurrentStep
    {
        get => _currentStep;
        set
        {
            if (SetProperty(ref _currentStep, value))
            {
                OnPropertyChanged(nameof(IsStep1Visible));
                OnPropertyChanged(nameof(IsStep2Visible));
                OnPropertyChanged(nameof(IsStep3Visible));
                OnPropertyChanged(nameof(Step1Active));
                OnPropertyChanged(nameof(Step2Active));
                OnPropertyChanged(nameof(Step3Active));
                OnPropertyChanged(nameof(IsNotFirstStep));
                OnPropertyChanged(nameof(NextButtonText));
                OnPropertyChanged(nameof(IsLastStep));
            }
        }
    }

    public bool IsStep1Visible => _currentStep == 1;
    public bool IsStep2Visible => _currentStep == 2;
    public bool IsStep3Visible => _currentStep == 3;
    public bool Step1Active => _currentStep >= 1;
    public bool Step2Active => _currentStep >= 2;
    public bool Step3Active => _currentStep >= 3;
    public bool IsNotFirstStep => _currentStep > 1;
    public bool IsLastStep => _currentStep == TotalSteps;
    public string NextButtonText => IsLastStep ? "Começar!" : "Próximo →";

    public bool IsLocalModeSelected { get; set; } = true;
    public bool IsCloudModeSelected { get; set; }

    public ICommand NextStepCommand { get; }
    public ICommand PreviousStepCommand { get; }
    public ICommand SkipCommand { get; }

    public void Reset()
    {
        CurrentStep = 1;
    }

    private void Next()
    {
        if (_currentStep < TotalSteps)
            CurrentStep++;
        else
            Close();
    }

    private void Previous()
    {
        if (_currentStep > 1)
            CurrentStep--;
    }

    private void Skip()
    {
        Close();
    }

    private void Close()
    {
        foreach (var window in System.Windows.Application.Current.Windows)
        {
            if (window is System.Windows.Window w && w.DataContext is MainViewModel vm)
            {
                vm.ShowWelcomeWizard = false;
                break;
            }
        }
    }
}
