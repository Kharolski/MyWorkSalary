using SQLite;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Services.Interfaces;

namespace MyWorkSalary.Services.Repositories
{
    public class OnCallShiftRepository : IOnCallRepository
    {
        private readonly DatabaseService _databaseService;
        private readonly IOnCallCalloutRepository _calloutRepo;
        private SQLiteConnection Database => _databaseService.GetConnection();

        public OnCallShiftRepository(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _calloutRepo = databaseService.OnCallCallouts;
        }

        public OnCallShift GetById(int id)
        {
            return Database.Table<OnCallShift>()
                          .FirstOrDefault(x => x.Id == id);
        }
        public OnCallShift GetByWorkShiftId(int workShiftId)
        {
            return Database.Table<OnCallShift>()
                          .FirstOrDefault(x => x.WorkShiftId == workShiftId);
        }
        public List<OnCallShift> GetByJobProfileId(int jobProfileId)
        {
            // Join med WorkShift för att filtrera på JobProfileId
            return Database.Query<OnCallShift>(@"
                SELECT ocs.* 
                FROM OnCallShift ocs
                INNER JOIN WorkShift ws ON ocs.WorkShiftId = ws.Id
                WHERE ws.JobProfileId = ?", jobProfileId);
        }
        public List<OnCallShift> GetForJobInDateRange(int jobProfileId, DateTime from, DateTime to)
        {
            // filtrerar på WorkShift.ShiftDate (datumet passet hör till)
            // to = exklusiv gräns
            return Database.Query<OnCallShift>(@"
                SELECT ocs.*
                FROM OnCallShift ocs
                INNER JOIN WorkShift ws ON ocs.WorkShiftId = ws.Id
                WHERE ws.JobProfileId = ?
                  AND ws.ShiftType = ?
                  AND ws.ShiftDate >= ?
                  AND ws.ShiftDate < ?
                ORDER BY ws.ShiftDate ASC
            ",
                    jobProfileId,
                    (int)Models.Enums.ShiftType.OnCall,
                    from.Date,
                    to.Date);
        }

        public List<OnCallShift> GetAll()
        {
            return Database.Table<OnCallShift>().ToList();
        }
        public int Insert(OnCallShift onCallShift)
        {
            return Database.Insert(onCallShift);
        }
        public int Update(OnCallShift onCallShift)
        {
            return Database.Update(onCallShift);
        }

        public int Delete(int id)
        {
            return Database.Delete<OnCallShift>(id);
        }
        public int DeleteByWorkShiftId(int workShiftId)
        {
            return Database.Execute("DELETE FROM OnCallShift WHERE WorkShiftId = ?", workShiftId);
        }
        public int DeleteShiftCascade(int workShiftId)
        {
            var onCall = GetByWorkShiftId(workShiftId);
            if (onCall == null)
                return 0;

            _calloutRepo.DeleteByOnCallShiftId(onCall.Id);
            return Delete(onCall.Id);
        }
    }
}
