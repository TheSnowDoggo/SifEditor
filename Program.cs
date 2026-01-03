using ConsoleUtility;

namespace SifEditor;

internal static class Program
{
    public static Config Config { get; private set; } = null!;

    private static void Main(string[] args)
    {
        InitializeConfig();

        Console.CursorVisible = false;

        var manager = new Manager();

        if (args.Length > 0)
        {
            manager.InitialFile = args[0];
        }

        manager.Start();
    }

    private static void InitializeConfig()
    {
        Config.Initialize();

        if (Config.TryLoadOrCreate(out Config? config))
        {
            Config = config;
            return;
        }

        ConsoleUtils.WriteLineColor("Config loading failed, using default config. [Config options will not be loaded]", ConsoleColor.Red);

        ConsoleUtils.WriteColor("Press enter to continue...", ConsoleColor.Yellow);

        while (Console.ReadKey(true).Key != ConsoleKey.Enter) { }

        Console.Clear();

        Config = new Config();
    }
}