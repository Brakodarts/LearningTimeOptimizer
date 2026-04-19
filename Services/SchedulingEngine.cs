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
            
            // Calculate Cap Debt to prevent impossible schedules
            int totalCoreDebt = allSkills.Where(s => s.Priority == PriorityLevel.Core).Sum(s => s.MinutesDebt);
            if (totalCoreDebt > 300) totalCoreDebt = 300; 

            DateTime currentDate = DateTime.Today;

            for (int i = 0; i < 7; i++)
            {
                bool isWeekend = (currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday);
                if (isWeekend && !profile.WeekendsAvailable) { currentDate = currentDate.AddDays(1); continue; }
                
                int dayBudget = isWeekend ? profile.WeekendAvailableMinutes : profile.WeekdayAvailableMinutes;

                // Calculate time allocation
                var buckets = SchedulingEngine.CalculateTimeDistribution(dayBudget, totalCoreDebt);

                // Adjust simulation debt if Core bucket was expanded to pay it down
                if (buckets.Core > 60 && totalCoreDebt > 0) 
                {
                     int paid = buckets.Core - 60;
                     totalCoreDebt -= paid;
                     if (totalCoreDebt < 0) totalCoreDebt = 0;
                }

                var coreQueue = GetSortedQueue(allSkills, PriorityLevel.Core, simLastPracticed);
                var builderQueue = GetSortedQueue(allSkills, PriorityLevel.Builder, simLastPracticed);
                var maintainerQueue = GetSortedQueue(allSkills, PriorityLevel.Maintainer, simLastPracticed);
                var dabblerQueue = GetSortedQueue(allSkills, PriorityLevel.Dabbler, simLastPracticed);

                // Schedule Categories
                ProcessCategory(currentDate, buckets.Core, coreQueue, dabblerQueue, ref buckets.Dabbler, simLastPracticed);
                ProcessCategory(currentDate, buckets.Builder, builderQueue, dabblerQueue, ref buckets.Dabbler, simLastPracticed);
                ProcessCategory(currentDate, buckets.Maintainer, maintainerQueue, dabblerQueue, ref buckets.Dabbler, simLastPracticed);

                // Allocate spare time
                if (buckets.Dabbler > 15 && dabblerQueue.Any())
                {
                    InsertTask(dabblerQueue.First().Skill, currentDate, buckets.Dabbler, simLastPracticed);
                }

                currentDate = currentDate.AddDays(1);
            }
        }

        private static List<dynamic> GetSortedQueue(List<Skill> skills, PriorityLevel level, Dictionary<int, DateTime> simDates)
        {
            return skills.Where(s => s.Priority == level)
                         .Select(s => new { Skill = s, Score = SchedulingEngine.CalculateUrgency(s, simDates[s.Id]) })
                         .OrderByDescending(x => x.Score)
                         .ToList<dynamic>();
        }

        private static void ProcessCategory(DateTime date, int timeBudget, List<dynamic> skillQueue, List<dynamic> dabblerQueue, ref int dabblerBudget, Dictionary<int, DateTime> simDates)
        {
            foreach (var item in skillQueue)
            {
                if (timeBudget < SkillRules.GetSurvivalMinimum(item.Skill.Priority)) break; 

                int duration = SkillRules.GetIdealSessionLength(item.Skill.Priority);
                
                // Attempt to recover debt by extending session duration
                if (item.Skill.MinutesDebt > 0)
                {
                    int wanted = duration + item.Skill.MinutesDebt;
                    duration = Math.Min(wanted, SkillRules.GetDailyMax(item.Skill.Priority));
                }

                duration = Math.Min(duration, timeBudget);

                InsertTask(item.Skill, date, duration, simDates);
                timeBudget -= duration;

                // Insert Interleaved Breaks (Dabbler)
                if (dabblerBudget >= 15 && dabblerQueue.Any())
                {
                    var breakSkill = dabblerQueue.First().Skill;
                    InsertTask(breakSkill, date, 15, simDates);
                    dabblerBudget -= 15; 
                    var used = dabblerQueue.First();
                    dabblerQueue.RemoveAt(0);
                    dabblerQueue.Add(used);
                }
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

        public static DailyTimeBuckets CalculateTimeDistribution(int availableMinutes, int coreDebt)
        {
            var buckets = new DailyTimeBuckets();

            // Minimal allocation check
            if (availableMinutes <= 60)
            {
                buckets.Core = availableMinutes;
                return buckets;
            }

            buckets.Core = 60;
            int remaining = availableMinutes - 60;

            if (remaining > 0)
            {
                if (availableMinutes >= 120)
                {
                    int coreSlice = (int)(remaining * 0.40);
                    int builderSlice = (int)(remaining * 0.30);
                    int maintainerSlice = (int)(remaining * 0.20);
                    int dabblerSlice = remaining - (coreSlice + builderSlice + maintainerSlice);

                    // Overflow handling for Core limit
                    if (buckets.Core + coreSlice > 240)
                    {
                        int overflow = (buckets.Core + coreSlice) - 240;
                        coreSlice -= overflow;
                        dabblerSlice += overflow; 
                    }

                    buckets.Core += coreSlice;
                    buckets.Builder += builderSlice;
                    buckets.Maintainer += maintainerSlice;
                    buckets.Dabbler += dabblerSlice;
                }
                else
                {
                    int coreSlice = (int)(remaining * 0.70);
                    int builderSlice = remaining - coreSlice;
                    buckets.Core += coreSlice;
                    buckets.Builder += builderSlice;
                }
            }

            // Priority Reallocation: Shift time from lower priorities to cover Core debt
            if (coreDebt > 0)
            {
                int needed = coreDebt;
                
                // 1. Reallocate from Dabbler
                int stealDabbler = Math.Min(buckets.Dabbler, needed);
                buckets.Dabbler -= stealDabbler;
                buckets.Core += stealDabbler;
                needed -= stealDabbler;

                // 2. Reallocate from Maintainer
                if (needed > 0)
                {
                    int stealMaint = Math.Min(buckets.Maintainer, needed);
                    buckets.Maintainer -= stealMaint;
                    buckets.Core += stealMaint;
                    needed -= stealMaint;
                }

                // 3. Reallocate from Builder (Preserve minimum if possible)
                if (needed > 0)
                {
                    int availableBuilder = Math.Max(0, buckets.Builder - 20); 
                    int stealBuild = Math.Min(availableBuilder, needed);
                    buckets.Builder -= stealBuild;
                    buckets.Core += stealBuild;
                }
            }

            return buckets;
        }

        public static double GetPriorityWeight(PriorityLevel priority)
        {
            switch (priority)
            {
                case PriorityLevel.Core: return 0.50;
                case PriorityLevel.Builder: return 0.30;
                case PriorityLevel.Maintainer: return 0.25;
                case PriorityLevel.Dabbler: return 0.15;
                default: return 0.15;
            }
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
    }
}