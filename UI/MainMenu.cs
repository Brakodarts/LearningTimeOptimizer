using System;
using System.Threading;
using LTO.Models;

namespace LTO.UI
{
    public class MainMenu
    {
        public static void Menu()
        {
            bool exitProgram = false;
            
            do
            {
                Console.WriteLine("------Main Menu--------");
                var currentProfile = Program.DataService.GetProfile();

                if (currentProfile == null)
                {
                    Console.WriteLine("Please complete your profile to get started.");
                    Console.WriteLine("1. Create Profile");
                    Console.WriteLine("5. Exit");
                    Console.Write("Select an option: ");
                }
                else
                {
                    Console.WriteLine($"Welcome back, {currentProfile.Name}! You have {Program.DataService.GetTotalSkills()} skills in your plan.");
                    Console.WriteLine("1. Manage Profile");
                    Console.WriteLine("2. Manage Skills");
                    Console.WriteLine("3. View Skill Plan");
                    Console.WriteLine("4. Daily Check-in (Log Progress)");
                    Console.WriteLine("0. Exit");
                    Console.Write("Select an option: ");
                }


                string? userChoice = Console.ReadLine();

                switch (userChoice)
                {
                    case "1": Console.Clear(); ProfileMenu.Menu(); break;
                    case "2": Console.Clear(); SkillMenu.Menu(); break;
                    case "3": Console.Clear(); SkillPlanView.ViewPlan(); break;
                    case "4": Console.Clear(); CheckInView.DailyCheckIn(); break;
                    case "0": 
                        exitProgram = true; 
                        Console.Clear();
                        Console.WriteLine("Exiting. Goodbye!");
                        Thread.Sleep(1000); 
                        break;
                    default: Console.WriteLine("Invalid choice."); break;
                }

            } while (!exitProgram);
        }

    }
}