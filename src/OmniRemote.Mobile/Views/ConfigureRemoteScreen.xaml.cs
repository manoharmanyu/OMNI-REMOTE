using OmniRemote.Mobile.ViewModels;

namespace OmniRemote.Mobile.Views;

public partial class ConfigureRemoteScreen : ContentPage
{
    public ConfigureRemoteScreen(ConfigureRemoteViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
