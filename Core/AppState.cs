using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Sharplings.Utils;
using Spectre.Console;

namespace Sharplings;

class AppState {
    const string StateFileName = ".sharplings-state";
    const string StateFileHeader = "DON'T EDIT THIS FILE!\n\n";

    AppState(List<Exercise> exercises, string finalMessage, FileStream stateFileStream, bool officialExercises) {
        Exercises = exercises;
        FinalMessage = finalMessage;
        StateFileStream = stateFileStream;
        OfficialExercises = officialExercises;
    }

    public int CurrentExerciseIndex { get; set; }
    public int ExercisesDone { get; set; }
    public List<Exercise> Exercises { get; }
    public FileStream StateFileStream { get; }
    public string FinalMessage { get; }
    public bool OfficialExercises { get; }

    // VS Code has its own file link handling
    public bool EmitFileLinks { get; } = Environment.GetEnvironmentVariable("TERM_PROGRAM") != "vscode";

    public Exercise CurrentExercise => Exercises[CurrentExerciseIndex];

    public static async Task<(AppState appState, StateFileParseResult parseResult)> ParseAsync(IList<ExerciseInfo> exerciseInfos, string finalMessage) {
        List<Exercise> exercises = exerciseInfos.Select(info => new Exercise {
            Directory = info.Directory,
            Name = info.Name,
            Test = info.Test,
            StrictAnalyzer = info.StrictAnalyzer,
            Hint = info.Hint,
            Done = false
        }).ToList();

        int currentExerciseIndex = 0;
        int exercisesDone = 0;
        StateFileParseResult parseResult = StateFileParseResult.NotRead;

        FileStream stateFileStream = File.Open(StateFileName, new FileStreamOptions {
            Access = FileAccess.ReadWrite,
            Mode = FileMode.OpenOrCreate,
            Share = FileShare.None,
            Options = FileOptions.SequentialScan
        });

        using (StreamReader reader = new(stateFileStream, leaveOpen: true)) {
            string stateFileContent = await reader.ReadToEndAsync();

            if (TryParseStateFile(stateFileContent, out string? currentExerciseName, out HashSet<string>? doneExercises)) {
                foreach (Exercise exercise in exercises) {
                    if (doneExercises.Contains(exercise.Name)) {
                        exercise.Done = true;
                        exercisesDone++;
                    }

                    if (exercise.Name == currentExerciseName) {
                        currentExerciseIndex = exercises.IndexOf(exercise);
                    }
                }

                parseResult = StateFileParseResult.Read;
            }
        }

        return (new AppState(exercises, finalMessage, stateFileStream, !File.Exists("info.toml")) {
            CurrentExerciseIndex = currentExerciseIndex,
            ExercisesDone = exercisesDone
        }, parseResult);
    }

    static bool TryParseStateFile(
        string stateFileContent,
        [NotNullWhen(true)] out string? currentExerciseName,
        [NotNullWhen(true)] out HashSet<string>? doneExercises) {
        doneExercises = null;
        currentExerciseName = null;

        // See `this.Write` for more information about the file format.
        string[] lines = stateFileContent.Split('\n');
        if (lines.Length < 4) return false;

        currentExerciseName = lines.ElementAt(2);
        if (string.IsNullOrWhiteSpace(currentExerciseName))
            return false;

        doneExercises = lines.Skip(4).TakeWhile(line => !string.IsNullOrWhiteSpace(line)).ToHashSet();
        return true;
    }

    /// Official exercises: Dump the solution file from the binary and return its path.<br/>
    /// Community exercises: Check if a solution file exists and return its path in that case.
    public async Task<string?> CurrentSolutionPath() {
        if (OfficialExercises)
            return await EmbeddedFilesFactory.Instance.WriteSolutionToDisk(CurrentExerciseIndex, CurrentExercise.Name);

        string solutionPath = CurrentExercise.SolutionPath;
        return File.Exists(solutionPath)
            ? solutionPath
            : null;
    }

    public async Task SetPending(int exerciseIndex) {
        if (SetStatus(exerciseIndex, false))
            await Write();
    }

    public bool SetStatus(int exerciseIndex, bool done) {
        Exercise exercise = Exercises[exerciseIndex];
        if (exercise.Done == done) return false;

        exercise.Done = done;

        if (done) ExercisesDone++;
        else ExercisesDone--;

        return true;
    }

    /// Mark the current exercise as done and move on to the next pending exercise if one exists.
    /// If all exercises are marked as done, run all of them to make sure that they are actually
    /// done. If an exercise which is marked as done fails, mark it as pending and continue on it.
    public async Task<ExercisesProgress> DoneCurrentExercise(bool clearBeforeFinalCheck) {
        if (!CurrentExercise.Done) {
            CurrentExercise.Done = true;
            ExercisesDone++;
        }

        int? nextPendingExerciseIndex = NextPendingExerciseIndex();
        if (nextPendingExerciseIndex != null) {
            await SetCurrentExerciseIndex(nextPendingExerciseIndex.Value);
            return ExercisesProgress.NewPending;
        }

        if (clearBeforeFinalCheck) AnsiConsole.Clear();
        else AnsiConsole.WriteLine();

        int? firstPendingExerciseIndex = await CheckAllExercises();
        if (firstPendingExerciseIndex != null) {
            await SetCurrentExerciseIndex(firstPendingExerciseIndex.Value);
            return ExercisesProgress.NewPending;
        }

        RenderFinalMessage();
        return ExercisesProgress.AllDone;
    }

    public async Task SetCurrentExerciseIndex(int exerciseIndex) {
        if (exerciseIndex == CurrentExerciseIndex) return;
        if (exerciseIndex >= Exercises.Count)
            throw new IndexOutOfRangeException("The current exercise index is higher than the number of exercises");

        CurrentExerciseIndex = exerciseIndex;
        await Write();
    }

    public async Task SetCurrentExerciseByName(string exerciseName) {
        int index = Exercises.FindIndex(exercise => exercise.Name == exerciseName);

        if (index == -1)
            throw new KeyNotFoundException($"No exercise found for '{exerciseName}'!");

        CurrentExerciseIndex = index;
        await Write();
    }

    /// Return the exercise index of the first pending exercise found.
    public async Task<int?> CheckAllExercises() {
        AnsiConsole.Cursor.Hide();
        int? result = await CheckAllExercisesImpl();
        AnsiConsole.Cursor.Show();

        return result;
    }

    public async Task<string> ResetCurrentExercise() {
        await SetPending(CurrentExerciseIndex);
        Exercise exercise = CurrentExercise;
        await Reset(CurrentExerciseIndex, exercise.Path);

        return exercise.Path;
    }

    public void RenderFinalMessage() {
        AnsiConsole.Clear();
        AnsiConsole.WriteLine("You made it!");

        if (!string.IsNullOrWhiteSpace(FinalMessage)) {
            AnsiConsole.WriteLine(FinalMessage);
        }
    }

    /// Write the state file.<br/>
    /// The file's format is very simple:<br/>
    /// - The first line is a comment.<br/>
    /// - The second line is an empty line.<br/>
    /// - The third line is the name of the current exercise. It must end with `\n` even if there
    /// are no done exercises.<br/>
    /// - The fourth line is an empty line.<br/>
    /// - All remaining lines are the names of done exercises.<br/>
    async Task Write() {
        StringBuilder builder = new(StateFileHeader);
        builder.Append(CurrentExercise.Name);
        builder.Append('\n');

        foreach (Exercise exercise in Exercises.Where(exercise => exercise.Done)) {
            builder.Append('\n');
            builder.Append(exercise.Name);
        }

        StateFileStream.Position = 0;
        StateFileStream.SetLength(0);
        await StateFileStream.WriteAsync(Encoding.Default.GetBytes(builder.ToString()));
        await StateFileStream.FlushAsync();
    }

    // Return the index of the next pending exercise or `None` if all exercises are done.
    int? NextPendingExerciseIndex() {
        int nextIndex = CurrentExerciseIndex + 1;

        if (nextIndex < Exercises.Count) {
            int laterIndex = Exercises[nextIndex..].FindIndex(exercise => !exercise.Done);
            if (laterIndex != -1) return laterIndex + nextIndex;
        }

        int prevIndex = Exercises[..CurrentExerciseIndex].FindIndex(exercise => !exercise.Done);
        return prevIndex == -1 ? null : prevIndex;
    }

    async Task<int?> CheckAllExercisesImpl() {
        // todo
        return -1;
    }

    async Task Reset(int exerciseIndex, string path) {
        if (OfficialExercises) {
            await EmbeddedFilesFactory.Instance.WriteExerciseToDisk(exerciseIndex, path);
            return;
        }

        Process? process = Process.Start(new ProcessStartInfo {
            FileName = "git",
            ArgumentList = { "stash", "push", "--", path },
            RedirectStandardInput = false,
            RedirectStandardOutput = false,
            RedirectStandardError = true
        });

        if (process == null)
            throw new InvalidOperationException($"Failed to run `git stash push -- {path}`");

        await process.WaitForExitAsync();

        if (process.ExitCode != 0) {
            string error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"`git stash push -- {path}` didn't run successfully: {error}");
        }
    }
}

enum StateFileParseResult {
    Read,
    NotRead,
}

enum ExercisesProgress {
    /// All exercises are done.
    AllDone,
    /// A new exercise is now pending.
    NewPending,
    /// The current exercise is still pending.
    CurrentPending,
}
