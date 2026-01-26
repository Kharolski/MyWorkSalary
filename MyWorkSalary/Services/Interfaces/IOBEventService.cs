using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Specialized;

namespace MyWorkSalary.Services.Interfaces
{
    public interface IOBEventService
    {
        ObSummary RebuildForWorkShift(WorkShift shift);
        ObSummary RebuildForWorkShift(WorkShift shift, IReadOnlyList<OBRate> obRates);

        // Rebuild för många pass i en period
        Task RebuildForJobLastMonths(int jobProfileId, int monthsBack = 4);
    }

    public sealed class ObSummary
    {
        public decimal TotalObHours { get; set; }
        public decimal TotalObPay { get; set; }

        public decimal EveningHours { get; set; }
        public decimal NightHours { get; set; }

        public decimal EveningPay { get; set; }
        public decimal NightPay { get; set; }

        // “indikator” (om flera events/rates i samma kategori)
        public decimal EveningRate { get; set; }
        public decimal NightRate { get; set; }
    }
}
