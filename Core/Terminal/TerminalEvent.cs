using System.Threading.Channels;
using Sharplings.Watch;

namespace Sharplings.Terminal;

static class TerminalEvent {
    public static async Task TerminalEventHandler(ChannelWriter<IWatchEvent> watchEventWriter, ChannelReader<byte> unpauseReader, bool manualRun) {
        // todo
    }
}
