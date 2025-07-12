using MyWorkSalary.Models.Core;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Models.Enums;
using MyWorkSalary.Services.Interfaces;

namespace MyWorkSalary.Services.Handlers
{
    public class OnCallHandler
    {
        private readonly IOnCallRepository _onCallRepository;
        private readonly IWorkShiftRepository _workShiftRepository;
        private readonly IJobProfileRepository _jobProfileRepository;

        public OnCallHandler(
            IOnCallRepository onCallRepository,
            IWorkShiftRepository workShiftRepository,
            IJobProfileRepository jobProfileRepository)
        {
            _onCallRepository = onCallRepository;
            _workShiftRepository = workShiftRepository;
            _jobProfileRepository = jobProfileRepository;
        }

        #region Create OnCall Shift

        public WorkShift CreateOnCallShift(
                int jobProfileId,
                DateTime shiftDate,
                TimeSpan standbyStartTime,
                TimeSpan standbyEndTime,
                decimal activeHours = 0,
                decimal onCallRatePerHour = 40,
                string notes = null)
        {
            var jobProfile = _jobProfileRepository.GetJobProfile(jobProfileId);
            if (jobProfile == null)
                throw new ArgumentException("JobProfile not found");

            // Beräkna standby-timmar
            var standbyHours = CalculateStandbyHours(standbyStartTime, standbyEndTime);

            // Preliminär beräkning
            var preliminaryPay = CalculatePreliminaryPay(standbyHours, activeHours, onCallRatePerHour, jobProfile.HourlyRate ?? 0);

            // Skapa WorkShift
            var workShift = new WorkShift
            {
                JobProfileId = jobProfileId,
                ShiftDate = shiftDate,
                ShiftType = ShiftType.OnCall,
                StartTime = shiftDate.Date.Add(standbyStartTime),
                EndTime = shiftDate.Date.Add(standbyEndTime),
                TotalHours = activeHours,  
                TotalPay = preliminaryPay,
                Notes = notes,
                CreatedDate = DateTime.Now
            };

            // Spara WorkShift
            var savedWorkShift = _workShiftRepository.SaveWorkShift(workShift);

            // Skapa OnCallShift (behåll all jour-info här)
            var onCallShift = new OnCallShift
            {
                WorkShiftId = savedWorkShift.Id,
                StandbyStartTime = standbyStartTime,
                StandbyEndTime = standbyEndTime,
                StandbyHours = standbyHours,        // 14h (för lön-beräkning)
                ActiveHours = activeHours,          // 2h (för lön-beräkning)
                OnCallRatePerHour = onCallRatePerHour,
                Notes = notes
            };

            // Spara OnCallShift
            _onCallRepository.Insert(onCallShift);

            return savedWorkShift;
        }

        #endregion

        #region Update OnCall Shift

        public WorkShift UpdateOnCallShift(
            int workShiftId,
            TimeSpan standbyStartTime,
            TimeSpan standbyEndTime,
            decimal activeHours,
            decimal onCallRatePerHour,
            string notes = null)
        {
            var workShift = _workShiftRepository.GetWorkShift(workShiftId);
            var onCallShift = _onCallRepository.GetByWorkShiftId(workShiftId);

            if (workShift == null || onCallShift == null)
                throw new ArgumentException("OnCall shift not found");

            var jobProfile = _jobProfileRepository.GetJobProfile(workShift.JobProfileId);

            // Beräkna nya värden
            var standbyHours = CalculateStandbyHours(standbyStartTime, standbyEndTime);
            var preliminaryPay = CalculatePreliminaryPay(standbyHours, activeHours, onCallRatePerHour, jobProfile.HourlyRate ?? 0);

            // Uppdatera WorkShift
            workShift.StartTime = workShift.ShiftDate.Date.Add(standbyStartTime);
            workShift.EndTime = workShift.ShiftDate.Date.Add(standbyEndTime);
            workShift.TotalHours = standbyHours + activeHours;
            workShift.TotalPay = preliminaryPay;
            workShift.Notes = notes;
            workShift.ModifiedDate = DateTime.Now;

            // Uppdatera OnCallShift
            onCallShift.StandbyStartTime = standbyStartTime;
            onCallShift.StandbyEndTime = standbyEndTime;
            onCallShift.StandbyHours = standbyHours;
            onCallShift.ActiveHours = activeHours;
            onCallShift.OnCallRatePerHour = onCallRatePerHour;
            onCallShift.Notes = notes;

            // Spara ändringar
            _workShiftRepository.SaveWorkShift(workShift);
            _onCallRepository.Update(onCallShift);

            return workShift;
        }

        #endregion

        #region Get OnCall Data

        public OnCallShift GetOnCallDetails(int workShiftId)
        {
            return _onCallRepository.GetByWorkShiftId(workShiftId);
        }

        public List<OnCallShift> GetOnCallShiftsForJobProfile(int jobProfileId)
        {
            return _onCallRepository.GetByJobProfileId(jobProfileId);
        }

        #endregion

        #region Calculations

        private decimal CalculateStandbyHours(TimeSpan startTime, TimeSpan endTime)
        {
            // Hantera över midnatt (t.ex. 18:00-08:00)
            if (endTime <= startTime)
            {
                endTime = endTime.Add(TimeSpan.FromDays(1));
            }

            var duration = endTime - startTime;
            return (decimal)duration.TotalHours;
        }

        private decimal CalculatePreliminaryPay(decimal standbyHours, decimal activeHours, decimal onCallRate, decimal hourlyRate)
        {
            var standbyPay = standbyHours * onCallRate;
            var activePay = activeHours * hourlyRate;

            return standbyPay + activePay;
        }

        #endregion

        #region Delete

        public bool DeleteOnCallShift(int workShiftId)
        {
            try
            {
                _onCallRepository.DeleteByWorkShiftId(workShiftId);
                _workShiftRepository.DeleteWorkShift(workShiftId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
