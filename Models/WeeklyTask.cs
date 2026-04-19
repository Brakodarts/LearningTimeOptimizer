using SQLite;

namespace LTO.Models
{
    [Table("WeeklyTasks")]
    public class WeeklyTask
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int SkillId { get; set; }
        public string SkillName { get; set; }
        public DateTime ScheduledDate { get; set; }
        public int DurationMinutes { get; set; }
        public bool IsCompleted { get; set; }
    }
}