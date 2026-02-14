using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Services.Interfaces;

namespace MyWorkSalary.Services.Calculations
{
    public class OnCallRecalcService : IOnCallRecalcService
    {
        private readonly IJobProfileRepository _jobRepo;
        private readonly IOnCallRepository _onCallRepo;

        public OnCallRecalcService(IJobProfileRepository jobRepo, IOnCallRepository onCallRepo)
        {
            _jobRepo = jobRepo ?? throw new ArgumentNullException(nameof(jobRepo));
            _onCallRepo = onCallRepo ?? throw new ArgumentNullException(nameof(onCallRepo));
        }

        public Task<int> RebuildOnCallSnapshotsAsync(int jobProfileId, int monthsBack)
        {
            if (monthsBack <= 0)
                return Task.FromResult(0);

            var profile = _jobRepo.GetJobProfile(jobProfileId);
            if (profile == null)
                return Task.FromResult(0);

            // Intervall: senaste X månader inkl nuvarande månad
            var from = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-monthsBack + 1);
            var to = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1); // exklusiv

            var shifts = _onCallRepo.GetForJobInDateRange(jobProfileId, from, to) ?? new List<OnCallShift>();

            var updated = 0;

            foreach (var ocs in shifts)
            {
                // Snapshots (standby)
                ocs.StandbyPayTypeSnapshot = profile.OnCallStandbyPayType;
                ocs.StandbyPayAmountSnapshot = profile.OnCallStandbyPayAmount;

                // Snapshots (active)
                ocs.ActivePayModeSnapshot = profile.OnCallActivePayMode;
                ocs.ActiveHourlyRateSnapshot = ResolveActiveHourlyRateSnapshot(profile);

                _onCallRepo.Update(ocs);
                updated++;
            }

            return Task.FromResult(updated);
        }

        private decimal ResolveActiveHourlyRateSnapshot(JobProfile profile)
        {
            // Om CustomHourly -> använd den
            if (profile.OnCallActivePayMode == OnCallActivePayMode.CustomHourly)
                return profile.OnCallActiveHourlyRate;

            // DefaultHourly -> beror på anställning
            if (profile.EmploymentType == EmploymentType.Temporary)
                return profile.HourlyRate ?? 0m;

            // Permanent -> månadslön / månadstimmar
            if ((profile.MonthlySalary ?? 0m) > 0m)
            {
                var monthlyHours = profile.ExpectedHoursPerMonth > 0 ? profile.ExpectedHoursPerMonth : 173.33m;
                return (profile.MonthlySalary ?? 0m) / monthlyHours;
            }

            return 0m;
        }
    }
}
