using Dynastream.Fit;

namespace WahooFitToGarmin_Desktop.Core.DeviceEmulation
{
    /// <summary>
    /// Reads and writes FIT files, preserving everything the decoder does not
    /// model.
    /// </summary>
    /// <remarks>
    /// A real Wahoo export carries around nine hundred messages of types the
    /// specification does not define, and sixteen thousand developer field
    /// values. Both survive because messages are kept as they arrive, rather
    /// than being projected onto typed views and rebuilt from those.
    /// </remarks>
    public static class FitCodec
    {
        public static List<Mesg> Decode(byte[] content)
        {
            var messages = new List<Mesg>();

            using var input = new MemoryStream(content);
            var decode = new Decode();
            decode.MesgEvent += (_, e) => messages.Add(e.mesg);

            if (!decode.Read(input))
            {
                throw new InvalidOperationException("the content is not a readable FIT file");
            }

            return messages;
        }

        public static byte[] Encode(IEnumerable<Mesg> messages)
        {
            using var output = new MemoryStream();

            var encode = new Encode(ProtocolVersion.V20);
            encode.Open(output);

            foreach (var message in messages)
            {
                encode.Write(message);
            }

            encode.Close();

            return output.ToArray();
        }
    }
}
