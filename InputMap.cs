namespace SifEditor;

internal sealed class InputMap
{
    public string Name { get; init; } = string.Empty;

    public Action? Action { get; init; }
}