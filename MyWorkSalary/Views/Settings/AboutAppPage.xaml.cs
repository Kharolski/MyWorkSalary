using MyWorkSalary.Helpers;
using MyWorkSalary.ViewModels.Settings;

namespace MyWorkSalary.Views.Settings;

public partial class AboutAppPage : ContentPage
{
	public AboutAppPage()
	{
		InitializeComponent();
        BindingContext = new AboutAppViewModel();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Fixar ett känt MAUI/Shell-problem där sidan som navigeras tillbaka till
        NavigationHelper.UseNoAnimationBackButton(this);
    }
}