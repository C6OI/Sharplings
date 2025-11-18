using System;

class If1 {
    public static void Main() => RunTests();

    static int Bigger(int a, int b) {
        if (a > b) return a;
        else return b;

        // In the above, the `else` statement can be safely removed because
        // its if-clause returns from the method:
        // if (a > b) return a;
        // return b;

        // Or you can use the ternary operator:
        // return a > b ? a : b;
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
