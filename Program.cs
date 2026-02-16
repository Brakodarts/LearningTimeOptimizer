using System.Diagnostics;
using SQLite;

namespace SkillPlan
{
    public enum PriorityLevel
    {
        Core = 1,
        Builder = 2,
        Maintainer = 3,
        Dabbler = 4
    }

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
        [Ignore]
        public int TotalSkills => Program.GetTotalSkills();
    }

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

    // Fields used instead of Properties to allow passing by reference
    public class DailyTimeBuckets
    {
        public int Core;
        public int Builder;
        public int Maintainer;
        public int Dabbler;
    }

    public class Program
    {
        private static string dbPath = "userData.db";
        private static SQLiteConnection database = new SQLiteConnection(dbPath);

        public static void Main(string[] args)
        {
            database.CreateTable<Skill>();
            database.CreateTable<Profile>();
            database.CreateTable<WeeklyTask>();
            MainMenu();
        }

        public static void MainMenu()
        {
            bool exitProgram = false;
            
            do
            {
                Console.WriteLine("------Main Menu--------");
                var currentProfile = database.Table<Profile>().FirstOrDefault();

                if (currentProfile == null)
                {
                    Console.WriteLine("Please complete your profile to get started.");
                }
                else
                {
                    Console.WriteLine($"Welcome back, {currentProfile.Name}! You have {database.Table<Skill>().Count()} skills in your plan.");
                }
                Console.WriteLine("1. Manage Profile");
                Console.WriteLine("2. Manage Skills");
                Console.WriteLine("3. View Skill Plan");
                Console.WriteLine("4. Daily Check-in (Log Progress)");
                Console.WriteLine("5. Exit");

                string? userChoice = Console.ReadLine();

                switch (userChoice)
                {
                    case "1": Console.Clear(); ManageProfile(); break;
                    case "2": Console.Clear(); ManageSkills(); break;
                    case "3": Console.Clear(); ViewSkillPlan(); break;
                    case "4": Console.Clear(); DailyCheckIn(); break;
                    case "5": 
                        exitProgram = true; 
                        Console.Clear();
                        Console.WriteLine("Exiting. Goodbye!");
                        Thread.Sleep(1000); 
                        break;
                    default: Console.WriteLine("Invalid choice."); break;
                }

            } while (!exitProgram);
        }

        // ================= SKILL MANAGEMENT =================
        public static void ManageSkills()
        {
            Console.WriteLine("------Manage Skills--------");
            Console.WriteLine("1. View Skill List");
            Console.WriteLine("2. Add Skill");
            Console.WriteLine("3. Remove Skill");

            string? userChoice = Console.ReadLine();
            switch (userChoice)
            {
                case "1": Console.Clear(); ViewSkills(); break;
                case "2": Console.Clear(); AddSkill(); break;
                case "3": Console.Clear(); RemoveSkill(); break;
                default: Console.WriteLine("Invalid choice."); break;
            }
        }

        public static void ViewSkills()
        {
            var skills = database.Table<Skill>().ToList();
            if (skills.Count == 0)
            {
                Console.Clear();
                Console.WriteLine("No skills found.");
                return;
            }
            Console.WriteLine("------Skill List--------");
            foreach (var skill in skills)
            {
                string debtWarning = skill.MinutesDebt > 0 ? $" [DEBT: {skill.MinutesDebt}m]" : "";
                Console.WriteLine($"ID: {skill.Id} | {skill.Name} | {skill.Priority}{debtWarning} | Last: {skill.LastPracticed.ToShortDateString()}");
            }
        }

        public static void AddSkill()
        {
            string? name;
            do
            {
                Console.WriteLine("Enter skill name:");
                name = Console.ReadLine();
                if (database.Table<Skill>().Any(s => s.Name.ToLower() == name?.ToLower()))
                {
                    Console.WriteLine("Skill already exists.");
                    name = null;
                }
                else if (string.IsNullOrWhiteSpace(name))
                {
                    Console.WriteLine("Name cannot be empty.");
                }
            } while (name == null);

            Console.WriteLine("Select Priority (1: Core, 2: Builder, 3: Maintainer, 4: Dabbler):");
            
            int priorityInt;
            while (!int.TryParse(Console.ReadLine(), out priorityInt) || !Enum.IsDefined(typeof(PriorityLevel), priorityInt))
            {
                Console.WriteLine("Invalid input. Enter 1-4.");
            }

            // --- Capacity Validation Logic ---
            if (priorityInt == (int)PriorityLevel.Core)
            {
                int coreCount = database.Table<Skill>().ToList().Count(s => s.Priority == PriorityLevel.Core);
                var profile = database.Table<Profile>().FirstOrDefault();

                if (coreCount >= 2)
                {
                    Console.WriteLine("\nLimit Reached: Maximum 2 Core Skills allowed to ensure focus.");
                    Console.ReadLine();
                    return;
                }

                if (coreCount >= 1 && (profile == null || profile.WeekdayAvailableMinutes < 240))
                {
                     Console.WriteLine("\nCapacity Warning: < 4 hours available. Recommended limit is 1 Core Skill.");
                     Console.ReadLine();
                     return;
                }
            }
            else if (priorityInt == (int)PriorityLevel.Builder)
            {
                int builderCount = database.Table<Skill>().ToList().Count(s => s.Priority == PriorityLevel.Builder);
                var profile = database.Table<Profile>().FirstOrDefault();
                
                int limit = (profile != null && profile.WeekdayAvailableMinutes > 240) ? 4 : 2;

                if (builderCount >= limit)
                {
                    Console.WriteLine($"\nLimit Reached: Maximum {limit} Builder Skills allowed based on available time.");
                    Console.ReadLine();
                    return;
                }
            }

            Skill newSkill = new Skill
            {
                Name = name,
                Priority = (PriorityLevel)priorityInt,
                UrgencyLevel = 1,
                LastPracticed = DateTime.MinValue,
                MinutesInvested = 0,
                MinutesDebt = 0
            };

            database.Insert(newSkill);
            Console.WriteLine("Skill added.");
        }

        public static void RemoveSkill()
        {
            Console.WriteLine("Enter ID to remove:");
            if(int.TryParse(Console.ReadLine(), out int skillId))
            {
                var skill = database.Table<Skill>().FirstOrDefault(s => s.Id == skillId);
                if (skill != null)
                {
                    database.Delete(skill);
                    Console.WriteLine("Skill removed.");
                }
                else Console.WriteLine("Not found.");
            }
        }

        // ================= PROFILE MANAGEMENT =================
        public static void ManageProfile()
        {
            Console.WriteLine("------Manage Profile--------");
            Console.WriteLine("1. View Profile\n2. Edit Profile");
            string? choice = Console.ReadLine();
            
            if (choice == "1") { Console.Clear(); ViewProfile(); }
            else if (choice == "2") { Console.Clear(); EditProfile(); }
        }

        public static void ViewProfile()
        {
            var profile = database.Table<Profile>().FirstOrDefault();
            if (profile != null)
            {
                Console.WriteLine($"Name: {profile.Name}");
                Console.WriteLine($"Weekday Mins: {profile.WeekdayAvailableMinutes} | Weekend Mins: {profile.WeekendAvailableMinutes}");
                Console.WriteLine($"Skills Tracked: {profile.TotalSkills}");
            }
            else Console.WriteLine("No profile found.");
        }

        public static void EditProfile()
        {
            var profile = database.Table<Profile>().FirstOrDefault();
            bool isNew = profile == null;
            if (isNew) profile = new Profile();
            
            ProfileForm(profile, isNew);
        }

        public static void ProfileForm(Profile profile, bool isNew)
        {
            Console.WriteLine("Name:");
            profile.Name = Console.ReadLine();

            Console.WriteLine("Age:");
            int.TryParse(Console.ReadLine(), out int age);
            profile.Age = age;

            Console.WriteLine("Weekday Minutes:");
            int.TryParse(Console.ReadLine(), out int wdMins);
            profile.WeekdayAvailableMinutes = wdMins;

            Console.WriteLine("Weekend Minutes:");
            int.TryParse(Console.ReadLine(), out int weMins);
            profile.WeekendAvailableMinutes = weMins;

            Console.WriteLine("Holidays Available? (y/n):");
            profile.HolidaysAvailable = Console.ReadLine()?.ToLower() == "y";

            Console.WriteLine("Practice on Weekends? (y/n):");
            profile.WeekendsAvailable = Console.ReadLine()?.ToLower() == "y";

            if (isNew) database.Insert(profile);
            else database.Update(profile);
            
            Console.WriteLine("Profile Saved.");
        }

        // ================= PLAN GENERATION & CHECK-IN =================
        public static void ViewSkillPlan()
        {
            var profile = database.Table<Profile>().FirstOrDefault();
            if (profile == null)
            {
                Console.WriteLine("Profile required.");
                return;
            }

            Console.WriteLine("Refreshing Plan...");
            GenerateWeeklyPlan(profile);

            var futureTasks = database.Table<WeeklyTask>()
                .Where(t => t.ScheduledDate >= DateTime.Today)
                .OrderBy(t => t.ScheduledDate).ToList();

            if (futureTasks.Count == 0)
            {
                Console.Clear();
                Console.WriteLine("No tasks scheduled.");
            }
            else
            {
                Console.Clear();
                Console.WriteLine("------ WEEKLY PLAN --------");
                var groupedTasks = futureTasks.GroupBy(t => t.ScheduledDate.Date).OrderBy(g => g.Key);

                foreach (var group in groupedTasks)
                {
                    Console.WriteLine($"\n{group.Key:dddd, MMM dd}");
                    Console.WriteLine(new string('-', 20));

                    foreach (var task in group)
                    {
                        string status = task.IsCompleted ? "[DONE]" : "[ ]";
                        Console.WriteLine($" {status} {task.SkillName} ({task.DurationMinutes}m)");
                    }
                }
                Console.ReadLine();
            }
        }

        public static void DailyCheckIn()
        {
            Console.WriteLine("------ Check-in ------");
            var tasks = database.Table<WeeklyTask>()
                                .Where(t => t.ScheduledDate.Date == DateTime.Today.Date && !t.IsCompleted)
                                .ToList();

            if (tasks.Count == 0)
            {
                Console.WriteLine("No pending tasks.");
                Console.ReadLine();
                return;
            }

            foreach (var task in tasks)
            {
                Console.WriteLine($"\nCompleted: {task.SkillName} ({task.DurationMinutes}m)? (Y/N)");
                string input = Console.ReadLine()?.ToUpper() ?? "N";
                var skill = database.Table<Skill>().FirstOrDefault(s => s.Id == task.SkillId);
                
                if (input == "Y")
                {
                    task.IsCompleted = true;
                    database.Update(task);

                    if (skill != null)
                    {
                        skill.LastPracticed = DateTime.Now;
                        skill.MinutesInvested += task.DurationMinutes;
                        if (skill.MinutesDebt > 0) skill.MinutesDebt = Math.Max(0, skill.MinutesDebt - task.DurationMinutes);
                        database.Update(skill);
                    }
                    Console.WriteLine("Logged.");
                }
                else
                {
                    Console.WriteLine("Skipped. Time added to debt.");
                    if (skill != null)
                    {
                        skill.MinutesDebt += task.DurationMinutes;
                        database.Update(skill);
                    }
                }
            }
            Thread.Sleep(1000);
        }

        public static void GenerateWeeklyPlan(Profile profile)
        {
            // Clear future uncompleted tasks to allow regeneration based on new parameters
            database.Execute("DELETE FROM WeeklyTasks WHERE ScheduledDate >= ? AND IsCompleted = 0", DateTime.Today.Ticks);

            var allSkills = database.Table<Skill>().ToList();
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
                var buckets = CalculateTimeDistribution(dayBudget, totalCoreDebt);

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
                         .Select(s => new { Skill = s, Score = CalculateUrgency(s, simDates[s.Id]) })
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
            database.Insert(task);
            simDates[skill.Id] = date;
        }

        public static int GetTotalSkills()
        {
            return database.Table<Skill>().Count();
        }

        //============ LOGIC HELPERS ============

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