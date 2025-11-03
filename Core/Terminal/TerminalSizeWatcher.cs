using JetBrains.Annotations;
using Spectre.Console;

namespace Sharplings.Terminal;

[PublicAPI]
class TerminalSizeWatcher { // Not a good solution, but I haven't found anything better :(
    const int PollIntervalMs = 50;

    bool _isRunning;
    CancellationTokenSource CancellationTokenSource { get; set; } = null!;
    Task? WatchTask { get; set; }

    public Task StartAsync(Action<TerminalResizedEventArgs> onResize, CancellationToken cancellationToken = default) =>
        StartAsync(async args => onResize(args), cancellationToken);

    public Task StartAsync(Func<TerminalResizedEventArgs, Task> onResize, CancellationToken cancellationToken = default) {
        if (Interlocked.CompareExchange(ref _isRunning, true, false))
            throw new InvalidOperationException("This instance of watcher is already running");

        CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        CancellationToken token = CancellationTokenSource.Token;

        return WatchTask = Task.Run(async () => {
            int lastWidth = AnsiConsole.Profile.Width;
            int lastHeight = AnsiConsole.Profile.Height;

            while (!token.IsCancellationRequested) {
                int currentWidth = AnsiConsole.Profile.Width;
                int currentHeight = AnsiConsole.Profile.Height;

                if (currentWidth != lastWidth || currentHeight != lastHeight) {
                    await onResize(new TerminalResizedEventArgs(lastWidth, lastHeight, currentWidth, currentHeight));

                    lastWidth = currentWidth;
                    lastHeight = currentHeight;
                }

                try {
                    await Task.Delay(PollIntervalMs, token);
                } catch (OperationCanceledException) {
                    break;
                }
            }
        }, token);
    }

    public async Task StopAsync() {
        await CancellationTokenSource.CancelAsync();
        if (WatchTask != null) await WatchTask;
        WatchTask = null;

        Interlocked.Exchange(ref _isRunning, false);
    }
}
