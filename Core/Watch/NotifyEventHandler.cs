using System.Threading.Channels;
using Sharplings.Utils;

namespace Sharplings.Watch;

class NotifyEventHandler {
    const int DebounceDurationMs = 200;

    public NotifyEventHandler(ChannelWriter<IWatchEvent> watchEventWriter, IList<string> exerciseNames, CancellationToken cancellationToken) {
        (UpdateWriter, ChannelReader<int> updateReader) = Channel.CreateBounded<int>(new BoundedChannelOptions(1) {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait
        });

        WatchEventWriter = watchEventWriter;
        ExerciseNames = exerciseNames;

        DebounceTask = Task.Run(() => DebounceLoop(updateReader, cancellationToken), cancellationToken);
    }

    public Task DebounceTask { get; }
    ChannelWriter<IWatchEvent> WatchEventWriter { get; }
    ChannelWriter<int> UpdateWriter { get; }
    IList<string> ExerciseNames { get; }

    async Task DebounceLoop(ChannelReader<int> updateReader, CancellationToken cancellationToken) {
        bool[] exercisesUpdated = new bool[ExerciseNames.Count];

        Task<bool>? readWaitTask = null;

        while (!cancellationToken.IsCancellationRequested) {
            try {
                readWaitTask ??= updateReader.WaitToReadAsync(cancellationToken).AsTask();
                Task debounceWaitTask = Task.Delay(DebounceDurationMs, cancellationToken);

                Task task = await Task.WhenAny(readWaitTask, debounceWaitTask);

                if (task == readWaitTask) { // signal from the channel
                    bool readAvailable = await readWaitTask;
                    if (!readAvailable) break;

                    if (!updateReader.TryRead(out int exerciseIndex))
                        throw new InvalidOperationException($"WaitToReadAsync returned {readAvailable}, but TryRead returned false");

                    exercisesUpdated[exerciseIndex] = true;
                    readWaitTask = null;
                } else { // debounce timeout
                    for (int i = 0; i < exercisesUpdated.Length; i++) {
                        if (!exercisesUpdated[i]) continue;
                        await WatchEventWriter.WriteAsync(new FileChange(i), cancellationToken);
                        exercisesUpdated[i] = false;
                    }
                }
            } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                break;
            } catch (Exception e) {
                await HandleException(e);
                break;
            }
        }

        UpdateWriter.Complete();
    }

    public async void OnFileSystemWatcherEvent(object _, FileSystemEventArgs e) {
        try {
            if (InputPauseGuard.ExerciseRunning) return;

            string exerciseName = Path.GetFileNameWithoutExtension(e.FullPath);
            int exerciseIndex = ExerciseNames.IndexOf(exerciseName);

            await UpdateWriter.WriteAsync(exerciseIndex);
        } catch (Exception ex) {
            await HandleException(ex);
        }
    }

    public void FileSystemWatcherOnError(object sender, ErrorEventArgs e) =>
        _ = HandleException(e.GetException());

    async Task HandleException(Exception e) =>
        await WatchEventWriter.WriteAsync(new NotifyErr(e));
}
