namespace Sharplings.Utils;

public static class EnumerableExtensions {
    public static int FindIndex<T>(this IEnumerable<T> enumerable, Func<T, bool> match) {
        if (enumerable is List<T> list)
            return list.FindIndex(e => match(e));

        int index = -1;

        foreach (T element in enumerable) {
            checked { index++; };
            if (match(element)) return index;
        }

        return -1;
    }
}
