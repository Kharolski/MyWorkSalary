using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MyWorkSalary.ViewModels;

namespace MyWorkSalary.Helpers.Localization
{
    public static class CurrencyHelper
    {
        #region Configuration

        // Kopplar valuta-kod till kultur (för symbol och formatering)
        private static readonly Dictionary<string, string> CurrencyCultures = new()
        {
            { "SEK", "sv-SE" },
            { "EUR", "en-IE" },
            { "USD", "en-US" },
            { "NOK", "nb-NO" },
            { "DKK", "da-DK" },
            { "PLN", "pl-PL" }
        };

        #endregion

        #region Symbols

        /// <summary>
        /// Hämtar symbolen för en valuta, t.ex. "kr", "$", "€".
        /// </summary>
        public static string GetSymbol(string currencyCode)
        {
            try
            {
                if (CurrencyCultures.TryGetValue(currencyCode, out var cultureName))
                {
                    var region = new RegionInfo(cultureName);
                    return region.CurrencySymbol;
                }
                return currencyCode; // fallback
            }
            catch
            {
                return currencyCode;
            }
        }

        #endregion

        #region DisplayName

        /// <summary>
        /// Visningsnamn för en valuta, t.ex. "Svenska kronor (SEK)".
        /// </summary>
        public static string GetDisplayName(string currencyCode)
        {
            return currencyCode switch
            {
                "SEK" => "Svenska kronor (SEK)",
                "EUR" => "Euro (EUR)",
                "USD" => "US Dollar (USD)",
                "NOK" => "Norska kronor (NOK)",
                "DKK" => "Danska kronor (DKK)",
                "PLN" => "Polska zloty (PLN)",
                _ => currencyCode
            };
        }

        #endregion

        #region Lists

        /// <summary>
        /// Returnerar en lista med alla valutor (Code + Symbol).
        /// </summary>
        public static List<CurrencyOption> GetAllCurrencies()
        {
            return CurrencyCultures.Select(c => new CurrencyOption
            {
                Code = c.Key,
                Symbol = GetSymbol(c.Key)
            }).ToList();
        }

        /// <summary>
        /// Returnerar en lista med valutor för bindning i UI (Value + DisplayName).
        /// </summary>
        public static List<LocalizedOption<string>> GetAllCurrenciesLocalized()
        {
            return GetAllCurrencies().Select(c => new LocalizedOption<string>
            {
                Value = c.Code,
                DisplayName = GetDisplayName(c.Code)
            }).ToList();
        }

        #endregion

        #region Formatting

        /// <summary>
        /// Formaterar ett belopp enligt valutan – oberoende av appens språk.
        /// </summary>
        public static string FormatCurrency(decimal amount, string currencyCode)
        {
            try
            {
                var cultureName = CurrencyCultures.ContainsKey(currencyCode)
                    ? CurrencyCultures[currencyCode]
                    : "en-IE"; // fallback

                var culture = new CultureInfo(cultureName);
                culture.NumberFormat.CurrencySymbol = GetSymbol(currencyCode);

                return string.Format(culture, "{0:C}", amount);
            }
            catch
            {
                return $"{amount:N2} {currencyCode}";
            }
        }

        #endregion
    }

    public class CurrencyOption
    {
        public string Code { get; set; }
        public string Symbol { get; set; }
        public string DisplayName => $"{Code} ({Symbol})";
    }
}
