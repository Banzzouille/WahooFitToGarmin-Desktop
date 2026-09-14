using System.Text;

namespace WahooFitToGarmin_Desktop.Core.Helpers
{
    internal static class StringExtensions
    {
        public static string? EncodeBase64(this string? plainText, Encoding? encoding = null)
        {
            if (plainText is null) return null;

            // use UTF8 as default encoding type
            encoding ??= Encoding.UTF8;
            var bytes = encoding.GetBytes(plainText);
            return Convert.ToBase64String(bytes);
        }

        public static string? DecodeBase64(this string? base64Text, Encoding? encoding = null)
        {
            if (base64Text is null) return null;

            encoding ??= Encoding.UTF8;
            var bytes = Convert.FromBase64String(base64Text);
            return encoding.GetString(bytes);
        }
    }
}
