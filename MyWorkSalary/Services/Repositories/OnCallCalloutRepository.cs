using MyWorkSalary.Models.Specialized;
using MyWorkSalary.Services.Interfaces;
using SQLite;

namespace MyWorkSalary.Services.Repositories
{
    public class OnCallCalloutRepository : IOnCallCalloutRepository
    {
        private readonly DatabaseService _databaseService;
        private SQLiteConnection Database => _databaseService.GetConnection();

        public OnCallCalloutRepository(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public List<OnCallCallout> GetByOnCallShiftId(int onCallShiftId)
        {
            return Database.Table<OnCallCallout>()
                           .Where(x => x.OnCallShiftId == onCallShiftId)
                           .ToList();
        }

        public int Insert(OnCallCallout callout)
        {
            return Database.Insert(callout);
        }

        public int DeleteByOnCallShiftId(int onCallShiftId)
        {
            return Database.Execute("DELETE FROM OnCallCallout WHERE OnCallShiftId = ?", onCallShiftId);
        }
    }
}
