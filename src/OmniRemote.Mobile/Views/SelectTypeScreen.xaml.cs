using OmniRemote.Mobile.ViewModels;

namespace OmniRemote.Mobile.Views;

public partial class SelectTypeScreen : ContentPage
{
    public SelectTypeScreen(SelectTypeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
