using Binance.Net.Clients;
using Binance.Net.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExodvsBot.Binance
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
            return await ExecuteWithRetry(async () =>
            {
                var result = await _client.SpotApi.ExchangeData.GetTickerAsync("BTCUSDT");

                if (result.Success)
                {
                    return result.Data.LastPrice;
                }
                else
                {
                    throw new Exception($"Erro ao buscar preço do Bitcoin: {result.Error}");
                }
            });
        }

        public async Task<List<decimal>> GetHistoricalPrices(string symbol, KlineInterval interval, int limit)
        {
            return await ExecuteWithRetry(async () =>
            {
                var result = await _client.SpotApi.ExchangeData.GetKlinesAsync(symbol, interval, limit: limit);

                if (result.Success)
                {
                    return result.Data.Select(k => k.ClosePrice).ToList();
                }
                else
                {
                    throw new Exception($"Erro ao buscar preços históricos: {result.Error}");
                }
            });
        }

        //Busca do volume
        public async Task<List<decimal>> GetVolumeData(string symbol, KlineInterval interval, int limit)
        {
            return await ExecuteWithRetry(async () =>
            {
                var result = await _client.SpotApi.ExchangeData.GetKlinesAsync(symbol, interval, limit: limit);

                if (result.Success)
                {
                    return result.Data.Select(k => k.Volume).ToList(); // Retorna a lista de volumes
                }
                else
                {
                    throw new Exception($"Erro ao buscar dados de volume: {result.Error}");
                }
            });
        }

        public async Task<(decimal High, decimal Low)> GetCurrentHighAndLowPrice(string symbol, KlineInterval interval)
        {
            return await ExecuteWithRetry(async () =>
            {
                var result = await _client.SpotApi.ExchangeData.GetKlinesAsync(symbol, interval, limit: 1);

                if (result.Success)
                {
                    var kline = result.Data.First(); // Obtém a última vela
                    return (kline.HighPrice, kline.LowPrice);
                }
                else
                {
                    throw new Exception($"Erro ao buscar preços de alta e baixa: {result.Error}");
                }
            });
        }

        public async Task<List<decimal>> GetLastHighPrices(string symbol, KlineInterval interval, int limit)
        {
            return await ExecuteWithRetry(async () =>
            {
                var result = await _client.SpotApi.ExchangeData.GetKlinesAsync(symbol, interval, limit: limit);

                if (result.Success)
                {
                    return result.Data.Select(k => k.HighPrice).ToList(); // Retorna a lista de preços máximos
                }
                else
                {
                    throw new Exception($"Erro ao buscar preços máximos: {result.Error}");
                }
            });
        }

        // Método genérico de execução com tentativas
        private async Task<T> ExecuteWithRetry<T>(Func<Task<T>> action, int maxRetries = 3, int delayMilliseconds = 1000)
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    return await action();
                }
                catch (Exception ex)
                {
                    attempt++;
                    Console.WriteLine($"Tentativa {attempt} falhou: {ex.Message}");

                    if (attempt >= maxRetries)
                    {
                        Console.WriteLine("Número máximo de tentativas atingido. Exceção final: " + ex.Message);
                        throw;
                    }

                    // Aguardar um tempo antes de tentar novamente
                    await Task.Delay(delayMilliseconds * attempt); // Aumenta o delay a cada tentativa (backoff exponencial)
                }
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
