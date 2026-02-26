using Microsoft.Maui.Controls;
using MyWorkSalary.Services.Interfaces;

namespace MyWorkSalary.Services.Premium
{
    public class AdService
    {
        private readonly string _bannerAdUnitId;
        private readonly IPremiumService _premiumService;
        private readonly HashSet<string> _banneredPages = new();  // Håll koll på vilka sidor har banner
        
        public AdService(IPremiumService premiumService)
        {
            // Android Banner Ad Unit ID (ditt riktiga ID)
            _bannerAdUnitId = "ca-app-pub-7524471800705106/8795599090";
            _premiumService = premiumService;
        }
        
        public void ShowBanner()
        {
            // Visa inte ads för premium-användare!
            if (_premiumService.IsPremium || _premiumService.IsSubscriber)
            {
                // Dölj befintliga banners om användare blev premium
                HideExistingBanners();
                return;
            }
            
            // Hämta nuvarande sidan från Shell
            var currentPage = Shell.Current.CurrentPage as ContentPage;
            if (currentPage == null) return;
            
            // Kolla om sidan redan har overlay med banner
            if (currentPage.Content is Grid existingGrid && existingGrid.Children.Count > 1)
            {
                // Om banner finns men är dold (premium-användare), visa den igen
                var existingBannerView = existingGrid.Children[1] as View;
                if (existingBannerView != null && !existingBannerView.IsVisible)
                {
                    // Återställ padding och visa banner
                    var existingOriginalContent = existingGrid.Children[0] as View;
                    if (existingOriginalContent != null)
                    {
                        if (existingOriginalContent is Grid existingGridContent)
                        {
                            existingGridContent.Padding = new Thickness(0, 50, 0, 0);
                        }
                        else if (existingOriginalContent is VerticalStackLayout existingStackContent)
                        {
                            existingStackContent.Padding = new Thickness(0, 50, 0, 0);
                        }
                        else if (existingOriginalContent is ScrollView existingScrollContent)
                        {
                            existingScrollContent.Padding = new Thickness(0, 50, 0, 0);
                        }
                    }
                    existingBannerView.IsVisible = true;
                }
                return;
            }
            
            // Skapa en overlay container för fixed banner
            var overlayGrid = new Grid
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                ZIndex = 1000,
                InputTransparent = true  // Låt klick gå igenom till innehållet under
            };

            // Skapa en fixed banner
            var bannerView = new Label
            {
                Text = $" Ad Banner ({_bannerAdUnitId.Substring(0, 20)}...)",
                BackgroundColor = Colors.LightGray,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Start,
                Padding = 10,
                Margin = 0,  // Tillbaka till toppen
                HeightRequest = 50
            };

            overlayGrid.Add(bannerView);

            // Lägg till padding på originalt innehåll så det inte hamnar under bannern
            var originalContent = currentPage.Content;
            if (originalContent is Grid gridContent)
            {
                gridContent.Padding = new Thickness(0, 40, 0, 0);  // 40px top padding
            }
            else if (originalContent is VerticalStackLayout stackContent)
            {
                stackContent.Padding = new Thickness(0, 40, 0, 0);  // 40px top padding
            }
            else if (originalContent is ScrollView scrollContent)
            {
                scrollContent.Padding = new Thickness(0, 40, 0, 0);  // 40px top padding
            }

            // Skapa overlay grid med originalt innehåll + banner
            var overlayContainer = new Grid
            {
                Children = 
                {
                    originalContent,  // Originalt innehåll (med padding)
                    overlayGrid       // Fixed banner overlay
                }
            };

            // Sätt overlay som ny content
            currentPage.Content = overlayContainer;
        }
        
        /// <summary>
        /// Dölj befintliga banners för premium-användare
        /// </summary>
        private void HideExistingBanners()
        {
            var currentPage = Shell.Current.CurrentPage as ContentPage;
            if (currentPage == null) return;
            
            // Om sidan har overlay med banner, dölj bara bannern
            if (currentPage.Content is Grid overlayGrid && overlayGrid.Children.Count > 1)
            {
                // Hämta banner (andra child) och casta till View
                var hideBannerView = overlayGrid.Children[1] as View;
                if (hideBannerView != null)
                {
                    // Dölj bannern istället för att ta bort hela overlay
                    hideBannerView.IsVisible = false;
                }
                
                // Ta bort padding från originalt innehåll
                var hideOriginalContent = overlayGrid.Children[0] as View;
                if (hideOriginalContent != null)
                {
                    if (hideOriginalContent is Grid hideGridContent)
                    {
                        hideGridContent.Padding = new Thickness(0);
                    }
                    else if (hideOriginalContent is VerticalStackLayout hideStackContent)
                    {
                        hideStackContent.Padding = new Thickness(0);
                    }
                    else if (hideOriginalContent is ScrollView hideScrollContent)
                    {
                        hideScrollContent.Padding = new Thickness(0);
                    }
                }
            }
        }
        
        public void HideBanner(ContentPage page)
        {
            // Implementera senare för att ta bort banner
        }
        
        /// <summary>
        /// Forcera refresh av alla banners när premium-status ändras
        /// </summary>
        public void RefreshAllBanners()
        {
            // Hämta nuvarande sidan och forcera refresh
            var currentPage = Shell.Current.CurrentPage as ContentPage;
            if (currentPage == null) return;
            
            // Ta bort ALLA overlays och återskapa från grunden
            if (currentPage.Content is Grid overlayGrid && overlayGrid.Children.Count > 1)
            {
                var originalContent = overlayGrid.Children[0] as View;
                if (originalContent != null)
                {
                    // Återställ till originalt innehåll UTAN overlay
                    currentPage.Content = originalContent;
                    
                    // Nollställ padding
                    if (originalContent is Grid gridContent)
                    {
                        gridContent.Padding = new Thickness(0);
                    }
                    else if (originalContent is VerticalStackLayout stackContent)
                    {
                        stackContent.Padding = new Thickness(0);
                    }
                    else if (originalContent is ScrollView scrollContent)
                    {
                        scrollContent.Padding = new Thickness(0);
                    }
                }
            }
            
            // Visa banner igen (eller inte) baserat på ny premium-status
            ShowBanner();
        }
    }
}
