namespace MyWorkSalary.Services.Interfaces
{
    public interface IOnCallRecalcService
    {
        Task<int> RebuildOnCallSnapshotsAsync(int jobProfileId, int monthsBack);
    }
}
