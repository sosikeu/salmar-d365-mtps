using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace salmar_d365_mtps_convertor
{
    public class MTPostedEntry
    {
        public string MaritechMessageId { get; set; }
        public string w625lnr { get; set; }
        public string ClientNo { get; set; }
        public string Code { get; set; }
        public string AccountNo { get; set; }
        public string DepartmentNo { get; set; }
        public string SubDepartmentNo { get; set; }
        public string DocumentNo { get; set; }
        public string DocumentDate { get; set; }
        public string DocumentText { get; set; }
        public string VATCode { get; set; }
        public string CurrencyCode { get; set; }
        public string CurrencyAmount { get; set; }
        public string ExchangeRate { get; set; }
        public string Amount { get; set; }
        public string CustomerNo { get; set; }
        public string VendorNo { get; set; }
        public string InvoiceDate { get; set; }
        public string DueDate { get; set; }
        public string PaymentConditionNo { get; set; }
        public string KID { get; set; }
        public string VATAmount { get; set; }
        public string RegistrationType { get; set; }
        public string Status { get; set; }
        public string DocumentType { get; set; }
        public string DiscountDate { get; set; }
        public string DiscountPercent { get; set; }
        public string IncomingInvoiceNo { get; set; }
        public string ForeignVATRegistryNo { get; set; }
        public string VATCurrencyCode { get; set; }
        public string VATExchangeRate { get; set; }
        public string VATCurrencyAmount { get; set; }
        public string Dimension3 { get; set; }
        public string Quantity { get; set; }
        public string CurrencyHedged { get; set; }
        public string VatBasis { get; set; }
        public string ItemAccount { get; set; }
        public string ClaimId { get; set; }
        public string ExternalCustomerVendorNo { get; set; }
        public string GrossAmount { get; set; }
        public string GrossCurrencyAmount { get; set; }
        public string CurrencyFactor { get; set; }
        public string ExternalRef { get; set; }

    }
}
