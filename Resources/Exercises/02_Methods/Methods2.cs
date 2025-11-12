using System;

class Methods2 {
    public static void Main() {
        CallMe(3);
    }

    // TODO: Replace the `???` with missing type of the argument `num`.
    static void CallMe(??? num) {
        for (int i = 0; i < num; i++) {
            Console.WriteLine($"Ring! Call number {i + 1}");
        }
    }
}
