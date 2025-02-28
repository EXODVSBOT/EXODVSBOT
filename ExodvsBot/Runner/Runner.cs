using Binance.Net.Enums;
using CryptoExchange.Net.CommonObjects;
using ExodvsBot.Binance;
using ExodvsBot.Calculos;
using ExodvsBot.Dto;
using ExodvsBot.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ExodvsBot.Runner
{
    public class Runner
    {
        // Tamanho máximo das listas
        private const int MaxLogs = 100;
        private const int MaxOcorrencias = 100;

        public static List<string> Logs { get; } = new List<string>();
        public static List<OcorrenciaDto> Ocorrencias { get; } = new List<OcorrenciaDto>();

        public static async Task RunAsyncn(
            CancellationToken cancellationToken,
            StartSettingsDto settings,
            Action<Exception> errorHandler = null)
        {
            FileManagement.CreateFile();
            FileManagement.UpdateFileWithTranslatedWords();
            var binance = new BinanceRequests(settings.txtApiKey, settings.txtApiSecret);
            var buySell = new BuySell(settings.txtApiKey, settings.txtApiSecret);
            var calculos = new Calculos.Calculos();
            var ocorrencias = await FileManagement.ReadFile();
            Ocorrencias.AddRange(ocorrencias);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        bool hasInternetConnection = await CheckInternetConnectionAsync();

                        if (hasInternetConnection)
                        {
                            Run(binance, settings, calculos, buySell);
                        }
                        // Espera o período para rodar
                        await Task.Delay(TimeSpan.FromSeconds((double)settings.cmbRunInterval));
                    }
                    catch (OperationCanceledException)
                    {
                        // O bot foi cancelado
                        break;
                    }
                    catch (Exception ex)
                    {
                        errorHandler?.Invoke(ex);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Cancellation normal
            }
        }

        private static async void Run(BinanceRequests binance, 
            StartSettingsDto settings, 
            Calculos.Calculos calculos, 
            BuySell buySell)
        {
            try
            {
                // Busca preço atual do bitcoin
                decimal bitcoinPrice = await binance.GetAssetPrice();
                // Busca 10 últimos preços por minuto
                var PrecosCurtoPrazo = await binance.GetHistoricalPrices("BTCUSDT", (KlineInterval)settings.cmbKlineInterval, 20);
                // Busca 50 últimos preços por minuto
                var PrecosLongoPrazo = await binance.GetHistoricalPrices("BTCUSDT", (KlineInterval)settings.cmbKlineInterval, 50);
                // Busca 14 últimos preços por minuto para RSI
                var precosParaRSI = await binance.GetHistoricalPrices("BTCUSDT", (KlineInterval)settings.cmbKlineInterval, 14);
                // Busca 20 últimos preços para bandas de Bollinger
                var precosParaBandas = await binance.GetHistoricalPrices("BTCUSDT", (KlineInterval)settings.cmbKlineInterval, 20);
                // Busca o volume dos últimos 50 minutos
                var volumeList = await binance.GetVolumeData("BTCUSDT", (KlineInterval)settings.cmbKlineInterval, 50);
                // Inicia cálculos
                decimal rsi = calculos.CalcularRSI(precosParaRSI, 14);
                var (bandaSuperior, bandaInferior, mediaMovelBollinger) = calculos.CalcularBandasDeBollinger(precosParaBandas, 20, 2);
                var (mcad, signal) = calculos.CalcularMACD(PrecosLongoPrazo);
                var (k, d) = calculos.CalcularEstocastico(PrecosCurtoPrazo, 14, 3);

                // Aqui você pode adicionar a lógica de compra/venda com base nos cálculos
                var decisao = await Decisao.TomarDecisao(bitcoinPrice, rsi, settings.numBuyRSI, settings.numSellRSI, bandaSuperior, bandaInferior, volumeList, settings.cmbStoploss, settings.cmbTakeProfit);
                //operação
                var ocorrencia = await buySell.IniciarOperacao(decisao);

                if (ocorrencia.Executou)
                {
                    ocorrencia.Data = DateTime.Now;
                    ocorrencia.PrecoBitcoin = bitcoinPrice;
                    ocorrencia.Decisao = decisao;
                    FileManagement.Write(ocorrencia);
                    Ocorrencias.Add(ocorrencia);
                }

                var decision = string.Empty;
                if (decisao == "Keep")
                {
                    decision = "Keep Position";
                }
                else if (decisao == "Sell")
                {
                    decision = "Sell Position";
                }
                else if (decisao == "Buy")
                {
                    decision = "Buy Position";
                }

                Logs.Add($"----------------------------------------");
                Logs.Add("🤖 Analysing... " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                Logs.Add($"🤔 Decision: {decision}");
                Logs.Add($"🤑 BTC Price: {bitcoinPrice.ToString("0.00")}");
                Logs.Add($"📊 RSI: {rsi.ToString("0.00")}");
                Logs.Add($"💰 UsdBalance: {ocorrencia.SaldoUsdt.ToString("0.00")}");
                Logs.Add($"----------------------------------------");

                // Limpa as listas se excederem o tamanho máximo
                ClearMemory(Logs, MaxLogs);
                ClearMemory(Ocorrencias, MaxOcorrencias);

            }
            catch (Exception)
            {

                throw;
            }
        }

        // Método para limpar a memória das listas
        private static void ClearMemory<T>(List<T> list, int maxSize)
        {
            if (list.Count > maxSize)
            {
                list.RemoveRange(0, list.Count - maxSize);
            }
        }

        public static async Task<bool> CheckInternetConnectionAsync()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Tenta fazer uma requisição a um servidor confiável
                    HttpResponseMessage response = await client.GetAsync("http://www.google.com");
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                // Se ocorrer uma exceção, assume-se que não há conexão com a internet
                return false;
            }
        }
    }
}