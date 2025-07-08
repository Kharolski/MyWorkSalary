using SQLite;

namespace MyWorkSalary.Models.Core
{
    [Table("AppSettings")]
    public class AppSettings
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // Tema-inställningar
        public bool IsDarkTheme { get; set; } = false;      // Default: ljust tema

        // Språk (framtida funktion)
        public string Language { get; set; } = "sv";        // Svenska som default

        // Metadata
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastModified { get; set; } = DateTime.Now;
    }
}
