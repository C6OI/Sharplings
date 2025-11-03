using System.Threading.Channels;
using Sharplings.Terminal;
using Sharplings.Utils;
using Spectre.Console;

namespace Sharplings.Watch;

class WatchState {
    public WatchState(AppState appState, ChannelWriter<IWatchEvent> watchEventWriter, bool manualRun) {
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

        TermEventHandlerTask = Task.Run(() => TerminalEvent.TerminalEventHandler(watchEventWriter, termEventUnpauseReader, manualRun));
    }

    Terminal.Terminal Terminal { get; }
    AppState AppState { get; }
    bool ShowHint { get; set; } = false;
    IDoneStatus DoneStatus { get; set; } = new Pending();
    bool ManualRun { get; }
    int TerminalWidth { get; } = AnsiConsole.Profile.Width;
    ChannelWriter<byte> TermEventUnpauseWriter { get; }
    Task TermEventHandlerTask { get; }

    public async Task RunCurrentExercise() {
        using InputPauseGuard _ = new();

        ShowHint = false; // ?

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
}
