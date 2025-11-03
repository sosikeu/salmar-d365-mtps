using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace salmar_d365_mtps_convertor
{

    public class MTFakturaXML
    {
        public string versionno { get; set; }
        public string electronic_invoice_type { get; set; }
        public string timezone { get; set; }
        public string head_fakturanr { get; set; }
        public string head_fakturadato { get; set; }
        public string head_fakturatype { get; set; }
        public string head_kredit { get; set; }
        public string head_klientnr { get; set; }
        public string row_linjenr { get; set; }
        public string row_beskrivelse { get; set; }
        public string row_antall { get; set; }
        public string row_enhkvant { get; set; }
        public string row_valpris { get; set; }
        public string row_valbelop { get; set; }
        public string row_totkvant { get; set; }
        public string row_pakkemerke { get; set; }
        public string row_linjetype { get; set; }
        public string row_rabattprosent { get; set; }
        public string row_valbeloputenrabatt { get; set; }
        public string row_vareanr { get; set; }
        public string row_gtin { get; set; }
        public string row_mvaprosent { get; set; }
        public string row_fakturert { get; set; }
        public string row_fakturert_enhet { get; set; }
        public string row_uttakstype { get; set; }
        public string row_uttaksnr { get; set; }
        public string row_mvakode { get; set; }
        public string row_fakturert_bermate { get; set; }
        public string head_klient_valutakode { get; set; }
        public string head_reffakturanr { get; set; }
        public string head_systemnr { get; set; }
        public string head_subinvoice { get; set; }
        public string head_meldingstype { get; set; }
        public string head_interchangeid { get; set; }
        public string head_betalingsbetingelse { get; set; }
        public string head_use_iban { get; set; }
        public string supplier_gln { get; set; }
        public string supplier_navn { get; set; }
        public string supplier_adresse1 { get; set; }
        public string supplier_adresse2 { get; set; }
        public string supplier_adresse3 { get; set; }
        public string supplier_landkode { get; set; }
        public string supplier_postnrkode { get; set; }
        public string supplier_poststed { get; set; }
        public string supplier_telefon { get; set; }
        public string supplier_fax { get; set; }
        public string supplier_x400adr { get; set; }
        public string supplier_email { get; set; }
        public string supplier_kontaktperson { get; set; }
        public string supplier_orgnrkode { get; set; }
        public string supplier_bankkonto { get; set; }
        public string supplier_swiftkode { get; set; }
        public string supplier_mvapliktig { get; set; }
        public string payee_gln { get; set; }
        public string payee_navn { get; set; }
        public string payee_adresse1 { get; set; }
        public string payee_adresse2 { get; set; }
        public string payee_adresse3 { get; set; }
        public string payee_landkode { get; set; }
        public string payee_postnrkode { get; set; }
        public string payee_poststed { get; set; }
        public string payee_telefon { get; set; }
        public string payee_fax { get; set; }
        public string payee_x400adr { get; set; }
        public string payee_email { get; set; }
        public string payee_kontaktperson { get; set; }
        public string payee_orgnrkode { get; set; }
        public string payee_bankkonto { get; set; }
        public string payee_swiftkode { get; set; }
        public string payee_mvapliktig { get; set; }
        public string buyer_gln { get; set; }
        public string buyer_kundenr { get; set; }
        public string buyer_navn { get; set; }
        public string buyer_adresse1 { get; set; }
        public string buyer_adresse2 { get; set; }
        public string buyer_adresse3 { get; set; }
        public string buyer_landkode { get; set; }
        public string buyer_postnrkode { get; set; }
        public string buyer_poststed { get; set; }
        public string buyer_orgnrkode { get; set; }
        public string invoicee_gln { get; set; }
        public string invoicee_kundenr { get; set; }
        public string invoicee_navn { get; set; }
        public string invoicee_adresse1 { get; set; }
        public string invoicee_adresse2 { get; set; }
        public string invoicee_adresse3 { get; set; }
        public string invoicee_landkode { get; set; }
        public string invoicee_postnrkode { get; set; }
        public string invoicee_poststed { get; set; }
        public string invoicee_orgnrkode { get; set; }
        public string invoicee_email { get; set; }
        public string invoicee_emailinvoice { get; set; }
        public string dp_gln { get; set; }
        public string dp_kundenr { get; set; }
        public string dp_navn { get; set; }
        public string dp_adresse1 { get; set; }
        public string dp_adresse2 { get; set; }
        public string dp_adresse3 { get; set; }
        public string dp_landkode { get; set; }
        public string dp_postnrkode { get; set; }
        public string dp_poststed { get; set; }
        public string dp_orgnrkode { get; set; }
        public string head_deresref { get; set; }
        public string head_ordrenrkunde { get; set; }
        public string head_tilgangsnrkunde { get; set; }
        public string head_forfallsdato { get; set; }
        public string head_valutakode { get; set; }
        public string head_enhetbet { get; set; }
        public string head_leveringsnavn { get; set; }
        public string head_atttekst { get; set; }
        public string head_anspersnr { get; set; }
        public string head_ankomstdato { get; set; }
        public string head_avgangsdato { get; set; }
        public string head_levstedanr { get; set; }
        public string head_kidkode { get; set; }
        public string head_mvapliktig { get; set; }
        public string head_valutakurs { get; set; }
        public string head_levbetkode { get; set; }
        public string order_ordrenrkunde { get; set; }
        public string head_customsexport { get; set; }
        public string order_tilgangsnrkunde { get; set; }
        public string order_ordredato { get; set; }
        public string order_deresref { get; set; }
        public string order_ankomstdato { get; set; }
        public string order_orginalordrenr { get; set; }
        public string head_embeddedpdf { get; set; }

    }
}
