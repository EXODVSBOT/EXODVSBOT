using ExodvsBot.Repository.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExodvsBot.Services.Calculos
{
    public class Decisao
    {
        public static async Task<string> TomarDecisao(decimal bitcoinPrice,
                                decimal rsiCalculo,
                                decimal rsiBuy,
                                decimal rsiSell,
                                decimal bandaSuperior,
                                decimal bandaInferior,
                                List<decimal> volumeList,
                                int stopLoss,
                                int takeProfit)
        {
            string decisao = "Keep";

            // Cálculo da média ajustada do volume
            var mediaVolume = volumeList.Average();
            var volumeAtual = volumeList.Last();
            var desvioPadraoVolume = Math.Sqrt(volumeList.Select(v => Math.Pow((double)(v - mediaVolume), 2)).Average());
            var mediaVolumeAjustada = mediaVolume + (decimal)desvioPadraoVolume;

            // Condição de compra
            if (rsiCalculo < rsiBuy &&
                bitcoinPrice < bandaInferior &&
                volumeAtual > mediaVolumeAjustada)
            {
                return "Buy";
            }

            // Recupera última operação
            var ultimaOperacao = await FileManagement.GetLastLine();
            if (ultimaOperacao == null) return "Keep";

            // Cálculo da diferença percentual (-10 para -10%, 1 para 1%, etc.)
            decimal diferencaPercentual = (bitcoinPrice - ultimaOperacao.PrecoBitcoin) / ultimaOperacao.PrecoBitcoin * 100m;

            // Garantindo que stopLoss seja sempre negativo
            decimal stopLossNegativo = Math.Abs(stopLoss) * -1;
            // Condição de venda por Stop Loss (se o preço caiu abaixo do limite negativo)
            if (diferencaPercentual <= stopLossNegativo)
            {
                decisao = "Sell";
            }

            // Condição de venda por Take Profit (se o preço subiu acima do limite positivo)
            if (diferencaPercentual >= takeProfit)
            {
                decisao = "Sell";
            }

            // Converter takeProfit para formato multiplicativo (1% → 1.01, 10% → 1.10)
            decimal takeProfitMultiplicativo = 1m + takeProfit / 100m;

            // Condição de venda por RSI e banda superior
            if (rsiCalculo > rsiSell &&
                bitcoinPrice > bandaSuperior &&
                bitcoinPrice > ultimaOperacao.PrecoBitcoin * takeProfitMultiplicativo)
            {
                decisao = "Sell";
            }

            return decisao;
        }
    }
}