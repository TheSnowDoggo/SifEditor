namespace SifEditor;

internal static class Program
{
    private static void Main(string[] args)
    {
        Console.CursorVisible = false;

        var manager = new Manager();

        if (args.Length > 0)
        {
            manager.InitialFile = args[0];
        }

        manager.Start();
    }
}