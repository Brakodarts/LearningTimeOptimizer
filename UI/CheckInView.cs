using LTO.Models;
using System;
using System.Threading;

namespace LTO.UI
{
    public static class CheckInView
    {
        public static void DailyCheckIn()
        {
            Console.WriteLine("------ Check-in ------");
            var tasks = Program.DataService.GetTodayTasks();

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
                
                if (input == "Y")
                {
                    Program.DataService.LogTaskCompletion(task.Id, true);
                    Console.WriteLine("Logged.");
                }
                else
                {
                    Program.DataService.LogTaskCompletion(task.Id, false);
                    Console.WriteLine("Skipped. Time added to debt.");
                }
            }
            Thread.Sleep(1000);
        }
    }
}