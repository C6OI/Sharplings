namespace Sharplings.Watch;

interface IWatchEvent; // todo

record struct FileChange(int ExerciseIndex) : IWatchEvent;

record struct TerminalResize(int Width) : IWatchEvent;
