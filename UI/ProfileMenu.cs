using LTO.Models;
using System;

namespace LTO.UI
{
    public static class ProfileMenu
    {
        public static void Menu()
        {
            Console.WriteLine("------Manage Profile--------");
            Console.WriteLine("1. View Profile\n2. Edit Profile");
            string? choice = Console.ReadLine();

            if (choice == "1") { Console.Clear(); ViewProfile(); }
            else if (choice == "2") { Console.Clear(); EditProfile(); }
        }

        public static void ViewProfile()
        {
            var profile = Program.DataService.GetProfile();
            if (profile != null)
            {
                Console.WriteLine($"Name: {profile.Name}");
                Console.WriteLine($"Weekday Mins: {profile.WeekdayAvailableMinutes} | Weekend Mins: {profile.WeekendAvailableMinutes}");
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

            Console.Write("Would you like to Practice on Weekends? (y/n):");
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

            Console.Write("Holidays Available? (y/n):");
            profile.HolidaysAvailable = Console.ReadLine()?.ToLower() == "y";

            Program.DataService.UpdateProfile(profile);

            Console.WriteLine("Profile Saved.");
        }
    }
}