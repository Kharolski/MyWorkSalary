#if ANDROID
using Android.Gms.Ads;
using Microsoft.Maui.Handlers;
using MyWorkSalary.Services.Handlers.Ads;

namespace MyWorkSalary.Platforms.Android.Handlers.Ads
{
    public class AdBannerViewMapper : PropertyMapper<AdBannerView, AdBannerViewHandler>
    {
        public static AdBannerViewMapper Default = new AdBannerViewMapper();
    }

    public class AdBannerViewHandler : ViewHandler<AdBannerView, AdView>
    {
        public AdBannerViewHandler() : base(AdBannerViewMapper.Default)
        {
        }

        public AdBannerViewHandler(IPropertyMapper mapper) : base(mapper)
        {
        }

        protected override AdView CreatePlatformView()
        {
            var adView = new AdView(Context)
            {
                AdSize = AdSize.Banner,
                AdUnitId = VirtualView.AdUnitId
            };

            // Lägg till callbacks
            adView.AdListener = new MauiAdListener(
                onAdLoaded: () => Console.WriteLine("Ad loaded successfully"),
                onAdFailed: (error) => Console.WriteLine($"Ad failed: {error}"),
                onAdClicked: () => Console.WriteLine("Ad clicked")
            );

            var request = new AdRequest.Builder().Build();
            adView.LoadAd(request);

            return adView;
        }

    }
}
#endif
