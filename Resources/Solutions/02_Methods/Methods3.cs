using System;

class Methods3 {
    public static void Main() {
        // `CallMe` expects an argument.
        CallMe(5);
    }

    static void CallMe(byte num) {
        for (int i = 0; i < num; i++) {
            Console.WriteLine($"Ring! Call number {i + 1}");
        }
    }
}
