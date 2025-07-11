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

    public enum VABType
    {
        FullDay,     // Hela dagen VAB
        PartialDay   // Delvis VAB (jobbade en del)
    }

    public enum VacationType
    {
        PaidVacation,      // Fast anställd - betald semester
        UnpaidVacation     // Timanställd - obetald ledighet  
    }

    public enum OnCallType
    {
        StandbyOnly,        // Bara jourtid, ingen utryckning
        ActiveOnly,         // Bara aktiv arbetstid (ovanligt)
        Mixed,              // Både jour + aktiv tid (vanligast)
        Emergency           // Akut inkallning
    }
}
