using SQLite;
using LTO.Models;
namespace LTO.Services
{


    public static class SkillRules
    {
        // Standard session duration for optimal focus
        public static int GetIdealSessionLength(PriorityLevel p) => p switch
        {
            PriorityLevel.Core => 60,
            PriorityLevel.Builder => 40,
            PriorityLevel.Maintainer => 20,
            PriorityLevel.Dabbler => 20,
            _ => 20
        };

        // Minimum duration required for effective practice
        public static int GetSurvivalMinimum(PriorityLevel p) => p switch
        {
            PriorityLevel.Core => 20,
            PriorityLevel.Builder => 20,
            PriorityLevel.Maintainer => 10,
            PriorityLevel.Dabbler => 5,
            _ => 5
        };

        // Maximum daily duration to prevent fatigue
        public static int GetDailyMax(PriorityLevel p) => p switch
        {
            PriorityLevel.Core => 240,     // 4 hours max
            PriorityLevel.Builder => 90,   // 1.5 hours max
            PriorityLevel.Maintainer => 45, // 45 mins max
            PriorityLevel.Dabbler => 60,    // 1 hour max
            _ => 60
        };
    }
}