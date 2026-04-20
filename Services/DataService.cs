using System;
using System.Collections.Generic;
using System.Linq;
using LTO.Models;
using LTO.Data;

namespace LTO.Services
{
    public class DataService : IDataService
    {
        public List<Skill> GetAllSkills()
        {
            return DatabaseManager.Connection.Table<Skill>().ToList();
        }

        public Skill GetSkillById(int id)
        {
            return DatabaseManager.Connection.Table<Skill>().FirstOrDefault(s => s.Id == id);
        }

        public (bool Success, string Message) AddSkill(Skill skill)
        {
            // --- Validation Logic (Moved from UI) ---
            if (skill.Priority == PriorityLevel.Core)
            {
                int coreCount = DatabaseManager.Connection.Table<Skill>().Count(s => s.Priority == PriorityLevel.Core);
                var profile = GetProfile();

                if (coreCount >= 2)
                {
                    return (false, "Limit Reached: Maximum 2 Core Skills allowed to ensure focus.");
                }

                if (coreCount >= 1 && (profile == null || profile.WeekdayAvailableMinutes*5 + profile.WeekendAvailableMinutes*2 < 1680))
                {
                    return (false, "Capacity Warning: You have less than 4 hours available. Recommended limit is 1 Core Skill.");
                }
            }
            else if (skill.Priority == PriorityLevel.Builder)
            {
                int builderCount = DatabaseManager.Connection.Table<Skill>().Count(s => s.Priority == PriorityLevel.Builder);
                var profile = GetProfile();
                
                int limit = (profile != null && profile.WeekdayAvailableMinutes*5 + profile.WeekendAvailableMinutes*2 > 1680) ? 4 : 2;

                if (builderCount >= limit)
                {
                    return (false, $"Limit Reached: Maximum {limit} Builder Skills allowed based on available time.");
                }
            }

            if (DatabaseManager.Connection.Table<Skill>().Any(s => s.Name.ToLower() == skill.Name.ToLower()))
            {
                return (false, "Skill already exists.");
            }

            DatabaseManager.Connection.Insert(skill);
            return (true, "Skill added.");
        }

        public bool DeleteSkill(int id)
        {
            var skill = GetSkillById(id);
            if (skill != null)
            {
                DatabaseManager.Connection.Delete(skill);
                return true;
            }
            return false;
        }

        public int GetTotalSkills()
        {
            return DatabaseManager.Connection.Table<Skill>().Count();
        }

        public Profile GetProfile()
        {
            return DatabaseManager.Connection.Table<Profile>().FirstOrDefault();
        }

        public void UpdateProfile(Profile profile)
        {
            if (profile.Id == 0) DatabaseManager.Connection.Insert(profile);
            else DatabaseManager.Connection.Update(profile);
        }

        public List<WeeklyTask> GetTodayTasks()
        {
            return DatabaseManager.Connection.Table<WeeklyTask>()
                                .Where(t => t.ScheduledDate.Date == DateTime.Today.Date && !t.IsCompleted)
                                .ToList();
        }

        public List<WeeklyTask> GetFutureTasks()
        {
            return DatabaseManager.Connection.Table<WeeklyTask>()
                .Where(t => t.ScheduledDate >= DateTime.Today)
                .OrderBy(t => t.ScheduledDate)
                .ToList();
        }

        public void LogTaskCompletion(int taskId, bool isCompleted)
        {
            var task = DatabaseManager.Connection.Table<WeeklyTask>().FirstOrDefault(t => t.Id == taskId);
            if (task == null) return;

            var skill = GetSkillById(task.SkillId);

            if (isCompleted)
            {
                task.IsCompleted = true;
                DatabaseManager.Connection.Update(task);

                if (skill != null)
                {
                    skill.LastPracticed = DateTime.Now;
                    skill.MinutesInvested += task.DurationMinutes;
                    if (skill.MinutesDebt > 0) skill.MinutesDebt = Math.Max(0, skill.MinutesDebt - task.DurationMinutes);
                    DatabaseManager.Connection.Update(skill);
                }
            }
            else
            {
                // Task skipped
                if (skill != null)
                {
                    skill.MinutesDebt += task.DurationMinutes;
                    DatabaseManager.Connection.Update(skill);
                }
            }
        }

        public void RegeneratePlan()
        {
            var profile = GetProfile();
            if (profile != null)
            {
                SchedulingEngine.GenerateWeeklyPlan(profile);
            }
        }
    }
}