using System.Diagnostics;
using System.Text;
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

        TermEventHandlerTask =
            Task.Run(() => TerminalEvent.TerminalEventHandler(watchEventWriter, termEventUnpauseReader, manualRun, cancellationToken),
                cancellationToken);
    }

    AppState AppState { get; }
    StringBuilder Output { get; } = new(1 << 14);
    IDoneStatus DoneStatus { get; set; } = new Pending();
    bool IsShowHint { get; set; }
    int TerminalWidth { get; set; } = AnsiConsole.Profile.Width;
    bool ManualRun { get; }
    ChannelWriter<byte> TermEventUnpauseWriter { get; }
    Task TermEventHandlerTask { get; }

    public async Task RunCurrentExercise() {
        using InputPauseGuard _ = new();

        IsShowHint = false; // ?

        AnsiConsole.WriteLine($"\nChecking the exercise `{AppState.CurrentExercise.Name}`. Please wait…");

        bool success = await AppState.CurrentExercise.RunExercise(Output);
        Output.AppendLine();

        if (success) {
            string? currentSolutionPath = await AppState.CurrentSolutionPath();

            DoneStatus = currentSolutionPath != null
                ? new DoneWithSolution(currentSolutionPath)
                : new DoneWithoutSolution();
        } else {
            await AppState.SetPending(AppState.CurrentExerciseIndex);
            DoneStatus = new Pending();
        }

        Render();
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
        Render();
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

            switch (key.KeyChar) {
                case 'y' or 'Y': {
                    await AppState.ResetCurrentExercise();

                    if (ManualRun) await RunCurrentExercise();
                    break;
                }

                case 'n' or 'N': {
                    Render();
                    break;
                }

                default: continue;
            }

            break;
        }

        await TermEventUnpauseWriter.WriteAsync(0);
    }

    public void UpdateTerminalWidth(int width) {
        if (TerminalWidth == width) return;

        TerminalWidth = width;
        Render();
    }

    public void Render() {
        // Prevent having the first line shifted if clearing wasn't successful.
        AnsiConsole.Write('\n');
        AnsiConsole.Clear();

        AnsiConsole.Markup(Output.ToString());

        if (IsShowHint) {
            AnsiConsole.MarkupLine("[bold underline cyan]Hint[/]");
            AnsiConsole.WriteLine(AppState.CurrentExercise.Hint);
            AnsiConsole.WriteLine();
        }

        if (DoneStatus is not Pending) {
            AnsiConsole.MarkupLine("[bold lime]Exercise done ✓[/]");

            if (DoneStatus is DoneWithSolution(var solutionPath)) {
                Exercise.SolutionLinkLine(solutionPath, AppState.EmitFileLinks);
            }

            AnsiConsole.WriteLine("When done experimenting, enter `n` to move on to the next exercise #️⃣");
            AnsiConsole.WriteLine();
        }

        ProgressBar(AppState.ExercisesDone, AppState.Exercises.Count, TerminalWidth);
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLineInterpolated(
            $"Current exercise: [blue underline]{Exercise.TerminalFileLink(AppState.CurrentExercise.Path, AppState.EmitFileLinks)}[/]");
        AnsiConsole.WriteLine();

        ShowPrompt();
    }

    static void ProgressBar(int currentValue, int maxValue, int termWidth) {
        Debug.Assert(maxValue <= 999);
        Debug.Assert(currentValue <= maxValue);

        const string prefix = "Progress: [";
        const int prefixWidth = 11;
        const int postfixWidth = 9;
        const int wrapperWidth = prefixWidth + postfixWidth;
        const int minLineWidth = wrapperWidth + 4;

        if (termWidth < minLineWidth) {
            AnsiConsole.Write($"Progress: {{currentValue}}/{maxValue}");
            return;
        }

        StringBuilder builder = new(Markup.Escape(prefix));

        int width = termWidth - wrapperWidth;
        int filled = width * currentValue / maxValue;

        builder.Append("[lime]");
        builder.Append('#', filled);

        if (filled < width)
            builder.Append('>');

        builder.Append("[/]");

        int widthMinusFilled = width - filled;

        if (widthMinusFilled > 1) {
            int redPathWidth = widthMinusFilled - 1;
            builder.Append("[red]");
            builder.Append('-', redPathWidth);
            builder.Append("[/]");
        }

        builder.Append(Markup.Escape($"] {currentValue,3}/{maxValue}"));
        AnsiConsole.Markup(builder.ToString());
    }

    void ShowPrompt() {
        if (DoneStatus is not Pending) {
            AnsiConsole.Markup("[bold]n[/]:[underline]next[/] / ");
        }

        if (ManualRun) {
            ShowKey('r', ":run / ");
        }

        if (!IsShowHint) {
            ShowKey('h', ":hint / ");
        }

        ShowKey('l', ":list / ");
        ShowKey('c', ":check all / ");
        ShowKey('x', ":reset / ");
        ShowKey('q', ":quit ? ");

        return;

        static void ShowKey(char key, string postfix) =>
            AnsiConsole.Markup($"[bold]{key}[/]{postfix}");
    }
}
