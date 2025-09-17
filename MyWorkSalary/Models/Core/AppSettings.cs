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
        
        // Språk
        public string LanguageCode { get; set; } = "";  // default tom

        // Metadata
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastModified { get; set; } = DateTime.Now;

        
    }
}
