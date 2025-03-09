using ExodvsBot.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExodvsBot.Services.Calculos
{
    public class Calculos
    {
        public decimal CalcularMedia(List<decimal> medias)
        {
            if(medias.Count() == 0) return 0;   
            return medias.Average();
        }

        public decimal CalcularMediaMovelExponencial(List<decimal> precos, int periodo)
        {
            if (precos.Count < periodo) return 0; // Verifica se há preços suficientes

            decimal alpha = 2.0m / (periodo + 1); // Fator de suavização
            decimal mme = precos.Take(periodo).Average(); // Calcula a primeira MME como a média dos primeiros "n" períodos

            for (int i = periodo; i < precos.Count; i++)
            {
                mme = precos[i] * alpha + mme * (1 - alpha); // Fórmula da MME
            }

            return mme;
        }

        public decimal CalcularRSI(List<decimal> precos, int periodo)
        {
            if(precos.Count() == 0) return 0;

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
            decimal rsi = 100 - 100 / (1 + rs);

            return rsi;
        }

        public (decimal macd, decimal signal) CalcularMACD(List<decimal> precos)
        {
            if (precos.Count() == 0) return (0, 0);

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
            decimal bandaSuperior = mediaMovel + desvioPadrao * multiplicador;
            decimal bandaInferior = mediaMovel - desvioPadrao * multiplicador;

            return (bandaSuperior, bandaInferior, mediaMovel);
        }

        public (decimal k, decimal d) CalcularEstocastico(List<decimal> precos, int periodoK, int periodoD)
        {
            if (precos.Count < periodoK) return (0, 0); // Verifica se há preços suficientes

            decimal maxPeriodo = precos.Take(periodoK).Max();
            decimal minPeriodo = precos.Take(periodoK).Min();
            decimal fechamentoAtual = precos.Last();

            decimal k = (fechamentoAtual - minPeriodo) / (maxPeriodo - minPeriodo) * 100;

            // Para o %D, calculamos a média móvel dos últimos valores de %K
            var valoresK = new List<decimal> { k };
            for (int i = 1; i < periodoD; i++)
            {
                if (precos.Count - i < periodoK) break;

                maxPeriodo = precos.Skip(precos.Count - i - periodoK).Take(periodoK).Max();
                minPeriodo = precos.Skip(precos.Count - i - periodoK).Take(periodoK).Min();
                decimal kAnterior = (precos[precos.Count - i - 1] - minPeriodo) / (maxPeriodo - minPeriodo) * 100;
                valoresK.Add(kAnterior);
            }

            decimal d = valoresK.Take(periodoD).Average();

            return (k, d);
        }

        // Método principal que recebe os dados brutos e retorna o intervalo recomendado
        public KlineIntervalEnum CalcularMelhorIntervalo(List<decimal> precos, List<decimal> volumes)
        {
            if(precos.Count == 0 || volumes.Count == 0) return KlineIntervalEnum.FifteenMinutes;

            if (precos == null || precos.Count < 168) // 1 semana de dados em intervalos de 1h (7 dias * 24 horas)
                return KlineIntervalEnum.OneHour; // Valor padrão seguro

            // Etapa 1: Análise de volatilidade
            var volatilidadeCurtoPrazo = CalcularVolatilidade(precos.TakeLast(24).ToList()); // Últimas 24 horas
            var volatilidadeLongoPrazo = CalcularVolatilidade(precos); // Semana inteira

            // Etapa 2: Identificar tendência
            var tendencia = IdentificarTendencia(precos);

            // Etapa 3: Analisar volume
            var volumeMedio = volumes.Any() ? volumes.Average() : 0;

            // Etapa 4: Tomar decisão com base nos fatores
            return DeterminarIntervaloOtimizado(volatilidadeCurtoPrazo, volatilidadeLongoPrazo, tendencia, volumeMedio);
        }

        // Método auxiliar 1: Cálculo de volatilidade (Desvio Padrão dos Retornos)
        private decimal CalcularVolatilidade(List<decimal> precos)
        {
            var retornos = new List<decimal>();
            for (int i = 1; i < precos.Count; i++)
            {
                if (precos[i - 1] == 0) continue;
                retornos.Add((precos[i] - precos[i - 1]) / precos[i - 1]);
            }

            if (!retornos.Any()) return 0;

            decimal media = retornos.Average();
            decimal somaQuadrados = retornos.Sum(r => (r - media) * (r - media));
            return (decimal)Math.Sqrt((double)(somaQuadrados / retornos.Count));
        }

        // Método auxiliar 2: Identificação de tendência (Regressão Linear)
        private TrendEnum IdentificarTendencia(List<decimal> precos)
        {
            int n = precos.Count;
            decimal sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;

            for (int i = 0; i < n; i++)
            {
                sumX += i;
                sumY += precos[i];
                sumXY += i * precos[i];
                sumX2 += i * i;
            }

            decimal slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            return slope > 0.005m ? TrendEnum.Alta : slope < -0.005m ? TrendEnum.Baixa : TrendEnum.Neutra;
        }

        // Método auxiliar 3: Lógica de decisão final
        private KlineIntervalEnum DeterminarIntervaloOtimizado(
            decimal volatilidadeCurto,
            decimal volatilidadeLongo,
            TrendEnum tendencia,
            decimal volumeMedio)
        {
            // Fator de diferença de volatilidade
            decimal diferencaVolatilidade = volatilidadeCurto / volatilidadeLongo;

            // Regras de decisão
            if (tendencia == TrendEnum.Alta)
            {
                return (diferencaVolatilidade > 1.5m) ?
                    KlineIntervalEnum.ThirtyMinutes :
                    KlineIntervalEnum.TwoHour;
            }

            if (volatilidadeCurto > 0.08m) // Volatilidade muito alta
            {
                return volumeMedio > 10000 ?
                    KlineIntervalEnum.FourHour :
                    KlineIntervalEnum.OneHour;
            }

            if (volatilidadeLongo < 0.03m) // Mercado estável
            {
                return KlineIntervalEnum.FifteenMinutes;
            }

            // Default para mercado moderado
            return KlineIntervalEnum.OneHour;
        }
    }
}
