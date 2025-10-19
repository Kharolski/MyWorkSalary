namespace MyWorkSalary.Models.Enums
{
    public enum SupportedCountry
    {
        Sweden,
        Norway,
        Denmark
        //Finland,
        //France,
        //Ireland
    }

    public class CountryOption
    {
        public SupportedCountry Country { get; set; }
        public string DisplayName => Country.GetDisplayName();
    }

    public static class SupportedCountryExtensions
    {
        public static string GetCode(this SupportedCountry country) => country switch
        {
            SupportedCountry.Sweden => "SE",
            SupportedCountry.Norway => "NO",
            SupportedCountry.Denmark => "DK",
            //SupportedCountry.Finland => "FI",
            //SupportedCountry.France => "FR",
            //SupportedCountry.Ireland => "IE",
            _ => throw new NotImplementedException()
        };

        public static string GetDisplayName(this SupportedCountry country) => country switch
        {
            SupportedCountry.Sweden => Resources.Resx.Resources.Country_Sweden,
            SupportedCountry.Norway => Resources.Resx.Resources.Country_Norway,
            SupportedCountry.Denmark => Resources.Resx.Resources.Country_Denmark,
            //SupportedCountry.Finland => Resources.Resx.Resources.Country_Finland,
            //SupportedCountry.France => Resources.Resx.Resources.Country_France,
            //SupportedCountry.Ireland => Resources.Resx.Resources.Country_Ireland,
            _ => country.ToString()
        };

        public static string GetCurrencyCode(this SupportedCountry country) => country switch
        {
            SupportedCountry.Sweden => "SEK",
            SupportedCountry.Norway => "NOK",
            SupportedCountry.Denmark => "DKK",
            _ => "EUR" // fallback 
        };
    }
}
