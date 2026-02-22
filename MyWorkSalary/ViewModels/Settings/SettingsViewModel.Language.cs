using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Specialized;
using System.Collections.ObjectModel;

namespace MyWorkSalary.ViewModels
{
    public partial class SettingsViewModel
    {
        #region Language Methods

        private void ApplyLanguage(LanguageOption lang)
        {
            if (lang == null)
                return;

            _selectedLanguage = lang;
            OnPropertyChanged(nameof(SelectedLanguage));

            // Hämta korrekt kultur (med region) via helper
            var culture = CultureHelper.GetCulture(lang.Code);

            // Uppdatera TranslationManager + trådculture
            TranslationManager.Instance.ChangeCulture(culture);

            // Trigga språkändringshändelsen globalt
            LocalizationHelper.NotifyLanguageChanged();

            // Uppdatera ExtraShift picker-texter
            RefreshExtraShiftTypeTexts();
            OnPropertyChanged(nameof(ExtraShiftAmountLabelText));
            OnPropertyChanged(nameof(ExtraShiftSummaryText));
            OnPropertyChanged(nameof(SelectedExtraShiftType));

            // Uppdatera OnCall picker texter
            InitOnCallOptionTexts();
            OnPropertyChanged(nameof(OnCallStandbyAmountLabelText));
            OnPropertyChanged(nameof(OnCallSettingsSummaryText));

            // Spara inställningen 
            _appSettings.LanguageCode = lang.Code;
            _databaseService.AppSettings.SaveAppSettings(_appSettings);

            // Trigger UI-uppdateringar (t.ex CollectionView valutaformat)
            var old = OBRates;
            OBRates = null;
            OBRates = old;

            OnPropertyChanged(nameof(ThemeDescription));
        }

        private void OnCultureChanged(object sender, EventArgs e)
        {
            // Tvinga UI att uppdatera valutor & datum när språk byts
            var currentRates = OBRates.ToList();
            OBRates = new ObservableCollection<OBRate>(currentRates);
        }

        #endregion
    }
}
