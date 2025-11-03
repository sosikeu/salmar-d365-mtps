using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace salmar_d365_mtps_convertor
{
    public class CustomerD365
    {
        public string ItemInternalId { get; set; }
        public string OnHoldStatus { get; set; }
        public string OrganizationName { get; set; }
        public string AddressStreet { get; set; }
        public string AddressZipCode { get; set; }
        public string AddressCity { get; set; }
        public string AddressCountryRegionISOCode { get; set; }
        public string SalesCurrencyCode { get; set; }
        public string LanguageId { get; set; }
        public string SalesTaxGroup { get; set; }
        public string CustomerAccount { get; set; }
        public string DeliveryTerms { get; set; }
        public string PaymentTerms { get; set; }
        public string IdentificationNumber { get; set; }
        public string SalesSegmentId { get; set; }
        public string CustomerGroupId { get; set; }
        public string TaxExemptNumber { get; set; }
        public decimal? CreditLimit { get; set; }
        public DateTime? CredManCreditLimitExpiryDate { get; set; }
        public string EmployeeResponsibleNumber { get; set; }
        public string MBSFinancingDNB { get; set; }
        public string DefaultDimensionDisplayValue { get; set; }
        public string SalesDistrict { get; set;}
    }
}
