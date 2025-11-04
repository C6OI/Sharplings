using System.CommandLine;
using System.CommandLine.Invocation;
using Sharplings.Watch;
using Spectre.Console;

namespace Sharplings.Commands;

class RootAction(
    Subcommand subcommand
) : AsynchronousCommandLineAction {
    const int CurrentFormatVersion = 1;

    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default) {
        //Directory.SetCurrentDirectory("Sharplings");
        if (!Directory.Exists("Exercises")) {
            AnsiConsole.WriteLine("""
                   Welcome to...                             
              __ _                      _ _                  
             / _\ |__   __ _ _ __ _ __ | (_)_ __   __ _ ___  
             \ \| '_ \ / _` | '__| '_ \| | | '_ \ / _` / __| 
             _\ \ | | | (_| | |  | |_) | | | | | | (_| \__ \ 
             \__/_| |_|\__,_|_|  | .__/|_|_|_| |_|\__, |___/ 
                                 |_|              |___/      

            The `Exercises/` directory couldn't be found in the current directory.
            If you are just starting with Sharplings, run the command `Sharplings init` to initialize it.
            """);
            return 1;
        }

        InfoFile infoFile = await InfoFile.ParseAsync(cancellationToken);

        if (infoFile.FormatVersion > CurrentFormatVersion) {
            throw new InvalidOperationException("""
            The format version specified in the `info.toml` file is higher than the last one supported.
            It is possible that you have an outdated version of Sharplings.
            Try to install the latest Sharplings version first.                                    
            """);
        }

        (AppState appState, StateFileParseResult stateParseResult) = await AppState.ParseAsync(infoFile.Exercises, infoFile.FinalMessage);

        if (!string.IsNullOrWhiteSpace(infoFile.WelcomeMessage) && stateParseResult == StateFileParseResult.NotRead) {
            AnsiConsole.Clear();
            AnsiConsole.WriteLine(infoFile.WelcomeMessage);
            AnsiConsole.WriteLine("Press ENTER to continue ");
            Console.ReadLine();
            AnsiConsole.Clear();
        }

        if (!AnsiConsole.Profile.Out.IsTerminal) {
            throw new InvalidOperationException("Unsupported or missing terminal/TTY");
        }

        bool manualRun = parseResult.GetRequiredValue<bool>("--manual-run");

        switch (subcommand) {
            case Subcommand.None: {
                await Watcher.StartWatch(appState, manualRun, cancellationToken);
                break;
            }

            case Subcommand.Hint: {
                throw new NotImplementedException();
            }

            default: throw new ArgumentOutOfRangeException(nameof(subcommand), subcommand, null);
        }

        return 0;
    }
}

enum Subcommand {
    None,
    Hint
}
