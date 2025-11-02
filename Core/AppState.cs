using System.Diagnostics.CodeAnalysis;

namespace Sharplings;

public class AppState {
    const string StateFileName = ".sharplings-state";

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

    public static async Task<(AppState appState, StateFileParseResult parseResult)> ParseAsync(IList<ExerciseInfo> exerciseInfos, string finalMessage) {
        string exercisesPath = Path.GetFullPath("Exercises");

        List<Exercise> exercises = exerciseInfos.Select(info => {
            string path = info.Path;
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
                Path = path,
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

        // The first line: comment
        // The second line: `\n`
        // The third line: current exercise name
        // The fourth line: `\n`
        // The fifth and all other lines: done exercises names
        string[] lines = stateFileContent.Split('\n');
        if (lines.Length < 4) return false;

        currentExerciseName = lines.ElementAt(2);
        if (string.IsNullOrWhiteSpace(currentExerciseName))
            return false;

        doneExercises = lines.Skip(4).TakeWhile(line => !string.IsNullOrWhiteSpace(line)).ToHashSet();
        return true;
    }
}

public enum StateFileParseResult {
    Read,
    NotRead,
}
