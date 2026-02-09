using MyWorkSalary.Models.Specialized;

namespace MyWorkSalary.Services.Interfaces
{
    public interface IOnCallCalloutRepository
    {
        List<OnCallCallout> GetByOnCallShiftId(int onCallShiftId);
        int Insert(OnCallCallout callout);
        int DeleteByOnCallShiftId(int onCallShiftId);
    }
}
