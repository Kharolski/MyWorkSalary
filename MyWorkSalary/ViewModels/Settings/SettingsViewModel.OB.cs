using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Views.Pages;
using MyWorkSalary.Views.Pages.Templates;
using System.Collections.ObjectModel;

namespace MyWorkSalary.ViewModels
{
    public partial class SettingsViewModel
    {
        #region OB Methods

        private void LoadOBRates()
        {
            if (ActiveJob != null)
            {
                var obRates = _databaseService.OBRates.GetOBRates(ActiveJob.Id);

                // Sätt rätt valuta för varje OBRate
                foreach (var ob in obRates)
                {
                    ob.CurrencyCode = string.IsNullOrWhiteSpace(ActiveJob.CurrencyCode)
                        ? "SEK"
                        : ActiveJob.CurrencyCode;
                }

                OBRates = new ObservableCollection<OBRate>(obRates);

                ((Command)ReplaceWithTemplateCommand).ChangeCanExecute();
                OnPropertyChanged(nameof(HasOBRates));
                OnPropertyChanged(nameof(FreeOBTemplateButtonText));
            }
            else
            {
                OBRates.Clear();
                OnPropertyChanged(nameof(HasOBRates));
                OnPropertyChanged(nameof(FreeOBTemplateButtonText));
            }
        }

        private async Task OnOpenOBTemplates(bool replaceMode)
        {
            if (ActiveJob == null)
                return;

            var mode = replaceMode ? "replace" : "add";

            await Shell.Current.GoToAsync(
                $"{nameof(OBTemplatesPage)}?jobId={ActiveJob.Id}&mode={mode}");
        }

        private async void OnAddOBRate()
        {
            await Shell.Current.GoToAsync(nameof(AddOBRatePage));
        }

        private async void OnEditOBRate(OBRate obRate)
        {
            if (obRate == null)
                return;

            await Shell.Current.GoToAsync(
                $"{nameof(AddOBRatePage)}?obRateId={obRate.Id}");
        }

        private async void OnDeleteOBRate(OBRate obRate)
        {
            if (obRate == null)
                return;

            bool confirm = await Shell.Current.DisplayAlert(
                Resources.Resx.Resources.DeleteOBRateTitle,
                string.Format(Resources.Resx.Resources.DeleteOBRateMessageFormat, obRate.Name),
                Resources.Resx.Resources.ConfirmDelete,
                Resources.Resx.Resources.Cancel);

            if (!confirm)
                return;

            int deletedRows = _databaseService.OBRates.DeleteOBRate(obRate.Id);

            if (deletedRows > 0)
            {
                await _obEventService.RebuildForJobLastMonths(ActiveJob.Id, 4);
                LoadOBRates();
            }
            else
            {
                await Shell.Current.DisplayAlert(
                    Resources.Resx.Resources.ErrorTitle,
                    Resources.Resx.Resources.DeleteOBRateFailedMessage,
                    Resources.Resx.Resources.Ok);
            }
        }

        #endregion
    }
}
