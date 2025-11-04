using System.Threading.Channels;
using Sharplings.Terminal;
using Sharplings.Utils;
using Spectre.Console;

namespace Sharplings.Watch;

class WatchState {
    public WatchState(AppState appState, ChannelWriter<IWatchEvent> watchEventWriter, bool manualRun, CancellationToken cancellationToken) {
        (TermEventUnpauseWriter, ChannelReader<byte> termEventUnpauseReader) =
            Channel.CreateBounded<byte>(new BoundedChannelOptions(1) {
                SingleReader = true,
                FullMode = BoundedChannelFullMode.Wait
            });

        AppState = appState;
        ManualRun = manualRun;

        Terminal = new Terminal.Terminal(new TerminalOutputData {
            CompletedExercisesCount = AppState.ExercisesDone,
            AllExercisesCount = AppState.Exercises.Count,
            ModuleName = AppState.CurrentExercise.Directory ?? "",
            ScriptFile = AppState.CurrentExercise.Name
        });

        TermEventHandlerTask =
            Task.Run(() => TerminalEvent.TerminalEventHandler(watchEventWriter, termEventUnpauseReader, manualRun, cancellationToken),
                cancellationToken);
    }

    Terminal.Terminal Terminal { get; }
    AppState AppState { get; }
    bool IsShowHint { get; set; } = false;
    IDoneStatus DoneStatus { get; set; } = new Pending();
    bool ManualRun { get; }
    int TerminalWidth { get; } = AnsiConsole.Profile.Width;
    ChannelWriter<byte> TermEventUnpauseWriter { get; }
    Task TermEventHandlerTask { get; }

    public async Task RunCurrentExercise() {
        using InputPauseGuard _ = new();

        IsShowHint = false; // ?

        AnsiConsole.WriteLine($"\nChecking the exercise `{AppState.CurrentExercise.Name}`. Please wait…");

        TerminalOutputData outputData = Terminal.OutputData;

        bool success = await AppState.CurrentExercise.RunExercise(outputData);
        if (success) {
            string? currentSolutionPath = await AppState.CurrentSolutionPath();

            DoneStatus = currentSolutionPath != null
                ? new DoneWithSolution(currentSolutionPath)
                : new DoneWithoutSolution();
        } else {
            await AppState.SetPending(AppState.CurrentExerciseIndex);
            DoneStatus = new Pending();
        }

        Terminal.OutputData = outputData;
    }

    public async Task HandleFileChange(int exerciseIndex) {
        if (AppState.CurrentExerciseIndex != exerciseIndex) return;

        await RunCurrentExercise();
    }

    public async Task<ExercisesProgress> NextExercise() {
        if (DoneStatus is Pending) return ExercisesProgress.CurrentPending;

        return await AppState.DoneCurrentExercise(true);
    }

    public void ShowHint() {
        if (IsShowHint) return;

        IsShowHint = true;
        Terminal.OutputData = Terminal.OutputData with {
            ExerciseOutput = AppState.CurrentExercise.Hint // todo
        };
    }

    public async Task<ExercisesProgress> CheckAllExercises() {
        // Ignore any input until checking all exercises is done.
        using InputPauseGuard _ = new();

        int? firstPendingExerciseIndex = await AppState.CheckAllExercises();

        if (firstPendingExerciseIndex != null) {
            if (!AppState.CurrentExercise.Done)
                return ExercisesProgress.CurrentPending;

            await AppState.SetCurrentExerciseIndex(firstPendingExerciseIndex.Value);
            return ExercisesProgress.NewPending;
        }

        AppState.RenderFinalMessage();
        return ExercisesProgress.AllDone;
    }

    public async Task ResetExercise() {
        AnsiConsole.Clear();

        AnsiConsole.WriteLine($"Resetting will undo all your changes to the file {AppState.CurrentExercise.Path}");
        AnsiConsole.Write("Reset (y/n)? ");

        while (true) {
            ConsoleKeyInfo key = Console.ReadKey();

            switch (key.Key) {
                case ConsoleKey.Y: {
                    await AppState.ResetCurrentExercise();

                    if (ManualRun) await RunCurrentExercise();
                    break;
                }

                case ConsoleKey.N: {
                    RefreshTerminal();
                    break;
                }

                default: continue;
            }

            break;
        }

        await TermEventUnpauseWriter.WriteAsync(0);
    }

    [Obsolete("Will be rewritten soon")]
    public void RefreshTerminal() => Terminal.OutputData = Terminal.OutputData;
}
