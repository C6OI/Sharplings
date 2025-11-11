namespace Sharplings.Utils;

public static class EnumerableExtensions {
    extension<T>(IEnumerable<T> enumerable) {
        public int FindIndex(Func<T, bool> match) {
            if (enumerable is List<T> list)
                return list.FindIndex(e => match(e));

            int index = -1;

            foreach (T element in enumerable) {
                checked { index++; }
                if (match(element)) return index;
            }

            return -1;
        }
    }
}
