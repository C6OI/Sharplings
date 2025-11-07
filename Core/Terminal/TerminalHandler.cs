using System.Threading.Channels;
using Sharplings.Utils;
using Spectre.Console;

namespace Sharplings.Terminal;

static class TerminalHandler {
    static TerminalHandler() {
        (EventWriter, EventReader) = Channel.CreateUnbounded<ITerminalEvent>(new UnboundedChannelOptions {
            SingleReader = true // we assume that there will be only one reader at a time
        });

        SizeWatcherTask = Task.Run(SizeWatcher);
        InputWatcherTask = Task.Run(InputWatcher);
    }

    public static ChannelReader<ITerminalEvent> EventReader { get; }
    static ChannelWriter<ITerminalEvent> EventWriter { get; }

    static Task SizeWatcherTask { get; }
    static Task InputWatcherTask { get; }

    static async Task InputWatcher() {
        while (true) {
            ConsoleKeyInfo keyInfo = Console.ReadKey(true);
            await EventWriter.WriteAsync(new Key(keyInfo));
        }
    }

    static async Task SizeWatcher() {
        const int pollIntervalMs = 50;
        int lastWidth = AnsiConsole.Profile.Width;
        int lastHeight = AnsiConsole.Profile.Height;

        while (true) {
            int currentWidth = AnsiConsole.Profile.Width;
            int currentHeight = AnsiConsole.Profile.Height;

            if (currentWidth != lastWidth || currentHeight != lastHeight) {
                await EventWriter.WriteAsync(new Resize(currentWidth, currentHeight));

                lastWidth = currentWidth;
                lastHeight = currentHeight;
            }

            await Task.Delay(pollIntervalMs);
        }
    }
}

interface ITerminalEvent;

record struct Resize(int Width, int Height) : ITerminalEvent;

record struct Key(ConsoleKeyInfo KeyInfo) : ITerminalEvent;
