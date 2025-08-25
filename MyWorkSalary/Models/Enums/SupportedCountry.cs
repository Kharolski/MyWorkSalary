using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyWorkSalary.Models.Enums
{
    public enum SupportedCountry
    {
        Sweden,
        Norway,
        Denmark,
        Finland,
        France,
        Ireland
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
            SupportedCountry.Finland => "FI",
            SupportedCountry.France => "FR",
            SupportedCountry.Ireland => "IE",
            _ => throw new NotImplementedException()
        };

        public static string GetDisplayName(this SupportedCountry country) => country switch
        {
            SupportedCountry.Sweden => "Sverige",
            SupportedCountry.Norway => "Norge",
            SupportedCountry.Denmark => "Danmark",
            SupportedCountry.Finland => "Finland",
            SupportedCountry.France => "Frankrike",
            SupportedCountry.Ireland => "Irland",
            _ => throw new NotImplementedException()
        };
    }
}
