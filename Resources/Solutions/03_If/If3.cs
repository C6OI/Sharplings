using System;

class If3 {
    public static void Main() => RunTests();

    static string AnimalHabitat(string animal) {
        int identifier = animal == "crab" ? 1
            : animal == "gopher" ? 2
            : animal == "snake" ? 3
            : 4; // Any unused identifier.

        // Instead of such an identifier, you would use an enum in C#.
        // But we didn't get into enums yet.
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
