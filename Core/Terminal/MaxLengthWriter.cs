using System.Text;
using Sharplings.Utils;

namespace Sharplings.Terminal;

public class MaxLengthWriter(int maxLength) {
    StringBuilder Builder { get; set; } = new(capacity: maxLength);
    int IgnoreLength { get; set; }

    int RemainingLength => maxLength - Builder.Length.SaturatingSub(IgnoreLength);

    public void Write(string value, bool ignoreLength = false) {
        if (ignoreLength) {
            int oldLength = Builder.Length;
            Builder.Append(value);
            IgnoreLength += Builder.Length - oldLength;
            return;
        }

        int count = value.Length.Min(RemainingLength);

        if (count > 0) {
            Builder.Append(value, 0, count);
        }
    }

    public void FillRemainingLength(char ch) {
        if (RemainingLength == 0) return;

        string fill = new(ch, RemainingLength);
        Write(fill);
    }

    public string Build() => Builder.ToString();

    public void Reset() {
        Builder = new StringBuilder(capacity: maxLength);
        IgnoreLength = 0;
    }

    public override string ToString() => Build();

    public static implicit operator string(MaxLengthWriter writer) => writer.ToString();
}
