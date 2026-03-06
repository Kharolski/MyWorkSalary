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
            return new BannerView(AdSizeCons.Banner)
            {
                AdUnitId = VirtualView.AdUnitId
            };
        }

        protected override void ConnectHandler(BannerView platformView)
        {
            base.ConnectHandler(platformView);

            var root = UIKit.UIApplication.SharedApplication
                .ConnectedScenes
                .OfType<UIKit.UIWindowScene>()
                .FirstOrDefault()?
                .Windows
                .FirstOrDefault()?
                .RootViewController;

            platformView.RootViewController = root;

            var request = Request.GetDefaultRequest();
            platformView.LoadRequest(request);
        }

        protected override void DisconnectHandler(BannerView platformView)
        {
            base.DisconnectHandler(platformView);
        }
    }
}
