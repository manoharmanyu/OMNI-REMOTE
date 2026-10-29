using System.Windows.Input;
using OmniRemote.Core.Abstractions;
using OmniRemote.Core.Simulation;

namespace OmniRemote.Mobile.ViewModels;

public sealed class SimulationSettingsViewModel : BaseViewModel
{
    private readonly SimulationSettings _settings;
    private readonly ISettingsStore _store;

    private SimulationScenario _scenario;
    private int _latencyMs;
    private int _failurePercent;
    private int _timeoutPercent;
    private string _statusMessage = string.Empty;

    public SimulationScenario Scenario
    {
        get => _scenario;
        set
        {
            if (SetProperty(ref _scenario, value))
            {
                _settings.Scenario = value;
                OnPropertyChanged(nameof(EffectiveDescription));
            }
        }
    }

    public int LatencyMs
    {
        get => _latencyMs;
        set
        {
            if (SetProperty(ref _latencyMs, value))
            {
                _settings.CustomLatencyMs = value;
            }
        }
    }

    public int FailurePercent
    {
        get => _failurePercent;
        set
        {
            if (SetProperty(ref _failurePercent, value))
            {
                _settings.CustomFailurePercent = value;
            }
        }
    }

    public int TimeoutPercent
    {
        get => _timeoutPercent;
        set
        {
            if (SetProperty(ref _timeoutPercent, value))
            {
                _settings.CustomTimeoutPercent = value;
            }
        }
    }

    public string EffectiveDescription => Scenario switch
    {
        SimulationScenario.Normal => "Normal: Fast responses (~150ms), 0% error rate.",
        SimulationScenario.Slow => "Slow: 2500ms latency to test responsive UI & cancellation.",
        SimulationScenario.Flaky => "Flaky: 400ms latency, 25% failures, 15% timeouts.",
        SimulationScenario.Offline => "Offline: All virtual devices unreachable.",
        _ => "Custom: Configured using the sliders below.",
    };

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public IReadOnlyList<SimulationScenario> Scenarios { get; } =
        [SimulationScenario.Normal, SimulationScenario.Slow, SimulationScenario.Flaky, SimulationScenario.Offline, SimulationScenario.Custom];

    public ICommand SaveCommand { get; }
    public ICommand BackCommand { get; }

    public SimulationSettingsViewModel(SimulationSettings settings, ISettingsStore store)
    {
        _settings = settings;
        _store = store;
        Title = "Simulation settings";

        _scenario = settings.Scenario;
        _latencyMs = settings.CustomLatencyMs;
        _failurePercent = settings.CustomFailurePercent;
        _timeoutPercent = settings.CustomTimeoutPercent;

        SaveCommand = new Command(async () =>
        {
            await _store.SaveAsync("simulation", _settings, CancellationToken.None);
            StatusMessage = "Settings saved successfully.";
        });

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }
}
