namespace MyWorkSalary.ViewModels.Settings
{
    public partial class AboutAppViewModel : BaseViewModel
    {
        public string AppVersion => $"Version {AppInfo.VersionString}";
    }
}
