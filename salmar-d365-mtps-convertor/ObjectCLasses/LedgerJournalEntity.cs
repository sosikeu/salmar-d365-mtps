using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace salmar_d365_mtps_convertor
{
    [XmlRoot("Document")]
    public class LEDGERJOURNALENTITY
    {
        [XmlElement("LEDGERJOURNALENTITY")]
        public List<LedgerJournalEntityLine> LedgerJournalEntityLines { get; set; }
    }
    public class LedgerJournalEntityLine
    {
        public string ACCOUNTDISPLAYVALUE { get; set; }
        public string ACCOUNTTYPE { get; set; }
        public decimal CREDITAMOUNT { get; set; }
        public string CURRENCYCODE { get; set; }
        public decimal DEBITAMOUNT { get; set; }
        public string DEFAULTDIMENSIONDISPLAYVALUE { get; set; }
        public string DESCRIPTION { get; set; }
        public string DOCUMENT { get; set; }
        public string DOCUMENTDATE { get; set; }
        public string DUEDATE { get; set; }
        public decimal EXCHANGERATE { get; set; }
        public string INVOICE { get; set; }
        public string JOURNALNAME { get; set; }
        public int LINENUMBER { get; set; }
        public string MBSMERGER { get; set; }
        public string POSTINGLAYER { get; set; }
        public string PREPAYMENT { get; set; }
        public decimal QUANTITY { get; set; }
        public string SALESTAXCODE { get; set; }
        public string SALESTAXGROUP { get; set; }
        public string TEXT { get; set; }
        public string TRANSDATE { get; set; }
        public string VOUCHER { get; set; }
    }
}
