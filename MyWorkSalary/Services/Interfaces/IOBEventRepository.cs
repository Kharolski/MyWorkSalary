using MyWorkSalary.Models.Specialized;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyWorkSalary.Services.Interfaces
{
    public interface IOBEventRepository
    {
        // Hämta alla OB-händelser för ett specifikt jobb
        List<OBEvent> GetForJob(int jobProfileId);

        // Hämta OB-händelser för en specifik period
        List<OBEvent> GetForPeriod(int jobProfileId, DateTime startDate, DateTime endDate);

        // Hämta OB-händelser för ett specifikt pass
        List<OBEvent> GetForWorkShift(int workShiftId);

        // Spara en ny OB-händelse
        int Save(OBEvent obEvent);

        // Ta bort en OB-händelse
        int Delete(OBEvent obEvent);

        // Ta bort alla OB-händelser för ett specifikt pass
        int DeleteForWorkShift(int workShiftId);
    }
}
