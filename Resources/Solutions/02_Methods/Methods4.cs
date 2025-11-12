using System;

class Methods4 {
    public static void Main() {
        int originalPrice = 51;
        Console.WriteLine($"Your sale price is {SalePrice(originalPrice)}");
    }

    // The return type is int.
    static int SalePrice(int price) {
        if (IsEven(price)) {
            return price - 10;
        }

        return price - 3;
    }

    static bool IsEven(int num) {
        return num % 2 == 0;
    }
}
