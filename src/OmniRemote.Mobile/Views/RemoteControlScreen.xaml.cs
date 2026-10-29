using OmniRemote.Mobile.ViewModels;

namespace OmniRemote.Mobile.Views;

public partial class RemoteControlScreen : ContentPage
{
    public RemoteControlScreen(RemoteControlViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
