using MyWorkSalary.Helpers;
using MyWorkSalary.Services.Premium;
using MyWorkSalary.ViewModels.Settings;

namespace MyWorkSalary.Views.Settings;

public partial class AboutAppPage : ContentPage
{
    private readonly IPremiumService _premiumService = new PremiumService();
    private readonly AboutAppViewModel _viewModel;

    public AboutAppPage(AboutAppViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
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
        RefreshDebugStatus();
    }

    private void OnActivateSubscriptionClicked(object sender, EventArgs e)
    {
        _premiumService.SetSubscription(true);
        DisplayAlert("Prenumeration aktiverad", "Appen är nu i prenumerationsläge.", "OK");
        RefreshDebugStatus();
    }

    private void OnClearPremiumClicked(object sender, EventArgs e)
    {
        _premiumService.ClearAll();
        DisplayAlert("Premium rensat", "Appen är nu i gratisläge.", "OK");
        RefreshDebugStatus();
    }

    private void OnCancelSubscriptionClicked(object sender, EventArgs e)
    {
        _premiumService.SetSubscription(false);
        DisplayAlert("Prenumeration avslutat", "Appen är nu i gratisläge.", "OK");
        RefreshDebugStatus();
    }

    private void RefreshDebugStatus()
    {
        if (BindingContext is AboutAppViewModel vm)
            vm.RaisePremiumDebugStatusChanged();
    }
}
