using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ExodvsBot.Domain.Dto;

namespace ExodvsBot.Repository.Files
{
    public class FileManagement
    {
        public async static void CreateFile()
        {
            // Define o caminho do diretório e do arquivo
            string directoryPath = @"C:\Exodvs";
            string filePath = Path.Combine(directoryPath, "notes.txt");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            if (!File.Exists(filePath))
            {
                using (File.Create(filePath)) { } // Cria o arquivo vazio se não existir
            }

        }

        public async static Task Write(OcorrenciaDto ocorrencia)
        {
            string filePath = @"C:\Exodvs\notes.txt";

            // Formata a nova linha como CSV
            string newLine = $"{ocorrencia.Data:yyyy-MM-dd HH:mm:ss}," +
                             $"{ocorrencia.Executou}," +
                             $"\"{ocorrencia.Decisao}\"," +
                             $"{ocorrencia.SaldoUsdt.ToString(CultureInfo.InvariantCulture)}," +
                             $"{ocorrencia.PrecoBitcoin.ToString(CultureInfo.InvariantCulture)}";

            // Abre o arquivo com FileShare.ReadWrite para evitar bloqueios
            using (var fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (var streamWriter = new StreamWriter(fileStream, Encoding.UTF8))
            {
                // Lê todo o conteúdo atual do arquivo
                fileStream.Seek(0, SeekOrigin.Begin);
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, false, 1024, true))
                {
                    string existingContent = await streamReader.ReadToEndAsync();

                    // Escreve a nova linha no topo
                    fileStream.Seek(0, SeekOrigin.Begin);
                    await streamWriter.WriteLineAsync(newLine);
                    await streamWriter.WriteAsync(existingContent);
                }
            }
        }
        public async static Task<OcorrenciaDto?> GetLastLine()
        {
            string filePath = @"C:\Exodvs\notes.txt";

            if (!File.Exists(filePath))
            {
                return null; // Retorna nulo se o arquivo não existir
            }

            // Lê todas as linhas do arquivo
            string[] lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8);

            if (lines.Length == 0)
            {
                return null; // Retorna nulo se o arquivo estiver vazio
            }

            // Itera as linhas de cima para baixo
            foreach (string line in lines)
            {
                string[] parts = line.Split(',');

                if (parts.Length < 5)
                {
                    continue; // Ignora linhas com formato incorreto
                }

                // Verifica se a segunda coluna é "True" e a terceira coluna é "Comprar"
                if (parts[1].Trim().Equals("True", StringComparison.OrdinalIgnoreCase) &&
                    parts[2].Trim().Equals("\"Buy\"", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        // Converte a linha em um objeto OcorrenciaDto
                        return new OcorrenciaDto
                        {
                            Data = DateTime.ParseExact(parts[0], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                            Executou = bool.Parse(parts[1]),
                            Decisao = parts[2].Trim('"'), // Remove as aspas da decisão
                            SaldoUsdt = decimal.Parse(parts[3], CultureInfo.InvariantCulture),
                            PrecoBitcoin = decimal.Parse(parts[4], CultureInfo.InvariantCulture)
                        };
                    }
                    catch
                    {
                        // Ignora erros de conversão e continua procurando
                        continue;
                    }
                }
            }

            return null; // Retorna nulo se nenhuma linha válida for encontrada
        }
        public async static Task<List<OcorrenciaDto>> ReadFile()
        {
            string filePath = @"C:\Exodvs\notes.txt";

            // Verifica se o arquivo existe
            if (!File.Exists(filePath))
            {
                // Retorna uma lista vazia se o arquivo não existir
                return new List<OcorrenciaDto>();
            }

            var ocorrencias = new List<OcorrenciaDto>();

            // Abre o arquivo com FileShare.ReadWrite para evitar bloqueios
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                // Lê todas as linhas do arquivo
                string[] lines = (await streamReader.ReadToEndAsync()).Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                // Inverte a ordem das linhas (lê de baixo para cima)
                var reversedLines = lines.Reverse();

                foreach (var line in reversedLines)
                {
                    // Pula linhas em branco
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Divide a linha usando vírgula como separador
                    var parts = line.Split(',');

                    // Verifica se a linha possui exatamente 5 partes
                    if (parts.Length != 5)
                        continue;

                    // Converte os dados extraindo e tratando as aspas quando necessário
                    if (!DateTime.TryParse(parts[0].Trim(), out DateTime data))
                        continue;

                    if (!bool.TryParse(parts[1].Trim(), out bool executou))
                        continue;

                    // Remove as aspas do campo decisão
                    string decisao = parts[2].Trim().Trim('"');

                    // Converte os valores decimais utilizando a cultura invariante
                    if (!decimal.TryParse(parts[3].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal saldoUsdt))
                        continue;

                    if (!decimal.TryParse(parts[4].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal precoBitcoin))
                        continue;

                    // Cria o objeto OcorrenciaDto e adiciona à lista
                    var ocorrencia = new OcorrenciaDto
                    {
                        Data = data,
                        Executou = executou,
                        Decisao = decisao,
                        SaldoUsdt = saldoUsdt,
                        PrecoBitcoin = precoBitcoin
                    };

                    ocorrencias.Add(ocorrencia);
                }
            }

            return ocorrencias;
        }
        public async static Task UpdateFileWithTranslatedWords()
        {
            string filePath = @"C:\Exodvs\notes.txt";

            // Verifica se o arquivo existe
            if (!File.Exists(filePath))
            {
                return; // Sai do método se o arquivo não existir
            }

            // Abre o arquivo com FileShare.ReadWrite para evitar bloqueios
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            using (var streamWriter = new StreamWriter(fileStream, Encoding.UTF8))
            {
                // Lê todas as linhas do arquivo
                string[] lines = (await streamReader.ReadToEndAsync()).Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                // Lista para armazenar as linhas atualizadas
                var updatedLines = new List<string>();

                foreach (var line in lines)
                {
                    // Substitui "Comprar" por "buy" e "Vender" por "sell"
                    var updatedLine = line
                        .Replace("\"Comprar\"", "\"Buy\"")
                        .Replace("\"Vender\"", "\"Sell\"");

                    updatedLines.Add(updatedLine);
                }

                // Escreve as linhas atualizadas de volta no arquivo
                fileStream.Seek(0, SeekOrigin.Begin);
                await streamWriter.WriteAsync(string.Join(Environment.NewLine, updatedLines));
                fileStream.SetLength(fileStream.Position); // Trunca o arquivo para remover conteúdo antigo
            }
        }


    }
}
