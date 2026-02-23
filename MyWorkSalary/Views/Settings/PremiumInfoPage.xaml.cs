using MyWorkSalary.Helpers;
using MyWorkSalary.ViewModels.Settings;

namespace MyWorkSalary.Views.Settings;

public partial class PremiumInfoPage : ContentPage
{
    public PremiumInfoPage(PremiumInfoViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();
        NavigationHelper.UseNoAnimationBackButton(this);
    }
}