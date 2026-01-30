using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace salmar_d365_mtps_convertor
{
    public class MTPostedEntriesRequestObject
    {
        public Dictionary<string, MTDocument> MTDoc { get; set; }
        public Dictionary<string, CustomerD365> CustomersD365 { get; set; }
        public Dictionary<string, VendorD365> VendorsD365 { get; set; }
        public Dictionary<string, ExchangeRateD365> ExchangeRatesD365 { get; set; }
        public List<ExchangeRateFactorD365> ExchangeRateFactorsD365 { get; set; }
        public string ExchangeRateFactorIntegrationId { get; set; }
        public List<VATmap> VATmaps { get; set; }
        public DateTime? ShiftDateOlderThan { get; set; }
        public DateTime? ShiftDateChangeTo { get; set; }
        public string OverrideSalesOrderLineSalesTaxItemGroupCode { get; set; }
        public string OverrideSalesOrderLineItemNo { get; set; }
        public string PurchaseOrderMultiHeaderPoolId { get; set; }
        public Dictionary<string, DimensionIntegrationFormat> DimensionIntegrationFormats { get; set; }
        public string MaritechPSFilterVendors { get; set; }
        public string MaritechPSSalesOrderDefaultSalesTaxItemGroupCode { get; set; }
        public string MaritechPSSalesOrderPennyDifferenceAccountNr { get; set; }
    }
}
