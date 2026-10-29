using System.Collections.ObjectModel;
using System.Windows.Input;
using OmniRemote.Core.Devices;
using OmniRemote.Domain.Devices;

namespace OmniRemote.Mobile.ViewModels;

[QueryProperty(nameof(DeviceIdQuery), "deviceId")]
public sealed class ActivityHistoryViewModel : BaseViewModel
{
    private readonly ActivityLog _activity;
    private Guid? _filterDeviceId;

    public string DeviceIdQuery
    {
        set
        {
            if (Guid.TryParse(value, out var id))
            {
                _filterDeviceId = id;
                ReloadEntries();
            }
        }
    }

    public ObservableCollection<ActivityEntry> Entries { get; } = [];

    public ICommand ClearHistoryCommand { get; }
    public ICommand BackCommand { get; }

    public ActivityHistoryViewModel(ActivityLog activity)
    {
        _activity = activity;
        Title = "Activity history";

        ClearHistoryCommand = new Command(async () =>
        {
            await _activity.ClearAsync(_filterDeviceId);
            ReloadEntries();
        });

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    public async Task InitializeAsync()
    {
        await _activity.EnsureLoadedAsync();
        ReloadEntries();
    }

    private void ReloadEntries()
    {
        Entries.Clear();
        var list = _filterDeviceId.HasValue
            ? _activity.ForDevice(_filterDeviceId.Value)
            : _activity.Entries;

        foreach (var e in list)
        {
            Entries.Add(e);
        }
    }
}
