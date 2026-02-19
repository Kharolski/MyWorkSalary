using MyWorkSalary.Models.Enums;

namespace MyWorkSalary.Models.Reports
{
    public class OnCallDetail
    {
        public DateTime Date { get; set; }

        public DateTime StandbyStart { get; set; }
        public DateTime StandbyEnd { get; set; }
        public decimal StandbyHours { get; set; }
        public decimal ActiveHours { get; set; }

        public List<OnCallCalloutDetail> Callouts { get; set; } = new();

        // snapshot från OnCallShift
        public OnCallStandbyPayType StandbyPayType { get; set; }
        public decimal StandbyPayAmount { get; set; }  // kr/tim eller kr/pass

        public decimal StandbyPay { get; set; }        // uträknat belopp (det som summeras)
        public string? ShiftNote { get; set; }         // “telefonjour”
    }
}
