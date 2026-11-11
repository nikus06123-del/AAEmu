using System.Drawing;
using System.Text.RegularExpressions;

using AAEmu.Game.Models.Game;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Models.Game.Chat;

namespace AAEmu.Game.Utils.Scripts;

/// <summary>
/// Message output sink for commands executed from the server's console window
/// (upstream issue #1125). Writes every message to stdout and optionally mirrors
/// it to an in-game character when the command was prefixed with "as <player>".
/// </summary>
public partial class ConsoleMessageOutput : IMessageOutput
{
    private static readonly Regex ColorTagRegex = ColorTag();

    private readonly List<string> _messages = [];
    private readonly List<string> _errorMessages = [];
    private readonly ICharacter _mirrorCharacter;

    public ConsoleMessageOutput(ICharacter mirrorCharacter = null)
    {
        _mirrorCharacter = mirrorCharacter;
    }

    public IEnumerable<string> Messages => _messages;
    public IEnumerable<string> ErrorMessages => _errorMessages;

    public void SendMessage(string message) => SendMessage(ChatType.System, message, null);

    public void SendMessage(ChatType chatType, string message, Color? color = null)
    {
        if (color != null)
            message = $"|c{color.Value.A:X2}{color.Value.R:X2}{color.Value.G:X2}{color.Value.B:X2}{message}|r";

        _messages.Add(message);
        Console.WriteLine(StripColorTags(message));
        _mirrorCharacter?.SendMessage(chatType, message);
    }

    public void SendMessage(ICharacter target, string message)
    {
        _messages.Add($"Target: {target.Name} - {message}");
        Console.WriteLine($"Target: {target.Name} - {StripColorTags(message)}");
        target.SendMessage(message);
    }

    public void SendErrorMessage(ErrorMessageType messageType, uint type, bool isNotify)
    {
        _errorMessages.Add(messageType.ToString());
        Console.WriteLine($"[Error] {messageType}");
        _mirrorCharacter?.SendErrorMessage(messageType, type, isNotify);
    }

    private static string StripColorTags(string message)
    {
        return ColorTagRegex.Replace(message, string.Empty);
    }

    [GeneratedRegex(@"\|c[0-9A-Fa-f]{8}|\|r")]
    private static partial Regex ColorTag();
}
