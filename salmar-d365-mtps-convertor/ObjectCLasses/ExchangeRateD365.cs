using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace salmar_d365_mtps_convertor
{
    public class ExchangeRateD365
    {
        public string RateTypeName { get; set; }
        public string FromCurrency { get; set; }
        public string ToCurrency { get; set; }
        public string StartDate { get; set; }
        public decimal Rate { get; set; }
        public string EndDate { get; set; }
        public string ConversionFactor { get; set; }
    }
}
