using JetBrains.Annotations;

namespace Sharplings.Terminal;

[PublicAPI]
record TerminalOutputData {
    public string CompilationOutput { get; set; } = "";
    public string ExerciseOutput { get; set; } = "";
    public bool ExerciseDone { get; set; }
    public string ModuleName { get; set; } = "";
    public string ScriptFile { get; set; } = "";
    public int CompletedExercisesCount { get; set; }
    public int AllExercisesCount { get; set; }
    public string Keybinds { get; set; } = "";

    public string ScriptRelativePath => $"{ModuleName}/{ScriptFile}.cs";
    public string ExercisePath => $"Exercises/{ScriptRelativePath}";
    public string SolutionPath => $"Solutions/{ScriptRelativePath}";
}
