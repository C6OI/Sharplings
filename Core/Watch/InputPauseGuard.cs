using JetBrains.Annotations;

namespace Sharplings.Watch;

[MustDisposeResource]
public readonly struct InputPauseGuard : IDisposable {
    static volatile uint _exerciseRunning;

    public InputPauseGuard() => Interlocked.Increment(ref _exerciseRunning);

    public static bool ExerciseRunning => Interlocked.CompareExchange(ref _exerciseRunning, 0, 0) != 0;

    public void Dispose() => Interlocked.Decrement(ref _exerciseRunning);
}
