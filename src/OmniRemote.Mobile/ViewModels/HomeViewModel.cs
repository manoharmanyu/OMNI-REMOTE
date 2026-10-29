using System.Collections.ObjectModel;
using System.Windows.Input;
using OmniRemote.Core.Devices;
using OmniRemote.Domain.Commands;
using OmniRemote.Domain.Devices;

namespace OmniRemote.Mobile.ViewModels;

public sealed class HomeViewModel : BaseViewModel
{
    private readonly DeviceRegistry _registry;
    private readonly CommandRouter _router;
    private readonly TimeProvider _clock;

    public ObservableCollection<RemoteDevice> Remotes { get; } = [];

    public bool HasRemotes => Remotes.Count > 0;
    public bool IsEmpty => Remotes.Count == 0;

    public ICommand AddRemoteCommand { get; }
    public ICommand SelectRemoteCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand OpenHistoryCommand { get; }
    public ICommand DeleteRemoteCommand { get; }
    public ICommand TogglePowerCommand { get; }

    public HomeViewModel(DeviceRegistry registry, CommandRouter router, TimeProvider clock)
    {
        _registry = registry;
        _router = router;
        _clock = clock;

        _registry.Changed += (_, _) => RefreshRemotes();

        AddRemoteCommand = new Command(async () => await Shell.Current.GoToAsync("select-type"));
        SelectRemoteCommand = new Command<RemoteDevice>(async device =>
        {
            if (device is null) return;
            await Shell.Current.GoToAsync($"remote-control?deviceId={device.Id}");
        });
        OpenSettingsCommand = new Command(async () => await Shell.Current.GoToAsync("simulation-settings"));
        OpenHistoryCommand = new Command(async () => await Shell.Current.GoToAsync("activity-history"));
        DeleteRemoteCommand = new Command<RemoteDevice>(async device =>
        {
            if (device is null) return;
            var confirm = await Shell.Current.DisplayAlertAsync("Delete Remote", $"Remove {device.Name}?", "Delete", "Cancel");
            if (confirm)
            {
                await _registry.RemoveAsync(device.Id);
                RefreshRemotes();
            }
        });
        TogglePowerCommand = new Command<RemoteDevice>(async device =>
        {
            if (device is null || !device.Profile.Supports(Capability.Power)) return;
            var cmd = DeviceCommand.Create(device.Id, CommandNames.PowerSet, _clock, ("on", "true"));
            await _router.SendAsync(cmd);
        });
    }

    public async Task InitializeAsync()
    {
        await _registry.EnsureLoadedAsync();
        RefreshRemotes();
    }

    private void RefreshRemotes()
    {
        Remotes.Clear();
        foreach (var dev in _registry.Devices)
        {
            Remotes.Add(dev);
        }
        OnPropertyChanged(nameof(HasRemotes));
        OnPropertyChanged(nameof(IsEmpty));
    }
}
