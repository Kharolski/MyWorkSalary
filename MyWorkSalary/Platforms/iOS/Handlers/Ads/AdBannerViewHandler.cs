using Microsoft.Maui.Handlers;
using Google.MobileAds;
using MyWorkSalary.Services.Handlers.Ads;

namespace MyWorkSalary.Platforms.iOS.Handlers.Ads
{
    public class AdBannerViewMapper : PropertyMapper<AdBannerView, AdBannerViewHandler>
    {
        public static AdBannerViewMapper Default = new AdBannerViewMapper();
    }

    public class AdBannerViewHandler : ViewHandler<AdBannerView, BannerView>
    {
        public AdBannerViewHandler() : base(AdBannerViewMapper.Default)
        {
        }

        public AdBannerViewHandler(IPropertyMapper mapper) : base(mapper)
        {
        }

        protected override BannerView CreatePlatformView()
        {
            var bannerView = new BannerView(AdSizeCons.Banner)
            {
                AdUnitId = VirtualView.AdUnitId
            };

            // Load ad
            var request = Request.GetDefaultRequest();
            bannerView.LoadRequest(request);
            
            return bannerView;
        }

        protected override void ConnectHandler(BannerView platformView)
        {
            base.ConnectHandler(platformView);
        }

        protected override void DisconnectHandler(BannerView platformView)
        {
            base.DisconnectHandler(platformView);
        }
    }
}
