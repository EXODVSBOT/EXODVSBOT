using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExodvsBot.Calculos
{
    public class Calculos
    {
        public decimal CalcularMedia(List<decimal> medias)
        {
            return medias.Average();
        }

        public decimal CalcularMediaMovelExponencial(List<decimal> precos, int periodo)
        {
            if (precos.Count < periodo) return 0; // Verifica se há preços suficientes

            decimal alpha = 2.0m / (periodo + 1); // Fator de suavização
            decimal mme = precos.Take(periodo).Average(); // Calcula a primeira MME como a média dos primeiros "n" períodos

            for (int i = periodo; i < precos.Count; i++)
            {
                mme = (precos[i] * alpha) + (mme * (1 - alpha)); // Fórmula da MME
            }

            return mme;
        }

        public decimal CalcularRSI(List<decimal> precos, int periodo)
        {
            if (precos.Count < periodo)
                throw new ArgumentException("A lista de preços deve ter pelo menos o número de períodos desejados.");

            decimal ganhoTotal = 0;
            decimal perdaTotal = 0;

            // Calcular ganhos e perdas
            for (int i = 1; i < periodo; i++)
            {
                decimal variacao = precos[i] - precos[i - 1];

                if (variacao > 0)
                    ganhoTotal += variacao;
                else
                    perdaTotal -= variacao; // Convertendo perda para um valor positivo
            }

            // Média dos ganhos e perdas
            decimal ganhoMedio = ganhoTotal / periodo;
            decimal perdaMedia = perdaTotal / periodo;

            // Evitar divisão por zero
            if (perdaMedia == 0) return 100; // Se não há perdas, RSI é 100

            // Cálculo do RSI
            decimal rs = ganhoMedio / perdaMedia;
            decimal rsi = 100 - (100 / (1 + rs));

            return rsi;
        }

        public (decimal macd, decimal signal) CalcularMACD(List<decimal> precos)
        {
            if (precos.Count < 26) return (0, 0); // Verifica se há preços suficientes

            // Calcula MME de 12 e 26 períodos
            var ema12 = CalcularMediaMovelExponencial(precos, 12);
            var ema26 = CalcularMediaMovelExponencial(precos, 26);

            // Calcula MACD
            decimal macd = ema12 - ema26;

            // Para calcular a linha de sinal, precisamos dos últimos 9 valores do MACD
            var macdValores = new List<decimal>();
            macdValores.Add(macd); // Adiciona o MACD atual

            // Se precisar, calcule o MACD para os períodos anteriores
            for (int i = 1; i < precos.Count; i++)
            {
                if (macdValores.Count >= 9) break; // Mantém apenas os últimos 9 valores

                var ema12Anterior = CalcularMediaMovelExponencial(precos.Take(precos.Count - i).ToList(), 12);
                var ema26Anterior = CalcularMediaMovelExponencial(precos.Take(precos.Count - i).ToList(), 26);
                decimal macdAnterior = ema12Anterior - ema26Anterior;

                macdValores.Add(macdAnterior);
            }

            // Calcula a linha de sinal como a média móvel exponencial de 9 períodos do MACD
            decimal signal = CalcularMediaMovelExponencial(macdValores, 9);

            return (macd, signal);
        }

        public (decimal bandaSuperior, decimal bandaInferior, decimal mediaMovel) CalcularBandasDeBollinger(List<decimal> precos, int periodo, decimal multiplicador)
        {
            if (precos.Count < periodo) return (0, 0, 0); // Verifica se há preços suficientes

            // Calcula a média móvel simples
            decimal mediaMovel = precos.Take(periodo).Average();

            // Calcula o desvio padrão
            var variancias = precos.Take(periodo).Select(preco => (preco - mediaMovel) * (preco - mediaMovel));
            decimal desvioPadrao = (decimal)Math.Sqrt((double)variancias.Average());

            // Calcula as bandas
            decimal bandaSuperior = mediaMovel + (desvioPadrao * multiplicador);
            decimal bandaInferior = mediaMovel - (desvioPadrao * multiplicador);

            return (bandaSuperior, bandaInferior, mediaMovel);
        }

        public (decimal k, decimal d) CalcularEstocastico(List<decimal> precos, int periodoK, int periodoD)
        {
            if (precos.Count < periodoK) return (0, 0); // Verifica se há preços suficientes

            decimal maxPeriodo = precos.Take(periodoK).Max();
            decimal minPeriodo = precos.Take(periodoK).Min();
            decimal fechamentoAtual = precos.Last();

            decimal k = ((fechamentoAtual - minPeriodo) / (maxPeriodo - minPeriodo)) * 100;

            // Para o %D, calculamos a média móvel dos últimos valores de %K
            var valoresK = new List<decimal> { k };
            for (int i = 1; i < periodoD; i++)
            {
                if (precos.Count - i < periodoK) break;

                maxPeriodo = precos.Skip(precos.Count - i - periodoK).Take(periodoK).Max();
                minPeriodo = precos.Skip(precos.Count - i - periodoK).Take(periodoK).Min();
                decimal kAnterior = ((precos[precos.Count - i - 1] - minPeriodo) / (maxPeriodo - minPeriodo)) * 100;
                valoresK.Add(kAnterior);
            }

            decimal d = valoresK.Take(periodoD).Average();

            return (k, d);
        }
    }
}
