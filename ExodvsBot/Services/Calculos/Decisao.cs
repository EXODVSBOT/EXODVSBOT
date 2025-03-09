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
                                int stopLoss,
                                int takeProfit)
        {
            string decisao = "Keep";

            if( bitcoinPrice == 0 || rsiCalculo == 0)
            {
                return decisao;
            }

            // Condição de compra
            if (rsiCalculo < rsiBuy)
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
                bitcoinPrice > ultimaOperacao.PrecoBitcoin * takeProfitMultiplicativo)
            {
                decisao = "Sell";
            }

            return decisao;
        }
    }
}