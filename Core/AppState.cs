using System.Diagnostics.CodeAnalysis;
using System.Text;
#if !DEBUG
using Sharplings.Utils;
#endif

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

    public Exercise CurrentExercise => Exercises[CurrentExerciseIndex];

    public static async Task<(AppState appState, StateFileParseResult parseResult)> ParseAsync(IList<ExerciseInfo> exerciseInfos, string finalMessage) {
        string exercisesPath = Path.GetFullPath("Exercises");

        List<Exercise> exercises = exerciseInfos.Select(info => {
            string name = info.Name;
            string? dir = info.Directory;
            string hint = info.Hint;

            string fullPath = !string.IsNullOrWhiteSpace(dir)
                ? Path.Combine(exercisesPath, dir)
                : exercisesPath;

            fullPath = Path.Combine(fullPath, $"{name}.cs");

            return new Exercise {
                Directory = dir,
                Name = name,
                FullPath = fullPath,
                Test = info.Test,
                StrictAnalyzer = info.StrictAnalyzer,
                Hint = hint,
                Done = false
            };
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
#if !DEBUG
        if (OfficialExercises)
            return await EmbeddedFilesFactory.Instance.WriteSolutionToDisk(CurrentExerciseIndex, CurrentExercise.Name);

        string solutionPath = CurrentExercise.SolutionPath;
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (File.Exists(solutionPath)) return solutionPath;
#endif

        return null;
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
        builder.AppendLine(CurrentExercise.Name);

        foreach (Exercise exercise in Exercises.Where(exercise => exercise.Done)) {
            builder.Append('\n');
            builder.Append(exercise.Name);
        }

        StateFileStream.Position = 0;
        StateFileStream.SetLength(0);
        await StateFileStream.WriteAsync(Encoding.Default.GetBytes(builder.ToString()));
        await StateFileStream.FlushAsync();
    }
}

enum StateFileParseResult {
    Read,
    NotRead,
}
