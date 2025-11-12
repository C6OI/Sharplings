using System;

class Methods3 {
    public static void Main() {
        // TODO: Fix the method call.
        CallMe();
    }

    static void CallMe(byte num) {
        for (int i = 0; i < num; i++) {
            Console.WriteLine($"Ring! Call number {i + 1}");
        }
    }
}
