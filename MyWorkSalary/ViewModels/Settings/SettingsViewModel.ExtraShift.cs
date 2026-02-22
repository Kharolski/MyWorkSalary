using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace MyWorkSalary.ViewModels
{
    public partial class SettingsViewModel
    {
        #region Extra Shift Settings

        public class ExtraShiftTypeOption
        {
            public ExtraShiftPayType Type { get; set; }
            public string Text { get; set; } = "";
        }

        public ObservableCollection<ExtraShiftTypeOption> ExtraShiftTypes { get; } =
            new ObservableCollection<ExtraShiftTypeOption>
            {
                new ExtraShiftTypeOption
                {
                    Type = ExtraShiftPayType.PerHour,
                    Text = Resources.Resx.Resources.ExtraShiftPayType_PerHour
                },
                new ExtraShiftTypeOption
                {
                    Type = ExtraShiftPayType.FixedAmount,
                    Text = Resources.Resx.Resources.ExtraShiftPayType_FixedAmount
                }
            };

        private ExtraShiftTypeOption _selectedExtraShiftType;
        public ExtraShiftTypeOption SelectedExtraShiftType
        {
            get
            {
                if (_selectedExtraShiftType != null)
                    return _selectedExtraShiftType;

                _selectedExtraShiftType =
                    ExtraShiftTypes.FirstOrDefault(x => x.Type == ExtraShiftPayType)
                    ?? ExtraShiftTypes.First();

                return _selectedExtraShiftType;
            }
            set
            {
                if (value == null)
                    return;

                _selectedExtraShiftType = value;
                ExtraShiftPayType = value.Type;

                OnPropertyChanged();
            }
        }

        private void RefreshExtraShiftTypeTexts()
        {
            ExtraShiftTypes.Clear();

            ExtraShiftTypes.Add(new ExtraShiftTypeOption
            {
                Type = ExtraShiftPayType.PerHour,
                Text = Resources.Resx.Resources.ExtraShiftPayType_PerHour
            });

            ExtraShiftTypes.Add(new ExtraShiftTypeOption
            {
                Type = ExtraShiftPayType.FixedAmount,
                Text = Resources.Resx.Resources.ExtraShiftPayType_FixedAmount
            });

            _selectedExtraShiftType = null;

            OnPropertyChanged(nameof(ExtraShiftTypes));
            OnPropertyChanged(nameof(SelectedExtraShiftType));
        }

        public bool ExtraShiftEnabled
        {
            get => ActiveJob?.ExtraShiftEnabled ?? false;
            set
            {
                if (ActiveJob == null || ActiveJob.ExtraShiftEnabled == value)
                    return;

                ActiveJob.ExtraShiftEnabled = value;
                SaveActiveJob();

                OnPropertyChanged();
                OnPropertyChanged(nameof(ExtraShiftSummaryText));
                OnPropertyChanged(nameof(ShowExtraShiftSettings));
            }
        }

        public bool ShowExtraShiftSettings =>
            HasActiveJob && ExtraShiftEnabled;

        public ExtraShiftPayType ExtraShiftPayType
        {
            get => ActiveJob?.ExtraShiftPayType ?? ExtraShiftPayType.PerHour;
            set
            {
                if (ActiveJob == null || ActiveJob.ExtraShiftPayType == value)
                    return;

                ActiveJob.ExtraShiftPayType = value;
                SaveActiveJob();

                OnPropertyChanged();
                OnPropertyChanged(nameof(ExtraShiftSummaryText));
                OnPropertyChanged(nameof(ExtraShiftAmountLabelText));
                OnPropertyChanged(nameof(SelectedExtraShiftType));
            }
        }

        public decimal ExtraShiftAmount
        {
            get => ActiveJob?.ExtraShiftAmount ?? 0m;
            set
            {
                if (ActiveJob == null || ActiveJob.ExtraShiftAmount == value)
                    return;

                ActiveJob.ExtraShiftAmount = value;
                SaveActiveJob();

                OnPropertyChanged();
                OnPropertyChanged(nameof(ExtraShiftSummaryText));
            }
        }

        public string ExtraShiftAmountLabelText =>
            ExtraShiftPayType == ExtraShiftPayType.PerHour
                ? Resources.Resx.Resources.ExtraShiftSettings_AmountPerHour
                : Resources.Resx.Resources.ExtraShiftSettings_AmountFixed;

        public string ExtraShiftSummaryText
        {
            get
            {
                if (ActiveJob == null ||
                    !ActiveJob.ExtraShiftEnabled ||
                    ActiveJob.ExtraShiftAmount <= 0)
                    return "—";

                var currency = string.IsNullOrWhiteSpace(ActiveJob.CurrencyCode)
                    ? "SEK"
                    : ActiveJob.CurrencyCode;

                var money = CurrencyHelper.FormatCurrency(
                    ActiveJob.ExtraShiftAmount,
                    currency);

                return ExtraShiftPayType == ExtraShiftPayType.PerHour
                    ? string.Format(Resources.Resx.Resources.ExtraShiftSettings_Summary_PerHour, money)
                    : string.Format(Resources.Resx.Resources.ExtraShiftSettings_Summary_Fixed, money);
            }
        }

        private string _extraShiftAmountText;
        public string ExtraShiftAmountText
        {
            get
            {
                if (_extraShiftAmountText != null)
                    return _extraShiftAmountText;

                _extraShiftAmountText =
                    (ActiveJob?.ExtraShiftAmount ?? 0m)
                    .ToString("0.##", CultureInfo.CurrentCulture);

                return _extraShiftAmountText;
            }
            set
            {
                _extraShiftAmountText = value;
                OnPropertyChanged();

                if (ActiveJob == null)
                    return;

                var input = (value ?? "").Trim().Replace(",", ".");
                if (decimal.TryParse(input,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var amount))
                {
                    if (ActiveJob.ExtraShiftAmount != amount)
                    {
                        ActiveJob.ExtraShiftAmount = amount;
                        SaveActiveJob();
                        OnPropertyChanged(nameof(ExtraShiftSummaryText));
                    }
                }
            }
        }

        #endregion
    }
}
