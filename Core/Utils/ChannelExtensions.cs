using System.Threading.Channels;

namespace Sharplings.Utils;

public static class ChannelExtensions {
    public static void Deconstruct<T>(this Channel<T> channel, out ChannelWriter<T> writer, out ChannelReader<T> reader) {
        writer = channel.Writer;
        reader = channel.Reader;
    }
}
