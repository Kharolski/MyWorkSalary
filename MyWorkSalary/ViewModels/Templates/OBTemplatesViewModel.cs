using MyWorkSalary.Helpers.Localization;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Models.Templates;
using MyWorkSalary.Services;
using MyWorkSalary.Services.Interfaces;
using MyWorkSalary.Services.Templates;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace MyWorkSalary.ViewModels.Templates
{
    public class OBTemplatesViewModel : BaseViewModel
    {
        #region Fields
        private readonly IOBRateRepository _obRateRepository;
        private readonly IOBEventService _obEventService;
        private readonly DatabaseService _databaseService;

        private int _jobProfileId;
        private bool _isReplaceMode;
        #endregion

        #region Properties
        public ObservableCollection<OBTemplateGroup> TemplateGroups { get; } = new();
        public IEnumerable<OBRateTemplateRule> PreviewRules => SelectedTemplate?.Rules ?? Enumerable.Empty<OBRateTemplateRule>();

        private OBRateTemplate? _selectedTemplate;
        public OBRateTemplate? SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                _selectedTemplate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedTemplate));
                OnPropertyChanged(nameof(PreviewRules));

                ((Command)AddTemplateCommand).ChangeCanExecute();
                ((Command)ReplaceTemplateCommand).ChangeCanExecute();
            }
        }

        public bool HasSelectedTemplate => SelectedTemplate != null;
        public bool IsReplaceMode => _isReplaceMode;
        #endregion

        #region Commands
        public ICommand AddTemplateCommand { get; }
        public ICommand ReplaceTemplateCommand { get; }
        public ICommand SelectTemplateCommand { get; }
        #endregion

        #region Constructor
        public OBTemplatesViewModel(DatabaseService databaseService, IOBRateRepository obRateRepository, IOBEventService obEventService)
        {
            _databaseService = databaseService;
            _obRateRepository = obRateRepository;
            _obEventService = obEventService;

            AddTemplateCommand = new Command(async () => await OnAddTemplate(), () => HasSelectedTemplate);
            ReplaceTemplateCommand = new Command(async () => await OnReplaceTemplate(), () => HasSelectedTemplate);

            SelectTemplateCommand = new Command<OBRateTemplate>(t =>
            {
                if (t == null)
                    return;
                SelectedTemplate = t;

            });
        }
        #endregion

        #region Initialization
        public void Initialize(int jobProfileId, bool replaceMode)
        {
            _jobProfileId = jobProfileId;
            _isReplaceMode = replaceMode;

            OnPropertyChanged(nameof(IsReplaceMode));

            LoadTemplates();
            ApplyCurrencyToTemplates(jobProfileId);
        }
        #endregion

        #region Template logic
        private void LoadTemplates()
        {
            TemplateGroups.Clear();

            // Fullständiga mallar
            var fullTemplates = new List<OBRateTemplate>
            {
                TemplateFactory.CreateKommunalTemplate(),
                TemplateFactory.CreateVardforbundetTemplate(),
                TemplateFactory.CreateHandelsTemplate(),
                TemplateFactory.CreateHRFTemplate()
            };

            // Enkla mallar
            var simpleTemplates = new List<OBRateTemplate>
            {
                TemplateFactory.CreateEveningOnlyTemplate(),
                TemplateFactory.CreateNightOnlyTemplate(),
                TemplateFactory.CreateWeekendOnlyTemplate()
            };

            TemplateGroups.Add(new OBTemplateGroup(
                LocalizationHelper.Translate("OBTemplates_FullTemplates"),
                fullTemplates));

            TemplateGroups.Add(new OBTemplateGroup(
                LocalizationHelper.Translate("OBTemplates_SimpleTemplates"),
                simpleTemplates));
        }

        #endregion

        #region Actions
        private async Task OnAddTemplate()
        {
            if (SelectedTemplate == null)
                return;

            var obRates = BuildOBRatesFromTemplate(SelectedTemplate);
            _obRateRepository.SaveMultipleOBRates(obRates);

            await _obEventService.RebuildForJobLastMonths(_jobProfileId, 4);
            await Shell.Current.GoToAsync("..");
        }

        private async Task OnReplaceTemplate()
        {
            if (SelectedTemplate == null)
                return;

            bool confirm = await Shell.Current.DisplayAlert(
                LocalizationHelper.Translate("OBTemplates_ReplaceConfirmTitle"),
                LocalizationHelper.Translate("OBTemplates_ReplaceConfirmMessage"),
                LocalizationHelper.Translate("Common_Replace"),
                LocalizationHelper.Translate("Common_Cancel"));

            if (!confirm)
                return;

            _obRateRepository.DeleteAllOBRates(_jobProfileId);

            var obRates = BuildOBRatesFromTemplate(SelectedTemplate);
            _obRateRepository.SaveMultipleOBRates(obRates);

            await _obEventService.RebuildForJobLastMonths(_jobProfileId, 4);
            await Shell.Current.GoToAsync("..");
        }
        #endregion

        #region Currency

        private void ApplyCurrencyToTemplates(int jobProfileId)
        {
            var job = _databaseService.JobProfiles.GetJobProfile(jobProfileId);
            var currency = job?.CurrencyCode ?? "SEK";

            foreach (var group in TemplateGroups)
            {
                foreach (var template in group)
                {
                    foreach (var rule in template.Rules)
                    {
                        rule.CurrencyCode = currency;
                    }
                }
            }

            OnPropertyChanged(nameof(PreviewRules));
        }
        #endregion

        #region Helpers
        private List<OBRate> BuildOBRatesFromTemplate(OBRateTemplate template)
        {
            return template.Rules.Select(rule => new OBRate
            {
                JobProfileId = _jobProfileId,
                Name = rule.Name,
                StartTime = rule.StartTime,
                EndTime = rule.EndTime,
                RatePerHour = rule.RatePerHour,
                Priority = rule.Priority,
                Category = rule.Category,

                Monday = rule.Monday,
                Tuesday = rule.Tuesday,
                Wednesday = rule.Wednesday,
                Thursday = rule.Thursday,
                Friday = rule.Friday,
                Saturday = rule.Saturday,
                Sunday = rule.Sunday,

                Holidays = rule.Holidays,
                BigHolidays = rule.BigHolidays,

                IsActive = true
            }).ToList();
        }
        #endregion

    }
}
