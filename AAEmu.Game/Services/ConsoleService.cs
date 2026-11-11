using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game.Char;
using AAEmu.Game.Utils.Scripts;

using Microsoft.Extensions.Hosting;

using NLog;

namespace AAEmu.Game.Services;

/// <summary>
/// Reads lines from the server's console window (stdin) and executes them as GM
/// commands (upstream issue #1125). Console access equals physical host access,
/// so commands run at the highest access level.
///
/// Prefixing a line with "as &lt;playerName&gt; " executes the rest of the line as
/// if that online character had typed it, which also enables player-bound
/// commands (teleport, heal, item add, ...). Commands that only make sense for
/// an in-game player will report an error on the console when no character is
/// bound; they never crash the server.
/// </summary>
public class ConsoleService : IHostedService
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    private Task _consoleTask;
    private CancellationTokenSource _cancellationTokenSource;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Do not steal stdin when the host runs without an interactive console
        // (piped input, docker -d, windows service, ...). ReadLine would return
        // null immediately in those setups anyway, but skipping keeps logs clean.
        if (Console.IsInputRedirected)
        {
            Logger.Info("ConsoleService: stdin is redirected, interactive console commands are disabled");
            return Task.CompletedTask;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _consoleTask = Task.Run(() => ConsoleLoop(_cancellationTokenSource.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource?.Cancel();
        try
        {
            // Give the loop a brief chance to exit on its own
            _consoleTask?.Wait(TimeSpan.FromSeconds(1));
        }
        catch (Exception)
        {
            // ReadLine is blocked waiting for input; the process is shutting down anyway
        }

        return Task.CompletedTask;
    }

    private static void ConsoleLoop(CancellationToken token)
    {
        Logger.Info("Console command interface active - type a command (without the leading /) and press Enter");
        while (!token.IsCancellationRequested)
        {
            string line;
            try
            {
                line = Console.ReadLine();
            }
            catch (Exception)
            {
                break;
            }

            if (line == null) // stdin closed (service mode, docker detach, ...)
                break;

            line = line.Trim();
            if (line.Length == 0)
                continue;

            try
            {
                ExecuteConsoleCommand(line);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error while executing console command");
            }
        }

        Logger.Info("Console command interface stopped");
    }

    private static void ExecuteConsoleCommand(string line)
    {
        Character asCharacter = null;

        // "as <playerName> <command ...>" executes the command as that online character
        if (line.StartsWith("as ", StringComparison.OrdinalIgnoreCase))
        {
            var parts = line.Split(' ', 3);
            if (parts.Length >= 3)
            {
                asCharacter = WorldManager.Instance.GetCharacter(parts[1]);
                if (asCharacter == null)
                {
                    Logger.Info("[Console] Character '{0}' is not online", parts[1]);
                    return;
                }

                line = parts[2];
            }
            else
            {
                Logger.Info("[Console] Usage: as <playerName> <command ...>");
                return;
            }
        }

        Logger.Info("[Console] Executing: {0}", line);
        CommandManager.Instance.Handle(asCharacter, line, out _);
    }
}
