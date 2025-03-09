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

        public decimal CalcularRSI(List<decimal> precos, int periodo = 14)
        {
            if (precos == null || precos.Count < periodo + 1)
                return 50m; // Valor neutro quando não há dados suficientes

            List<decimal> ganhos = new List<decimal>();
            List<decimal> perdas = new List<decimal>();

            // Cálculo de ganhos e perdas individuais
            for (int i = 1; i < precos.Count; i++)
            {
                decimal variacao = precos[i] - precos[i - 1];
                ganhos.Add(variacao > 0 ? variacao : 0);
                perdas.Add(variacao < 0 ? Math.Abs(variacao) : 0);
            }

            // Média inicial (SMA dos primeiros 'periodo' valores)
            decimal mediaGanhos = ganhos.Take(periodo).Average();
            decimal mediaPerdas = perdas.Take(periodo).Average();

            // Aplicação da EMA nos ganhos e perdas (suavização)
            for (int i = periodo; i < ganhos.Count; i++)
            {
                mediaGanhos = (mediaGanhos * (periodo - 1) + ganhos[i]) / periodo;
                mediaPerdas = (mediaPerdas * (periodo - 1) + perdas[i]) / periodo;
            }

            // Evita divisão por zero
            if (mediaPerdas == 0) return 100m;
            if (mediaGanhos == 0) return 0m;

            decimal rs = mediaGanhos / mediaPerdas;
            return 100 - (100 / (1 + rs));
        }


        private int AjustarPeriodoParaVolatilidade(List<decimal> precos, int periodoBase)
        {
            if (precos.Count < 5) return periodoBase;

            // Medir volatilidade recente
            decimal volatilidade = 0;
            for (int i = 1; i < 5; i++)
            {
                volatilidade += Math.Abs(precos[^i] - precos[^(i + 1)]);
            }
            volatilidade /= 4;

            // Ajuste dinâmico do período
            return volatilidade > (precos.Average() * 0.05m)
                ? Math.Max(10, periodoBase - 4) // Reduz período em alta volatilidade
                : periodoBase;
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


        public async Task<int> DefinirQuantidadeDeCandles(KlineIntervalEnum intervalo, int periodo = 14)
        {
            // Definir um número razoável de candles para cálculos mais estáveis
            int fatorMultiplicador = 7; // Pega múltiplos do período para melhor precisão

            switch (intervalo)
            {
                case KlineIntervalEnum.OneSecond: return periodo * fatorMultiplicador;   // 14 * 7 = 98 segundos (~2 min)
                case KlineIntervalEnum.OneMinute: return periodo * fatorMultiplicador;   // 14 * 7 = 98 minutos (~1,5h)
                case KlineIntervalEnum.ThreeMinutes: return periodo * fatorMultiplicador;// 14 * 7 = 98 * 3 min (~5h)
                case KlineIntervalEnum.FiveMinutes: return periodo * fatorMultiplicador; // 14 * 7 = 98 * 5 min (~8h)
                case KlineIntervalEnum.FifteenMinutes: return periodo * fatorMultiplicador;// ~24h
                case KlineIntervalEnum.ThirtyMinutes: return periodo * fatorMultiplicador;// ~2 dias
                case KlineIntervalEnum.OneHour: return periodo * fatorMultiplicador;      // ~4 dias
                case KlineIntervalEnum.TwoHour: return periodo * fatorMultiplicador;      // ~8 dias
                case KlineIntervalEnum.FourHour: return periodo * fatorMultiplicador;     // ~16 dias
                case KlineIntervalEnum.SixHour: return periodo * fatorMultiplicador;      // ~24 dias
                case KlineIntervalEnum.EightHour: return periodo * fatorMultiplicador;    // ~32 dias
                case KlineIntervalEnum.TwelveHour: return periodo * fatorMultiplicador;   // ~42 dias
                case KlineIntervalEnum.OneDay: return periodo * fatorMultiplicador;       // ~3 meses
                case KlineIntervalEnum.ThreeDay: return periodo * fatorMultiplicador;     // ~9 meses
                default: return periodo * fatorMultiplicador; // Padrão
            }
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
