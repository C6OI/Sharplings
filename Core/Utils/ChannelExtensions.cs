using System.Threading.Channels;

namespace Sharplings.Utils;

static class ChannelExtensions {
    extension<T>(Channel<T> channel) {
        public void Deconstruct(out ChannelWriter<T> writer, out ChannelReader<T> reader) {
            writer = channel.Writer;
            reader = channel.Reader;
        }
    }
}
