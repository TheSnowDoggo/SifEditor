using SCEWin;

namespace SifEditor;

internal sealed class InputData
{
    public InputData(MessageType type, KBDLLHookStruct kbdll)
    {
        Type  = type;
        Kbdll = kbdll;
    }

    public MessageType Type { get; }
    public KBDLLHookStruct Kbdll { get; }
}
