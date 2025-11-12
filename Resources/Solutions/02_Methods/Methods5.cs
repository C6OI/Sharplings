using System;

class Methods5 {
    public static void Main() {
        int num = 3;
        Console.WriteLine($"The square of {num} is {Square(num)}");
    }

    // You can use expression-bodied method.
    // https://learn.microsoft.com/dotnet/csharp/methods#expression-bodied-members
    static int Square(int num) => num * num;
}
