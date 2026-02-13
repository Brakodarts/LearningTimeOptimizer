
using System.Configuration.Assemblies;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
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

    [Table("Profiles")]
    public class Profile
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public int AvailableMinutesPerDay { get; set; }
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
        public PriorityLevel Priority{ get; set; }
        public int UrgencyLevel { get; set; }
        public DateTime LastPracticed { get; set; }
        public int MinutesInvested { get; set; }

        
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
        

        // Displays the main menu and handles user input for navigating through the application.
        public static void MainMenu()
        {
            bool exitProgram = false;
            string? userChoice = "";
            database.CreateTable<Skill>();
            do
            {
                Console.WriteLine("------Main Menu--------");
                if (database.Table<Profile>().FirstOrDefault() == null)
                {
                    Console.WriteLine("Please complete your profile to get started.");

                }
                else
                {
                    Console.WriteLine($"Welcome back, {database.Table<Profile>().FirstOrDefault()?.Name}! You have {database.Table<Skill>().Count()} skills in your plan.");
                }
                Console.WriteLine("1. Manage Profile");
                Console.WriteLine("2. Manage Skills");
                Console.WriteLine("3. View Skill Plan");
                Console.WriteLine("4. Daily Check-in");
                Console.WriteLine("5. Exit");

                userChoice = Console.ReadLine();

                switch (userChoice)
                {
                    case "1":
                        Console.Clear();
                        ManageProfile();
                        break;
                    case "2":
                        Console.Clear();
                        ManageSkills();
                        break;
                    case "3":
                        Console.Clear();
                        ViewSkillPlan();
                        break;
                    case "4":
                        Console.Clear();
                        DailyCheckIn();
                        break;
                    case "5":
                        exitProgram = true;
                        Console.Clear();
                        Console.WriteLine("Exiting the program. Goodbye!");
                        Thread.Sleep(2000); // Pause for 2 seconds before exiting
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please select a valid option.");
                        break;
                }

            } while (!exitProgram);
        }
        public static void ManageSkills()
        {

            Console.WriteLine("------Manage Skills--------");
            Console.WriteLine("1. View Skill List");
            Console.WriteLine("2. Add Skill");
            Console.WriteLine("3. Remove Skill");

            string? userChoice = Console.ReadLine();
            switch (userChoice)
            {
                case "1":
                    Console.Clear();
                    ViewSkills();
                    break;
                case "2":
                    Console.Clear();
                    AddSkill();
                    break;
                case "3":
                    Console.Clear();
                    RemoveSkill();
                    break;
                default:
                    Console.WriteLine("Invalid choice. Please select a valid option.");
                    break;
            }
            
        }
        public static void ViewSkills()
        {
            var skills = database.Table<Skill>().ToList();
            if (skills.Count == 0)
            {
                Console.Clear();
                Console.WriteLine("No skills found. Please add some skills first.");
                return;
            }
            Console.WriteLine("------Skill List--------");
            foreach (var skill in skills)
            {
                Console.WriteLine($"ID: {skill.Id}, Name: {skill.Name}, Priority: {skill.Priority}, Urgency Level: {skill.UrgencyLevel}, Last Practiced: {skill.LastPracticed}, Minutes Invested: {skill.MinutesInvested}");
            }

        }  
        public static void AddSkill()
        {
            
            string? name;
            do
            {
                Console.WriteLine("Enter skill name:");
                name = Console.ReadLine();
                if(database.Table<Skill>().Where(s => s.Name.ToLower() == name.ToLower()).FirstOrDefault() != null)
                {
                    Console.WriteLine("Skill already exists. Please enter a different skill name.");
                    name = null;
                }
                else if (string.IsNullOrWhiteSpace(name))
                {
                    Console.WriteLine("Skill name cannot be empty. Please enter a valid skill name.");

                }
            } while (name == null);
            
            Console.WriteLine("Select a Priority Level (1 - 4):");
            Console.WriteLine("1. Core (Daily Practice - Critical Skills)");
            Console.WriteLine("2. Builder (3-4x Week - Major Hobbies)");
            Console.WriteLine("3. Maintainer (2x Week - Keeping sharp)");
            Console.WriteLine("4. Dabbler (1x Week - Low pressure)");
            int priorityInt;
            while (!int.TryParse(Console.ReadLine(), out priorityInt) || !Enum.IsDefined(typeof(PriorityLevel), priorityInt))
            {
                Console.WriteLine("Invalid input. Please enter a number between 1 and 4.");
            }

            Skill newSkill = new Skill
            {
                Name = name,
                Priority = (PriorityLevel)priorityInt,
                UrgencyLevel = 1,
                LastPracticed = DateTime.MinValue,
                MinutesInvested = 0
            };

            database.Insert(newSkill);

        }
        public static void RemoveSkill()
        {
            Console.WriteLine("Enter the ID of the skill to remove:");
            int skillId;
            while (!int.TryParse(Console.ReadLine(), out skillId))
            {
                Console.WriteLine("Invalid input. Please enter a valid skill ID.");
            }
            var skill = database.Table<Skill>().Where(s => s.Id == skillId).FirstOrDefault();   
            if (skill != null)
            {
                database.Delete(skill);
                Console.WriteLine("Skill removed successfully!");
            }
            else
            {
                Console.WriteLine("Skill not found. Please enter a valid skill ID.");
            }

        }

        public static void ManageProfile()
        {
            Console.WriteLine("------Manage Profile--------");
            Console.WriteLine("1. View Profile");
            Console.WriteLine("2. Edit Profile");

            string? userChoice = Console.ReadLine();
            switch (userChoice)
            {
                case "1":
                    Console.Clear();
                    ViewProfile();
                    break;
                case "2":
                    Console.Clear();        
                    EditProfile();
                    break;
                default:
                    Console.WriteLine("Invalid choice. Please select a valid option.");
                    break;
            }
        }
        public static void ViewProfile()
        {
            var profile = database.Table<Profile>().FirstOrDefault();
            if (profile != null)
            {
                Console.WriteLine($"Name: {profile.Name}");
                Console.WriteLine($"Age: {profile.Age}");
                Console.WriteLine($"Available Minutes Per Day: {profile.AvailableMinutesPerDay}");
                Console.WriteLine($"Holidays Available: {(profile.HolidaysAvailable ? "Yes" : "No")}");
                Console.WriteLine($"Weekends Available: {(profile.WeekendsAvailable ? "Yes" : "No")}");
                Console.WriteLine($"Total Skills: {profile.TotalSkills}");
            }
            else
            {
                Console.WriteLine("Profile not found. Please complete your profile first.");
            }
        }
        public static void EditProfile()
        {
            var profile = database.Table<Profile>().FirstOrDefault();
            if (profile == null)
            {
                profile = new Profile();
                Console.WriteLine("Creating a new profile.");
                ProfileForm(profile, true);
                
            }
            else
            {
                Console.WriteLine("Editing existing profile.");
                ProfileForm(profile, false);
            }     
        
        }
        public static void ProfileForm(Profile profile, bool isNew)
        {
            Console.WriteLine("Enter your name:");
            profile.Name = Console.ReadLine();

            Console.WriteLine("Enter your age:");
            int age;
            while (!int.TryParse(Console.ReadLine(), out age) || age <= 0)
            {
                Console.WriteLine("Invalid input. Please enter a valid age.");
            }
            profile.Age = age;

            Console.WriteLine("Enter available minutes per day:");
            int minutes;
            while (!int.TryParse(Console.ReadLine(), out minutes) || minutes <= 0)
            {
                Console.WriteLine("Invalid input. Please enter a valid number of minutes.");
            }
            profile.AvailableMinutesPerDay = minutes;

            Console.WriteLine("Do you have holidays available? (y/n):");
            string? holidaysInput = Console.ReadLine();
            profile.HolidaysAvailable = holidaysInput != null && holidaysInput.ToLower() == "y";

            Console.WriteLine("Are weekends available? (y/n):");
            string? weekendsInput = Console.ReadLine();
            profile.WeekendsAvailable = weekendsInput != null && weekendsInput.ToLower() == "y";

            if (isNew)
            {
                database.Insert(profile);
                Console.WriteLine("Profile created successfully!");
            }
            else
            {
                database.Update(profile);
                Console.WriteLine("Profile updated successfully!");
            }
        }

        public static void ViewSkillPlan()
        {
            Console.WriteLine("This feature is under development. Please check back later.");
            var futureTasks = database.Table<WeeklyTask>().Where(t => t.ScheduledDate >= DateTime.Today).OrderBy(t => t.ScheduledDate).ToList();

            if (futureTasks.Count == 0)
            {
                Console.Clear();
                Console.WriteLine("No upcoming skill practice sessions scheduled.");
                Console.WriteLine("Would you like to generate a new skill practice plan? (y/n):");
                if (Console.ReadLine()?.ToLower() == "y")
                {
                    var profile = database.Table<Profile>().FirstOrDefault();
                    if (profile == null)
                    {
                        Console.WriteLine("Profile not found. Please complete your profile first.");
                        return;
                    }
                    else
                    {
                        Console.WriteLine("Generating new skill practice plan...");
                        Thread.Sleep(2000);
                    }
                    GenerateWeeklyPlan(profile);
                    Console.WriteLine("New skill practice plan generated successfully!");
                }
            }
            else
            {   
                Console.Clear();
                Console.WriteLine("------YOUR WEEKLY LEARNING PLAN--------");
                var groupedTasks = futureTasks.GroupBy(t => t.ScheduledDate.Date).OrderBy(g => g.Key);

                foreach (var group in groupedTasks)
                {
                    string dateHeader = group.Key.ToString("dddd, MMMM dd");
                    Console.WriteLine($"\n{dateHeader}");
                    Console.WriteLine("--------------------------------");

                    foreach (var task in group)
                    {
                        Console.WriteLine($" - Skill: {task.SkillName}, Duration: {task.DurationMinutes} minutes, Completed: {(task.IsCompleted ? "Yes" : "No")}");
                    }

                    Console.WriteLine("\nPress Enter to return to menu...");
                    Console.ReadLine();
                }
            }


        }

        public static void GenerateWeeklyPlan(Profile profile)
        {
            Console.WriteLine("\nInitializing Planning Engine...");
            
            // 1. Clear old future plans to avoid duplicates
            // We use SQL directly here for efficiency
            database.Execute("DELETE FROM WeeklyTasks WHERE ScheduledDate >= ?", DateTime.Today.Ticks);

            var allSkills = database.Table<Skill>().ToList();
            if (allSkills.Count == 0)
            {
                Console.WriteLine("No skills found! Add some skills before generating a plan.");
                return;
            }

            // 2. The Simulation Dictionary
            // This tracks when we *pretend* to practice a skill during the simulation
            var simLastPracticed = new Dictionary<int, DateTime>();
            foreach(var s in allSkills)
            {
                simLastPracticed[s.Id] = s.LastPracticed;
            }

            DateTime currentDate = DateTime.Today;
            
            // 3. Loop through the next 7 days
            for (int i = 0; i < 7; i++)
            {
                // Skip weekends if the user said "No Weekends"
                bool isWeekend = (currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday);
                if (isWeekend && !profile.WeekendsAvailable)
                {
                    currentDate = currentDate.AddDays(1);
                    continue; 
                }

                int dailyBudget = profile.AvailableMinutesPerDay;

                // 4. Calculate Urgency based on SIMULATED dates
                var dailyQueue = allSkills
                    .Select(s => new 
                    { 
                        Skill = s, 
                        Score = CalculateUrgency(s, simLastPracticed[s.Id]) 
                    })
                    .OrderByDescending(x => x.Score) // Highest score first
                    .ToList();

                // 5. Fill the daily bucket
                foreach (var item in dailyQueue)
                {
                    // Logic: Core skills get 45 mins, others get 30 mins (Dynamic Sizing)
                    int sessionLength = (item.Skill.Priority == PriorityLevel.Core) ? 45 : 30;

                    if (dailyBudget >= sessionLength)
                    {
                        var task = new WeeklyTask
                        {
                            SkillId = item.Skill.Id,
                            SkillName = item.Skill.Name,
                            ScheduledDate = currentDate,
                            DurationMinutes = sessionLength,
                            IsCompleted = false
                        };
                        database.Insert(task);

                        // UPDATE SIMULATION: "Pretend" we practiced this today
                        simLastPracticed[item.Skill.Id] = currentDate;
                        
                        dailyBudget -= sessionLength;
                    }
                }
                
                currentDate = currentDate.AddDays(1);
            }
            
            Console.WriteLine("Weekly Plan Generated Successfully!");
            Thread.Sleep(1000); 
        }

        public static void DailyCheckIn()
        {
            Console.WriteLine("------Daily Check-in--------");
            Console.WriteLine("This feature is under development. Please check back later.");
        }
        public static int GetTotalSkills()
        {
            return database.Table<Skill>().Count();
        }

        //============ MATH & LOGIC HELPERS ============

        public static double GetPriorityWeight(PriorityLevel priority)
        {
            switch (priority)
            {
                case PriorityLevel.Core:
                    return 10.0;
                case PriorityLevel.Builder:
                    return 5.0;
                case PriorityLevel.Maintainer:
                    return 2.0;
                case PriorityLevel.Dabbler:
                    return 0.5;
                default:
                    return 1.0;
            }
        }

        public static double CalculateUrgency(Skill skill)
        {
            if (skill.LastPracticed == DateTime.MinValue) return 10000;
            double daysSice = (DateTime.Now - skill.LastPracticed).TotalDays;
            double priorityWeight = GetPriorityWeight(skill.Priority);

            return daysSice * priorityWeight;
        }
        public static double CalculateUrgency(Skill skill, DateTime effectiveLastPracticed)
        {
            // If never practiced, urgency is Infinite (must be scheduled!)
            if (effectiveLastPracticed == DateTime.MinValue) return 10000;

            double daysSince = (DateTime.Now - effectiveLastPracticed).TotalDays;
            
            // Prevent negative days if simulation looks slightly into future (edge case)
            if (daysSince < 0) daysSince = 0; 

            return daysSince * GetPriorityWeight(skill.Priority);
        }


    }

}