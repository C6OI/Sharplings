using JetBrains.Annotations;

namespace Sharplings.Terminal;

[PublicAPI]
public class TerminalResizedEventArgs(
    int oldWidth,
    int oldHeight,
    int newWidth,
    int newHeight
) : EventArgs {
    public int OldWidth { get; } = oldWidth;
    public int OldHeight { get; } = oldHeight;
    public int NewWidth { get; } = newWidth;
    public int NewHeight { get; } = newHeight;

    public bool WidthChanged => OldWidth != NewWidth;
    public bool HeightChanged => OldHeight != NewHeight;
    public bool BothChanged => WidthChanged && HeightChanged;
}
