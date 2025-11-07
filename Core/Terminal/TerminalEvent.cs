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
        int currentWidth = AnsiConsole.Profile.Width;

        try {
            await foreach (ITerminalEvent terminalEvent in TerminalHandler.EventReader.ReadAllAsync(cancellationToken)) {
                switch (terminalEvent) {
                    case Key(var key): {
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
                        break;
                    }

                    case Resize(var width, _): {
                        if (currentWidth == width) break;

                        await watchEventWriter.WriteAsync(new TerminalResize(width), cancellationToken);
                        currentWidth = width;
                        break;
                    }

                    default: throw new ArgumentOutOfRangeException(nameof(terminalEvent));
                }
            }
        } catch (ChannelClosedException) { }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception e) {
            try {
                await watchEventWriter.WriteAsync(new TerminalEventErr(e), cancellationToken);
            } catch (Exception) { /**/ }
        }
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
