using System.Collections.ObjectModel;
using System.Windows.Input;
using OmniRemote.Domain.Devices;
using DeviceType = OmniRemote.Domain.Devices.DeviceType;

namespace OmniRemote.Mobile.ViewModels;

public sealed record DeviceTypeOption(
    DeviceType Type,
    string DisplayName,
    string IconSymbol);

public sealed class SelectTypeViewModel : BaseViewModel
{
    public ObservableCollection<DeviceTypeOption> Categories { get; } = [];

    public ICommand SelectCategoryCommand { get; }
    public ICommand BackCommand { get; }

    public SelectTypeViewModel()
    {
        Title = "Select type";

        SelectCategoryCommand = new Command<DeviceTypeOption>(async option =>
        {
            if (option is null) return;
            await Shell.Current.GoToAsync($"select-brand?type={(int)option.Type}");
        });

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));

        // Order explicitly matching Screen B in reference screenshot
        Categories.Add(new DeviceTypeOption(DeviceType.AirConditioner, "Air conditioner", "❄"));
        Categories.Add(new DeviceTypeOption(DeviceType.SetTopBox, "Set-top box", "📺"));
        Categories.Add(new DeviceTypeOption(DeviceType.Tv, "TV", "🖥"));
        Categories.Add(new DeviceTypeOption(DeviceType.TvBox, "TV box", "📦"));
        Categories.Add(new DeviceTypeOption(DeviceType.Projector, "Projector", "📽"));
        Categories.Add(new DeviceTypeOption(DeviceType.Fan, "Fan", "🌀"));
        Categories.Add(new DeviceTypeOption(DeviceType.LightBulb, "Light bulb", "💡"));
        Categories.Add(new DeviceTypeOption(DeviceType.AirPurifier, "Air purifier", "🍃"));
        Categories.Add(new DeviceTypeOption(DeviceType.PowerAmplifier, "Power amplifier", "🔊"));
        Categories.Add(new DeviceTypeOption(DeviceType.DvdPlayer, "DVD player", "💿"));
        Categories.Add(new DeviceTypeOption(DeviceType.WaterHeater, "Water heater", "♨"));
        Categories.Add(new DeviceTypeOption(DeviceType.Camera, "Camera", "📷"));
        Categories.Add(new DeviceTypeOption(DeviceType.TemperatureHumiditySensor, "Temp & humidity", "🌡"));
        Categories.Add(new DeviceTypeOption(DeviceType.MotionSensor, "Motion sensor", "🚶"));
    }
}
