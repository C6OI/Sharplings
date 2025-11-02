namespace Sharplings;

/// <summary>
/// <see cref="ExerciseInfo"/>
/// </summary>
public class Exercise {
    public required string? Directory { get; init; }
    public required string Name { get; init; }
    public required string Path { get; init; }
    public required string? FullPath { get; init; }
    public required bool Test { get; init; }
    public required bool StrictAnalyzer { get; init; }
    public required string Hint { get; init; }
    public required bool Done { get; set; }
}
