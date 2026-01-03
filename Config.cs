using ConsoleUtility;
using SCENeo;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SifEditor;

internal sealed class Config
{
    public const string Filename = "config.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
    };

    public static string Filepath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Filename);

    public Config() { }

    public SCEColor DefaultBackgroundColor { get; set; } = SCEColor.DarkCyan;
    public int LockedFrameCap { get; set; } = 60;
    public bool StartFrameCapped { get; set; } = true;
    public double CameraSpeed { get; set; } = 40;
    public bool AlertsEnabled { get; set; } = true;
    public bool ShowFPS { get; set; } = true;
    public int DefaultCanvasWidth { get; set; } = 20;
    public int DefaultCanvasHeight { get; set; } = 10;
    public SCEColor DefaultCanvasColor { get; set; } = SCEColor.White;

    public static void Initialize()
    {
        var converter = new JsonStringEnumConverter<SCEColor>();

        Options.Converters.Add(converter.CreateConverter(typeof(SCEColor), Options));
    }

    public static bool TryLoadOrCreate([NotNullWhen(true)] out Config? config)
    {
        if (File.Exists(Filepath))
        {
            return TryLoad(out config);
        }

        config = new Config();

        if (config.Save())
        {
            return true;
        }

        return TryLoad(out config);
    }

    public static bool TryLoad([NotNullWhen(true)] out Config? config)
    {
        try
        {
            using (var stream = File.OpenRead(Filepath))
            {
                config = JsonSerializer.Deserialize<Config>(stream, Options);
            }

            return config != null;
        }
        catch (Exception ex)
        {
            ConsoleUtils.WriteLineColor($"<!> Failed to load config <!>\n{ex.Message}", ConsoleColor.DarkRed);

            ConsoleUtils.WriteColor("Would you like to reset the config?\nYes[y] or No[n]: ", ConsoleColor.Yellow);

            if (ConsoleUtils.BoolPrompt())
            {
                config = new Config();

                if (config.Save())
                {
                    return true;
                }
            }

            config = null;
            return false;
        }
    }

    public bool Save()
    {
        try
        {
            using (var stream = File.Open(Filepath, FileMode.Create)) 
            {
                JsonSerializer.Serialize(stream, this, Options);
            }

            return true;
        }
        catch (Exception ex)
        {
            ConsoleUtils.WriteLineColor($"<!> Failed to save config <!>\n{ex.Message}", ConsoleColor.DarkRed);

            return false;
        }
    }
}