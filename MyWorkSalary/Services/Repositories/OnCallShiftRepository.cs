using SQLite;
using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Services.Interfaces;

namespace MyWorkSalary.Services.Repositories
{
    public class OnCallShiftRepository : IOnCallRepository
    {
        private readonly DatabaseService _databaseService;
        private SQLiteConnection Database => _databaseService.GetConnection();

        public OnCallShiftRepository(DatabaseService databaseService)
        {
            _databaseService = databaseService;
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
    }
}
