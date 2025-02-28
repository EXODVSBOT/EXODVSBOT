using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExodvsBot.Dto
{
    public class OcorrenciaDto
    {
        public DateTime Data { get; set; }
        public bool Executou { get; set; }
        public string Decisao { get; set; }
        public decimal SaldoUsdt { get; set; }
        public decimal PrecoBitcoin { get; set; }
    }
}
