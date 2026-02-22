using MyWorkSalary.ViewModels.Settings;

namespace MyWorkSalary.Views.Settings;

public partial class AboutAppPage : ContentPage
{
	public AboutAppPage()
	{
		InitializeComponent();
        BindingContext = new AboutAppViewModel();
    }
}