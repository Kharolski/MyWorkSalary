using MyWorkSalary.Models.Core;

namespace MyWorkSalary.Helpers
{
    public static class ActiveJobProvider
    {
        public static JobProfile? Current { get; set; }
    }
}
