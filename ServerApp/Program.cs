using ServerShared.Controllers;
using Shared;
using Serilog.Events;

namespace ServerApp;

internal class Program
{
    static void Main(string[] args)
    {
        MainLogger.CreateNew();
        if (args.Contains("clean"))
        {
            // Deleting all files and the database too.
            var log_files = Directory.GetFiles(Environment.CurrentDirectory, "*.log", SearchOption.AllDirectories);
            foreach (var logfile in log_files)
                File.Delete(logfile);
            Directory.Delete("Database", true);
        }
        if (args.Contains("debug"))
        {
            MainLogger.LevelSwitch.MinimumLevel = LogEventLevel.Debug;
        }

        ServerController.Start();
        PluginController.LoadPlugins();
        string endCheck = "not";
        while (!endCheck.Equals("exit", StringComparison.CurrentCultureIgnoreCase))
        {
            endCheck = Console.ReadLine()!;
            if (endCheck.StartsWith('!'))
            {
                CommandController.Run(endCheck);
            }
        }
        PluginController.UnloadPlugins();
        ServerController.Stop();
        Console.ReadLine();
    }

}