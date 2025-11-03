using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace salmar_d365_mtps_convertor
{
    public class GuaranteeInsuranceD365
    {
        public string dataAreaId { get; set; }
        public string CustAccount { get; set; }
        public string GuaranteeInsurance { get; set; }
        public decimal? Value { get; set; }
        public DateTime? ValidTo { get; set; }
        public DateTime? ValidFrom { get; set; }
        public Int64? MBSInsInsurancePct { get; set; }
        public Int64? MBSInsInsuranceDays { get; set; }
    }
}
