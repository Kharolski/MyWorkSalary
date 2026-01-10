using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Services.Interfaces;
using SQLite;

namespace MyWorkSalary.Services.Repositories
{
    public class OBEventRepository : IOBEventRepository
    {
        private readonly SQLiteConnection _database;
        public OBEventRepository(DatabaseService databaseService)
        {
            _database = databaseService.GetConnection();
        }
        public List<OBEvent> GetForJob(int jobProfileId)
        {
            return _database.Table<OBEvent>()
                          .Where(e => e.JobProfileId == jobProfileId)
                          .OrderByDescending(e => e.WorkDate)
                          .ThenBy(e => e.StartTime)
                          .ToList();
        }
        public List<OBEvent> GetForPeriod(int jobProfileId, DateTime startDate, DateTime endDate)
        {
            return _database.Table<OBEvent>()
                          .Where(e => e.JobProfileId == jobProfileId &&
                                     e.WorkDate >= startDate.Date &&
                                     e.WorkDate <= endDate.Date)
                          .OrderBy(e => e.WorkDate)
                          .ThenBy(e => e.StartTime)
                          .ToList();
        }
        public List<OBEvent> GetForWorkShift(int workShiftId)
        {
            return _database.Table<OBEvent>()
                          .Where(e => e.WorkShiftId == workShiftId)
                          .OrderBy(e => e.StartTime)
                          .ToList();
        }
        public int Save(OBEvent obEvent)
        {
            if (obEvent.Id != 0)
            {
                return _database.Update(obEvent);
            }
            else
            {
                return _database.Insert(obEvent);
            }
        }
        public int Delete(OBEvent obEvent)
        {
            return _database.Delete(obEvent);
        }
        public int DeleteForWorkShift(int workShiftId)
        {
            var events = GetForWorkShift(workShiftId);
            int count = 0;

            foreach (var obEvent in events)
            {
                count += Delete(obEvent);
            }

            return count;
        }
    }
}
