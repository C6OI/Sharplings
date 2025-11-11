using System.Numerics;

namespace Sharplings.Utils;

public static class StdExtensions {
    extension<T>(T lhs) where T : IComparisonOperators<T, T, bool>, INumberBase<T> {
        public T SaturatingSub(T rhs) =>
            lhs > rhs ? lhs - rhs : T.Zero;
    }

    extension<T>(T lhs) where T : IComparisonOperators<T, T, bool> {
        public T Min(T rhs) =>
            lhs > rhs ? rhs : lhs;

        public T Max(T rhs) =>
            lhs > rhs ? lhs : rhs;
    }

    extension(bool value) {
        public T Into<T>() where T : INumberBase<T> =>
            value ? T.One : T.Zero;
    }
}
