using Spectre.Console;

namespace MixerApp;

public static class ConsoleUi
{
    public static void Configure()
    {
        // Spectre may classify an IDE/debugger console as non-interactive merely because
        // stdout is captured. If stdin is still attached, prompts are safe and should work.
        if (!Console.IsInputRedirected && !AnsiConsole.Profile.Capabilities.Interactive)
        {
            AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings
            {
                Interactive = InteractionSupport.Yes
            });
        }
    }

    public static bool CanPrompt => AnsiConsole.Profile.Capabilities.Interactive;
}
