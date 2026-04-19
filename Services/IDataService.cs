using System.Collections.Generic;
using LTO.Models;

namespace LTO.Services
{
    public interface IDataService
    {
        // Skill Operations
        List<Skill> GetAllSkills();
        Skill GetSkillById(int id);
        (bool Success, string Message) AddSkill(Skill skill);
        bool DeleteSkill(int id);
        int GetTotalSkills();

        // Profile Operations
        Profile GetProfile();
        void UpdateProfile(Profile profile);

        // Task Operations
        List<WeeklyTask> GetTodayTasks();
        List<WeeklyTask> GetFutureTasks();
        void LogTaskCompletion(int taskId, bool isCompleted);
        
        // Plan Operations
        void RegeneratePlan();
    }
}
