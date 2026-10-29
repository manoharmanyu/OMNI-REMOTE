using OmniRemote.Mobile.ViewModels;

namespace OmniRemote.Mobile.Views;

public partial class SelectBrandScreen : ContentPage
{
    public SelectBrandScreen(SelectBrandViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
