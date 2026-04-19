using SQLite;

namespace LTO.Models
{
    [Table("Profiles")]
    public class Profile
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public int WeekdayAvailableMinutes { get; set; }
        public int WeekendAvailableMinutes { get; set; }
        public bool HolidaysAvailable { get; set; }
        public bool WeekendsAvailable { get; set; }
    }

}