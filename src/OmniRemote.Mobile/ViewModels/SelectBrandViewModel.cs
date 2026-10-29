using System.Collections.ObjectModel;
using System.Windows.Input;
using OmniRemote.Core.Catalog;
using OmniRemote.Domain.Devices;
using DeviceType = OmniRemote.Domain.Devices.DeviceType;

namespace OmniRemote.Mobile.ViewModels;

[QueryProperty(nameof(TypeQuery), "type")]
public sealed class SelectBrandViewModel : BaseViewModel
{
    private readonly BrandCatalog _catalog;
    private DeviceType _selectedType = DeviceType.AirConditioner;
    private string _searchQuery = string.Empty;
    private string _searchPlaceholder = "Search for brand";

    public DeviceType SelectedType
    {
        get => _selectedType;
        set
        {
            if (SetProperty(ref _selectedType, value))
            {
                SearchPlaceholder = $"Search for brand {DeviceTypeInfo.DisplayName(_selectedType)}";
                ReloadBrands();
            }
        }
    }

    public string TypeQuery
    {
        set
        {
            if (int.TryParse(value, out var t) && Enum.IsDefined((DeviceType)t))
            {
                SelectedType = (DeviceType)t;
            }
        }
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                ReloadBrands();
            }
        }
    }

    public string SearchPlaceholder
    {
        get => _searchPlaceholder;
        set => SetProperty(ref _searchPlaceholder, value);
    }

    public ObservableCollection<Brand> TopBrands { get; } = [];
    public bool HasTopBrands => TopBrands.Count > 0;
    public ObservableCollection<BrandSection> BrandSections { get; } = [];
    public static IReadOnlyList<string> IndexKeys => BrandCatalog.IndexKeys;

    public ICommand SelectBrandCommand { get; }
    public ICommand SelectIndexCommand { get; }
    public ICommand BackCommand { get; }

    public SelectBrandViewModel(BrandCatalog catalog)
    {
        _catalog = catalog;

        SelectBrandCommand = new Command<Brand>(async brand =>
        {
            if (brand is null) return;
            await Shell.Current.GoToAsync($"configure-remote?type={(int)SelectedType}&brandId={brand.Id}");
        });

        SelectIndexCommand = new Command<string>(key =>
        {
            // Scrolling target selection
        });

        BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    private void ReloadBrands()
    {
        TopBrands.Clear();
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            foreach (var b in _catalog.TopBrands(SelectedType))
            {
                TopBrands.Add(b);
            }
        }
        OnPropertyChanged(nameof(HasTopBrands));

        var results = _catalog.Search(SelectedType, SearchQuery);
        var sections = BrandCatalog.Group(results);

        BrandSections.Clear();
        foreach (var s in sections)
        {
            BrandSections.Add(s);
        }
    }
}
