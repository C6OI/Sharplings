using System.Numerics;

namespace Sharplings.Utils;

public static class StdExtensions {
    public static T SaturatingSub<T>(this T lhs, T rhs) where T : IComparisonOperators<T, T, bool>, INumberBase<T> =>
        lhs > rhs ? lhs - rhs : T.Zero;

    public static T Into<T>(this bool value) where T : INumberBase<T> =>
        value ? T.One : T.Zero;

    public static T Min<T>(this T lhs, T rhs) where T : IComparisonOperators<T, T, bool> =>
        lhs > rhs ? rhs : lhs;

    public static T Max<T>(this T lhs, T rhs) where T : IComparisonOperators<T, T, bool> =>
        lhs > rhs ? lhs : rhs;
}
