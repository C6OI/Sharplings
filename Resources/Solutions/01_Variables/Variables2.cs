using System;

class Variables2 {
    public static void Main() {
        // The easiest way to fix the compiler error is to initialize the
        // variable `x`. By setting its value to an integer, C# infers its type
        // as `int` which is the default type for integers.
        var x = 42;

        // But we can enforce a type different from the default `int` by
        // specifying the type explicitly:
        // ushort x = 42;

        if (x == 10) {
            Console.WriteLine($"x is ten!");
        } else {
            Console.WriteLine($"x is not ten!");
        }
    }
}
