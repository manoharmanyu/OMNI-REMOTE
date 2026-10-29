using OmniRemote.Mobile.ViewModels;

namespace OmniRemote.Mobile.Views;

public partial class ActivityHistoryScreen : ContentPage
{
    private readonly ActivityHistoryViewModel _viewModel;

    public ActivityHistoryScreen(ActivityHistoryViewModel viewModel)
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
