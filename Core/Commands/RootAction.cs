using System.CommandLine;
using System.CommandLine.Invocation;
using Sharplings.Watch;
using Spectre.Console;

namespace Sharplings.Commands;

class RootAction(
    Subcommand subcommand
) : AsynchronousCommandLineAction {
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

        if (infoFile.FormatVersion > InfoFile.CurrentFormatVersion) {
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

            case Subcommand.Run: {
                string? exerciseName = parseResult.GetValue<string>("name");
                if (!string.IsNullOrWhiteSpace(exerciseName)) {
                    await appState.SetCurrentExerciseByName(exerciseName);
                }

                return await Run.RunExercise(appState);
            }

            case Subcommand.CheckAll: {
                int firstPendingExerciseIndex = await appState.CheckAllExercises();
                if (firstPendingExerciseIndex == -1) {
                    appState.RenderFinalMessage();
                    break;
                }

                if (appState.CurrentExercise.Done) {
                    await appState.SetCurrentExerciseIndex(firstPendingExerciseIndex);
                }

                AnsiConsole.WriteLine();
                AnsiConsole.WriteLine();

                AnsiConsole.Write(appState.ExercisesPending == 1
                    ? "One exercise pending: "
                    : $"{appState.ExercisesPending}/{appState.Exercises.Count} exercises pending. The first: ");

                AnsiConsole.MarkupLineInterpolated($"[blue underline]{appState.CurrentExercise.TerminalFileLink(appState.EmitFileLinks)}[/]");
                return -1;
            }

            case Subcommand.Reset: {
                string exerciseName = parseResult.GetRequiredValue<string>("name");

                await appState.SetCurrentExerciseByName(exerciseName);
                string exercisePath = await appState.ResetCurrentExercise();

                AnsiConsole.MarkupLineInterpolated(
                    $"The exercise [blue underline]{Exercise.TerminalFileLink(exercisePath, appState.EmitFileLinks)}[/] has been reset");
                break;
            }

            case Subcommand.Hint: {
                string? exerciseName = parseResult.GetValue<string>("name");
                if (!string.IsNullOrWhiteSpace(exerciseName)) {
                    await appState.SetCurrentExerciseByName(exerciseName);
                }

                AnsiConsole.WriteLine(appState.CurrentExercise.Hint);
                break;
            }

            default: throw new ArgumentOutOfRangeException(nameof(subcommand), subcommand, null);
        }

        return 0;
    }
}

enum Subcommand {
    None,
    Run,
    CheckAll,
    Reset,
    Hint
}
