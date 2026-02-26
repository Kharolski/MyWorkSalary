using Microsoft.Maui.Controls;
using MyWorkSalary.Services.Interfaces;
using MyWorkSalary.Services.Handlers.Ads;

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
        }

        public void ShowBanner()
        {
            var currentPage = Shell.Current.CurrentPage as ContentPage;
            if (currentPage == null)
                return;

            var pageType = currentPage.GetType();

            if (_premiumService.IsPremium || _premiumService.IsSubscriber)
            {
                HideExistingBanners();
                _banneredPages.Remove(pageType);
                return;
            }

            if (_banneredPages.Contains(pageType))
                return;

            // Lägg till banner överst på sidan
            var adBanner = new AdBannerView
            {
                AdUnitId = _bannerAdGoogleDemoId,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Start,
                HeightRequest = 50,
                ZIndex = 1000
            };

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

            _banneredPages.Add(pageType);
        }


        public void HideExistingBanners()
        {
            var currentPage = Shell.Current.CurrentPage as ContentPage;
            if (currentPage == null)
                return;

            // NY LOGIK: Ta bort banners från befintlig content
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
            _banneredPages.Remove(pageType);
            ShowBanner();
        }
    }
}
