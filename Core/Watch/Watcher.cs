using System.Threading.Channels;
using JetBrains.Annotations;
using Sharplings.Utils;

namespace Sharplings.Watch;

static class Watcher {
    public static async Task StartWatch(AppState appState, bool manualRun, CancellationToken cancellationToken) {
        // todo https://github.com/rust-lang/rustlings/blob/f80fbca12e47014a314e5e931678529c28cd9fd8/src/watch.rs#L156-L172
        // I don't know why the code in Rustligns in the link above is needed, and will not implement it now

        await WatchListLoop(appState, manualRun, cancellationToken);
    }

    static async Task WatchListLoop(AppState appState, bool manualRun, CancellationToken cancellationToken) {
        while (true) {
            switch (await RunWatch(appState, manualRun, cancellationToken)) {
                case WatchExit.Shutdown: return;
                case WatchExit.List: throw new NotImplementedException();
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }

    [MustUseReturnValue]
    static async Task<WatchExit> RunWatch(AppState appState, bool manualRun, CancellationToken cancellationToken) {
        (ChannelWriter<IWatchEvent> watchEventWriter, ChannelReader<IWatchEvent> watchEventReader) = Channel.CreateUnbounded<IWatchEvent>(
            new UnboundedChannelOptions {
                SingleReader = true
            });

        // We store the reference to the FileSystemWatcher here because GC will collect it otherwise
        // ReSharper disable once TooWideLocalVariableScope
        FileSystemWatcher fileSystemWatcher;

        if (!manualRun) {
            NotifyEventHandler notifyEventHandler =
                new(watchEventWriter, appState.Exercises.Select(exercise => exercise.Name).ToList(), cancellationToken);

            fileSystemWatcher = new FileSystemWatcher {
                Path = "Exercises",
                Filter = "*.cs",
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true,
            };

            fileSystemWatcher.Changed += notifyEventHandler.OnFileSystemWatcherEvent;
            fileSystemWatcher.Renamed += notifyEventHandler.OnFileSystemWatcherEvent;
            fileSystemWatcher.Error += notifyEventHandler.FileSystemWatcherOnError;
        }

        WatchState watchState = new(appState, watchEventWriter, manualRun);
        await watchState.RunCurrentExercise();

        try {
            await foreach (IWatchEvent watchEvent in watchEventReader.ReadAllAsync(cancellationToken)) {
                await HandleEvent(watchEvent, watchState);
            }
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }

        return WatchExit.Shutdown;
    }

    static async Task HandleEvent(IWatchEvent watchEvent, WatchState watchState) {
        switch (watchEvent) {
            case FileChange fileChange:
                await watchState.HandleFileChange(fileChange.ExerciseIndex);
                break;

            // case TerminalResize terminalResize:
            //     break;

            default: throw new ArgumentOutOfRangeException(nameof(watchEvent));
        }
    }

    /// Returned by the watch mode to indicate what to do afterward.
    enum WatchExit {
        /// Exit the program.
        Shutdown,
        /// Enter the list mode and restart the watch mode afterward.
        List
    }
}
