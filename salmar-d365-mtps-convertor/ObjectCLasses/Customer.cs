using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace salmar_d365_mtps_convertor
{
    public class Customer
    {
        public string ClientNo { get; set; }
        public Int64 CustomerNo { get; set; }
        public int Active { get; set; }
        public string Name { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string CountryCode { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
        public string CurrencyCode { get; set; }
        public int LanguageCode { get; set; }
        public string OrganizationNo { get; set; }
        public string EORINo { get; set; }
        public string VATNo { get; set; }
        public int SubjectToVAT { get; set; }
        public int CorporateCustomer { get; set; }
        //public string ExtCustomerNo { get; set; }
        public decimal CreditLimit { get; set; }
        //P&S removed: public string CreditLimitDate { get; set; }
        public decimal InsuranceLimit { get; set; }
        //P&S removed: public string InsuranceValidTo { get; set; }
        public int UseFactoringCompany { get; set; }
        public int AccumulatedInvoice { get; set; }
        public string Segment { get; set; }
        public string TermsOfDeliveryCode { get; set; }
        public string TermsOfPayment { get; set; }
        public string RefSalesPerson { get; set; }
        //public string InsuranceValidFrom { get; set; }
        //P&S removed: public Int64 InsuranceRate { get; set; }
        //P&S removed: public Int64 InsuranceGracePeriod { get; set; }

    }
}
