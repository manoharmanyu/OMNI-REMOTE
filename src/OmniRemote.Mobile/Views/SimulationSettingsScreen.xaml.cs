using OmniRemote.Mobile.ViewModels;

namespace OmniRemote.Mobile.Views;

public partial class SimulationSettingsScreen : ContentPage
{
    public SimulationSettingsScreen(SimulationSettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
