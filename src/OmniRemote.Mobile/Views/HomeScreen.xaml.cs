using OmniRemote.Mobile.ViewModels;

namespace OmniRemote.Mobile.Views;

public partial class HomeScreen : ContentPage
{
    private readonly HomeViewModel _viewModel;

    public HomeScreen(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
