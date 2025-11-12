using System;

// This store is having a sale where if the price is an even number, you get 10
// Sharpbucks off, but if it's an odd number, it's 3 Sharpbucks off.
// Don't worry about the function bodies themselves, we are only interested in
// the signatures for now.

class Methods4 {
    public static void Main() {
        int originalPrice = 51;
        Console.WriteLine($"Your sale price is {SalePrice(originalPrice)}");
    }

    // TODO: Fix the function signature.
    static void SalePrice(int price) {
        if (IsEven(price)) {
            return price - 10;
        }

        return price - 3;
    }

    static bool IsEven(int num) => num % 2 == 0;
}
