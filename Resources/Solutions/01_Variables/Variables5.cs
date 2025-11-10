using System;

class Variables5 {
    public static void Main() {
        // Constant values should be known at compile-time.
        const int x = 34;
        const int y = x + 8;

        Console.WriteLine($"Number {y}");
    }
}
