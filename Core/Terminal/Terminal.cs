using System.Diagnostics;
using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Sharplings.Terminal;

class Terminal {
    public Terminal(TerminalOutputData outputData) {
        OutputData = outputData;
        SizeWatcher = new TerminalSizeWatcher();

        _ = SizeWatcher.StartAsync(args => {
            if (!args.WidthChanged) return;
            Refresh();
        });
    }

    static int Width => AnsiConsole.Profile.Width;
    readonly Lock _lock = new();
    TerminalSizeWatcher SizeWatcher { get; }

    public TerminalOutputData OutputData {
        get;
        set {
            lock (_lock) {
                field = value;
                Refresh();
            }
        }
    }

    IRenderable GenerateOutput() {
        lock (_lock) {
            List<IRenderable> rows = [
                ProcessCompilationOutput(OutputData.CompilationOutput),
                ProcessExerciseOutput(OutputData.ExerciseOutput)
            ];

            if (OutputData.ExerciseDone) {
                rows.Add(new Markup("[lime]Exercise done ✓[/]"));
                rows.Add(new Markup($"[bold]Solution[/] for comparison: [cyan underline link]{Markup.Escape(OutputData.SolutionPath)}[/]"));
                rows.Add(new Text("When done experimenting, enter `n` to move on to the next exercise #️⃣\n"));
            }

            rows.Add(ProcessProgress(OutputData.CompletedExercisesCount, OutputData.AllExercisesCount));
            rows.Add(new Markup($"Current exercise: [blue underline link]{Markup.Escape(OutputData.ExercisePath)}[/]\n"));

            rows.Add(GeneratePrompt(OutputData.ExerciseDone));

            return new Rows(rows);
        }
    }

    void Refresh() {
        lock (_lock) {
            AnsiConsole.Clear();
            AnsiConsole.Write(GenerateOutput());
        }
    }

    static IRenderable ProcessCompilationOutput(string compilationOutput) =>
        new Text(compilationOutput);

    static IRenderable ProcessExerciseOutput(string exerciseOutput) =>
        new Markup($"[underline]Output[/]\n{Markup.Escape(exerciseOutput).Trim()}\n");

    // https://github.com/rust-lang/rustlings/blob/f80fbca12e47014a314e5e931678529c28cd9fd8/src/term.rs#L194
    static IRenderable ProcessProgress(int currentValue, int maxValue) {
        Debug.Assert(maxValue <= 999);
        Debug.Assert(currentValue <= maxValue);

        const string prefix = "Progress: [";
        const int prefixWidth = 11;
        const int postfixWidth = 9;
        const int wrapperWidth = prefixWidth + postfixWidth;
        const int minLineWidth = wrapperWidth + 4;

        if (Width < minLineWidth)
            return new Text($"Progress: {currentValue}/{maxValue}");

        StringBuilder builder = new(Markup.Escape(prefix));

        int width = Width - wrapperWidth;
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
        return new Markup(builder.ToString());
    }

    static IRenderable GeneratePrompt(bool exerciseDone) { // todo
        StringBuilder builder = new();

        if (exerciseDone)
            builder.Append("[bold]n[/]:[underline]next[/] / ");

        ShowKey('h', ":hint / ");

        ShowKey('l', ":list / ");
        ShowKey('c', ":check all / ");
        ShowKey('x', ":reset / ");
        ShowKey('q', ":quit ? ");

        return new Markup(builder.ToString());

        void ShowKey(char key, string postfix) =>
            builder.Append($"[bold]{key}[/]{postfix}");
    }
}
