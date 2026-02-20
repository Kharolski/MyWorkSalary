using MyWorkSalary.Models.Enums;
using MyWorkSalary.Models.Templates;
using MyWorkSalary.Helpers.Localization;

namespace MyWorkSalary.Services.Templates
{
    public static class TemplateFactory
    {
        #region Standard Municipality
        public static OBRateTemplate CreateKommunalTemplate()
        {
            return new OBRateTemplate
            {
                Title = LocalizationHelper.Translate("OBTemplate_Standard_Title"),
                Description = LocalizationHelper.Translate("OBTemplate_Standard_Desc"),
                Rules = new()
        {
            // Kväll (18–22, vardagar)
            new OBRateTemplateRule
            {
                Name = LocalizationHelper.Translate("OBRule_WeekdayEvening_Name"),
                StartTime = new TimeSpan(18, 0, 0),
                EndTime = new TimeSpan(22, 0, 0),
                RatePerHour = 25.00m, // exempelvärde
                Priority = 5,
                Monday = true, Tuesday = true, Wednesday = true,
                Thursday = true, Friday = true,
                Category = OBCategory.Evening
            },

            // Vardagsnatt (22–06, mån–fre)
            new OBRateTemplateRule
            {
                Name = LocalizationHelper.Translate("OBRule_WeekdayNight_Name"),
                StartTime = new TimeSpan(22, 0, 0),
                EndTime = new TimeSpan(6, 0, 0),
                RatePerHour = 45.00m, // lägre natt-OB
                Priority = 10,
                Monday = true, Tuesday = true, Wednesday = true,
                Thursday = true, Friday = true,
                Category = OBCategory.Night
            },

            // Helgnatt (22–06, fre–lör–sön)
            new OBRateTemplateRule
            {
                Name = LocalizationHelper.Translate("OBRule_WeekendNight_Name"),
                StartTime = new TimeSpan(22, 0, 0),
                EndTime = new TimeSpan(6, 0, 0),
                RatePerHour = 60.00m, // högre helg-natt-OB
                Priority = 15,
                Friday = true, Saturday = true, Sunday = true,
                Category = OBCategory.Night
            },

            // Lördag dag/kväll
            new OBRateTemplateRule
            {
                Name = LocalizationHelper.Translate("OBRule_WeekendDay_Name"),
                StartTime = TimeSpan.Zero,
                EndTime = TimeSpan.FromHours(24),
                RatePerHour = 55.00m, // exempelvärde
                Priority = 20,
                Saturday = true,
                Category = OBCategory.Day
            },

            // Söndag dag/kväll
            new OBRateTemplateRule
            {
                Name = LocalizationHelper.Translate("OBRule_WeekendDayExtra_Name"),
                StartTime = TimeSpan.Zero,
                EndTime = TimeSpan.FromHours(24),
                RatePerHour = 60.00m, // exempelvärde
                Priority = 25,
                Sunday = true,
                Category = OBCategory.Day
            },

            // Helgdag (röda dagar)
            new OBRateTemplateRule
            {
                Name = LocalizationHelper.Translate("OBRule_Holiday_Name"),
                StartTime = TimeSpan.Zero,
                EndTime = TimeSpan.FromHours(24),
                RatePerHour = 70.00m, // exempelvärde
                Priority = 30,
                Holidays = true,
                Category = OBCategory.Day
            },

            // Storhelg
            new OBRateTemplateRule
            {
                Name = LocalizationHelper.Translate("OBRule_BigHoliday_Name"),
                StartTime = TimeSpan.Zero,
                EndTime = TimeSpan.FromHours(24),
                RatePerHour = 100.00m, // exempelvärde
                Priority = 40,
                BigHolidays = true,
                Category = OBCategory.Day
            }
        }
            };
        }
        #endregion

        #region Nurses Union
        public static OBRateTemplate CreateVardforbundetTemplate()
        {
            return new OBRateTemplate
            {
                Title = LocalizationHelper.Translate("OBTemplate_VF_Title"),
                Description = LocalizationHelper.Translate("OBTemplate_VF_Desc"),
                Rules = new()
        {
            // Kväll (18–22, vardagar)
            new OBRateTemplateRule
            {
                Name = LocalizationHelper.Translate("OBRule_VF_Evening_Name"),
                StartTime = new TimeSpan(18, 0, 0),
                EndTime = new TimeSpan(22, 0, 0),
                RatePerHour = 30.00m, // exempelvärde
                Priority = 5,
                Monday = true, Tuesday = true, Wednesday = true,
                Thursday = true, Friday = true,
                Category = OBCategory.Evening
            },

            // Vardagsnatt (22–06, mån–fre)
            new OBRateTemplateRule
            {
                Name = LocalizationHelper.Translate("OBRule_VF_Night_Name"),
                StartTime = new TimeSpan(22, 0, 0),
                EndTime = new TimeSpan(6, 0, 0),
                RatePerHour = 60.00m, // lägre natt-OB än helg
                Priority = 10,
                Monday = true, Tuesday = true, Wednesday = true,
                Thursday = true, Friday = true,
                Category = OBCategory.Night
            },

            // Helgnatt (22–06, fre–lör–sön)
            new OBRateTemplateRule
            {
                Name = LocalizationHelper.Translate("OBRule_VF_WeekendNight_Name"),
                StartTime = new TimeSpan(22, 0, 0),
                EndTime = new TimeSpan(6, 0, 0),
                RatePerHour = 75.00m, // högre helg-natt-OB
                Priority = 15,
                Friday = true, Saturday = true, Sunday = true,
                Category = OBCategory.Night
            },

            // Helg (lör–sön, dag/kväll)
            new OBRateTemplateRule
            {
                Name = LocalizationHelper.Translate("OBRule_VF_Weekend_Name"),
                StartTime = TimeSpan.Zero,
                EndTime = TimeSpan.FromHours(24),
                RatePerHour = 70.00m, // exempelvärde
                Priority = 20,
                Saturday = true,
                Sunday = true,
                Category = OBCategory.Day
            },

            // Helgdag (röda dagar, ej storhelg)
            new OBRateTemplateRule
            {
                Name = LocalizationHelper.Translate("OBRule_VF_Holiday_Name"),
                StartTime = TimeSpan.Zero,
                EndTime = TimeSpan.FromHours(24),
                RatePerHour = 85.00m, // exempelvärde
                Priority = 30,
                Holidays = true,
                Category = OBCategory.Day
            },

            // Storhelg
            new OBRateTemplateRule
            {
                Name = LocalizationHelper.Translate("OBRule_VF_BigHoliday_Name"),
                StartTime = TimeSpan.Zero,
                EndTime = TimeSpan.FromHours(24),
                RatePerHour = 120.00m, // exempelvärde
                Priority = 40,
                BigHolidays = true,
                Category = OBCategory.Day
            }
        }
            };
        }
        #endregion

        #region Handels
        public static OBRateTemplate CreateHandelsTemplate()
        {
            return new OBRateTemplate
            {
                Title = LocalizationHelper.Translate("OBTemplate_Handels_Title"),
                Description = LocalizationHelper.Translate("OBTemplate_Handels_Desc"),
                Rules = new()
        {
            // Kväll (18–22, vardagar)
            new OBRateTemplateRule
            {
                Name = LocalizationHelper.Translate("OBRule_Handels_Evening_Name"),
                StartTime = new TimeSpan(18, 0, 0),
                EndTime = new TimeSpan(22, 0, 0),
                RatePerHour = 25.00m, // exempelvärde
                Priority = 5,
                Monday = true, Tuesday = true, Wednesday = true,
                Thursday = true, Friday = true,
                Category = OBCategory.Evening
            },

            // Vardagsnatt (22–06, mån–fre)
            new OBRateTemplateRule
            {
                Name = LocalizationHelper.Translate("OBRule_Handels_Night_Name"),
                StartTime = new TimeSpan(22, 0, 0),
                EndTime = new TimeSpan(6, 0, 0),
                RatePerHour = 45.00m, // lägre natt-OB
                Priority = 10,
                Monday = true, Tuesday = true, Wednesday = true,
                Thursday = true, Friday = true,
                Category = OBCategory.Night
            },

            // Helgnatt (22–06, fre–lör–sön)
            new OBRateTemplateRule
            {
                Name = LocalizationHelper.Translate("OBRule_Handels_WeekendNight_Name"),
                StartTime = new TimeSpan(22, 0, 0),
                EndTime = new TimeSpan(6, 0, 0),
                RatePerHour = 60.00m, // högre helg-natt-OB
                Priority = 15,
                Friday = true, Saturday = true, Sunday = true,
                Category = OBCategory.Night
            },

            // Helg (lör–sön, dag/kväll)
            new OBRateTemplateRule
            {
                Name = LocalizationHelper.Translate("OBRule_Handels_Weekend_Name"),
                StartTime = TimeSpan.Zero,
                EndTime = TimeSpan.FromHours(24),
                RatePerHour = 55.00m, // exempelvärde
                Priority = 20,
                Saturday = true,
                Sunday = true,
                Category = OBCategory.Day
            },

            // Helgdag (röda dagar, ej storhelg)
            new OBRateTemplateRule
            {
                Name = LocalizationHelper.Translate("OBRule_Handels_Holiday_Name"),
                StartTime = TimeSpan.Zero,
                EndTime = TimeSpan.FromHours(24),
                RatePerHour = 70.00m, // exempelvärde
                Priority = 30,
                Holidays = true,
                Category = OBCategory.Day
            },

            // Storhelg
            new OBRateTemplateRule
            {
                Name = LocalizationHelper.Translate("OBRule_Handels_BigHoliday_Name"),
                StartTime = TimeSpan.Zero,
                EndTime = TimeSpan.FromHours(24),
                RatePerHour = 100.00m, // exempelvärde
                Priority = 40,
                BigHolidays = true,
                Category = OBCategory.Day
            }
        }
            };
        }
        #endregion

        #region HRF
        public static OBRateTemplate CreateHRFTemplate()
        {
            return new OBRateTemplate
            {
                Title = LocalizationHelper.Translate("OBTemplate_HRF_Title"),
                Description = LocalizationHelper.Translate("OBTemplate_HRF_Desc"),
                Rules = new()
        {
            // Kväll (18–22, vardagar)
            new OBRateTemplateRule
            {
                Name = LocalizationHelper.Translate("OBRule_HRF_Evening_Name"),
                StartTime = new TimeSpan(18, 0, 0),
                EndTime = new TimeSpan(22, 0, 0),
                RatePerHour = 25.00m, // exempelvärde
                Priority = 5,
                Monday = true, Tuesday = true, Wednesday = true,
                Thursday = true, Friday = true,
                Category = OBCategory.Evening
            },

            // Vardagsnatt (22–06, mån–fre)
            new OBRateTemplateRule
            {
                Name = LocalizationHelper.Translate("OBRule_HRF_Night_Name"),
                StartTime = new TimeSpan(22, 0, 0),
                EndTime = new TimeSpan(6, 0, 0),
                RatePerHour = 45.00m, // lägre natt-OB
                Priority = 10,
                Monday = true, Tuesday = true, Wednesday = true,
                Thursday = true, Friday = true,
                Category = OBCategory.Night
            },

            // Helgnatt (22–06, fre–lör–sön)
            new OBRateTemplateRule
            {
                Name = LocalizationHelper.Translate("OBRule_HRF_WeekendNight_Name"),
                StartTime = new TimeSpan(22, 0, 0),
                EndTime = new TimeSpan(6, 0, 0),
                RatePerHour = 60.00m, // högre helg-natt-OB
                Priority = 15,
                Friday = true, Saturday = true, Sunday = true,
                Category = OBCategory.Night
            },

            // Helg (lör–sön, dag/kväll)
            new OBRateTemplateRule
            {
                Name = LocalizationHelper.Translate("OBRule_HRF_Weekend_Name"),
                StartTime = TimeSpan.Zero,
                EndTime = TimeSpan.FromHours(24),
                RatePerHour = 55.00m, // exempelvärde
                Priority = 20,
                Saturday = true,
                Sunday = true,
                Category = OBCategory.Day
            },

            // Helgdag (röda dagar)
            new OBRateTemplateRule
            {
                Name = LocalizationHelper.Translate("OBRule_HRF_Holiday_Name"),
                StartTime = TimeSpan.Zero,
                EndTime = TimeSpan.FromHours(24),
                RatePerHour = 70.00m, // exempelvärde
                Priority = 30,
                Holidays = true,
                Category = OBCategory.Day
            },

            // Storhelg
            new OBRateTemplateRule
            {
                Name = LocalizationHelper.Translate("OBRule_HRF_BigHoliday_Name"),
                StartTime = TimeSpan.Zero,
                EndTime = TimeSpan.FromHours(24),
                RatePerHour = 100.00m, // exempelvärde
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
