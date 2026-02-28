using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using MyWorkSalary.Services.Interfaces;
using MyWorkSalary.Services.Handlers.Ads;
using System.Threading.Tasks;

namespace MyWorkSalary.Services.Premium
{
    public class AdService
    {
        private readonly string _bannerAdUnitId;
        private readonly string _bannerAdGoogleDemoId;
        private readonly IPremiumService _premiumService;

        private readonly HashSet<Type> _banneredPages = new();

        public AdService(IPremiumService premiumService)
        {
            _bannerAdUnitId = "ca-app-pub-7524471800705106/8795599090";
            _bannerAdGoogleDemoId = "ca-app-pub-3940256099942544/6300978111";
            _premiumService = premiumService;

            // Lyssna på premium-ändringar
            _premiumService.PremiumStatusChanged += OnPremiumStatusChanged;
            _premiumService.SubscriptionStatusChanged += OnSubscriptionStatusChanged;
        }

        private void OnPremiumStatusChanged(object sender, bool isPremium)
        {
            System.Diagnostics.Debug.WriteLine($"🎯 AdService: Premium status changed to {isPremium}");
            RefreshAllBanners();
        }

        private void OnSubscriptionStatusChanged(object sender, bool isSubscriber)
        {
            System.Diagnostics.Debug.WriteLine($"🎯 AdService: Subscription status changed to {isSubscriber}");
            RefreshAllBanners();
        }

        public void ShowBanner()
        {
            var currentPage = Shell.Current.CurrentPage as ContentPage;
            if (currentPage == null)
                return;

            var pageType = currentPage.GetType();
            
            // Debug logging
            System.Diagnostics.Debug.WriteLine($"🎯 ShowBanner called for {pageType.Name}");
            System.Diagnostics.Debug.WriteLine($"🎯 IsPremium: {_premiumService.IsPremium}");
            System.Diagnostics.Debug.WriteLine($"🎯 IsSubscriber: {_premiumService.IsSubscriber}");

            if (_premiumService.IsPremium || _premiumService.IsSubscriber)
            {
                System.Diagnostics.Debug.WriteLine($"🎯 User is premium/subscriber - hiding banner");
                HideExistingBanners();
                _banneredPages.Remove(pageType);
                return;
            }

            // Ta bort befintlig banner från sidan först (för att hantera återbesök)
            HideExistingBanners();

            System.Diagnostics.Debug.WriteLine($"🎯 Adding banner to page");

            // Lägg till banner överst på sidan
            var adBanner = new AdBannerView
            {
                AdUnitId = _bannerAdGoogleDemoId,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Start,
                HeightRequest = 50,
                ZIndex = 1000
            };

            // Försök hitta AdBannerContainer först (nya HomePage layout)
            var adBannerContainer = currentPage.FindByName<Grid>("AdBannerContainer");
            if (adBannerContainer != null)
            {
                // Använd dedikerad container
                System.Diagnostics.Debug.WriteLine($"🎯 Using AdBannerContainer");
                adBannerContainer.Children.Add(adBanner);
            }
            else
            {
                // Fallback till gammal logik
                System.Diagnostics.Debug.WriteLine($"🎯 Using fallback logic");
                AddBannerToContent(currentPage, adBanner);
            }

            _banneredPages.Add(pageType);
        }

        private void AddBannerToContent(ContentPage currentPage, AdBannerView adBanner)
        {
            // Lägg till banner i befintlig content
            if (currentPage.Content is Layout layout)
            {
                // Banner i TOPPEN av layout
                var bannerContainer = new VerticalStackLayout
                {
                    Children = { adBanner },
                    Spacing = 0,
                    Padding = new Thickness(0)
                };
                
                layout.Children.Insert(0, bannerContainer); // Lägg till först
                layout.Padding = new Thickness(0, 0, 0, 0); // Ingen extra padding
            }
            else if (currentPage.Content is ScrollView scrollView)
            {
                // Banner i TOPPEN av scroll content
                if (scrollView.Content is Layout scrollLayout)
                {
                    var bannerContainer = new VerticalStackLayout
                    {
                        Children = { adBanner },
                        Spacing = 0,
                        Padding = new Thickness(0)
                    };
                    
                    scrollLayout.Children.Insert(0, bannerContainer); // Lägg till först
                    scrollView.Padding = new Thickness(0, 0, 0, 0); // Ingen extra padding
                }
            }
        }


        public void HideExistingBanners()
        {
            var currentPage = Shell.Current.CurrentPage as ContentPage;
            if (currentPage == null)
                return;

            System.Diagnostics.Debug.WriteLine($"🎯 HideExistingBanners called for {currentPage.GetType().Name}");

            // Försök hitta AdBannerContainer först (nya HomePage layout)
            var adBannerContainer = currentPage.FindByName<Grid>("AdBannerContainer");
            if (adBannerContainer != null)
            {
                // Ta bort alla AdBannerViews från container på UI-tråden
                var bannersToRemove = adBannerContainer.Children.OfType<AdBannerView>().ToList();
                System.Diagnostics.Debug.WriteLine($"🎯 Found {bannersToRemove.Count} banners in AdBannerContainer");
                
                if (bannersToRemove.Any())
                {
                    // Använd Dispatcher för att säkert ta bort UI-element
                    Dispatcher.GetForCurrentThread()?.DispatchAsync(() =>
                    {
                        try
                        {
                            foreach (var adBanner in bannersToRemove.ToList())
                            {
                                if (adBannerContainer.Children.Contains(adBanner))
                                {
                                    adBannerContainer.Children.Remove(adBanner);
                                    System.Diagnostics.Debug.WriteLine($"🎯 Removed banner from AdBannerContainer");
                                }
                            }
                            
                            // Rensa sidan från _banneredPages när alla banners är borttagna
                            _banneredPages.Remove(currentPage.GetType());
                            System.Diagnostics.Debug.WriteLine($"🎯 Removed page from _banneredPages");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"🚨 Error removing banner: {ex}");
                        }
                    });
                }
            }
            else
            {
                // Fallback till gammal logik
                System.Diagnostics.Debug.WriteLine($"🎯 Using fallback hide logic");
                HideBannersFromContent(currentPage);
            }
        }

        private void HideBannersFromContent(ContentPage currentPage)
        {
            // Ta bort banners från befintlig content
            if (currentPage.Content is Layout layout)
            {
                // Hitta och ta bort banner-container
                var bannerToRemove = layout.Children.OfType<VerticalStackLayout>()
                    .FirstOrDefault(container => container.Children.OfType<AdBannerView>().Any());
                
                if (bannerToRemove != null)
                {
                    // Koppla loss AdView
                    var adBanner = bannerToRemove.Children.OfType<AdBannerView>().FirstOrDefault();
                    adBanner?.Handler?.DisconnectHandler();
                    adBanner.Handler = null;
                    
                    // Ta bort hela container
                    layout.Children.Remove(bannerToRemove);
                }
            }
            else if (currentPage.Content is ScrollView scrollView)
            {
                if (scrollView.Content is Layout scrollLayout)
                {
                    // Hitta och ta bort banner-container
                    var bannerToRemove = scrollLayout.Children.OfType<VerticalStackLayout>()
                        .FirstOrDefault(container => container.Children.OfType<AdBannerView>().Any());
                    
                    if (bannerToRemove != null)
                    {
                        // Koppla loss AdView
                        var adBanner = bannerToRemove.Children.OfType<AdBannerView>().FirstOrDefault();
                        adBanner?.Handler?.DisconnectHandler();
                        adBanner.Handler = null;
                        
                        // Ta bort hela container
                        scrollLayout.Children.Remove(bannerToRemove);
                    }
                }
            }
        }

        public void RefreshAllBanners()
        {
            var currentPage = Shell.Current.CurrentPage as ContentPage;
            if (currentPage == null)
                return;

            var pageType = currentPage.GetType();
            System.Diagnostics.Debug.WriteLine($"🎯 RefreshAllBanners called for {pageType.Name}");
            
            // Ta bort befintlig banner först
            HideExistingBanners();
            
            // Vänta lite och försök igen
            Task.Delay(100).ContinueWith(_ =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _banneredPages.Remove(pageType);
                    ShowBanner();
                });
            });
        }
    }
}
