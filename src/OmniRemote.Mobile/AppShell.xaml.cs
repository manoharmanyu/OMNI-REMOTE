using OmniRemote.Mobile.Views;

namespace OmniRemote.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("select-type", typeof(SelectTypeScreen));
        Routing.RegisterRoute("select-brand", typeof(SelectBrandScreen));
        Routing.RegisterRoute("configure-remote", typeof(ConfigureRemoteScreen));
        Routing.RegisterRoute("remote-control", typeof(RemoteControlScreen));
        Routing.RegisterRoute("activity-history", typeof(ActivityHistoryScreen));
        Routing.RegisterRoute("simulation-settings", typeof(SimulationSettingsScreen));
    }
}
