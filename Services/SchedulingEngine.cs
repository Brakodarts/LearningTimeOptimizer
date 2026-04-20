using System;
using LTO.Models;
using LTO.Data;
using System.Collections.Generic;


namespace LTO.Services
{
    public static class SchedulingEngine
    {
        public static void GenerateWeeklyPlan(Profile profile)
        {
            // Clear future uncompleted tasks to allow regeneration based on new parameters
            DatabaseManager.Connection.Execute("DELETE FROM WeeklyTasks WHERE ScheduledDate >= ? AND IsCompleted = 0", DateTime.Today.Ticks);

            var allSkills = DatabaseManager.Connection.Table<Skill>().ToList();
            if (allSkills.Count == 0) return;

            // Track last practiced dates for urgency simulation
            var simLastPracticed = allSkills.ToDictionary(s => s.Id, s => s.LastPracticed);


            DateTime currentDate = DateTime.Today;

            for (int i = 0; i < 7; i++)
            {
                bool isWeekend = (currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday);
                if (isWeekend && !profile.WeekendsAvailable) { currentDate = currentDate.AddDays(1); continue; }

                int dayBudget = isWeekend ? profile.WeekendAvailableMinutes : profile.WeekdayAvailableMinutes;

                // Calculate time allocation for the categories based on available time
                var buckets = SchedulingEngine.CalculateTimeDistribution(dayBudget);

                // Sort skills by urgency within each category
                var coreQueue = GetSortedQueue(allSkills, PriorityLevel.Core, simLastPracticed);
                var builderQueue = GetSortedQueue(allSkills, PriorityLevel.Builder, simLastPracticed);
                var maintainerQueue = GetSortedQueue(allSkills, PriorityLevel.Maintainer, simLastPracticed);

                // Schedule Categories
                ProcessCategory(currentDate, buckets.Core, coreQueue, simLastPracticed);
                ProcessCategory(currentDate, buckets.Builder, builderQueue, simLastPracticed);
                ProcessCategory(currentDate, buckets.Maintainer, maintainerQueue, simLastPracticed);

                currentDate = currentDate.AddDays(1);
            }
        }

        public static DailyTimeBuckets CalculateTimeDistribution(int availableMinutes)
        {
            var buckets = new DailyTimeBuckets();

            // Minimal allocation check
            if (availableMinutes <= 60)
            {
                buckets.Core = availableMinutes;
                return buckets;
            }
            else if (availableMinutes <= 120)
            {
                buckets.Core = 60;
                buckets.Builder = availableMinutes - 60;
                return buckets;
            }
            else
            {
                buckets.Core = 60;
                buckets.Builder = 60;
                int remaining = availableMinutes - 120;
                buckets.Core += (int)((remaining * 0.80) / 10) * 10;  // Round to nearest 10
                remaining = availableMinutes - buckets.Core;
                buckets.Builder = (int)((remaining * 0.80) / 10) * 10;  // Round to nearest 10
                remaining = availableMinutes - buckets.Core - buckets.Builder;
                buckets.Maintainer = remaining;
                return buckets;
            }
        }

        private static List<dynamic> GetSortedQueue(List<Skill> skills, PriorityLevel level, Dictionary<int, DateTime> simDates)
        {
            return skills.Where(s => s.Priority == level)
                         .Select(s => new { Skill = s, Score = SchedulingEngine.CalculateUrgency(s, simDates[s.Id]) })
                         .OrderByDescending(x => x.Score)
                         .ToList<dynamic>();
        }

        public static double CalculateUrgency(Skill skill, DateTime effectiveLastPracticed)
        {
            if (effectiveLastPracticed == DateTime.MinValue) return 10000;

            double daysSince = (DateTime.Now - effectiveLastPracticed).TotalDays;
            if (daysSince < 0) daysSince = 0;

            double urgency = daysSince * GetPriorityWeight(skill.Priority);

            // Weight debt to prioritize recovery
            if (skill.MinutesDebt > 0)
            {
                urgency += (skill.MinutesDebt * 0.1);
            }

            return urgency;
        }

        public static double GetPriorityWeight(PriorityLevel priority)
        {
            switch (priority)
            {
                case PriorityLevel.Core: return 0.50;
                case PriorityLevel.Builder: return 0.30;
                case PriorityLevel.Maintainer: return 0.25;
                default: return 0.10;
            }
        }

        private static void ProcessCategory(DateTime date, int timeBudget, List<dynamic> skillQueue, Dictionary<int, DateTime> simDates)
        {

            int skillsRemaining = skillQueue.Count;

            foreach (var item in skillQueue)
            {
                if (timeBudget < SkillRules.GetSurvivalMinimum(item.Skill.Priority)) break;

                int ideal = SkillRules.GetIdealSessionLength(item.Skill.Priority);

                int duration = Math.Max(ideal, timeBudget / skillsRemaining);

                duration = Math.Min(duration, timeBudget);

                InsertTask(item.Skill, date, duration, simDates);
                timeBudget -= duration;

                skillsRemaining--;
            }
        }

        private static void InsertTask(Skill skill, DateTime date, int duration, Dictionary<int, DateTime> simDates)
        {
            var task = new WeeklyTask
            {
                SkillId = skill.Id,
                SkillName = skill.Name,
                ScheduledDate = date,
                DurationMinutes = duration,
                IsCompleted = false
            };
            DatabaseManager.Connection.Insert(task);
            simDates[skill.Id] = date;
        }
    }
}