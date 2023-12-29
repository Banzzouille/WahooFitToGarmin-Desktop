﻿using System;
using System.Threading.Tasks;
using Flurl.Http;
using NLog;
using WahooFitToGarmin_Desktop.Core.GARMIN.Dto;

namespace WahooFitToGarmin_Desktop.Core.GARMIN
{
    public class ClientFactory
    {

        private static ILogger _logger => NLog.LogManager.GetLogger("ClientFactory");
        public static IClient Create(string consumerKey, string consumerSecret)
        {
            Logger.CreateLogger();
            var client = new Client(consumerKey, consumerSecret);
            return client;
        }

        public static async Task<IClient> Create()
        {
            Logger.CreateLogger();
            var keys = await URLs.GARMIN_API_CONSUMER_KEYS
                            .GetAsync()
                            .ReceiveJson<GarminApiConsumerKeys>();

            if(keys == null)
            {
                _logger.Error($"Could not parse consumer keys from url: {URLs.GARMIN_API_CONSUMER_KEYS}");
                throw new Exception($"Could not parse consumer keys from url: {URLs.GARMIN_API_CONSUMER_KEYS}");
                
            }
            _logger.Info("Consumer Keys received");
            var client = Create(keys.ConsumerKey, keys.ConsumerSecret);
            return client;
        }

       
    }
}
