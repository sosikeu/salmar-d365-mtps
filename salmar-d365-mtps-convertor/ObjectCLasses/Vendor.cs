using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace salmar_d365_mtps_convertor
{
    public class Vendor
    {
        public string ClientNo { get; set; }
        public Int64 VendorNo { get; set; }
        public int Active { get; set; }
        public string Name { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string CountryCode { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
        public string Telephone1 { get; set; }
        public string Email { get; set; }
        public string OrganizationNo { get; set; }
        //P&S removed: public string BankAccount { get; set; }
        public string CurrencyCode { get; set; }
        //P&S removed: public int LanguageCode { get; set; }
        //P&S removed: public Int64 VATCode { get; set; }
        //public string ExtVendorNo { get; set; }
        //P&S removed: public int CustNoAtVendor { get; set; }
        //Clarification session 2022-01-14 / removed: public string VATNo { get; set; }


    }
}
