using LTO.Models;
using System;
using System.Linq;

namespace LTO.UI
{
    public static class SkillPlanView
    {
        public static void ViewPlan()
        {
            Console.Clear();
            Console.WriteLine("------ WEEKLY PLAN --------");

            Console.WriteLine("Refreshing Plan...");
            Program.DataService.RegeneratePlan();

            var futureTasks = Program.DataService.GetFutureTasks();

            if (futureTasks.Count == 0)
            {
                Console.Clear();
                Console.WriteLine("No tasks scheduled.");
            }
            else
            {
                Console.Clear();
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
    }
}