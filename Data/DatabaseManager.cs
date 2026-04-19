using SQLite;
using LTO.Models;

namespace LTO.Data
{
    public static class DatabaseManager
    {
        private static string dbPath = "userData.db";
        public static SQLiteConnection Connection { get; private set; }
        
        static DatabaseManager()
        {
            Connection = new SQLiteConnection(dbPath);
            
            // Der Manager baut jetzt die Tabellen selbst auf!
            Connection.CreateTable<Skill>();
            Connection.CreateTable<Profile>();
            Connection.CreateTable<WeeklyTask>();
        }
    }
}