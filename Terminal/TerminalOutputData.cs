using JetBrains.Annotations;

namespace Sharplings.Terminal;

[PublicAPI]
public class TerminalOutputData {
    public string CompilationOutput { get; set; } = "";
    public string ExerciseOutput { get; set; } = "";
    public bool ExerciseDone { get; set; }
    public string ModuleName { get; set; } = "";
    public string ScriptFile { get; set; } = "";
    public int CompletedExercisesCount { get; set; }
    public int AllExercisesCount { get; set; }
    public string Keybinds { get; set; } = "";

    public string ScriptRelativePath => $"{ModuleName}/{ScriptFile}";
    public string ExercisePath => $"exercises/{ScriptRelativePath}";
    public string SolutionPath => $"solutions/{ScriptRelativePath}";
}
