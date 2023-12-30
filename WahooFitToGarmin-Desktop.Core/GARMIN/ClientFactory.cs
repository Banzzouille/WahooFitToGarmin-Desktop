using System;
using System.Threading.Tasks;
using Flurl.Http;
using WahooFitToGarmin_Desktop.Core.GARMIN.Dto;

namespace WahooFitToGarmin_Desktop.Core.GARMIN
{
    public class ClientFactory
    {

        public static IClient Create(string consumerKey, string consumerSecret)
        {
            var client = new Client(consumerKey, consumerSecret);
            return client;
        }

        public static async Task<IClient> Create()
        {
            var keys = await URLs.GARMIN_API_CONSUMER_KEYS
                            .GetAsync()
                            .ReceiveJson<GarminApiConsumerKeys>();

            if(keys == null)
            {
                throw new Exception($"Could not parse consumer keys from url: {URLs.GARMIN_API_CONSUMER_KEYS}");
                
            }
            var client = Create(keys.ConsumerKey, keys.ConsumerSecret);
            return client;
        }

       
    }
}
