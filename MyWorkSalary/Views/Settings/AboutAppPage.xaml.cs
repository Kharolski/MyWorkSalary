using MyWorkSalary.Helpers;
using MyWorkSalary.Services.Premium;
using MyWorkSalary.ViewModels.Settings;

namespace MyWorkSalary.Views.Settings;

public partial class AboutAppPage : ContentPage
{
    private readonly IPremiumService _premiumService = new PremiumService();

    public AboutAppPage()
    {
        InitializeComponent();
        BindingContext = new AboutAppViewModel();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        NavigationHelper.UseNoAnimationBackButton(this);
    }

    private void OnActivatePremiumClicked(object sender, EventArgs e)
    {
        _premiumService.SetPremium(true);
        DisplayAlert("Premium aktiverat", "Appen är nu i Premium-läge.", "OK");
    }

    private void OnActivateSubscriptionClicked(object sender, EventArgs e)
    {
        _premiumService.SetSubscription(true);
        DisplayAlert("Prenumeration aktiverad", "Appen är nu i prenumerationsläge.", "OK");
    }

    private void OnClearPremiumClicked(object sender, EventArgs e)
    {
        _premiumService.ClearAll();
        DisplayAlert("Premium rensat", "Appen är nu i gratisläge.", "OK");
    }
}
