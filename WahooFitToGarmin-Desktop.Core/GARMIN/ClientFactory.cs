using Flurl.Http;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using WahooFitToGarmin_Desktop.Core.GARMIN.Dto;

namespace WahooFitToGarmin_Desktop.Core.GARMIN
{
    public class ClientFactory
    {
        public static IClient Create(string consumerKey, string consumerSecret, ILogger? logger = null)
        {
            return new Client(consumerKey, consumerSecret, logger ?? NullLogger.Instance);
        }

        public static async Task<IClient> Create(ILogger? logger = null)
        {
            logger ??= NullLogger.Instance;

            var keys = await URLs.GARMIN_API_CONSUMER_KEYS
                            .GetAsync()
                            .ReceiveJson<GarminApiConsumerKeys>();

            if (keys?.ConsumerKey is null || keys.ConsumerSecret is null)
            {
                logger.LogError(
                    "Could not parse consumer keys from url: {Url}",
                    URLs.GARMIN_API_CONSUMER_KEYS);

                throw new Exception($"Could not parse consumer keys from url: {URLs.GARMIN_API_CONSUMER_KEYS}");
            }

            return Create(keys.ConsumerKey, keys.ConsumerSecret, logger);
        }
    }
}
