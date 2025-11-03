using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace salmar_d365_mtps_convertor
{
    public class VendorD365
    {
        public string ItemInternalId { get; set; }
        public string OnHoldStatus { get; set; }
        public string VendorOrganizationName { get; set; }
        public string AddressStreet { get; set; }
        public string AddressCountryRegionISOCode { get; set; }
        public string AddressZipCode { get; set; }
        public string AddressCity { get; set; }
        public string PrimaryPhoneNumber { get; set; }
        public string PrimaryEmailAddress { get; set; }
        public string OrganizationNumber { get; set; }
        public string BankAccountId { get; set; }
        public string CurrencyCode { get; set; }
        public string LanguageId { get; set; }
        public string VendorAccountNumber { get; set; }
        public string VendorGroupId { get; set; }
        public string TaxExemptNumber { get; set; }
        public string SalesTaxGroupCode { get; set; }
        public string MBSMarkedForTransfer { get; set; }
        public string DefaultLedgerDimensionDisplayValue { get; set; }
    }
}
