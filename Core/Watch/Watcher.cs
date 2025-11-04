using System.Threading.Channels;
using JetBrains.Annotations;
using Sharplings.Terminal;
using Sharplings.Utils;
using Spectre.Console;

namespace Sharplings.Watch;

static class Watcher {
    const string NotifyError =
        """
        
        The automatic detection of exercise file changes failed :(
        Please try running `Sharplings` again.

        If you keep getting this error, run `Sharplings --manual-run` to deactivate the file watcher.
        You need to manually trigger running the current exercise using `r` then.
        
        """;

    public static async Task StartWatch(AppState appState, bool manualRun, CancellationToken cancellationToken) {
        // todo https://github.com/rust-lang/rustlings/blob/f80fbca12e47014a314e5e931678529c28cd9fd8/src/watch.rs#L156-L172
        // I don't know why the code in Rustlings in the link above is needed, and will not implement it now

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

        WatchState watchState = new(appState, watchEventWriter, manualRun, cancellationToken);
        await watchState.RunCurrentExercise();

        try {
            await foreach (IWatchEvent watchEvent in watchEventReader.ReadAllAsync(cancellationToken)) {
                switch (watchEvent) {
                    case Input { InputEvent: InputEvent.Next }: {
                        switch (await watchState.NextExercise()) {
                            case ExercisesProgress.AllDone: goto ExitLoop;
                            case ExercisesProgress.CurrentPending: break;

                            case ExercisesProgress.NewPending:
                                await watchState.RunCurrentExercise();
                                break;

                            default: throw new ArgumentOutOfRangeException();
                        }

                        break;
                    }

                    case Input { InputEvent: InputEvent.Run }: {
                        await watchState.RunCurrentExercise();
                        break;
                    }

                    case Input { InputEvent: InputEvent.Hint }: {
                        // todo
                        break;
                    }

                    case Input { InputEvent: InputEvent.List }: {
                        return WatchExit.List;
                    }

                    case Input { InputEvent: InputEvent.CheckAll }: {
                        // todo
                        break;
                    }

                    case Input { InputEvent: InputEvent.Reset }: {
                        // todo
                        break;
                    }

                    case Input { InputEvent: InputEvent.Quit }: {
                        AnsiConsole.WriteLine("""
                        
                        We hope you're enjoying learning C#!
                        If you want to continue working on the exercises at a later point, you can simply run `Sharplings` again in this directory.                      
                        """);
                        goto ExitLoop;
                    }

                    case FileChange(var exerciseIndex): {
                        await watchState.HandleFileChange(exerciseIndex);
                        break;
                    }

                    case TerminalResize(var width): {

                        break;
                    }

                    case NotifyErr(var exception): {
                        throw new AggregateException(NotifyError, exception);
                    }

                    case TerminalEventErr(var exception): {
                        throw new AggregateException("Terminal event listener failed", exception);
                    }

                    default: throw new ArgumentOutOfRangeException(nameof(watchEvent));
                }
            }
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }

        ExitLoop:
        return WatchExit.Shutdown;
    }

    /// Returned by the watch mode to indicate what to do afterward.
    enum WatchExit {
        /// Exit the program.
        Shutdown,
        /// Enter the list mode and restart the watch mode afterward.
        List
    }
}
