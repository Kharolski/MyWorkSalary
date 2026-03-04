using MyWorkSalary.Helpers;
using MyWorkSalary.Services.Premium;
using MyWorkSalary.ViewModels.Settings;

namespace MyWorkSalary.Views.Settings;

public partial class PremiumInfoPage : ContentPage
{
    private readonly IPremiumService _premiumService;
    private PremiumInfoViewModel ViewModel => (PremiumInfoViewModel)BindingContext;

    public PremiumInfoPage(PremiumInfoViewModel vm, IPremiumService premiumService)
    {
        InitializeComponent();
        BindingContext = vm;
        _premiumService = premiumService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        NavigationHelper.UseNoAnimationBackButton(this);
        RefreshDebugStatus();
    }

    private void OnBuyPremiumClicked(object sender, EventArgs e)
    {
        ViewModel.PurchasePremium();
        DisplayAlert("Demo", "Premium köpt! (Demo-läge)", "OK");
    }

    private void OnSubscribeClicked(object sender, EventArgs e)
    {
        ViewModel.PurchaseSubscription();
        DisplayAlert("Demo", "Prenumeration startad! (Demo-läge)", "OK");
    }

    private void OnCancelSubscriptionClicked(object sender, EventArgs e)
    {
        ViewModel.CancelSubscription();
        DisplayAlert("Demo", "Prenumeration avslutad! (Demo-läge)", "OK");
    }

    private void OnClearPremiumClicked(object sender, EventArgs e)
    {
        ViewModel.ClearAllPremium();
        DisplayAlert("Demo", "All premium-status rensad! (Demo-läge)", "OK");
    }

    private void RefreshDebugStatus()
    {
        if (BindingContext is PremiumInfoViewModel vm)
            vm.RaisePremiumDebugStatusChanged();
    }
}