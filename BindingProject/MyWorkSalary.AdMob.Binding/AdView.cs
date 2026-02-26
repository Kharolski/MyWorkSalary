using Android.Content;
using Android.Views;
using Android.Widget;

namespace MyWorkSalary.AdMob.Binding
{
    public partial class AdView : FrameLayout
    {
        public AdView(Context context) : base(context)
        {
            // 🎯 Native implementation kommer att skapas av binding
        }
        
        public string AdUnitId
        {
            get => GetProperty<string>("adUnitId");
            set => SetProperty("adUnitId", value);
        }
        
        public AdSize AdSize
        {
            get => GetProperty<AdSize>("adSize");
            set => SetProperty("adSize", value);
        }
        
        public void LoadAd(AdRequest request)
        {
            CallVoidMethod("loadAd", request);
        }
    }
    
    public class AdSize
    {
        public static readonly AdSize Banner = new AdSize("BANNER");
        public static readonly AdSize LargeBanner = new AdSize("LARGE_BANNER");
        
        public string Size { get; }
        
        public AdSize(string size)
        {
            Size = size;
        }
    }
    
    public class AdRequest
    {
        public class Builder
        {
            public Builder()
            {
                // 🎯 Native implementation
            }
            
            public AdRequest Build()
            {
                return new AdRequest();
            }
        }
    }
}
