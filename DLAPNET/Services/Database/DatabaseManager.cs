using Discord;
using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;

namespace Shrimpbot.Services.Database
{
    /// <summary>
    /// Provides static methods for interacting with the database.
    /// </summary>
    public class DatabaseManager
    {
        /// <summary>
        /// The directory where the database is located.
        /// </summary>
        public string DatabaseDirectory;
        public LiteDatabase Database;
        public DatabaseManager()
        {
            DatabaseDirectory = Path.Combine(Directory.GetCurrentDirectory(),
                                                "Database"
                                                );
            if (!Directory.Exists(DatabaseDirectory)) Directory.CreateDirectory(DatabaseDirectory);
            Database = new LiteDatabase(Path.Combine(DatabaseDirectory, "database.sdb1"));
        }
        /// <summary>
        /// Gets a <see cref="DatabaseUser"/> from the database. Creates a user if they aren't already in the database.
        /// </summary>
        /// <param name="id">The Discord user ID of the user.</param>
        public DatabaseUser GetUser(ulong id)
        {
            DatabaseUser user;

            user = Database.GetCollection<DatabaseUser>("Users").FindOne(x => id == x.Id);
            if (user is null)
            {
                LoggingService.LogToTerminal(LogSeverity.Verbose, "Created a user!");
                Database.GetCollection<DatabaseUser>("Users").Insert(new DatabaseUser { Id = id });
                user = Database.GetCollection<DatabaseUser>("Users").FindOne(x => id == x.Id);
            }
            return user;
        }
        /// <summary>
        /// Writes a <see cref="DatabaseUser"/> to the database. Creates a user if they aren't already in the database.
        /// </summary>
        /// <param name="id">The Discord user ID of the user.</param>
        public void WriteUser(DatabaseUser user)
        {
            if (!Database.GetCollection<DatabaseUser>("Users").Update(user))
            {
                Database.GetCollection<DatabaseUser>("Users").Insert(user);
            }
        }
        /// <summary>
        /// Gets a list of all users in the database.
        /// </summary>
        /// <returns></returns>
        public List<DatabaseUser> GetAllUsers() => Database.GetCollection<DatabaseUser>("Users").Query().ToList();
        /// <summary>
        /// Gets a <see cref="DatabaseServer"/> from the database. Creates a server if they aren't already in the database.
        /// </summary>
        /// <param name="id">The Discord server ID of the server.</param>
        public DatabaseServer GetServer(ulong id)
        {
            DatabaseServer server;

            server = Database.GetCollection<DatabaseServer>("Servers").FindOne(x => id == x.Id);
            if (server is null)
            {
                LoggingService.LogToTerminal(LogSeverity.Verbose, "Created a server!");
                Database.GetCollection<DatabaseServer>("Servers").Insert(new DatabaseServer { Id = id });
                server = Database.GetCollection<DatabaseServer>("Servers").FindOne(x => id == x.Id);
            }
            return server;
        }
        /// <summary>
        /// Writes a <see cref="DatabaseServer"/> to the database. Creates a server if they aren't already in the database.
        /// </summary>
        /// <param name="id">The Discord server ID of the server.</param>
        public void WriteServer(DatabaseServer server)
        {
            if (!Database.GetCollection<DatabaseServer>("Servers").Update(server))
            {
                Database.GetCollection<DatabaseServer>("Servers").Insert(server);
            }
        }
        /// <summary>
        /// Gets a list of all servers in the database.
        /// </summary>
        /// <returns></returns>
        public List<DatabaseServer> GetAllServers() => Database.GetCollection<DatabaseServer>("Servers").Query().ToList();

        public void ExecuteSql(string sql) => Database.Execute(sql);
    }
}
