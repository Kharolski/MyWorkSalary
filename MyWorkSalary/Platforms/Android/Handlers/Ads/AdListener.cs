#if ANDROID
using Android.Gms.Ads;
using LoadAdError = Android.Gms.Ads.LoadAdError;

namespace MyWorkSalary.Platforms.Android.Handlers.Ads
{
    public class MauiAdListener : global::Android.Gms.Ads.AdListener
    {
        private readonly Action _onAdLoaded;
        private readonly Action<string> _onAdFailed;
        private readonly Action _onAdClicked;

        public MauiAdListener(Action onAdLoaded, Action<string> onAdFailed, Action onAdClicked)
        {
            _onAdLoaded = onAdLoaded;
            _onAdFailed = onAdFailed;
            _onAdClicked = onAdClicked;
        }

        public override void OnAdLoaded()
        {
            base.OnAdLoaded();
            _onAdLoaded?.Invoke();
        }

        public override void OnAdFailedToLoad(LoadAdError error)
        {
            base.OnAdFailedToLoad(error);
            _onAdFailed?.Invoke(error?.ToString() ?? "Unknown error");
        }

        public override void OnAdClicked()
        {
            base.OnAdClicked();
            _onAdClicked?.Invoke();
        }
    }
}
#endif
