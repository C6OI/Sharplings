using System.Diagnostics;
using System.Text;
using Spectre.Console;

namespace Sharplings.Terminal;

static class Progress {
    public static string BuildProgressBar(int currentValue, int maxValue, int termWidth) {
        Debug.Assert(maxValue <= 999);
        Debug.Assert(currentValue <= maxValue);

        const string prefix = "Progress: [";
        const int prefixWidth = 11;
        const int postfixWidth = 9;
        const int wrapperWidth = prefixWidth + postfixWidth;
        const int minLineWidth = wrapperWidth + 4;

        if (termWidth < minLineWidth)
            return $"Progress: {{currentValue}}/{maxValue}";

        StringBuilder builder = new(Markup.Escape(prefix), termWidth);

        int width = termWidth - wrapperWidth;
        int filled = width * currentValue / maxValue;

        builder.Append("[lime]");
        builder.Append('#', filled);

        if (filled < width)
            builder.Append('>');

        builder.Append("[/]");

        int widthMinusFilled = width - filled;

        if (widthMinusFilled > 1) {
            int redPathWidth = widthMinusFilled - 1;
            builder.Append("[red]");
            builder.Append('-', redPathWidth);
            builder.Append("[/]");
        }

        builder.Append(Markup.Escape($"] {currentValue,3}/{maxValue}"));
        return builder.ToString();
    }
}
