using System;

class If2 {
    public static void Main() {
        RunTests();

        // You can optionally experiment here.
    }

    // TODO: Fix the compiler error on this method.
    static string PickyEater(string food) {
        if (food == "strawberry") {
            return "Yummy!";
        } else {
            return 1;
        }
    }

    // TODO: Read the tests to understand the desired behavior.
    // Make all tests pass without changing them.
    #region Tests

    static void RunTests() {
        YummyFood();
        NeutralFood();
        DefaultDislikedFood();
    }

    // This means that calling `PickyEater` with the argument "strawberry" should return "Yummy!".
    static void YummyFood() =>
        ArgumentOutOfRangeException.ThrowIfNotEqual(PickyEater("strawberry"), "Yummy!");

    static void NeutralFood() =>
        ArgumentOutOfRangeException.ThrowIfNotEqual(PickyEater("potato"), "I guess I can eat that.");

    static void DefaultDislikedFood() {
        ArgumentOutOfRangeException.ThrowIfNotEqual(PickyEater("broccoli"), "No thanks!");
        ArgumentOutOfRangeException.ThrowIfNotEqual(PickyEater("gummy bears"), "No thanks!");
        ArgumentOutOfRangeException.ThrowIfNotEqual(PickyEater("literally anything"), "No thanks!");
    }

    #endregion
}
