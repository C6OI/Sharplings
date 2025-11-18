using System;

class If3 {
    public static void Main() {
        RunTests();

        // You can optionally experiment here.
    }

    static string AnimalHabitat(string animal) {
        // TODO: Fix the compiler error in the statement below.
        int identifier = animal == "crab" ? 1
            : animal == "gopher" ? 2.0
            : animal == "snake" ? 3
            : "Unknown";

        // Don't change the code below!
        if (identifier == 1) return "Beach";
        else if (identifier == 2) return "Burrow";
        else if (identifier == 3) return "Desert";
        else return "Unknown";
    }

    // Don't change the tests!
    #region Tests

    static void RunTests() {
        GopherLivesInBurrow();
        SnakeLivesInDesert();
        CrabLivesOnBeach();
        UnknownAnimal();
    }

    static void GopherLivesInBurrow() =>
        ArgumentOutOfRangeException.ThrowIfNotEqual(AnimalHabitat("gopher"), "Burrow");

    static void SnakeLivesInDesert() =>
        ArgumentOutOfRangeException.ThrowIfNotEqual(AnimalHabitat("snake"), "Desert");

    static void CrabLivesOnBeach() =>
        ArgumentOutOfRangeException.ThrowIfNotEqual(AnimalHabitat("crab"), "Beach");

    static void UnknownAnimal() =>
        ArgumentOutOfRangeException.ThrowIfNotEqual(AnimalHabitat("dinosaur"), "Unknown");

    #endregion
}
