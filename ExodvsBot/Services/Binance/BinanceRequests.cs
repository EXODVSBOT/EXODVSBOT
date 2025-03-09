using Binance.Net.Clients;
using Binance.Net.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ExodvsBot.Services.Binance
{
    public class BinanceRequests
    {
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly BinanceRestClient _client;

        public BinanceRequests(string apiKey, string apiSecret)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _client = new BinanceRestClient();
        }


        public async Task<decimal> GetAssetPrice()
        {
            var result = await _client.SpotApi.ExchangeData.GetTickerAsync("BTCUSDT");

            if (result.Success)
            {
                return result.Data.LastPrice;
            }
            else
            {
                Console.WriteLine($"Erro ao buscar preço do Bitcoin: {result.Error}");
                return 0;
            }
        }

        public async Task<List<decimal>> GetHistoricalPrices(string symbol, KlineInterval interval, int limit)
        {
            var result = await _client.SpotApi.ExchangeData.GetKlinesAsync(symbol, interval, limit: limit);

            if (result.Success)
            {
                return result.Data.Select(k => k.ClosePrice).ToList();
            }
            else
            {
                Console.WriteLine($"Erro ao buscar preço do Bitcoin: {result.Error}");
                return new List<decimal>();
            }
        }

        //Busca do volume
        public async Task<List<decimal>> GetVolumeData(string symbol, KlineInterval interval, int limit)
        {
            var result = await _client.SpotApi.ExchangeData.GetKlinesAsync(symbol, interval, limit: limit);

            if (result.Success)
            {
                return result.Data.Select(k => k.Volume).ToList(); // Retorna a lista de volumes
            }
            else
            {
                return new List<decimal>();
            }
        }

        public async Task<(decimal High, decimal Low)> GetCurrentHighAndLowPrice(string symbol, KlineInterval interval)
        {

            var result = await _client.SpotApi.ExchangeData.GetKlinesAsync(symbol, interval, limit: 1);

            if (result.Success)
            {
                var kline = result.Data.First(); // Obtém a última vela
                return (kline.HighPrice, kline.LowPrice);
            }
            else
            {
                return (0, 0);
            }
        }

        public async Task<List<decimal>> GetLastHighPrices(string symbol, KlineInterval interval, int limit)
        {

            var result = await _client.SpotApi.ExchangeData.GetKlinesAsync(symbol, interval, limit: limit);

            if (result.Success)
            {
                return result.Data.Select(k => k.HighPrice).ToList(); // Retorna a lista de preços máximos
            }
            else
            {
                return new List<decimal>();
            }
        }

        public async Task<decimal> GetBalance(string asset)
        {
            try
            {
                var accountInfo = await _client.SpotApi.Account.GetAccountInfoAsync();

                if (accountInfo.Success)
                {
                    var USDT = accountInfo.Data.Balances.FirstOrDefault(x => x.Asset == "USDT");
                    return USDT.Total;
                }

                Console.WriteLine($"Erro ao obter informações da conta: {accountInfo.Error}");
                return 0.00m;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar saldo: {ex.Message}");
                throw;
            }
        }


    }
}
