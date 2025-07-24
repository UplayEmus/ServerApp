using System.Text.Json;

namespace ServerApp;

internal class Settings
{
    private static Settings? instance;
    private static JsonSerializerOptions serializerOptions = new()
    {
        WriteIndented = true,
        IndentCharacter = '\t',
        IndentSize = 1,
    };

    public static Settings Instance
    {
        get
        {
            if (instance != null)
                return instance;
            if (File.Exists("Settings.json"))
                instance = JsonSerializer.Deserialize<Settings>(File.ReadAllText("Settings.json"));
            instance ??= new();
            File.WriteAllText("Settings.json", JsonSerializer.Serialize(instance, serializerOptions));
            return instance;
        }
    }

    internal static void Save()
    {
        File.WriteAllText("Settings.json", JsonSerializer.Serialize(instance, serializerOptions));
    }

    public class Server
    {
        public string Name { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool UseCerts { get; set; }
    }

    public class CertDetail
    {
        public string Name { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }

    public List<Server> Servers { get; set; } = [];

    public List<CertDetail> CertDetails { get; set; } = [];
}
