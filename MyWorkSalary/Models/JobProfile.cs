//using System.ComponentModel;
//using System.ComponentModel.DataAnnotations;
//using System.Runtime.CompilerServices;
//using MyWorkSalary.Models.Enums;
//using SQLite;

//namespace MyWorkSalary.Models
//{
//    public class JobProfile : INotifyPropertyChanged
//    {
//        #region Database Properties

//        [PrimaryKey, AutoIncrement]
//        public int Id { get; set; }

//        [Required]
//        public string JobTitle { get; set; } = string.Empty;

//        [Required]
//        public string Workplace { get; set; } = string.Empty;

//        public EmploymentType EmploymentType { get; set; }

//        #endregion

//        #region Salary Properties

//        public decimal? MonthlySalary { get; set; }
//        public decimal? HourlyRate { get; set; }
//        public decimal ExpectedHoursPerMonth { get; set; }
//        public string SalaryDisplayText
//        {
//            get
//            {
//                if (HourlyRate.HasValue)
//                {
//                    var employmentText = EmploymentType switch
//                    {
//                        EmploymentType.Permanent => "Tillsvidare",
//                        EmploymentType.Temporary => "Visstid",
//                        EmploymentType.OnCall => "Timanställd",
//                        _ => "Anställd"
//                    };
//                    return $"{HourlyRate:F0} kr/tim • {employmentText}";
//                }
//                else if (MonthlySalary.HasValue)
//                {
//                    return $"{MonthlySalary:N0} kr/mån • Månadslön";
//                }
//                return "Lön ej angiven";
//            }
//        }
//        #endregion

//        #region Pay Period Properties

//        public PayPeriodType PayPeriodType { get; set; } = PayPeriodType.CalendarMonth;
//        public int PayPeriodStartDay { get; set; } = 25;

//        #endregion

//        #region Tax Properties

//        public TaxCalculationMethod TaxMethod { get; set; } = TaxCalculationMethod.Manual;
//        public decimal ManualTaxRate { get; set; } = 0.33m;
//        public decimal? LastMonthGrossPay { get; set; }
//        public decimal? LastMonthTaxDeduction { get; set; }

//        [Ignore]
//        public decimal EffectiveTaxRate => TaxMethod switch
//        {
//            TaxCalculationMethod.FromPayslip when LastMonthGrossPay > 0 && LastMonthTaxDeduction > 0
//                => (decimal)(LastMonthTaxDeduction / LastMonthGrossPay),
//            _ => ManualTaxRate
//        };

//        #endregion

//        #region Ignored Properties

//        [Ignore]
//        public List<OBRate> OBRates { get; set; } = new();

//        [Ignore]
//        public bool IsHourlyEmployee => HourlyRate.HasValue && !MonthlySalary.HasValue;

//        [Ignore]
//        public bool IsSalariedEmployee => MonthlySalary.HasValue;
//        #endregion

//        #region Metadata Properties

//        public DateTime CreatedDate { get; set; } = DateTime.Now;
//        public DateTime? ModifiedDate { get; set; }

//        // IsActive med INotifyPropertyChanged
//        private bool _isActive = true;
//        public bool IsActive
//        {
//            get => _isActive;
//            set
//            {
//                if (_isActive != value)
//                {
//                    _isActive = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        #endregion

//        #region INotifyPropertyChanged Implementation

//        public event PropertyChangedEventHandler PropertyChanged;

//        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }

//        #endregion
//    }

//}
