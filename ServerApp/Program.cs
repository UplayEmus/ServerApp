using ModdableWebServer.Helper;
using NetCoreServer;
using Serilog;
using Serilog.Events;
using ServerShared.CommonModels;
using ServerShared.Controllers;
using Shared;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace ServerApp;

internal class Program
{
    static void Main(string[] args)
    {
        Settings ServerAppSettings = JsonController.Read<Settings>("ServerAppSettings.json");
        if (args.Contains("example"))
        {
            ServerAppSettings.Servers.Add(new()
            {
                Name = "MainWeb",
                Port = 80,
            });
            ServerAppSettings.Servers.Add(new()
            {
                Name = "MainWebSSL",
                Port = 443,
                UseCerts = true
            });
            ServerAppSettings.CertDetails.Add(new()
            {
                Name = "UbisoftCert",
                Password = "ServerEmus"
            });
            ServerAppSettings.CertDetails.Add(new()
            {
                Name = "ServerEmusPFX",
                Password = "ServerEmus"
            });
            JsonController.Save(ServerAppSettings, "ServerAppSettings.json");
            return;
        }
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
            // Verbose expose MWS to us. we using DEBUG.
            MainLogger.LevelSwitch.MinimumLevel = LogEventLevel.Debug;
            MainLogger.ConsoleLevelSwitch.MinimumLevel = LogEventLevel.Debug;
            MainLogger.FileLevelSwitch.MinimumLevel = LogEventLevel.Debug;
        }
        if (args.Contains("verbose"))
        {
            MainLogger.LevelSwitch.MinimumLevel = LogEventLevel.Verbose;
            MainLogger.ConsoleLevelSwitch.MinimumLevel = LogEventLevel.Verbose;
            MainLogger.FileLevelSwitch.MinimumLevel = LogEventLevel.Verbose;
        }

        bool supportSSL = Directory.Exists("Cert");

        if (!supportSSL && ServerAppSettings.Servers.Any(x => x.UseCerts = true))
        {
            Log.Error("You have not created a 'Cert' folder to include your certificates, but your settings have to user cert. Please make a 'Cert' directory and install any certificate!");
            return;
        }

        X509Certificate2Collection Collection = [];
        foreach (var cert in Directory.GetFiles("Cert"))
        {
            if (cert.EndsWith("key"))
                continue;
            var certDetail = ServerAppSettings.CertDetails.FirstOrDefault(x => cert.Contains(x.Name));
            string password = string.Empty;
            if (certDetail != null)
                password = certDetail.Password;
            if (cert.EndsWith("pfx"))
            {
                Collection.Add(CertHelper.GetCert(cert, password));
                continue;
            }
            var name = Path.GetFileNameWithoutExtension(cert);
            var keyName = Path.Combine("Cert", $"{name}.key");
            Collection.Add(CertHelper.GetCertPem(cert, keyName));
        }
        SslContext context = new(SslProtocols.Tls12, Collection, CertHelper.NoCertificateValidator);

        List<ServerModel> servers = [];

        foreach (var server in ServerAppSettings.Servers)
        {
            servers.Add(new()
            { 
                Name  = server.Name,
                Port = server.Port,
                Context = server.UseCerts ? context : null,
            });
        }

        ServerController.Start(servers);
        PluginController.LoadPlugins();
        List<string> quitList =
        [
            "q",
            "quit",
            "exit",
        ];
        string endCheck = "not";
        while (!quitList.Contains(endCheck))
        {
            endCheck = Console.ReadLine()!;
            if (endCheck.StartsWith('!'))
            {
                CommandController.Run(endCheck[1..]);
            }
        }
        PluginController.StopPlugins();
        ServerController.Stop();
        MainLogger.Close();
    }

}