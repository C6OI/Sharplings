using Sharplings.Terminal;

namespace Sharplings.Watch;

interface IWatchEvent;

record struct FileChange(int ExerciseIndex) : IWatchEvent;

record struct TerminalResize(int Width) : IWatchEvent;

record struct Input(InputEvent InputEvent) : IWatchEvent;

record NotifyErr(Exception Exception) : IWatchEvent;

record TerminalEventErr(Exception Exception) : IWatchEvent;
