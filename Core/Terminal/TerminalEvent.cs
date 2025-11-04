using System.Threading.Channels;
using Sharplings.Watch;
using Spectre.Console;

namespace Sharplings.Terminal;

static class TerminalEvent {
    public static async Task TerminalEventHandler(
        ChannelWriter<IWatchEvent> watchEventWriter,
        ChannelReader<byte> unpauseReader,
        bool manualRun,
        CancellationToken cancellationToken) {
        Task widthWatcher = WidthWatcher(watchEventWriter, cancellationToken);

        try {
            while (!cancellationToken.IsCancellationRequested) {
                ConsoleKeyInfo key = Console.ReadKey(true);
                AnsiConsole.WriteLine(key.KeyChar);

                if (InputPauseGuard.ExerciseRunning) continue;

                InputEvent inputEvent = key.KeyChar switch {
                    'n' => InputEvent.Next,
                    'r' when manualRun => InputEvent.Run,
                    'h' => InputEvent.Hint,
                    'l' => InputEvent.List,
                    'c' => InputEvent.CheckAll,
                    'x' => InputEvent.Reset,
                    'q' => InputEvent.Quit,
                    _ => InputEvent.None
                };

                if (inputEvent == InputEvent.None) continue;

                if (inputEvent is InputEvent.List or InputEvent.Quit) {
                    await watchEventWriter.WriteAsync(new Input(inputEvent), cancellationToken);
                    break;
                }

                if (inputEvent is InputEvent.Reset) {
                    await watchEventWriter.WriteAsync(new Input(inputEvent), cancellationToken);

                    // Pause input until quitting the confirmation prompt.
                    await unpauseReader.ReadAsync(cancellationToken);
                    continue;
                }

                await watchEventWriter.WriteAsync(new Input(inputEvent), cancellationToken);
            }
        } catch (ChannelClosedException) { }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception e) {
            try {
                await watchEventWriter.WriteAsync(new TerminalEventErr(e), cancellationToken);
            } catch (Exception) { /**/ }
        } finally {
            await widthWatcher;
        }
    }

    static async Task WidthWatcher(ChannelWriter<IWatchEvent> watchEventWriter, CancellationToken cancellationToken) {
        const int pollIntervalMs = 200;
        int lastWidth = AnsiConsole.Profile.Width;

        try {
            while (!cancellationToken.IsCancellationRequested) {
                int currentWidth = AnsiConsole.Profile.Width;

                if (currentWidth != lastWidth) {
                    try {
                        await watchEventWriter.WriteAsync(new TerminalResize(currentWidth), cancellationToken);
                    } catch (ChannelClosedException) {
                        break;
                    }

                    lastWidth = currentWidth;
                }

                await Task.Delay(pollIntervalMs, cancellationToken);
            }
        } catch (OperationCanceledException) when(cancellationToken.IsCancellationRequested) { }
    }
}

enum InputEvent {
    None,
    Next,
    Run,
    Hint,
    List,
    CheckAll,
    Reset,
    Quit,
}
