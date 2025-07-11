using MyWorkSalary.Models.Specialized;

namespace MyWorkSalary.Services.Interfaces
{
    public interface IOnCallRepository
    {
        OnCallShift GetById(int id);
        OnCallShift GetByWorkShiftId(int workShiftId);
        List<OnCallShift> GetByJobProfileId(int jobProfileId);
        List<OnCallShift> GetAll();
        int Insert(OnCallShift onCallShift);
        int Update(OnCallShift onCallShift);
        int Delete(int id);
        int DeleteByWorkShiftId(int workShiftId);
    }
}
