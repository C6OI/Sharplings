using System;

class If1 {
    public static void Main() {
        RunTests();

        // You can optionally experiment here.
    }

    static int Bigger(int a, int b) {
        // TODO: Complete this method to return the bigger number!
        // If both numbers are equal, any of them can be returned.
        // Do not use:
        // - another method call
        // - additional variables
    }

    // Don't mind this for now :)
    #region Tests

    static void RunTests() {
        TenIsBiggerThanEight();
        FortyTwoIsBiggerThanThirtyTwo();
        EqualNumbers();
    }

    static void TenIsBiggerThanEight() =>
        ArgumentOutOfRangeException.ThrowIfNotEqual(Bigger(10, 8), 10);

    static void FortyTwoIsBiggerThanThirtyTwo() =>
        ArgumentOutOfRangeException.ThrowIfNotEqual(Bigger(32, 42), 42);

    static void EqualNumbers() =>
        ArgumentOutOfRangeException.ThrowIfNotEqual(Bigger(42, 42), 42);

    #endregion
}
