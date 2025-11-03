using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace salmar_d365_mtps_convertor
{
    public class VendorBankAccountD365
    {
        public string VendorAccountNumber { get; set; }
        public string VendorBankAccountId { get; set; }
        public string BankAccountNumber { get; set; }
        public string ForeignBankAccountNumber { get; set; }
        public string ForeignBankSWIFTCode { get; set; }
        public string SWIFTCode { get; set; }
        public string IBAN { get; set; }
        public string CurrentCurrencyCode { get; set; }
        public DateTime? ActiveDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string BankName { get; set; }
        public string IsDefaultBankAccountForCurrentCurrency { get; set; }
        public string IsDefaultBankAccount { get; set; }
    }
}
