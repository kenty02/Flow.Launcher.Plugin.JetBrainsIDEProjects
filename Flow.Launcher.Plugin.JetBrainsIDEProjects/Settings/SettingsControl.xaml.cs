namespace Flow.Launcher.Plugin.JetBrainsIDEProjects.Settings;

public partial class SettingsControl
{
    private readonly Settings _settings;
    
    public bool IncludePathInSearch
    {
        get => _settings.IncludePathInSearch;
        set => _settings.IncludePathInSearch = value;
    }

    public SettingsControl(Settings settings)
    {
        _settings = settings;
        DataContext = this;
        
        InitializeComponent();
    }
}
