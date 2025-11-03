using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace salmar_d365_mtps_convertor
{
    public class ConversionResultMessage
    {
        public string PostedEntryMessageId { get; set; }
        public string Type { get; set; }
        public string StatusMessage { get; set; }
    }
    public class ConversionResultMessageV2
    {
        public string PostedEntryMessageId { get; set; }
        public string Type { get; set; }
        public string StatusMessage { get; set; }
        public SALESORDERHEADERV2ENTITY SalesOrder { get; set; }
        public PURCHPURCHASEORDERHEADERV2ENTITY PurchaseOrder { get; set; }
        public LEDGERJOURNALENTITY LedgerJournalEntity { get; set; }
        public string LedgerJournalEntityXML { get; set; }
    }
}
    