using System;

class Variables3 {
    public static void Main() {
        // Reading uninitialized variables isn't allowed in C#!
        // Therefore, we need to assign a value first.
        int x = 42;

        Console.WriteLine($"Number {x}");

        // It is possible to declare a variable and initialize it later.
        // But it can't be used before initialization.
        int y;
        y = 42;
        Console.WriteLine($"Number {y}");
    }
}
