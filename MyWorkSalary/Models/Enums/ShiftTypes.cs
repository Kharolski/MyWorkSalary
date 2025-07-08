namespace MyWorkSalary.Models.Enums
{
    public enum ShiftType
    {
        Regular,        // Vanligt arbetspass (inkl. övertid/OB baserat på tid)
        OnCall,         // Jour - speciella regler 
        SickLeave,      // Sjukskrivning
        Vacation,       // Semester
        VAB             // Vård av barn
    }

    public enum SickLeaveType
    {
        WorkedPartially,    // Jobbat delvis
        ShouldHaveWorked,   // Skulle jobbat
        WouldBeFree         // Skulle varit ledig
    }

}
