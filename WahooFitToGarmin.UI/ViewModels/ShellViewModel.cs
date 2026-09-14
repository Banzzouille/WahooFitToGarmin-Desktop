using CommunityToolkit.Mvvm.Input;

namespace WahooFitToGarmin.UI.ViewModels;

/// <summary>
/// The window shell: which page is showing, and the navigation between them.
/// </summary>
/// <remarks>
/// The previous shell went through a page service, a navigation service and a
/// frame, mapping view model type names to page types. Avalonia resolves a view
/// from a view model by convention, so the shell holds the current view model
/// and a back stack, and nothing else.
/// </remarks>
public sealed class ShellViewModel : ViewModelBase
{
    private readonly MainViewModel _main;
    private readonly SettingsViewModel _settings;
    private readonly Stack<ViewModelBase> _back = new();

    private ViewModelBase _current;

    public ShellViewModel(MainViewModel main, SettingsViewModel settings)
    {
        _main = main;
        _settings = settings;
        _current = main;

        ShowMainCommand = new RelayCommand(() => Navigate(_main));
        ShowSettingsCommand = new RelayCommand(() => Navigate(_settings));
        GoBackCommand = new RelayCommand(GoBack, () => _back.Count > 0);
    }

    public ViewModelBase Current
    {
        get => _current;
        private set => SetProperty(ref _current, value);
    }

    public bool IsSettingsSelected => ReferenceEquals(Current, _settings);

    public RelayCommand ShowMainCommand { get; }

    public RelayCommand ShowSettingsCommand { get; }

    public RelayCommand GoBackCommand { get; }

    private void Navigate(ViewModelBase target)
    {
        if (ReferenceEquals(target, Current))
        {
            return;
        }

        _back.Push(Current);
        Current = target;
        AfterNavigation();
    }

    private void GoBack()
    {
        if (_back.Count == 0)
        {
            return;
        }

        Current = _back.Pop();
        AfterNavigation();
    }

    private void AfterNavigation()
    {
        GoBackCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(IsSettingsSelected));
    }
}
