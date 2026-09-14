using System.Collections;
using System.Text.Json;

namespace WahooFitToGarmin_Desktop.Core.Helpers
{
    /// <summary>
    /// Converts between the application's untyped property bag and the typed
    /// shape <c>System.Text.Json</c> can serialize.
    /// </summary>
    /// <remarks>
    /// This is the one genuine incompatibility in moving off Newtonsoft:
    /// <c>System.Text.Json</c> has no support for the non-generic
    /// <see cref="IDictionary"/> the user interface stores settings in, and
    /// writes nothing useful for it.
    ///
    /// The logic lives here, rather than beside its caller, because it is pure,
    /// it carries the backward-compatibility rules for files written by earlier
    /// versions, and a platform-neutral home is the only one the tests can
    /// reach.
    /// </remarks>
    public static class PropertyBagSerialization
    {
        /// <summary>
        /// Projects an untyped property bag to a typed dictionary for writing.
        /// Entries without a usable key are dropped; null values become empty
        /// strings.
        /// </summary>
        public static Dictionary<string, string> Project(IDictionary properties)
        {
            var projected = new Dictionary<string, string>();

            foreach (DictionaryEntry entry in properties)
            {
                var key = entry.Key?.ToString();
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                projected[key] = entry.Value?.ToString() ?? string.Empty;
            }

            return projected;
        }

        /// <summary>
        /// Renders a stored value as the string the application expects.
        /// </summary>
        /// <remarks>
        /// Files written by version 1.1.0 stored the keep-uploaded-file option
        /// as a JSON boolean rather than a string, because the previous
        /// serializer wrote the property bag's values as they came. Reading
        /// straight into a string dictionary fails on exactly those files, so
        /// values are flattened rather than assumed to be strings.
        /// </remarks>
        public static string Flatten(JsonElement value) => value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
            _ => value.GetRawText(),
        };
    }
}
