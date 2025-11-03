using salmar_d365_mtps_convertor;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace salmar_d365_mtps_convertor
{
    public class PostedEntryResult
    {
        public string D365SalesOrdersXML { get; set; }
        public string D365PurchaseOrdersXML { get; set; }
        public List<ConversionResultMessage> ConvertedSalesOrders { get; set; }
        public List<ConversionResultMessage> FailedSalesOrders { get; set; }
        public string Status { get; set; }
        public string StatusMessage { get; set; }
    }
    public class PostedEntryResultV2
    {
        public string D365SalesOrdersXML { get; set; }
        public string D365PurchaseOrdersXML { get; set; }
        public string D365GLEntriesXML { get; set; }
        public List<ConversionResultMessageV2> ConvertedSalesOrders { get; set; }
        public List<ConversionResultMessageV2> FailedSalesOrders { get; set; }
        public List<ConversionResultMessageV2> ConvertedPurchaseOrders { get; set; }
        public List<ConversionResultMessageV2> FailedPurchaseOrders { get; set; }
        public List<ConversionResultMessageV2> ConvertedGLEntries { get; set; }
        public List<ConversionResultMessageV2> FailedGLEntries { get; set; }
        public string Status { get; set; }
        public string StatusMessage { get; set; }
    }
}
