using LTO.Models;
using System;
using System.Linq;

namespace LTO.UI
{
    public static class SkillMenu
    {
        public static void Menu()
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
            var skills = Program.DataService.GetAllSkills();
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
                if (string.IsNullOrWhiteSpace(name))
                {
                    Console.WriteLine("Name cannot be empty.");
                }
            } while (string.IsNullOrWhiteSpace(name));

            Console.WriteLine("Select Priority (1: Core, 2: Builder, 3: Maintainer, 4: Dabbler):");

            int priorityInt;
            while (!int.TryParse(Console.ReadLine(), out priorityInt) || !Enum.IsDefined(typeof(PriorityLevel), priorityInt))
            {
                Console.WriteLine("Invalid input. Enter 1-4.");
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

            var result = Program.DataService.AddSkill(newSkill);
            
            if (result.Success)
            {
                Console.WriteLine("Skill added.");
            }
            else
            {
                Console.WriteLine($"\n{result.Message}");
                Console.ReadLine();
            }
        }

        public static void RemoveSkill()
        {
            Console.WriteLine("Enter ID to remove:");
            if (int.TryParse(Console.ReadLine(), out int skillId))
            {
                if (Program.DataService.DeleteSkill(skillId))
                {
                    Console.WriteLine("Skill removed.");
                }
                else
                {
                    Console.WriteLine("Not found.");
                }
            }
        }
    }
}