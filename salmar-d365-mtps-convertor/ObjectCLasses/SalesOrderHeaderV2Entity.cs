using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace salmar_d365_mtps_convertor
{
    public class SALESORDERHEADERV2ENTITY
    {
        public string AREPRICESINCLUDINGSALESTAX { get; set; }
        public string ARETOTALSCALCULATED { get; set; }
        public string CURRENCYCODE { get; set; }
        public string CUSTOMERPOSTINGPROFILEID { get; set; }
        public string CUSTOMERTRANSACTIONSETTLEMENTTYPE { get; set; }
        public string DEFAULTSHIPPINGSITEID { get; set; }
        public string DEFAULTSHIPPINGWAREHOUSEID { get; set; }
        public string FIXEDDUEDATE { get; set; }
        public decimal FIXEDEXCHANGERATE { get; set; }
        public string INVENTORYRESERVATIONMETHOD { get; set; }
        public string INVOICECUSTOMERACCOUNTNUMBER { get; set; }
        public string LANGUAGEID { get; set; }
        public string MBSAUTOPOSTIC { get; set; }
        public string MBSFROMMARITECH { get; set; }
        public decimal MBSINVOICETOTAL { get; set; }
        public string MBSMARITECHINVOICEDATE { get; set; }
        public string MBSMARITECHPAYMID { get; set; }
        public string MBSMARITECHSALESID { get; set; }
        public decimal MBSQTY { get; set; }
        public string ORDERINGCUSTOMERACCOUNTNUMBER { get; set; }
        public string PAYMENTTERMSNAME { get; set; }
        public string REQUESTEDSHIPPINGDATE { get; set; }
        public string SALESORDERNAME { get; set; }
        public string SKIPCREATEAUTOCHARGES { get; set; }
        public string WILLAUTOMATICINVENTORYRESERVATIONCONSIDERBATCHATTRIBUTES { get; set; }
        public string MBSCURRENCYHEDGE { get; set; }


        //public string SALESORDERNUMBER { get; set; }

        [XmlElement("SALESORDERLINEV2ENTITY")]
        public List<SALESORDERLINEV2ENTITY> SalesOrderLineV2Entity { get; set; }
    }

    public class SALESORDERLINEV2ENTITY
    {
        public string CURRENCYCODE { get; set; }
        public string DEFAULTLEDGERDIMENSIONDISPLAYVALUE { get; set; }
        public string GIFTCARDTYPE { get; set; }
        public string ITEMNUMBER { get; set; }
        public decimal LINEAMOUNT { get; set; }
        public string LINECREATIONSEQUENCENUMBER { get; set; }
        public string LINEDESCRIPTION { get; set; }
        public decimal ORDEREDSALESQUANTITY { get; set; }
        public string REQUESTEDRECEIPTDATE { get; set; }
        public string REQUESTEDSHIPPINGDATE { get; set; }
        public decimal SALESPRICE { get; set; }
        public decimal SALESPRICEQUANTITY { get; set; }
        public string SALESTAXITEMGROUPCODE { get; set; }
        public string SALESUNITSYMBOL { get; set; }
        public string SHIPPINGSITEID { get; set; }
        public string SHIPPINGWAREHOUSEID { get; set; }
        public string SKIPCREATEAUTOCHARGES { get; set; }
        public string WILLAUTOMATICINVENTORYRESERVATIONCONSIDERBATCHATTRIBUTES { get; set; }
        public string WILLREBATECALCULATIONEXCLUDELINE { get; set; }

        //public string InventoryLotId { get; set; }
    }
}
