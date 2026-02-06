using MyWorkSalary.Models.Enums;
using MyWorkSalary.Models.Templates;
using MyWorkSalary.Helpers.Localization;

namespace MyWorkSalary.Services.Templates
{
    public static class TemplateFactory
    {
        #region Standard Municipality
        public static OBRateTemplate CreateStandardMunicipalityTemplate()
        {
            return new OBRateTemplate
            {
                Title = LocalizationHelper.Translate("OBTemplate_Standard_Title"),
                Description = LocalizationHelper.Translate("OBTemplate_Standard_Desc"),
                Rules = new()
                {
                    new OBRateTemplateRule
                    {
                        Name = LocalizationHelper.Translate("OBRule_WeekdayEvening_Name"),
                        StartTime = new TimeSpan(19,0,0),
                        EndTime = new TimeSpan(21,0,0),
                        RatePerHour = 25.60m,
                        Priority = 5,
                        Monday = true, Tuesday = true, Wednesday = true,
                        Thursday = true, Friday = true,
                        Category = OBCategory.Evening
                    },
                    new OBRateTemplateRule
                    {
                        Name = LocalizationHelper.Translate("OBRule_WeekdayNight_Name"),
                        StartTime = new TimeSpan(21,0,0),
                        EndTime = new TimeSpan(6,0,0),
                        RatePerHour = 56.70m,
                        Priority = 10,
                        Monday = true, Tuesday = true, Wednesday = true,
                        Thursday = true, Friday = true,
                        Category = OBCategory.Night
                    },

                    new OBRateTemplateRule
                    {
                        Name = LocalizationHelper.Translate("OBRule_Weekend_Name"),
                        StartTime = new TimeSpan(6,0,0),
                        EndTime = new TimeSpan(21,0,0),
                        RatePerHour = 66.10m,
                        Priority = 20,
                        Saturday = true,
                        Category = OBCategory.Day
                    },

                    new OBRateTemplateRule
                    {
                        Name = LocalizationHelper.Translate("OBRule_WeekendExtra_Name"),
                        StartTime = new TimeSpan(6,0,0),
                        EndTime = new TimeSpan(21,0,0),
                        RatePerHour = 76.00m,
                        Priority = 30,
                        Sunday = true,
                        Category = OBCategory.Day
                    },

                    new OBRateTemplateRule
                    {
                        Name = LocalizationHelper.Translate("OBRule_BigHoliday_Name"),
                        StartTime = TimeSpan.Zero,
                        EndTime = TimeSpan.FromHours(24),
                        RatePerHour = 126.90m,
                        Priority = 40,
                        BigHolidays = true,
                        Category = OBCategory.Day
                    }
                }
            };
        }
        #endregion

        #region Simple Evening
        public static OBRateTemplate CreateEveningOnlyTemplate()
        {
            return new OBRateTemplate
            {
                Title = LocalizationHelper.Translate("OBTemplate_EveningOnly_Title"),
                Description = LocalizationHelper.Translate("OBTemplate_EveningOnly_Desc"),
                Rules = new()
                {
                    new OBRateTemplateRule
                    {
                        Name = LocalizationHelper.Translate("OBRule_WeekdayEvening_Name"),
                        StartTime = new TimeSpan(19,0,0),
                        EndTime = new TimeSpan(21,0,0),
                        RatePerHour = 25.60m,
                        Priority = 5,
                        Monday = true, Tuesday = true, Wednesday = true, Thursday = true, Friday = true,
                        Category = OBCategory.Evening
                    }
                }
            };
        }
        #endregion

        #region Simple Night
        public static OBRateTemplate CreateNightOnlyTemplate()
        {
            return new OBRateTemplate
            {
                Title = LocalizationHelper.Translate("OBTemplate_NightOnly_Title"),
                Description = LocalizationHelper.Translate("OBTemplate_NightOnly_Desc"),
                Rules = new()
                {
                    new OBRateTemplateRule
                    {
                        Name = LocalizationHelper.Translate("OBRule_WeekdayNight_Name"),
                        StartTime = new TimeSpan(21,0,0),
                        EndTime = new TimeSpan(6,0,0),
                        RatePerHour = 56.70m,
                        Priority = 10,
                        Monday = true, Tuesday = true, Wednesday = true, Thursday = true, Friday = true,
                        Category = OBCategory.Night
                    }
                }
            };
        }
        #endregion

        #region Simple Weekend
        public static OBRateTemplate CreateWeekendOnlyTemplate()
        {
            return new OBRateTemplate
            {
                Title = LocalizationHelper.Translate("OBTemplate_WeekendOnly_Title"),
                Description = LocalizationHelper.Translate("OBTemplate_WeekendOnly_Desc"),
                Rules = new()
                {
                    new OBRateTemplateRule
                    {
                        Name = LocalizationHelper.Translate("OBRule_WeekendOB_Name"),
                        StartTime = TimeSpan.Zero,
                        EndTime = TimeSpan.FromHours(24),
                        RatePerHour = 60m,
                        Priority = 10,
                        Saturday = true,
                        Sunday = true,
                        Category = OBCategory.Day
                    }
                }
            };
        }
        #endregion
    }
}
