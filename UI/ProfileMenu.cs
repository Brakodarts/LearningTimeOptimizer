using LTO.Models;
using System;
using System.Diagnostics;

namespace LTO.UI
{
    public static class ProfileMenu
    {
        public static void Menu()
        {
            Profile currentProfile = Program.DataService.GetProfile();
            if (currentProfile == null)
            {
                EditProfile();
                return;
            }
            else
            {
                ViewProfile();
                Console.WriteLine("------Manage Profile--------");
                Console.WriteLine("1. Restart Profile");
                Console.WriteLine("2. Edit Availability");
                Console.WriteLine("0. Back to Main Menu");
                Console.Write("Select an option: ");
                string? choice = Console.ReadLine();

                switch (choice)
                {
                    case "0":
                        Console.Clear();
                        return;
                    case "1":
                        Console.Clear();
                        EditProfile();
                        break;
                    case "2":
                        Console.Clear();
                        EditAvailability();
                        break;
                    default:
                        Console.WriteLine("Invalid choice.");
                        break;
                }
            }
        }

        public static void ViewProfile()
        {
            var profile = Program.DataService.GetProfile();
            if (profile != null)
            {
                Console.WriteLine("------Your Profile--------");
                Console.WriteLine($"Name: {profile.Name}");
                Console.WriteLine($"Weekday Mins: {profile.WeekdayAvailableMinutes}\nWeekend Mins: {profile.WeekendAvailableMinutes}");
                Console.WriteLine($"Skills Tracked: {Program.DataService.GetTotalSkills()}");
            }
            else
            {
                Console.WriteLine($"No profile found. \nWould you like to create one? (y/n)");
                if (Console.ReadLine()?.ToLower() == "y")
                {
                    Console.Clear();
                    EditProfile();
                }
                else Console.Clear();
            }
        }

        public static void EditProfile()
        {
            var profile = Program.DataService.GetProfile();
            bool isNew = profile == null;
            if (isNew) profile = new Profile();

            ProfileForm(profile);
        }

        public static void EditAvailability()
        {
            var profile = Program.DataService.GetProfile();
            Console.WriteLine("------Edit Availability--------");
            Console.Write($"Available time on a Weekday (Minutes): {profile.WeekdayAvailableMinutes}) | New Value: ");
            int.TryParse(Console.ReadLine(), out int wdMins);
            profile.WeekdayAvailableMinutes = wdMins;

            Console.Write($"Practice on Weekends?: {profile.WeekendsAvailable} | New Value (y/n): ");
            profile.WeekendsAvailable = Console.ReadLine()?.ToLower() == "y";

            Console.Write($"Available time on a Weekend (Minutes): {profile.WeekendAvailableMinutes}) | New Value: ");
            int.TryParse(Console.ReadLine(), out int weMins);
            profile.WeekendAvailableMinutes = weMins;

            Console.Write($"Holidays Available?: {profile.HolidaysAvailable} | New Value (y/n): ");
            profile.HolidaysAvailable = Console.ReadLine()?.ToLower() == "y";

            Program.DataService.UpdateProfile(profile);
        }

        public static void ProfileForm(Profile profile)
        {
            Console.Write("Name: ");
            profile.Name = Console.ReadLine() ?? "";

            Console.Write("Age: ");
            int.TryParse(Console.ReadLine(), out int age);
            profile.Age = age;

            Console.Write("Available time on a Weekday (Minutes): ");
            int.TryParse(Console.ReadLine(), out int wdMins);
            profile.WeekdayAvailableMinutes = wdMins;

            Console.Write("Would you like to Practice on Weekends? (y/n): ");
            profile.WeekendsAvailable = Console.ReadLine()?.ToLower() == "y";
            if (profile.WeekendsAvailable)
            {
                 Console.Write("Available time on a Weekend day (Minutes): ");
                 int.TryParse(Console.ReadLine(), out int weMins);
                 profile.WeekendAvailableMinutes = weMins;
            }
            else
            {
                 profile.WeekendAvailableMinutes = 0;
            }

            Console.Write("Holidays Available? (y/n): ");
            profile.HolidaysAvailable = Console.ReadLine()?.ToLower() == "y";

            Program.DataService.UpdateProfile(profile);

            Console.WriteLine("Profile Saved.");
            if (Program.DataService.GetTotalSkills() == 0)
            {
                Console.WriteLine("You don't have any skills in your plan yet. Would you like to add one now? (y/n)");
                if (Console.ReadLine()?.ToLower() == "y")
                {
                    Console.Clear();
                    SkillMenu.AddSkill();
                }
                else Console.Clear();
            }
        }
    }
}