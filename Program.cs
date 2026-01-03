namespace SifEditor;

internal static class Program
{
    private static void Main()
    {
        Console.CursorVisible = false;

        var manager = new Manager();

        manager.Start();
    }
}