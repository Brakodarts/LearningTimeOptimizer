using LTO.UI;
using LTO.Services;
using System;


namespace LTO
{
    public static class Program
    {
        public static IDataService DataService { get; private set; } = new DataService();

        public static void Main()
        {
            // Start the main menu
            MainMenu.Menu();
        }
    }
}