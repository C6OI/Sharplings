using System.Text;
using Spectre.Console;

namespace Sharplings;

static class Run {
    public static async Task<int> RunExercise(AppState appState) {
        Exercise exercise = appState.CurrentExercise;
        StringBuilder output = new(1 << 14);
        bool success = await exercise.RunExercise(output);

        AnsiConsole.Markup(output.ToString());

        if (!success) {
            await appState.SetPending(appState.CurrentExerciseIndex);

            AnsiConsole.MarkupLineInterpolated(
                $"Ran [blue underline]{appState.CurrentExercise.TerminalFileLink(appState.EmitFileLinks)}[/] with errors");
            return -1;
        }

        AnsiConsole.MarkupLineInterpolated(
            $"[lime]✓ Successfully ran[/] [blue underline]{appState.CurrentExercise.TerminalFileLink(appState.EmitFileLinks)}[/]");

        string? solutionPath = await appState.CurrentSolutionPath();
        if (!string.IsNullOrWhiteSpace(solutionPath)) {
            AnsiConsole.WriteLine();
            Exercise.SolutionLinkLine(solutionPath, appState.EmitFileLinks);
            AnsiConsole.WriteLine();
        }

        ExercisesProgress exercisesProgress = await appState.DoneCurrentExercise(false);

        if (exercisesProgress is ExercisesProgress.NewPending or ExercisesProgress.CurrentPending) {
            AnsiConsole.MarkupLineInterpolated(
                $"Next exercise: [blue underline]{appState.CurrentExercise.TerminalFileLink(appState.EmitFileLinks)}[/]");
        }

        return 0;
    }
}
