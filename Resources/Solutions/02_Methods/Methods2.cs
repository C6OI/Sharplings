using System;

class Methods2 {
    public static void Main() {
        CallMe(3);
    }

    // The type of `num` is `int`.
    static void CallMe(int num) {
        for (int i = 0; i < num; i++) {
            Console.WriteLine($"Ring! Call number {i + 1}");
        }
    }
}
