using SQLite;
using System.Diagnostics;


namespace LTO.Models
{
    [Table("Skills")]
    public class Skill
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public PriorityLevel Priority { get; set; }
        public int UrgencyLevel { get; set; }
        public DateTime LastPracticed { get; set; }
        public int MinutesInvested { get; set; }
        // Tracks accumulated missed time to be rescheduled
        public int MinutesDebt { get; set; } 
    }
}