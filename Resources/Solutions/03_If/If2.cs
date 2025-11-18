using System;

class If2 {
    public static void Main() => RunTests();

    static string PickyEater(string food) {
        if (food == "strawberry") {
            return "Yummy!";
        } else if (food == "potato") {
            return "I guess I can eat that.";
        } else {
            return "No thanks!";
        }

        // You can also use the `switch`, but we will learn about it later =)
    }

    #region Tests

    static void RunTests() {
        YummyFood();
        NeutralFood();
        DefaultDislikedFood();
    }

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
