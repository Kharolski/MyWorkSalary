using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Views.Settings;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;

namespace MyWorkSalary.ViewModels
{
    public partial class SettingsViewModel
    {
        #region OnCall / Jour Settings
        public class Option<T>
        {
            public T Value { get; set; }
            public string Text { get; set; } = "";
        }
        public bool OnCallEnabled
        {
            get => ActiveJob?.OnCallEnabled ?? false;
            set
            {
                if (ActiveJob == null || ActiveJob.OnCallEnabled == value)
                    return;

                ActiveJob.OnCallEnabled = value;
                SaveActiveJob();
                NotifyOnCallBindings();
            }
        }
        public bool ShowOnCallSettings => HasActiveJob && OnCallEnabled;

        #region Standby Pay Type
        public ObservableCollection<Option<OnCallStandbyPayType>> OnCallStandbyPayTypes { get; } =
            new ObservableCollection<Option<OnCallStandbyPayType>>();

        private Option<OnCallStandbyPayType> _selectedOnCallStandbyPayType;
        public Option<OnCallStandbyPayType> SelectedOnCallStandbyPayType
        {
            get
            {
                if (_selectedOnCallStandbyPayType != null)
                    return _selectedOnCallStandbyPayType;

                var current = ActiveJob?.OnCallStandbyPayType ?? OnCallStandbyPayType.None;
                _selectedOnCallStandbyPayType =
                    OnCallStandbyPayTypes.FirstOrDefault(x => x.Value.Equals(current))
                    ?? OnCallStandbyPayTypes.FirstOrDefault();

                return _selectedOnCallStandbyPayType;
            }
            set
            {
                if (value == null || ActiveJob == null)
                    return;

                if (SelectedOnCallStandbyPayType?.Value.Equals(value.Value) == true)
                    return;

                _selectedOnCallStandbyPayType = value;
                ActiveJob.OnCallStandbyPayType = value.Value;
                SaveActiveJob();

                NotifyOnCallBindings();
            }
        }

        public bool ShowOnCallStandbyAmount =>
            HasActiveJob && OnCallEnabled &&
            (ActiveJob?.OnCallStandbyPayType != OnCallStandbyPayType.None);

        public string OnCallStandbyAmountLabelText =>
            ActiveJob?.OnCallStandbyPayType switch
            {
                OnCallStandbyPayType.PerHour => Resources.Resx.Resources.OnCallSettings_AmountPerHour,
                OnCallStandbyPayType.PerShift => Resources.Resx.Resources.OnCallSettings_AmountPerShift,
                _ => Resources.Resx.Resources.OnCallSettings_Amount
            };

        private string _onCallStandbyAmountText;
        public string OnCallStandbyAmountText
        {
            get
            {
                if (_onCallStandbyAmountText != null)
                    return _onCallStandbyAmountText;

                _onCallStandbyAmountText =
                    (ActiveJob?.OnCallStandbyPayAmount ?? 0m)
                    .ToString("0.##", CultureInfo.CurrentCulture);

                return _onCallStandbyAmountText;
            }
            set
            {
                _onCallStandbyAmountText = value;
                OnPropertyChanged();

                if (ActiveJob == null)
                    return;

                var input = (value ?? "").Trim().Replace(",", ".");
                if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                {
                    if (ActiveJob.OnCallStandbyPayAmount != amount)
                    {
                        ActiveJob.OnCallStandbyPayAmount = amount;
                        SaveActiveJob();
                        OnPropertyChanged(nameof(OnCallSettingsSummaryText));
                    }
                }
            }
        }

        #endregion

        #region Active Pay Mode

        public ObservableCollection<Option<OnCallActivePayMode>> OnCallActivePayModes { get; } =
            new ObservableCollection<Option<OnCallActivePayMode>>();

        private Option<OnCallActivePayMode> _selectedOnCallActivePayMode;
        public Option<OnCallActivePayMode> SelectedOnCallActivePayMode
        {
            get
            {
                if (_selectedOnCallActivePayMode != null)
                    return _selectedOnCallActivePayMode;

                var current = ActiveJob?.OnCallActivePayMode ?? OnCallActivePayMode.DefaultHourly;
                _selectedOnCallActivePayMode =
                    OnCallActivePayModes.FirstOrDefault(x => x.Value.Equals(current))
                    ?? OnCallActivePayModes.FirstOrDefault();

                return _selectedOnCallActivePayMode;
            }
            set
            {
                if (value == null || ActiveJob == null)
                    return;

                if (SelectedOnCallActivePayMode?.Value.Equals(value.Value) == true)
                    return;

                _selectedOnCallActivePayMode = value;
                ActiveJob.OnCallActivePayMode = value.Value;
                SaveActiveJob();

                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowOnCallActiveCustomRate));
                OnPropertyChanged(nameof(OnCallSettingsSummaryText));
            }
        }

        public bool ShowOnCallActiveCustomRate =>
            HasActiveJob && OnCallEnabled &&
            (ActiveJob?.OnCallActivePayMode == OnCallActivePayMode.CustomHourly);

        private string _onCallActiveCustomRateText;
        public string OnCallActiveCustomRateText
        {
            get
            {
                if (_onCallActiveCustomRateText != null)
                    return _onCallActiveCustomRateText;

                _onCallActiveCustomRateText =
                    (ActiveJob?.OnCallActiveHourlyRate ?? 0m)
                    .ToString("0.##", CultureInfo.CurrentCulture);

                return _onCallActiveCustomRateText;
            }
            set
            {
                _onCallActiveCustomRateText = value;
                OnPropertyChanged();

                if (ActiveJob == null)
                    return;

                var input = (value ?? "").Trim().Replace(",", ".");
                if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                {
                    if (ActiveJob.OnCallActiveHourlyRate != amount)
                    {
                        ActiveJob.OnCallActiveHourlyRate = amount;
                        SaveActiveJob();
                        OnPropertyChanged(nameof(OnCallSettingsSummaryText));
                    }
                }
            }
        }

        #endregion

        #region Recalc Options

        public ObservableCollection<Option<int>> OnCallRecalcOptions { get; } =
            new ObservableCollection<Option<int>>();

        private Option<int> _selectedOnCallRecalcOption;
        public Option<int> SelectedOnCallRecalcOption
        {
            get
            {
                if (_selectedOnCallRecalcOption != null)
                    return _selectedOnCallRecalcOption;

                var current = ActiveJob?.OnCallRecalcMonths ?? 0;
                _selectedOnCallRecalcOption =
                    OnCallRecalcOptions.FirstOrDefault(x => x.Value == current)
                    ?? OnCallRecalcOptions.FirstOrDefault();

                return _selectedOnCallRecalcOption;
            }
            set
            {
                if (value == null || ActiveJob == null)
                    return;

                if (ActiveJob.OnCallRecalcMonths == value.Value)
                    return;

                _selectedOnCallRecalcOption = value;
                ActiveJob.OnCallRecalcMonths = value.Value;
                SaveActiveJob();

                OnPropertyChanged();
                OnPropertyChanged(nameof(CanRecalcOnCall));
                OnPropertyChanged(nameof(RecalcButtonIsEnabled));
                OnPropertyChanged(nameof(RecalcButtonText));
                OnPropertyChanged(nameof(OnCallSettingsSummaryText));
            }
        }

        private bool _isRecalcRunning;
        public bool IsRecalcRunning
        {
            get => _isRecalcRunning;
            set
            {
                _isRecalcRunning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanRecalcOnCall));
                OnPropertyChanged(nameof(RecalcButtonText));
                OnPropertyChanged(nameof(RecalcButtonIsEnabled));
            }
        }

        private string _recalcButtonOverrideText;
        public string RecalcButtonText
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_recalcButtonOverrideText))
                    return _recalcButtonOverrideText;

                if ((ActiveJob?.OnCallRecalcMonths ?? 0) <= 0)
                    return Resources.Resx.Resources.OnCallSettings_RecalcPickMonthsFirst;

                return Resources.Resx.Resources.OnCallSettings_RecalcNow;
            }
        }

        public bool RecalcButtonIsEnabled => CanRecalcOnCall && !IsRecalcRunning;
        public bool CanRecalcOnCall =>
            HasActiveJob && OnCallEnabled &&
            (ActiveJob?.OnCallRecalcMonths ?? 0) > 0;

        public ICommand RecalcOnCallCommand => new Command(async () =>
        {
            if (!CanRecalcOnCall || IsRecalcRunning)
                return;

            try
            {
                IsRecalcRunning = true;

                _recalcButtonOverrideText =
                    Resources.Resx.Resources.OnCallSettings_RecalcRunning;
                OnPropertyChanged(nameof(RecalcButtonText));

                var months = ActiveJob.OnCallRecalcMonths;
                var updated =
                    await _onCallRecalcService.RebuildOnCallSnapshotsAsync(ActiveJob.Id, months);

                _recalcButtonOverrideText =
                    string.Format(Resources.Resx.Resources.OnCallSettings_RecalcDoneWithCount, updated);
                OnPropertyChanged(nameof(RecalcButtonText));

                await Task.Delay(900);

                _recalcButtonOverrideText = null;
                OnPropertyChanged(nameof(RecalcButtonText));
            }
            finally
            {
                IsRecalcRunning = false;
            }
        });

        #endregion

        #region Summary

        public string OnCallSettingsSummaryText
        {
            get
            {
                if (ActiveJob == null || !ActiveJob.OnCallEnabled)
                    return "—";

                var currency =
                    string.IsNullOrWhiteSpace(ActiveJob.CurrencyCode)
                    ? "SEK"
                    : ActiveJob.CurrencyCode;

                string standbyText = ActiveJob.OnCallStandbyPayType switch
                {
                    OnCallStandbyPayType.None =>
                        Resources.Resx.Resources.OnCallSettings_Standby_None,

                    OnCallStandbyPayType.PerHour =>
                        $"{CurrencyHelper.FormatCurrency(ActiveJob.OnCallStandbyPayAmount, currency)}" +
                        $"{Resources.Resx.Resources.OnCallSettings_PerHourSuffix}",

                    OnCallStandbyPayType.PerShift =>
                        $"{CurrencyHelper.FormatCurrency(ActiveJob.OnCallStandbyPayAmount, currency)}" +
                        $"{Resources.Resx.Resources.OnCallSettings_PerShiftSuffix}",


                    _ => "—"
                };

                string activeText = ActiveJob.OnCallActivePayMode switch
                {
                    OnCallActivePayMode.DefaultHourly =>
                        Resources.Resx.Resources.OnCallSettings_Active_DefaultHourly,

                    OnCallActivePayMode.CustomHourly =>
                        $"{Resources.Resx.Resources.OnCallSettings_Active_CustomHourly}: " +
                        $"{CurrencyHelper.FormatCurrency(ActiveJob.OnCallActiveHourlyRate, currency)}" +
                        $"{Resources.Resx.Resources.OnCallSettings_PerHourSuffix}",

                    _ => "—"
                };

                var recalc = ActiveJob.OnCallRecalcMonths;
                var recalcText = recalc <= 0
                    ? Resources.Resx.Resources.OnCallSettings_Recalc_None
                    : string.Format(Resources.Resx.Resources.OnCallSettings_Recalc_LastXMonths, recalc);

                return $"{Resources.Resx.Resources.OnCallSettings_StandbyLabel}: {standbyText} • {activeText} • {recalcText}";
            }
        }

        #endregion

        #region Init & Notify

        private void InitOnCallOptionTexts()
        {
            OnCallStandbyPayTypes.Clear();
            OnCallStandbyPayTypes.Add(new Option<OnCallStandbyPayType>
            {
                Value = OnCallStandbyPayType.None,
                Text = Resources.Resx.Resources.OnCallPayType_None
            });
            OnCallStandbyPayTypes.Add(new Option<OnCallStandbyPayType>
            {
                Value = OnCallStandbyPayType.PerHour,
                Text = Resources.Resx.Resources.OnCallPayType_PerHour
            });
            OnCallStandbyPayTypes.Add(new Option<OnCallStandbyPayType>
            {
                Value = OnCallStandbyPayType.PerShift,
                Text = Resources.Resx.Resources.OnCallPayType_PerShift
            });

            OnCallActivePayModes.Clear();
            OnCallActivePayModes.Add(new Option<OnCallActivePayMode>
            {
                Value = OnCallActivePayMode.DefaultHourly,
                Text = Resources.Resx.Resources.OnCallActivePay_DefaultHourly
            });
            OnCallActivePayModes.Add(new Option<OnCallActivePayMode>
            {
                Value = OnCallActivePayMode.CustomHourly,
                Text = Resources.Resx.Resources.OnCallActivePay_CustomHourly
            });

            OnCallRecalcOptions.Clear();
            OnCallRecalcOptions.Add(new Option<int> { Value = 0, Text = Resources.Resx.Resources.Recalc_None });
            OnCallRecalcOptions.Add(new Option<int> { Value = 1, Text = Resources.Resx.Resources.Recalc_1Month });
            OnCallRecalcOptions.Add(new Option<int> { Value = 2, Text = Resources.Resx.Resources.Recalc_2Months });
            OnCallRecalcOptions.Add(new Option<int> { Value = 3, Text = Resources.Resx.Resources.Recalc_3Months });

            OnPropertyChanged(nameof(OnCallStandbyPayTypes));
            OnPropertyChanged(nameof(OnCallActivePayModes));
            OnPropertyChanged(nameof(OnCallRecalcOptions));

            OnPropertyChanged(nameof(SelectedOnCallStandbyPayType));
            OnPropertyChanged(nameof(SelectedOnCallActivePayMode));
            OnPropertyChanged(nameof(SelectedOnCallRecalcOption));
        }

        private void NotifyOnCallBindings()
        {
            OnPropertyChanged(nameof(OnCallEnabled));
            OnPropertyChanged(nameof(ShowOnCallSettings));
            OnPropertyChanged(nameof(OnCallSettingsSummaryText));

            OnPropertyChanged(nameof(ShowOnCallStandbyAmount));
            OnPropertyChanged(nameof(ShowOnCallActiveCustomRate));
            OnPropertyChanged(nameof(OnCallStandbyAmountLabelText));
            OnPropertyChanged(nameof(OnCallStandbyAmountText));
            OnPropertyChanged(nameof(OnCallActiveCustomRateText));
            OnPropertyChanged(nameof(SelectedOnCallStandbyPayType));
            OnPropertyChanged(nameof(SelectedOnCallActivePayMode));
            OnPropertyChanged(nameof(CanRecalcOnCall));

            OnPropertyChanged(nameof(RecalcButtonText));
            OnPropertyChanged(nameof(RecalcButtonIsEnabled));
        }

        #endregion

        #endregion
    }
}
