namespace Sharplings.Watch;

public interface IDoneStatus;

public struct Pending : IDoneStatus;

public struct DoneWithoutSolution : IDoneStatus;

public record struct DoneWithSolution(string Path) : IDoneStatus;
