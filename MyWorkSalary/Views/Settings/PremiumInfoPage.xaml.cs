using MyWorkSalary.Helpers;

namespace MyWorkSalary.Views.Settings;

public partial class PremiumInfoPage : ContentPage
{
	public PremiumInfoPage()
	{
		InitializeComponent();
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        NavigationHelper.UseNoAnimationBackButton(this);
    }
}