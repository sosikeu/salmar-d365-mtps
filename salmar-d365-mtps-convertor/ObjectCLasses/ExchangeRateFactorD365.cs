using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace salmar_d365_mtps_convertor
{
    public class ExchangeRateFactorD365
    {
        public string IntegrationId { get; set; }
        public string CurrencyCode { get; set; }
        public string Factor { get; set; }
        public int FactorValue { get; set; }
    }
}
