using MyWorkSalary.Models.Core;

namespace MyWorkSalary.Services.Interfaces
{
    public interface IJobProfileRepository
    {
        // Basic CRUD
        List<JobProfile> GetJobProfiles();
        JobProfile GetJobProfile(int id);
        JobProfile GetActiveJob();
        void SaveJobProfile(JobProfile jobProfile);
        int DeleteJobProfile(int id);

        // Business logic queries
        bool HasActiveJob();
        void SetActiveJob(int jobProfileId);
        void DeactivateAllJobs();

        // Validation
        bool IsJobNameUnique(string jobName, int excludeId = 0);
        bool CanDeleteJob(int jobProfileId);

        // Statistics
        int GetTotalJobsCount();
        List<JobProfile> GetRecentJobs(int count = 5);
    }
}
