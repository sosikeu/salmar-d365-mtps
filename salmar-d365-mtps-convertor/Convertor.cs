namespace salmar_d365_mtps_convertor
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;

    using neu_d365mtinteg_convertor;

    using Newtonsoft.Json;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Metrics;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using System.Text;
    //using System.Text.Json;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Serialization;

    using static System.Runtime.InteropServices.JavaScript.JSType;

    public static class Convertor
    {
        [FunctionName("ConvertAndCompareCustomers")]
        public static async Task<IActionResult> ConvertAndCompareCustomers([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation($"ConvertAndCompareCustomers HTTP triggered {req.Method} request");
            try
            {
                log.LogInformation("ConvertAndCompareCustomers Get reqest..");
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                log.LogInformation("ConvertAndCompareCustomers Parse reqest..");

                #region Parse input

                byte[] data = Convert.FromBase64String(requestBody);
                string ReqString = Encoding.UTF8.GetString(data);

                //JsonDocument d = JsonDocument.Parse(ReqString);
                dynamic RequestJSON = JsonConvert.DeserializeObject(ReqString);
                /*
                Dictionary<string, CustomerD365> CustomersD365 = JsonConvert.DeserializeObject<List<CustomerD365>>(d.RootElement.GetProperty("CustomersD365").ToString()).ToDictionary(k => k.CustomerAccount.Trim().ToUpper());
                List<GuaranteeInsuranceD365> GuaranteesInsurancesD365 = JsonConvert.DeserializeObject<List<GuaranteeInsuranceD365>>(d.RootElement.GetProperty("GuaranteesInsurancesD365").ToString()).ToList();
                Dictionary<string, EmployeeD365> EmployeesD365 = JsonConvert.DeserializeObject<List<EmployeeD365>>(d.RootElement.GetProperty("EmployeesD365").ToString()).ToDictionary(k => k.PersonnelNumber);
                Dictionary<Int64, Customer> CustomersMaritechPersistent = JsonConvert.DeserializeObject<List<Customer>>(d.RootElement.GetProperty("CustomersMaritechPersistent").ToString()).ToDictionary(k => k.CustomerNo);
                string ClientNo = JsonConvert.DeserializeObject<string>(d.RootElement.GetProperty("ClientNo").ToString());
                */
                string R_CustomersD365 = RequestJSON.CustomersD365.ToString();
                Dictionary<string, CustomerD365> CustomersD365 = JsonConvert.DeserializeObject<List<CustomerD365>>(R_CustomersD365).ToDictionary(k => k.CustomerAccount.Trim().ToUpper());
                string R_GuaranteesInsurancesD365 = RequestJSON.GuaranteesInsurancesD365.ToString();
                List<GuaranteeInsuranceD365> GuaranteesInsurancesD365 = JsonConvert.DeserializeObject<List<GuaranteeInsuranceD365>>(R_GuaranteesInsurancesD365).ToList();
                string R_EmployeesD365 = RequestJSON.EmployeesD365.ToString();
                Dictionary<string, EmployeeD365> EmployeesD365 = JsonConvert.DeserializeObject<List<EmployeeD365>>(R_EmployeesD365).ToDictionary(k => k.PersonnelNumber);
                string R_CustomersMaritechPersistent = RequestJSON.CustomersMaritechPersistent.ToString();
                Dictionary<Int64, Customer> CustomersMaritechPersistent = JsonConvert.DeserializeObject<List<Customer>>(R_CustomersMaritechPersistent).ToDictionary(k => k.CustomerNo);
                string ClientNo = RequestJSON.ClientNo.ToString();

                //List<Customer> CustomersMaritechPersistent = JsonConvert.DeserializeObject<List<Customer>>(d.RootElement.GetProperty("CustomersMaritechPersistent").ToString());

                #endregion

                Dictionary<Int64, Customer> CustomersD365Converted = new Dictionary<Int64, Customer>();
                List<Customer> CustomersMaritechUpdated = new List<Customer>();
                string CustomersMaritechFailedMail = "";
                List<Customer> CustomersMaritechPersistentNew = new List<Customer>();

                log.LogInformation("ConvertAndCompareCustomers Convert data..");

                #region convert D365 to Maritech
                foreach (CustomerD365 cd in CustomersD365.Values)
                {
                    try
                    {
                        //Pre-check ClientNo , Active, Name, CountryCode are mandatory
                        if (cd.OrganizationName.Trim() != "" && cd.AddressCountryRegionISOCode.Trim() != "")
                        {
                            Customer cm = new Customer();
                            cm.ClientNo = ClientNo;
                            //Clarification 2022-02-16: Send Customer No in field CustomerNo, not as ExtCustomerNo
                            //cm.ExtCustomerNo = cd.CustomerAccount.Trim();

                            //TODO: Error handling
                            Int64 CustomerNo = 0;
                            Int64.TryParse(cd.CustomerAccount, out CustomerNo);
                            cm.CustomerNo = CustomerNo;

                            cm.AccumulatedInvoice = 0;
                            cm.Active = (cd.OnHoldStatus.ToLower() == "all" ? 0 : 1); // as per clarification 2022-01-13

                            #region AddressLines
                            if (cd.AddressStreet.Trim().Contains("\n") && cd.AddressStreet.Trim().Replace("\r", "").Split("\n").Length < 4)
                            {
                                cm.Address1 = ((cd.AddressStreet.Trim().Replace("\r", "").Split("\n").Length >= 1 && cd.AddressStreet.Trim().Replace("\r", "").Split("\n")[0] != "") ? cd.AddressStreet.Trim().Replace("\r", "").Split("\n")[0] : "").Trim();
                                cm.Address2 = ((cd.AddressStreet.Trim().Replace("\r", "").Split("\n").Length >= 2 && cd.AddressStreet.Trim().Replace("\r", "").Split("\n")[1] != "") ? cd.AddressStreet.Trim().Replace("\r", "").Split("\n")[1] : "").Trim();
                                cm.Address3 = ((cd.AddressStreet.Trim().Replace("\r", "").Split("\n").Length >= 3 && cd.AddressStreet.Trim().Replace("\r", "").Split("\n")[2] != "") ? cd.AddressStreet.Trim().Replace("\r", "").Split("\n")[2] : "").Trim();
                            }
                            else
                            {
                                cm.Address1 = cd.AddressStreet.Trim().Replace("\r", "").Replace("\n", " ").Trim();
                                cm.Address2 = "";
                                cm.Address3 = "";
                            }
                            #endregion

                            cm.City = cd.AddressCity.Trim();
                            //P&S removed: 
                            //cm.CorporateCustomer = (cd.CustomerGroupId.Trim() == "40" ? 1 : 0); // as per clarification 2022-01-13
                            cm.CorporateCustomer = (cd.CustomerGroupId.Trim() == "40" ? 1 : 0); //added 2026-01-09
                            cm.CountryCode = cd.AddressCountryRegionISOCode.Trim();
                            cm.CurrencyCode = cd.SalesCurrencyCode.Trim();
                            
                            #region LanguageCode: as per clarification 2022-01-13
                            switch (cd.LanguageId.Trim().ToLower())
                            {
                                case "nb-no":
                                { cm.LanguageCode = 47; break; }
                                default:
                                { cm.LanguageCode = 44; break; }
                            }
                            #endregion
                            
                            cm.Name = cd.OrganizationName.Trim();
                            //cm.OrganizationNo = cd.TaxExemptNumber.Trim(); // as per clarification 2022-01-13


                            //added 2026-01-09, 2026-01-19 removed:
                            //NO123456789MVA -> 123456789
                            /*
                            #region OrganizationNo
                            if (cd.TaxExemptNumber.Trim().ToLower().StartsWith("no"))
                            {
                                try
                                {
                                    cm.OrganizationNo = string.Join("",cd.TaxExemptNumber.Trim().Where(char.IsDigit).ToArray());
                                }
                                catch { }
                            }
                            #endregion
                            */
                            //added 2026-01-20:
                            //NO123456789MVA -> 123456789; 123456789 -> 123456789
                            #region OrganizationNo
                            if ((cd.AddressCountryRegionISOCode?.Trim().ToUpper()??"") == "NO")
                            {
                                if (cd.TaxExemptNumber.Trim().ToLower().StartsWith("no"))
                                { 
                                    try
                                    {
                                        cm.OrganizationNo = string.Join("", cd.TaxExemptNumber.Trim().Where(char.IsDigit).ToArray());
                                    }
                                    catch { }
                                }
                                else
                                {
                                    try
                                    {
                                        cm.OrganizationNo = (cd.TaxExemptNumber?.Trim() ?? "");
                                    }
                                    catch { }
                                }
                            }
                            else
                            {
                                cm.OrganizationNo = " ";
                            }
                            #endregion

                            //2026-01-20 cm.EORINo = cd.IdentificationNumber.Trim().ToUpper(); //added 2026-01-09
                            cm.EORINo = ((cd.IdentificationNumber == "" || cd.IdentificationNumber == null) ? " " : cd.IdentificationNumber.Trim().ToUpper()); //added 2026-01-20

                            cm.VATNo = cd.TaxExemptNumber.Trim(); //added 2026-01-09
                            //cm.PostalCode = (cd.AddressCountryRegionISOCode.Trim().ToUpper() == "NO" ? cd.AddressZipCode.Trim() : null);
                            cm.PostalCode = (cd.AddressZipCode.Trim() == "" ? "0" : cd.AddressZipCode.Trim());

                            #region RefSalesPerson
                            if (EmployeesD365.ContainsKey((cd.EmployeeResponsibleNumber ?? "")))
                            {
                                cm.RefSalesPerson = ((EmployeesD365[cd.EmployeeResponsibleNumber].PersonnelNumber != null && EmployeesD365[cd.EmployeeResponsibleNumber].PersonnelNumber != "") ? EmployeesD365[cd.EmployeeResponsibleNumber].PersonnelNumber : "");
                            }
                            else
                            {
                                cm.RefSalesPerson = "";
                            }
                            #endregion

                            #region Segment: as per clarification 2022-01-13 one-to-one
                            // removed 2025-03-01:int SalesSegmentId = 0;
                            // removed 2025-03-01:int.TryParse(cd.SalesSegmentId.Trim(), out SalesSegmentId);
                            // removed 2025-03-01: cm.Segment = (SalesSegmentId == 0 ? null : SalesSegmentId.ToString());
                            cm.Segment = (cd.SalesDistrict ?? "");
                            #endregion

                            cm.SubjectToVAT = (cd.SalesTaxGroup.Trim().ToUpper() == "KIPL" ? 0 : 1); // as per clarification 2022-01-13
                            cm.TermsOfDeliveryCode = cd.DeliveryTerms.Trim();
                            cm.TermsOfPayment = cd.PaymentTerms.Trim();

                            cm.CreditLimit = cd.CreditLimit ?? 0;
                            //P&S removed: 
                            /*
                            cm.CreditLimitDate = (
                                (
                                cd.CredManCreditLimitExpiryDate != null
                                &&
                                cd.CredManCreditLimitExpiryDate != DateTime.Parse("1900-01-01 12:00:00")
                                )
                                ?
                                (cd.CredManCreditLimitExpiryDate ?? DateTime.MaxValue).ToString("yyyy-MM-dd")
                                :
                                DateTime.MaxValue.ToString("yyyy-MM-dd")
                                );
                            */
                            #region Guarantees & Insurances
                            GuaranteeInsuranceD365 igD365 = GuaranteesInsurancesD365.Where(g =>
                                g.GuaranteeInsurance.ToUpper() == "INSURANCE"
                                && g.CustAccount.Trim().ToUpper() == cd.CustomerAccount.Trim().ToUpper()
                                && ((g.ValidFrom ?? DateTime.Parse("1900-01-01 12:00:00")) <= DateTime.Now)
                                && ((g.ValidTo ?? DateTime.Parse("1900-01-01 12:00:00")) >= DateTime.Now || (g.ValidTo ?? DateTime.Parse("1900-01-01 12:00:00")) == DateTime.Parse("1900-01-01 12:00:00"))
                                ).FirstOrDefault();
                            if (igD365 != null)
                            {
                                cm.InsuranceLimit = (igD365.Value ?? 0);
                                //P&S removed: 
                                /*
                                #region InsuranceValidFrom
                                if (igD365.ValidFrom != null && igD365.ValidFrom != DateTime.MinValue && (igD365.ValidFrom ?? DateTime.Parse("1900-01-01 12:00:00")).Year != 1900)
                                {
                                    cm.InsuranceValidFrom = (igD365.ValidFrom ?? DateTime.Parse("1900-01-01 12:00:00")).ToString("yyyy-MM-dd");
                                }
                                else
                                {
                                    cm.InsuranceValidFrom = DateTime.MinValue.ToString("yyyy-MM-dd");
                                }
                                #endregion
                                */
                                //P&S removed: 
                                /*
                                #region InsuranceValidTo
                                if (igD365.ValidTo != null && igD365.ValidTo != DateTime.MaxValue && (igD365.ValidTo ?? DateTime.Parse("1900-01-01 12:00:00")).Year != 1900)
                                {
                                    cm.InsuranceValidTo = (igD365.ValidTo ?? DateTime.Parse("1900-01-01 12:00:00")).ToString("yyyy-MM-dd");
                                }
                                else
                                {
                                    cm.InsuranceValidTo = DateTime.MaxValue.ToString("yyyy-MM-dd");
                                }
                                #endregion
                                */
                                //P&S removed: 
                                //cm.InsuranceRate = (igD365.MBSInsInsurancePct ?? 0);
                                //P&S removed: 
                                //cm.InsuranceGracePeriod = (igD365.MBSInsInsuranceDays ?? 0);
                            }
                            else
                            {
                                cm.InsuranceLimit = 0;
                                //P&S removed: 
                                /*
                                cm.InsuranceValidFrom = null;
                                cm.InsuranceValidTo = null;
                                cm.InsuranceRate = 0;
                                cm.InsuranceGracePeriod = 0;
                                */
                            }
                            #endregion
                            //P&S removed: 
                            /*
                            #region 2022-04-28 Thomas: If the customer has a valid insurance contract with an expiration date, and there’s not credit limit expiration date, can you send the insurance contract expiration date in both fields?
                            if (
                                cm.CreditLimitDate == DateTime.MaxValue.ToString("yyyy-MM-dd")
                                &&
                                cm.InsuranceValidTo != DateTime.MaxValue.ToString("yyyy-MM-dd")
                                )
                            {
                                cm.CreditLimitDate = cm.InsuranceValidTo;
                            }
                            #endregion
                            */
                            cm.UseFactoringCompany = (cd.MBSFinancingDNB.Trim().ToUpper() == "YES" ? 1 : 0);

                            //HACK: Limit only to customerNo > 10000
                            if (cm.CustomerNo > 10000 && cm.CustomerNo < 90000)
                            {
                                CustomersD365Converted.Add(cm.CustomerNo, cm);
                            }
                        }
                        else
                        {
                            //Failed coversions
                            CustomersMaritechFailedMail += $"<li><span style=\"color: rgb(226, 80, 65)\">Failed to convert D365 customer {cd.CustomerAccount} / {cd.OrganizationName}: missing mandatory {(cd.OrganizationName.Trim() == "" ? " name" : "")}{(cd.AddressCountryRegionISOCode.Trim() == "" ? "country code" : "")}</span></li>\r\n";
                        }
                    }
                    catch (Exception ex)
                    {
                        log.LogInformation($"ConvertAndCompareCustomers error {cd.CustomerAccount} - {cd.OrganizationName}: {ex.Message}");
                    }
                }
                #endregion

                log.LogInformation("ConvertAndCompareCustomers Compare data..");

                #region update & create

                foreach (Customer cd in CustomersD365Converted.Values)
                {
                    CustomersMaritechPersistentNew.Add(cd);
                    if (CustomersMaritechPersistent.ContainsKey(cd.CustomerNo))
                    {
                        if (JsonConvert.SerializeObject(cd) != JsonConvert.SerializeObject(CustomersMaritechPersistent[cd.CustomerNo]))
                        {
                            CustomersMaritechUpdated.Add(cd);
                        }
                    }
                    else if (cd.Active == 1)
                    {
                        CustomersMaritechUpdated.Add(cd);
                    }
                }

                #endregion

                #region deactivate

                foreach (Customer cm in CustomersMaritechPersistent.Values)
                {
                    //HACK: Limit only to customerNo > 10000
                    //2022-05-04: Added condition active = 1 in maritech (not to retry all the time)
                    if (!CustomersD365Converted.ContainsKey(cm.CustomerNo) && cm.Active == 1 && cm.CustomerNo > 10000)
                    {
                        cm.Active = 0;
                        CustomersMaritechUpdated.Add(cm);
                        CustomersMaritechPersistentNew.Add(cm);
                    }
                }

                #endregion

                string CustomersMaritechUpdatedXML = "";
                string CustomersMaritechUpdatedMail = "";

                List<List<Customer>> CustomersMaritechUpdatedChunked = CustomersMaritechUpdated
                    .Select((s, i) => new { Value = s, Index = i })
                    .GroupBy(x => x.Index / 100)
                    .Select(grp => grp.Select(x => x.Value).ToList())
                    .ToList();

                List<string> CustomersMaritechUpdatedXMLChunked = new List<string>();

                foreach (List<Customer> CustomersMaritechUpdatedChunk in CustomersMaritechUpdatedChunked)
                {
                    string CustomersMaritechUpdatedXMLChunk = "<DocumentElement>\r\n";
                    foreach (Customer c in CustomersMaritechUpdatedChunk)
                    {
                        CustomersMaritechUpdatedXMLChunk += XMLStringFromObject(c);
                        CustomersMaritechUpdatedMail += $"<li>{c.CustomerNo.ToString()}: {c.Name} (Active={(c.Active == 1)})</li>";
                    }
                    CustomersMaritechUpdatedXMLChunk += "\r\n</DocumentElement>";
                    byte[] ResXmlBytesChunk = Encoding.UTF8.GetBytes(CustomersMaritechUpdatedXMLChunk);
                    string ResXmlBase64Chunk = Convert.ToBase64String(ResXmlBytesChunk);
                    CustomersMaritechUpdatedXMLChunked.Add(ResXmlBase64Chunk);
                }

                if (CustomersMaritechUpdated.Count > 0)
                {
                    CustomersMaritechUpdatedXML += "<DocumentElement>\r\n";
                    foreach (Customer c in CustomersMaritechUpdated)
                    {
                        CustomersMaritechUpdatedXML += XMLStringFromObject(c);
                        //CustomersMaritechUpdatedMail += $"<li>{c.CustomerNo.ToString()}: {c.Name} (Active={(c.Active == 1)})</li>";
                    }
                    CustomersMaritechUpdatedXML += "\r\n</DocumentElement>";
                }


                byte[] ResXmlBytes = Encoding.UTF8.GetBytes(CustomersMaritechUpdatedXML);
                string ResXmlBase64 = Convert.ToBase64String(ResXmlBytes);
                CustomersMaritechUpdatedXML = ResXmlBase64;

                byte[] ResMailBytes = Encoding.UTF8.GetBytes(CustomersMaritechUpdatedMail);
                string ResMailBase64 = Convert.ToBase64String(ResMailBytes);
                CustomersMaritechUpdatedMail = ResMailBase64;

                byte[] ResFailedMailBytes = Encoding.UTF8.GetBytes(CustomersMaritechFailedMail);
                string ResFailedMailBase64 = Convert.ToBase64String(ResFailedMailBytes);
                CustomersMaritechFailedMail = ResFailedMailBase64;

                string Response = $"{{" +
                        $"\"CustomersMaritechUpdatedXML\": \"{CustomersMaritechUpdatedXML}\"," +
                        $"\"CustomersMaritechUpdatedXMLChunked\": {JsonConvert.SerializeObject(CustomersMaritechUpdatedXMLChunked)}," +
                        $"\"CustomersMaritechPersistentNew\": {JsonConvert.SerializeObject(CustomersMaritechPersistentNew)}," +
                        $"\"CustomersMaritechUpdatedMail\": \"{CustomersMaritechUpdatedMail}\"," +
                        $"\"CustomersMaritechFailedMail\": \"{CustomersMaritechFailedMail}\"" +
                        $"}}";
                byte[] ResponseBytes = Encoding.UTF8.GetBytes(Response);
                string ResponseBase64 = Convert.ToBase64String(ResponseBytes);
                return new OkObjectResult(ResponseBase64);

            }
            catch (Exception ex)
            {
                log.LogInformation($"ConvertAndCompareCustomers files error: {ex.Message}");
                return new BadRequestObjectResult($"ConvertAndCompareCustomers files error: {ex.Message}");
            }
        }

        [FunctionName("ConvertAndCompareVendors")]
        public static async Task<IActionResult> ConvertAndCompareVendors([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation($"ConvertAndCompareVendors HTTP triggered {req.Method} request");
            try
            {
                log.LogInformation("ConvertAndCompareVendors Get reqest..");
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                log.LogInformation("ConvertAndCompareVendors Parse reqest..");

                #region Parse input

                byte[] data = Convert.FromBase64String(requestBody);
                string ReqString = Encoding.UTF8.GetString(data);

                //JsonDocument d = JsonDocument.Parse(ReqString);
                dynamic RequestJSON = JsonConvert.DeserializeObject(ReqString);

                /*
                //Get only those vendors, where MBSMarkedForTransfer == "yes"
                Dictionary<string, VendorD365> VendorssD365 = JsonConvert.DeserializeObject<List<VendorD365>>(d.RootElement.GetProperty("VendorsD365").ToString()).Where(v => v.MBSMarkedForTransfer.ToLower() == "yes").ToDictionary(k => k.VendorAccountNumber);
                //Dictionary<string, VendorD365> VendorssD365 = JsonConvert.DeserializeObject<List<VendorD365>>(d.RootElement.GetProperty("VendorsD365").ToString()).ToDictionary(k => k.VendorAccountNumber);
                List<VendorBankAccountD365> VendorBankAccountsD365 = JsonConvert.DeserializeObject<List<VendorBankAccountD365>>(d.RootElement.GetProperty("VendorBankAccountsD365").ToString());
                Dictionary<Int64, Vendor> VendorsMaritechPersistent = JsonConvert.DeserializeObject<List<Vendor>>(d.RootElement.GetProperty("VendorsMaritechPersistent").ToString()).ToDictionary(k => k.VendorNo);
                string ClientNo = JsonConvert.DeserializeObject<string>(d.RootElement.GetProperty("ClientNo").ToString());
                */

                string R_VendorsD365 = RequestJSON.VendorsD365.ToString();
                Dictionary<string, VendorD365> VendorssD365 = JsonConvert.DeserializeObject<List<VendorD365>>(R_VendorsD365).Where(v => v.MBSMarkedForTransfer.ToLower() == "yes").ToDictionary(k => k.VendorAccountNumber);
                //Dictionary<string, VendorD365> VendorssD365 = JsonConvert.DeserializeObject<List<VendorD365>>(d.RootElement.GetProperty("VendorsD365").ToString()).ToDictionary(k => k.VendorAccountNumber);
                string R_VendorBankAccountsD365 = RequestJSON.VendorBankAccountsD365.ToString();
                List<VendorBankAccountD365> VendorBankAccountsD365 = JsonConvert.DeserializeObject<List<VendorBankAccountD365>>(R_VendorBankAccountsD365);
                string R_VendorsMaritechPersistent = RequestJSON.VendorsMaritechPersistent.ToString();
                Dictionary<Int64, Vendor> VendorsMaritechPersistent = JsonConvert.DeserializeObject<List<Vendor>>(R_VendorsMaritechPersistent).ToDictionary(k => k.VendorNo);
                string ClientNo = JsonConvert.DeserializeObject<string>(RequestJSON.ClientNo.ToString());

                #endregion

                Dictionary<Int64, Vendor> VendorsD365Converted = new Dictionary<Int64, Vendor>();
                List<Vendor> VendorsMaritechUpdated = new List<Vendor>();
                List<Vendor> VendorsMaritechPersistentNew = new List<Vendor>();
                string VendorsMaritechFailedMail = "";

                log.LogInformation("ConvertAndCompareVendors Convert data..");

                #region convert D365 to Maritech
                foreach (VendorD365 vd in VendorssD365.Values)
                {
                    try
                    {
                        Int64 VendorNo = 0;
                        Int64.TryParse(vd.VendorAccountNumber.Trim(), out VendorNo);
                        if (VendorNo != 0 && vd.VendorOrganizationName.Trim() != "")
                        {
                            Vendor vm = new Vendor();
                            //As per clarification 2022-02-16: Send to Maritech VendorNo instead of ExtVendorNo

                            vm.VendorNo = VendorNo;
                            vm.ClientNo = ClientNo;
                            if (vd.OnHoldStatus.Trim().ToUpper() != "ALL" || vd.MBSMarkedForTransfer == "yes")
                            {
                                vm.Active = 1;
                            }
                            else
                            {
                                vm.Active = 0;
                            }
                            //vm.Active = (vd.OnHoldStatus.Trim().ToUpper() == "ALL" ? 0 : 1); // as per clarification 2022-01-13

                            #region AddressLines
                            if (vd.AddressStreet.Trim().Contains("\n") && vd.AddressStreet.Trim().Replace("\r", "").Split("\n").Length < 4)
                            {
                                vm.Address1 = ((vd.AddressStreet.Trim().Replace("\r", "").Split("\n").Length >= 1 && vd.AddressStreet.Trim().Replace("\r", "").Split("\n")[0] != "") ? vd.AddressStreet.Trim().Replace("\r", "").Split("\n")[0] : "").Trim();
                                vm.Address2 = ((vd.AddressStreet.Trim().Replace("\r", "").Split("\n").Length >= 2 && vd.AddressStreet.Trim().Replace("\r", "").Split("\n")[1] != "") ? vd.AddressStreet.Trim().Replace("\r", "").Split("\n")[1] : "").Trim();
                                vm.Address3 = ((vd.AddressStreet.Trim().Replace("\r", "").Split("\n").Length >= 3 && vd.AddressStreet.Trim().Replace("\r", "").Split("\n")[2] != "") ? vd.AddressStreet.Trim().Replace("\r", "").Split("\n")[2] : "").Trim();
                            }
                            else
                            {
                                vm.Address1 = vd.AddressStreet.Trim().Replace("\r", "").Replace("\n", " ").Trim();
                                vm.Address2 = "";
                                vm.Address3 = "";
                            }
                            #endregion
                            //P&S removed: 
                            /*
                            #region VendorBankAccount
                            // Pick the primary vendor bank account - check, if expired. If yes, search for other active bank account.

                            //Default bank account is active 
                            VendorBankAccountD365 vdba = VendorBankAccountsD365.Where(b => b.VendorAccountNumber == vd.VendorAccountNumber && b.VendorBankAccountId == vd.BankAccountId).FirstOrDefault();
                            if (
                                vdba != null
                                &&
                                (vdba.ActiveDate ?? DateTime.MinValue) <= DateTime.Now
                                && (
                                    ((vdba.ExpirationDate ?? DateTime.MaxValue) >= DateTime.Now)
                                    ||
                                    (vdba.ExpirationDate == DateTime.Parse("1900-01-01 00:00:00"))
                                )
                            )
                            {
                                //1. Pick BankAccountNumber if not empty
                                //removed: 2. Pick ForeignBankAccountNumber if not empty
                                //removed: Removed: Pick IBAN if not empty
                                //// Update 2022-01-14 (clarification session) / removed: vm.BankAccount = (vdba.BankAccountNumber != "" ? vdba.BankAccountNumber : (vdba.ForeignBankAccountNumber != "" ? vdba.ForeignBankAccountNumber : (vdba.IBAN != "" ? vdba.IBAN : "")));
                                vm.BankAccount = (vdba.BankAccountNumber != "" ? vdba.BankAccountNumber : "");
                            }
                            //Default bank account is not active
                            else
                            {
                                //Other non-default active bank accounts
                                vdba = VendorBankAccountsD365.Where(b =>
                                    b.VendorAccountNumber == vd.VendorAccountNumber
                                    && (b.ActiveDate ?? DateTime.MinValue) <= DateTime.Now
                                    && (
                                        (b.ExpirationDate ?? DateTime.MaxValue) >= DateTime.Now
                                        ||
                                        b.ExpirationDate == DateTime.Parse("1900-01-01 00:00:00")
                                    )
                                ).FirstOrDefault();
                                if (vdba != null)
                                {
                                    // Update 2022-01-14 (clarification session) / removed: vm.BankAccount = (vdba.BankAccountNumber != "" ? vdba.BankAccountNumber : (vdba.ForeignBankAccountNumber != "" ? vdba.ForeignBankAccountNumber : (vdba.IBAN != "" ? vdba.IBAN : "")));
                                    vm.BankAccount = (vdba.BankAccountNumber != "" ? vdba.BankAccountNumber : "");
                                }
                                else
                                {
                                    vm.BankAccount = "";
                                }
                            }
                            #endregion
                            */
                            vm.City = vd.AddressCity.Trim();
                            vm.CountryCode = vd.AddressCountryRegionISOCode.Trim().ToUpper();
                            vm.CurrencyCode = vd.CurrencyCode.Trim().ToUpper();
                            //P&S removed: 
                            //vm.CustNoAtVendor = vd.VendorGroupId.Trim() == "70" ? 1 : 0; // as per clarification 2022-01-13
                            vm.Email = vd.PrimaryEmailAddress.Trim();
                            //vm.ExtVendorNo = vd.VendorAccountNumber.Trim();
                            //P&S removed: 
                            /*
                            #region LanguageCode  // as per clarification 2022-01-13
                            switch (vd.LanguageId.Trim().ToUpper())
                            {
                                // Clarification session 2022-01-14 - removed:
                                case "NB-NO":
                                { vm.LanguageCode = 47; break; }
                                default:
                                { vm.LanguageCode = 44; break; }
                            }
                            #endregion
                            */
                            vm.Name = vd.VendorOrganizationName.Trim();
                            vm.OrganizationNo = vd.TaxExemptNumber.Trim(); // as per clarification 2022-01-13 -> Can be empty (before vm.OrganizationNo = vd.OrganizationNumber.Trim(); )
                            vm.PostalCode = (vd.AddressZipCode.Trim() == "" ? "0" : vd.AddressZipCode.Trim());
                            vm.Telephone1 = vd.PrimaryPhoneNumber.Trim();
                            //P&S removed: 
                            //vm.VATCode = (vd.SalesTaxGroupCode.Trim().ToLower() == "lipl" ? 0 : 1); // as per clarification 2022-01-13
                            //Clarification session 2022-01-14 / removed: vm.VATNo = vd.TaxExemptNumber; // as per clarification 2022-01-13 -> Can be empty
                            //HACK: Limit only to vendorNo > 60000
                            if (vm.VendorNo > 60000 && vm.VendorNo < 100000)
                            {
                                VendorsD365Converted.Add(vm.VendorNo, vm);
                            }
                        }
                        else
                        {
                            VendorsMaritechFailedMail += $"<li><span style=\"color: rgb(226, 80, 65)\">Failed to convert D365 vendor {vd.VendorAccountNumber} / {vd.VendorOrganizationName}: {(VendorNo == 0 ? " unable to convert " + vd.VendorAccountNumber + " to integer" : "")}{(vd.VendorOrganizationName.Trim() == "" ? " missing vendor name" : "")}</span></li>\r\n";
                        }
                    }
                    catch (Exception ex)
                    {
                        log.LogInformation($"ConvertAndCompareVendors error {vd.VendorAccountNumber} - {vd.VendorOrganizationName}: {ex.Message}");
                        //return new BadRequestObjectResult($"ConvertAndCompareVendors files error: {ex.Message}");
                    }
                }
                #endregion

                log.LogInformation("ConvertAndCompareVendors Compare data..");

                #region update & create

                foreach (Vendor vd in VendorsD365Converted.Values)
                {
                    VendorsMaritechPersistentNew.Add(vd);
                    if (VendorsMaritechPersistent.ContainsKey(vd.VendorNo))
                    {
                        if (JsonConvert.SerializeObject(vd) != JsonConvert.SerializeObject(VendorsMaritechPersistent[vd.VendorNo]))
                        {
                            VendorsMaritechUpdated.Add(vd);
                        }
                    }
                    else if (vd.Active == 1)
                    {
                        VendorsMaritechUpdated.Add(vd);
                    }
                }

                #endregion

                #region deactivate

                foreach (Vendor vm in VendorsMaritechPersistent.Values)
                {
                    // HACK: Limit only to vendorNo > 60000
                    if (!VendorsD365Converted.ContainsKey(vm.VendorNo) && vm.VendorNo > 60000 && vm.VendorNo < 100000)
                    {
                        if ((VendorsMaritechPersistent.ContainsKey(vm.VendorNo) && VendorsMaritechPersistent[vm.VendorNo].Active == 1) || !VendorsMaritechPersistent.ContainsKey(vm.VendorNo))
                        {
                            vm.Active = 0;
                            VendorsMaritechUpdated.Add(vm);
                        }
                        VendorsMaritechPersistentNew.Add(vm);
                    }
                }

                #endregion

                /*
                {
                    "VendorssMaritechUpdatedXML": "[XML/string]",
                    "VendorsMaritechPersistentNew": "[JSON]"
                    "VendorsMaritechUpdatedMail": "[string]",
                }
                */

                string VendorsMaritechUpdatedXML = "";
                string VendorsMaritechUpdatedMail = "";

                List<List<Vendor>> VendorsMaritechUpdatedChunked = VendorsMaritechUpdated
                    .Select((s, i) => new { Value = s, Index = i })
                    .GroupBy(x => x.Index / 100)
                    .Select(grp => grp.Select(x => x.Value).ToList())
                    .ToList();

                List<string> VendorsMaritechUpdatedXMLChunked = new List<string>();

                foreach (List<Vendor> VendorsMaritechUpdatedChunk in VendorsMaritechUpdatedChunked)
                {
                    string VendorsMaritechUpdatedXMLChunk = "";
                    if (VendorsMaritechUpdatedChunk.Count > 0)
                    {
                        VendorsMaritechUpdatedXMLChunk += "<DocumentElement>\r\n";
                        foreach (Vendor v in VendorsMaritechUpdatedChunk)
                        {
                            VendorsMaritechUpdatedXMLChunk += XMLStringFromObject(v);
                            VendorsMaritechUpdatedMail += $"<li>{v.VendorNo}: {v.Name} (Active={(v.Active == 1)})</li>";
                        }
                        VendorsMaritechUpdatedXMLChunk += "\r\n</DocumentElement>";

                        byte[] ResXmlBytesChunk = Encoding.UTF8.GetBytes(VendorsMaritechUpdatedXMLChunk);
                        string ResXmlBase64Chunk = Convert.ToBase64String(ResXmlBytesChunk);
                        VendorsMaritechUpdatedXMLChunked.Add(ResXmlBase64Chunk);
                    }
                }

                if (VendorsMaritechUpdated.Count > 0)
                {
                    VendorsMaritechUpdatedXML += "<DocumentElement>\r\n";
                    foreach (Vendor v in VendorsMaritechUpdated)
                    {
                        VendorsMaritechUpdatedXML += XMLStringFromObject(v);
                        //VendorsMaritechUpdatedMail += $"<li>{v.VendorNo}: {v.Name} (Active={(v.Active == 1)})</li>";
                    }
                    VendorsMaritechUpdatedXML += "\r\n</DocumentElement>";
                }

                byte[] ResXmlBytes = Encoding.UTF8.GetBytes(VendorsMaritechUpdatedXML);
                string ResXmlBase64 = Convert.ToBase64String(ResXmlBytes);
                VendorsMaritechUpdatedXML = ResXmlBase64;

                byte[] ResMailBytes = Encoding.UTF8.GetBytes(VendorsMaritechUpdatedMail);
                string ResMailBase64 = Convert.ToBase64String(ResMailBytes);
                VendorsMaritechUpdatedMail = ResMailBase64;

                byte[] ResFailedMailBytes = Encoding.UTF8.GetBytes(VendorsMaritechFailedMail);
                string ResMFailedailBase64 = Convert.ToBase64String(ResFailedMailBytes);
                VendorsMaritechFailedMail = ResMFailedailBase64;

                string Response = $"{{" +
                    $"\"VendorsMaritechUpdatedXML\": \"{VendorsMaritechUpdatedXML}\"," +
                    $"\"VendorsMaritechUpdatedXMLChunked\": {JsonConvert.SerializeObject(VendorsMaritechUpdatedXMLChunked)}," +
                    $"\"VendorsMaritechPersistentNew\": {JsonConvert.SerializeObject(VendorsMaritechPersistentNew)}," +
                    $"\"VendorsMaritechUpdatedMail\": \"{VendorsMaritechUpdatedMail}\"," +
                    $"\"VendorsMaritechFailedMail\": \"{VendorsMaritechFailedMail}\"" +
                    $"}}";

                byte[] ResponseBytes = Encoding.UTF8.GetBytes(Response);
                string ResponseBase64 = Convert.ToBase64String(ResponseBytes);
                return new OkObjectResult(ResponseBase64);

            }
            catch (Exception ex)
            {
                log.LogInformation($"ConvertAndCompareVendors files error: {ex.Message}");
                return new BadRequestObjectResult($"ConvertAndCompareVendors files error: {ex.Message}");
            }
        }

        [FunctionName("ConvertPostedEntriesV2")]
        public static async Task<IActionResult> ConvertPostedEntriesV2([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation($"ConvertPostedEntries HTTP triggered {req.Method} request");
            PostedEntryResultV2 RES = new PostedEntryResultV2();
            RES.ConvertedSalesOrders = new List<ConversionResultMessageV2>();
            RES.FailedSalesOrders = new List<ConversionResultMessageV2>();
            RES.D365SalesOrdersXML = "";
            RES.ConvertedPurchaseOrders = new List<ConversionResultMessageV2>();
            RES.FailedPurchaseOrders = new List<ConversionResultMessageV2>();
            RES.D365PurchaseOrdersXML = "";
            RES.ConvertedGLEntries = new List<ConversionResultMessageV2>();
            RES.FailedGLEntries = new List<ConversionResultMessageV2>();
            //RES.D365GLEntriesXML = "";
            RES.StatusMessage = "";
            RES.Status = "Not executed";
            try
            {
                log.LogInformation("ConvertPostedEntriesV2 Get reqest..");
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                log.LogInformation("ConvertPostedEntriesV2 Process input..");
                MTPostedEntriesRequestObject RequestObj = ProcessInput(requestBody);
                Dictionary<string, MTDocument> mtDoc = RequestObj.MTDoc;
                Dictionary<string, CustomerD365> CustomersD365 = RequestObj.CustomersD365;
                Dictionary<string, VendorD365> VendorsD365 = RequestObj.VendorsD365;
                Dictionary<string, ExchangeRateD365> ExchangeRatesD365 = RequestObj.ExchangeRatesD365;
                List<VATmap> VATmaps = RequestObj.VATmaps;
                DateTime? ShiftDateOlderThan = RequestObj.ShiftDateOlderThan;
                DateTime? ShiftDateChangeTo = RequestObj.ShiftDateChangeTo;
                string PurchaseOrderMultiHeaderPoolId = RequestObj.PurchaseOrderMultiHeaderPoolId;
                log.LogInformation("ConvertPostedEntries Convert values..");
                List<SALESORDERHEADERV2ENTITY> SalesOrdersD365XML = new List<SALESORDERHEADERV2ENTITY>();
                LEDGERJOURNALENTITY GLEntryAllLines = new LEDGERJOURNALENTITY();
                GLEntryAllLines.LedgerJournalEntityLines = new List<LedgerJournalEntityLine>();
                ConversionResultMessageV2 GLEntryAllLinesCRM = new ConversionResultMessageV2();
                string GLEntryDescription = $"AVDB-{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff")}";
                int GLEntryLineNr = 0;
                int ErrCnt = 0;
                int RunCnt = 0;
                foreach (string DocumentNr in mtDoc.Keys)
                {
                    //Registration types:
                    //        1 = Sales ledger
                    //        2 = VAT
                    //        3 = General ledger

                    //Code:
                    //    H = GeneralLedger,
                    //    K = Customer,
                    //    L = Vendor

                    //Documen Types: Set of codes describing type of document:
                    //               SF = Outgoing invoice, SK = Outgoing credit note,
                    //               KF = Incoming invoice, KK = Incoming credit note,
                    //               IP = Internal posting, FF = Currency hedge, FC = Factoring 
                    if (mtDoc[DocumentNr].postedEntries.Where(p => p.RegistrationType.Trim().ToUpper() == "1" && p.Code.Trim().ToUpper() == "K" && (p.DocumentType.Trim().ToUpper() == "SF" || p.DocumentType.Trim().ToUpper() == "SK")).Any())
                    {
                        #region Sales Order
                        RunCnt++;
                        MTPostedEntry MTPEntry = mtDoc[DocumentNr].postedEntries.Where(p => p.RegistrationType.Trim().ToUpper() == "1" && p.Code.Trim().ToUpper() == "K" && (p.DocumentType.Trim().ToUpper() == "SF" || p.DocumentType.Trim().ToUpper() == "SK")).FirstOrDefault();
                        try
                        {
                            SALESORDERHEADERV2ENTITY SalesOrderHeaderXML = ConvertSalesOrder(DocumentNr, mtDoc[DocumentNr], CustomersD365, VATmaps, ShiftDateOlderThan, ShiftDateChangeTo, RequestObj.OverrideSalesOrderLineSalesTaxItemGroupCode, RequestObj.OverrideSalesOrderLineItemNo, ExchangeRatesD365, RequestObj.DimensionIntegrationFormats);
                            string XMLstr = $"<Document>{XMLStringFromObject(SalesOrderHeaderXML)}</Document>";
                            RES.ConvertedSalesOrders.Add(
                                new ConversionResultMessageV2
                                {
                                    PostedEntryMessageId = MTPEntry.MaritechMessageId,
                                    StatusMessage = $"Successfully converted document Number={MTPEntry.DocumentNo}, Type={MTPEntry.DocumentType}, CustomerNumber={MTPEntry.CustomerNo} {(SalesOrderHeaderXML.MBSMARITECHINVOICEDATE != SalesOrderHeaderXML.REQUESTEDSHIPPINGDATE ? ("(Invoice date changed from " + SalesOrderHeaderXML.REQUESTEDSHIPPINGDATE + " to " + SalesOrderHeaderXML.MBSMARITECHINVOICEDATE) + ")" : "")}",
                                    Type = MTPEntry.DocumentType,
                                    SalesOrder = SalesOrderHeaderXML,
                                    PurchaseOrder = null,
                                    LedgerJournalEntity = null,
                                    LedgerJournalEntityXML = ""
                                }
                                );
                            RES.StatusMessage += $"<LI>Successfully converted document Number={MTPEntry.DocumentNo}, Type={MTPEntry.DocumentType}, CustomerNumber={MTPEntry.CustomerNo}, MaritechMessageId={MTPEntry.MaritechMessageId} {(SalesOrderHeaderXML.MBSMARITECHINVOICEDATE != SalesOrderHeaderXML.REQUESTEDSHIPPINGDATE ? ("<b>(Invoice date changed from " + SalesOrderHeaderXML.REQUESTEDSHIPPINGDATE + " to " + SalesOrderHeaderXML.MBSMARITECHINVOICEDATE) + ")</b>" : "")}</LI>";
                            RES.D365SalesOrdersXML += XMLstr;
                        }
                        catch (Exception ex)
                        {
                            ErrCnt++;
                            RES.FailedSalesOrders.Add(
                                new ConversionResultMessageV2
                                {
                                    PostedEntryMessageId = MTPEntry.MaritechMessageId,
                                    StatusMessage = $"Converstion error for document nr. {MTPEntry.DocumentNo}: {ex.Message}",
                                    Type = MTPEntry.DocumentType,
                                    SalesOrder = null,
                                    PurchaseOrder = null,
                                    LedgerJournalEntity = null,
                                    LedgerJournalEntityXML = ""
                                }
                                );
                            RES.StatusMessage += $"<LI><span style=\"color: rgb(184,49,47)\"><b>Converstion ERROR</b> for MaritechMessageId={MTPEntry.MaritechMessageId}, Document Number={MTPEntry.DocumentNo}, Type={MTPEntry.DocumentType}, CustomerNumber={MTPEntry.CustomerNo}: {ex.Message}</span></LI>";
                        }
                        #endregion
                    }

                    else if (mtDoc[DocumentNr].postedEntries.Where(p => p.RegistrationType.Trim().ToUpper() == "1" && p.Code.Trim().ToUpper() == "L" && (p.DocumentType.Trim().ToUpper() == "KF" || p.DocumentType.Trim().ToUpper() == "KK")).Any())
                    {
                        #region Purchase Order
                        RunCnt++;
                        MTPostedEntry MTPEntry = mtDoc[DocumentNr].postedEntries.Where(p => p.RegistrationType.Trim().ToUpper() == "1" && p.Code.Trim().ToUpper() == "L" && (p.DocumentType.Trim().ToUpper() == "KF" || p.DocumentType.Trim().ToUpper() == "KK")).FirstOrDefault();
                        List<MTPostedEntry> MTPEntries = mtDoc[DocumentNr].postedEntries.Where(p => p.RegistrationType.Trim().ToUpper() == "1" && p.Code.Trim().ToUpper() == "L" && (p.DocumentType.Trim().ToUpper() == "KF" || p.DocumentType.Trim().ToUpper() == "KK")).ToList();
                        try
                        {
                            PURCHPURCHASEORDERHEADERV2ENTITY PurchaseOrderHeaderXML = ConvertPurchaseOrder(DocumentNr, mtDoc[DocumentNr], VendorsD365, VATmaps, ShiftDateOlderThan, ShiftDateChangeTo, ExchangeRatesD365, PurchaseOrderMultiHeaderPoolId, RequestObj.DimensionIntegrationFormats);
                            string XMLstr = $"<Document>{XMLStringFromObject(PurchaseOrderHeaderXML)}</Document>";
                            RES.ConvertedPurchaseOrders.Add(
                                new ConversionResultMessageV2
                                {
                                    PostedEntryMessageId = MTPEntry.MaritechMessageId,
                                    StatusMessage = $"Successfully converted document Number={MTPEntry.DocumentNo}, Type={MTPEntry.DocumentType}, VendorNo={MTPEntry.VendorNo} {(PurchaseOrderHeaderXML.MBSMARITECHINVOICEDATE != PurchaseOrderHeaderXML.REQUESTEDDELIVERYDATE ? ("(Invoice date changed from " + PurchaseOrderHeaderXML.REQUESTEDDELIVERYDATE + " to " + PurchaseOrderHeaderXML.MBSMARITECHINVOICEDATE) + ")" : "")}",
                                    Type = MTPEntry.DocumentType,
                                    SalesOrder = null,
                                    PurchaseOrder = PurchaseOrderHeaderXML,
                                    LedgerJournalEntity = null,
                                    LedgerJournalEntityXML = ""
                                }
                                );
                            RES.StatusMessage += $"<LI>Successfully converted document Number={MTPEntry.DocumentNo}, Type={MTPEntry.DocumentType}, VendorNo={MTPEntry.VendorNo}, MaritechMessageId={MTPEntry.MaritechMessageId} {(PurchaseOrderHeaderXML.MBSMARITECHINVOICEDATE != PurchaseOrderHeaderXML.REQUESTEDDELIVERYDATE ? ("<b>(Invoice date changed from " + PurchaseOrderHeaderXML.REQUESTEDDELIVERYDATE + " to " + PurchaseOrderHeaderXML.MBSMARITECHINVOICEDATE) + ")</b>" : "")}</LI>";
                            RES.D365PurchaseOrdersXML += XMLstr;

                        }
                        catch (Exception ex)
                        {
                            ErrCnt++;
                            RES.FailedPurchaseOrders.Add(
                                new ConversionResultMessageV2
                                {
                                    PostedEntryMessageId = MTPEntry.MaritechMessageId,
                                    StatusMessage = $"Converstion error for document nr. {MTPEntry.DocumentNo}: {ex.Message}",
                                    Type = MTPEntry.DocumentType,
                                    SalesOrder = null,
                                    PurchaseOrder = null,
                                    LedgerJournalEntity = null,
                                    LedgerJournalEntityXML = ""
                                }
                                );
                            RES.StatusMessage += $"<LI><span style=\"color: rgb(184,49,47)\"><b>Converstion ERROR</b> for MaritechMessageId={MTPEntry.MaritechMessageId}, Document Number={MTPEntry.DocumentNo}, Type={MTPEntry.DocumentType}, VendorNo={MTPEntry.VendorNo}: {ex.Message}</span></LI>";
                        }
                        #endregion
                    }
                    else if (
                        //There is no Customer (K) or Vendor (L) entry
                        !mtDoc[DocumentNr].postedEntries.Where(p =>
                            p.RegistrationType.Trim().ToUpper() == "1"
                            && (p.Code.Trim().ToUpper() == "L" || p.Code.Trim().ToUpper() == "K")
                        ).Any()
                        &&
                        //and there are only General Ledger (H) entries with General ledger registrations (3)
                        mtDoc[DocumentNr].postedEntries.Where(p =>
                            (p.RegistrationType.Trim().ToUpper() == "3")
                            && (p.Code.Trim().ToUpper() == "H")
                        ).Count() == mtDoc[DocumentNr].postedEntries.Count()
                        )
                    {
                        #region General ledger (Avdelningsbelastning)
                        RunCnt++;
                        try
                        {
                            LEDGERJOURNALENTITY GLEntry = ConvertGLEntry(DocumentNr, mtDoc[DocumentNr], ExchangeRatesD365, GLEntryDescription, GLEntryLineNr, RequestObj.DimensionIntegrationFormats);
                            foreach (LedgerJournalEntityLine GLEntryLine in GLEntry.LedgerJournalEntityLines)
                            {
                                GLEntryLineNr++;
                                GLEntryAllLines.LedgerJournalEntityLines.Add(GLEntryLine);
                            }
                            GLEntryAllLinesCRM.PostedEntryMessageId += (mtDoc[DocumentNr].postedEntries.FirstOrDefault() != null ? mtDoc[DocumentNr].postedEntries[0].MaritechMessageId + " " : " ");
                            GLEntryAllLinesCRM.StatusMessage += $"Successfully converted document Number={DocumentNr}, Type={"H"} <br>";
                            GLEntryAllLinesCRM.Type += "H ";

                            ConversionResultMessageV2 CRM = new ConversionResultMessageV2
                            {
                                PostedEntryMessageId = (mtDoc[DocumentNr].postedEntries.FirstOrDefault() != null ? mtDoc[DocumentNr].postedEntries[0].MaritechMessageId : ""),
                                StatusMessage = $"Successfully converted document Number={DocumentNr}, Type={"H"}",
                                Type = "H",
                                SalesOrder = null,
                                PurchaseOrder = null,
                                LedgerJournalEntity = GLEntry,
                                LedgerJournalEntityXML = XMLStringFromObject(GLEntry)
                            };
                            RES.ConvertedGLEntries.Add(CRM);
                            RES.StatusMessage += $"<LI>Successfully converted document Number={DocumentNr}, Type=H</LI>";
                        }
                        catch (Exception ex)
                        {
                            ErrCnt++;
                            RES.FailedGLEntries.Add(
                                new ConversionResultMessageV2
                                {
                                    PostedEntryMessageId = (mtDoc[DocumentNr].postedEntries.FirstOrDefault() != null ? mtDoc[DocumentNr].postedEntries[0].MaritechMessageId : ""),
                                    StatusMessage = $"Converstion error for document nr. {DocumentNr}: {ex.Message}",
                                    Type = "H",
                                    SalesOrder = null,
                                    PurchaseOrder = null,
                                    LedgerJournalEntity = null,
                                    LedgerJournalEntityXML = ""
                                }
                                );
                            RES.StatusMessage += $"<LI><span style=\"color: rgb(184,49,47)\"><b>Converstion ERROR</b> for MaritechMessageId={(mtDoc[DocumentNr].postedEntries.FirstOrDefault() != null ? mtDoc[DocumentNr].postedEntries[0].MaritechMessageId : "")}, Document Number={DocumentNr}, Type=H: {ex.Message}</span></LI>";

                        }


                        #endregion
                    }
                }
                /*
                LEDGERJOURNALENTITY ResLedgerJournalEntity = new LEDGERJOURNALENTITY();
                ResLedgerJournalEntity.LedgerJournalEntityLines = new List<LedgerJournalEntityLine>();
                int MsgLineNumber = 0;
                string VoucherGuid = Guid.NewGuid().ToString();
                foreach (ConversionResultMessageV2 resmsg in RES.ConvertedGLEntries)
                {
                    foreach (LedgerJournalEntityLine resmsgline in resmsg.LedgerJournalEntity.LedgerJournalEntityLines)
                    {
                        MsgLineNumber++;
                        resmsgline.LINENUMBER = MsgLineNumber;
                        resmsgline.VOUCHER = (RES.ConvertedGLEntries.Count > 1 ? $"AVDB-{VoucherGuid}" : resmsgline.VOUCHER);
                        resmsgline.DESCRIPTION = (RES.ConvertedGLEntries.Count > 1 ? $"AVDB-{VoucherGuid}" : resmsgline.DESCRIPTION);
                        ResLedgerJournalEntity.LedgerJournalEntityLines.Add(resmsgline);
                    }
                }
                RES.D365GLEntriesXML += $"{XMLStringFromObject(ResLedgerJournalEntity)}";
                */
                RES.D365GLEntriesXML = (GLEntryAllLines.LedgerJournalEntityLines.Count > 0 ? XMLStringFromObject(GLEntryAllLines) : "");
                byte[] ResGLEntriesXml = Encoding.UTF8.GetBytes(RES.D365GLEntriesXML);
                string ResGLEntriesXmlBase64 = Convert.ToBase64String(ResGLEntriesXml);
                RES.D365GLEntriesXML = ResGLEntriesXmlBase64;

                RES.D365SalesOrdersXML = (RES.D365SalesOrdersXML == "" ? "" : $"<?xml version=\"1.0\" encoding=\"utf-8\"?>{RES.D365SalesOrdersXML}");
                byte[] ResSalesOrdersXmlBytes = Encoding.UTF8.GetBytes(RES.D365SalesOrdersXML);
                string ResSalesOrdersXmlBase64 = Convert.ToBase64String(ResSalesOrdersXmlBytes);
                RES.D365SalesOrdersXML = ResSalesOrdersXmlBase64;

                RES.D365PurchaseOrdersXML = (RES.D365PurchaseOrdersXML == "" ? "" : $"<?xml version=\"1.0\" encoding=\"utf-8\"?>{RES.D365PurchaseOrdersXML}");
                byte[] ResPurchaseOrdersXmlBytes = Encoding.UTF8.GetBytes(RES.D365PurchaseOrdersXML);
                string ResPurchaseOrdersXmlBase64 = Convert.ToBase64String(ResPurchaseOrdersXmlBytes);
                RES.D365PurchaseOrdersXML = ResPurchaseOrdersXmlBase64;

                foreach (ConversionResultMessageV2 GLEmessage in RES.ConvertedGLEntries)
                {
                    GLEmessage.LedgerJournalEntityXML = Convert.ToBase64String(Encoding.UTF8.GetBytes(GLEmessage.LedgerJournalEntityXML));
                }
                //RES.D365GLEntriesXML = (RES.D365GLEntriesXML == "" ? "" : $"<?xml version=\"1.0\" encoding=\"utf-8\"?>{RES.D365GLEntriesXML}");
                //byte[] ResGLEntriesXmlBytes = Encoding.UTF8.GetBytes(RES.D365GLEntriesXML);
                //string ResGLEntriesXmlBase64 = Convert.ToBase64String(ResGLEntriesXmlBytes);
                //RES.D365GLEntriesXML = ResGLEntriesXmlBase64;

                RES.Status = (ErrCnt == 0 ? "OK" : (ErrCnt >= RunCnt ? "Failed" : "WithErrors"));
                return new OkObjectResult(RES);

                /*
                string ResStr = JsonConvert.SerializeObject(RES);

                byte[] ResBytes = Encoding.UTF8.GetBytes(ResStr);
                string ResBase64 = Convert.ToBase64String(ResBytes);

                return new OkObjectResult(ResBase64);
                */

            }
            catch (Exception ex)
            {
                log.LogInformation($"ConvertPostedEntries files error: {ex.Message}");
                return new BadRequestObjectResult($"ConvertPostedEntries files error: {ex.Message}");
            }
        }

        [FunctionName("ZIPme")]
        public static async Task<IActionResult> ZipMe(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"ZIPme HTTP triggered {req.Method} request");
            try
            {
                log.LogInformation("Get reqest..");
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                log.LogInformation("Get files..");
                PackFile[] FilesToPack = JsonConvert.DeserializeObject<PackFile[]>(requestBody);
                Packer packer = new Packer();
                log.LogInformation("Zip files..");
                return new FileContentResult(packer.PackerZIP(FilesToPack).ToArray(), "application/octet-stream");
            }
            catch (Exception ex)
            {
                log.LogInformation($"ZIP files error: {ex.Message}");
                return new BadRequestObjectResult($"ZIP files error: {ex.Message}");
            }
        }

        [FunctionName("UNZIPme")]
        public static async Task<IActionResult> UnZipMe(
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
           ILogger log)
        {
            log.LogInformation($"UNZIPme HTTP triggered {req.Method} request");
            try
            {
                log.LogInformation("Get request..");
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                PackFile ZIPArchive = JsonConvert.DeserializeObject<PackFile>(requestBody);
                log.LogInformation("Get file..");
                byte[] zipBuffer = Convert.FromBase64String(ZIPArchive.FileContentBase64String);
                log.LogInformation("Get bytes..");
                Packer packer = new Packer();
                log.LogInformation("Unzip..");
                List<PackFile> UnzippedArchive = packer.PackerUNZIP(zipBuffer);
                return new OkObjectResult(JsonConvert.SerializeObject(UnzippedArchive));
            }
            catch (Exception ex)
            {
                log.LogInformation($"UNZIP files error: {ex.Message}");
                return new BadRequestObjectResult($"UNZIP file error: {ex.Message}");
            }
        }

        private static MTPostedEntriesRequestObject ProcessInput(string requestBody)
        {
            MTPostedEntriesRequestObject RES = new MTPostedEntriesRequestObject();
            RES.MTDoc = new Dictionary<string, MTDocument>();
            RES.CustomersD365 = new Dictionary<string, CustomerD365>();
            RES.VendorsD365 = new Dictionary<string, VendorD365>();
            RES.ExchangeRatesD365 = new Dictionary<string, ExchangeRateD365>();

            RES.ShiftDateChangeTo = null;
            RES.ShiftDateOlderThan = null;

            #region Parse input

            //Debug: Base64 conversion
            //string ReqString = requestBody;

            byte[] data = Convert.FromBase64String(requestBody);
            string ReqString = Encoding.UTF8.GetString(data);

            //JsonDocument d = JsonDocument.Parse(ReqString);

            dynamic RequestJSON = JsonConvert.DeserializeObject(ReqString);
            string R_MTMessagesPostedEntries = RequestJSON.MessagesPostedEntries.ToString();
            List<MTMessage> MTMessagesPostedEntries = JsonConvert.DeserializeObject<List<MTMessage>>(R_MTMessagesPostedEntries).ToList();
            string R_MTMessagesFakturaXML = RequestJSON.MessagesFakturaXML.ToString();
            List<MTMessage> MTMessagesFakturaXML = JsonConvert.DeserializeObject<List<MTMessage>>(R_MTMessagesFakturaXML).ToList();

            //Request from Thomas for Migration
            string ShiftDateOlderThanStr = "";
            try
            {
                //ShiftDateOlderThanStr = d.RootElement.GetProperty("ShiftDateOlderThan").ToString();
                ShiftDateOlderThanStr = RequestJSON.ShiftDateOlderThan.ToString();
                RES.ShiftDateOlderThan = DateTime.Parse(ShiftDateOlderThanStr);
            }
            catch (Exception ex)
            { }
            string ShiftDateChangeToStr = "";
            try
            {
                //ShiftDateChangeToStr = d.RootElement.GetProperty("ShiftDateChangeTo").ToString();
                ShiftDateChangeToStr = RequestJSON.ShiftDateChangeTo.ToString();
                RES.ShiftDateChangeTo = DateTime.Parse(ShiftDateChangeToStr);
            }
            catch (Exception ex)
            { }
            try
            {
                //RES.VATmaps = JsonConvert.DeserializeObject<List<VATmap>>(d.RootElement.GetProperty("VATmap").ToString()).ToList();
                string R_VATmap = RequestJSON.VATmap.ToString();
                RES.VATmaps = JsonConvert.DeserializeObject<List<VATmap>>(R_VATmap).ToList();
            }
            catch (Exception ex)
            { }
            try
            {
                //RES.CustomersD365 = JsonConvert.DeserializeObject<List<CustomerD365>>(d.RootElement.GetProperty("CustomersD365").ToString()).ToDictionary(c => c.CustomerAccount.Trim().ToUpper());
                string R_CustomersD365 = RequestJSON.CustomersD365.ToString();
                RES.CustomersD365 = JsonConvert.DeserializeObject<List<CustomerD365>>(R_CustomersD365).ToDictionary(c => c.CustomerAccount.Trim().ToUpper());
            }
            catch (Exception ex)
            { }
            try
            {
                //RES.VendorsD365 = JsonConvert.DeserializeObject<List<VendorD365>>(d.RootElement.GetProperty("VendorsD365").ToString()).ToDictionary(c => c.VendorAccountNumber.Trim().ToUpper());
                string R_VendorsD365 = RequestJSON.VendorsD365.ToString();
                RES.VendorsD365 = JsonConvert.DeserializeObject<List<VendorD365>>(R_VendorsD365).ToDictionary(c => c.VendorAccountNumber.Trim().ToUpper());
            }
            catch (Exception ex)
            { }
            try
            {
                //RES.OverrideSalesOrderLineItemNo = d.RootElement.GetProperty("OverrideSalesOrderLineItemNo").ToString();
                string R_OverrideSalesOrderLineItemNo = RequestJSON.OverrideSalesOrderLineItemNo.ToString();
                RES.OverrideSalesOrderLineItemNo = R_OverrideSalesOrderLineItemNo;
            }
            catch (Exception ex)
            { }
            try
            {
                //RES.OverrideSalesOrderLineSalesTaxItemGroupCode = d.RootElement.GetProperty("OverrideSalesOrderLineSalesTaxItemGroupCode").ToString();
                RES.OverrideSalesOrderLineSalesTaxItemGroupCode = RequestJSON.OverrideSalesOrderLineSalesTaxItemGroupCode.ToString();
            }
            catch (Exception ex)
            { }
            try
            {
                //RES.ExchangeRatesD365 = JsonConvert.DeserializeObject<List<ExchangeRateD365>>(d.RootElement.GetProperty("ExchangeRates").ToString()).ToDictionary(c => c.FromCurrency.Trim().ToUpper());
                string R_ExchangeRates = RequestJSON.ExchangeRates.ToString();
                RES.ExchangeRatesD365 = JsonConvert.DeserializeObject<List<ExchangeRateD365>>(R_ExchangeRates).ToDictionary(c => c.FromCurrency.Trim().ToUpper());
            }
            catch (Exception ex)
            { }
            try
            {
                //RES.PurchaseOrderMultiHeaderPoolId = d.RootElement.GetProperty("PurchaseOrderMultiHeaderPoolId").ToString();
                string R_PurchaseOrderMultiHeaderPoolId = RequestJSON.PurchaseOrderMultiHeaderPoolId.ToString();
                RES.PurchaseOrderMultiHeaderPoolId = R_PurchaseOrderMultiHeaderPoolId;
            }
            catch (Exception ex)
            { }
            try
            {
                //RES.DimensionIntegrationFormats = JsonConvert.DeserializeObject<List<DimensionIntegrationFormat>>(d.RootElement.GetProperty("DimensionIntegrationFormats").ToString()).ToDictionary(c => c.DimensionFormatType.Trim());
                string R_DimensionIntegrationFormats = RequestJSON.DimensionIntegrationFormats.ToString();
                RES.DimensionIntegrationFormats = JsonConvert.DeserializeObject<List<DimensionIntegrationFormat>>(R_DimensionIntegrationFormats).ToDictionary(c => c.DimensionFormatType.Trim());
            }
            catch (Exception ex)
            { }
            #endregion

            Dictionary<string, MTDocument> mtDoc = new Dictionary<string, MTDocument>();

            List<string> UnprocessedMessages = new List<string>();

            #region Sort PostedEntries (Dictionary[DocumentNumber].ListOfPostedEntries)
            foreach (MTMessage Msg in MTMessagesPostedEntries)
            {
                byte[] ByteData = Convert.FromBase64String(Msg.MessageContent);
                string MContent = Encoding.UTF8.GetString(ByteData);

                //XML messages to objects
                XmlDocument xmldoc = new XmlDocument();
                xmldoc.LoadXml(MContent);
                XmlNodeList nodeList = xmldoc.SelectNodes("/DocumentElement/*");
                foreach (XmlNode node in nodeList)
                {
                    MTPostedEntry PostedEntry = new MTPostedEntry();
                    XmlSerializer serializer = new XmlSerializer(typeof(MTPostedEntry), new XmlRootAttribute("PostedEntries"));
                    using (XmlReader Xreader = new XmlNodeReader(node))
                    {
                        PostedEntry = (MTPostedEntry)serializer.Deserialize(Xreader);
                        PostedEntry.MaritechMessageId = Msg.messageId;
                    }
                    if (PostedEntry != null)
                    {
                        if (!mtDoc.ContainsKey(PostedEntry.DocumentNo))
                        {
                            mtDoc.Add(PostedEntry.DocumentNo, new MTDocument { fakturaXML = new List<MTFakturaXML>(), postedEntries = new List<MTPostedEntry>() });
                        }
                        mtDoc[PostedEntry.DocumentNo].postedEntries.Add(PostedEntry);
                    }
                }
            }
            #endregion

            #region SortFakturaXML (Dictionary[DocumentNumber].ListOfFakturaXML)
            foreach (MTMessage Msg in MTMessagesFakturaXML)
            {
                byte[] ByteData = Convert.FromBase64String(Msg.MessageContent);
                string MContent = Encoding.UTF8.GetString(ByteData);

                //XML messages to objects
                XmlDocument xmldoc = new XmlDocument();
                xmldoc.LoadXml(MContent);
                XmlNodeList nodeList = xmldoc.SelectNodes("/DocumentElement/*");
                foreach (XmlNode node in nodeList)
                {
                    MTFakturaXML faXML = null;
                    XmlSerializer serializer = new XmlSerializer(typeof(MTFakturaXML));
                    using (XmlReader Xreader = new XmlNodeReader(node))
                    {
                        faXML = (MTFakturaXML)serializer.Deserialize(Xreader);
                    }
                    if (faXML != null)
                    {
                        if (!mtDoc.ContainsKey(faXML.head_fakturanr))
                        {
                            mtDoc.Add(faXML.head_fakturanr, new MTDocument { fakturaXML = new List<MTFakturaXML>(), postedEntries = new List<MTPostedEntry>() });
                        }
                        mtDoc[faXML.head_fakturanr].fakturaXML.Add(faXML);
                    }
                }
            }
            #endregion

            RES.MTDoc = mtDoc;

            return RES;
        }
        private static SALESORDERHEADERV2ENTITY ConvertSalesOrder(string DocumentNr, MTDocument mtDoc, Dictionary<string, CustomerD365> CustomersD365, List<VATmap> VATMaps, DateTime? ShiftDateOlderThan, DateTime? ShiftDateChangeTo, string OverrideSalesOrderLineSalesTaxItemGroupCode, string OverrideSalesOrderLineItemNo, Dictionary<string, ExchangeRateD365> ExchangeRatesD365, Dictionary<string, DimensionIntegrationFormat> DimensionIntegrationFormats)
        {
            SALESORDERHEADERV2ENTITY SalesOrderHeaderXML = new SALESORDERHEADERV2ENTITY();
            SalesOrderHeaderXML.SalesOrderLineV2Entity = new List<SALESORDERLINEV2ENTITY>();
            MTPostedEntry MTPEntry = mtDoc.postedEntries.Where(p => p.RegistrationType.Trim().ToUpper() == "1" && p.Code.Trim().ToUpper() == "K" && (p.DocumentType.Trim().ToUpper() == "SF" || p.DocumentType.Trim().ToUpper() == "SK")).FirstOrDefault();
            int TotalQTY = 0;

            #region TotalQTY In case we get fakturaXML
            if (mtDoc.fakturaXML.Any())
            {
                foreach (MTFakturaXML faXML in mtDoc.fakturaXML)
                {
                    int qty = 0;
                    int.TryParse(faXML.row_totkvant, out qty);
                }
            }
            #endregion

            decimal postedEntryAmount = decimal.Round(decimal.Parse(MTPEntry.Amount, System.Globalization.CultureInfo.InvariantCulture), 2);
            decimal postedEntryCurrencyAmount = decimal.Round(decimal.Parse(MTPEntry.CurrencyAmount, System.Globalization.CultureInfo.InvariantCulture), 2);
            decimal MultiplicationFactor = 1;

            #region Exchange Rate Factor - should be remove as its OK for Sales Orders
            /*
            decimal MTCurrencyFactor = 1;
            decimal.TryParse(MTPEntry.CurrencyFactor, out MTCurrencyFactor);
            decimal D365CurrencyFactor = 1;
            ExchangeRateD365 ExchRate = ExchangeRatesD365.GetValueOrDefault(MTPEntry.CurrencyCode.Trim().ToUpper());
            if (ExchRate != null)
            {
                switch (ExchRate.ConversionFactor.Trim().ToUpper())
                {
                    case "ONE":
                        {
                            D365CurrencyFactor = 1;
                            break;
                        }
                    case "HUNDRED":
                        {
                            D365CurrencyFactor = 100;
                            break;
                        }
                    case "THOUSAND":
                        {
                            D365CurrencyFactor = 1000;
                            break;
                        }
                }
            }
            MultiplicationFactor = D365CurrencyFactor / MTCurrencyFactor;
            */
            #endregion

            decimal postedEntryFixedExchangeRate = decimal.Round((decimal.Parse(MTPEntry.ExchangeRate.Trim(), System.Globalization.CultureInfo.InvariantCulture) * MultiplicationFactor), 6);

            Int64 CustomerNumber = 0;
            Int64.TryParse(MTPEntry.CustomerNo.Trim().ToUpper(), out CustomerNumber);
            if (!CustomersD365.ContainsKey($"{CustomerNumber:00000}"))
            {
                throw new Exception($"Customer {CustomerNumber:00000} not found in Dynamics 365");
            }
            #region SalesOrderHeader

            #region Fixed values
            SalesOrderHeaderXML.AREPRICESINCLUDINGSALESTAX = "No";
            SalesOrderHeaderXML.ARETOTALSCALCULATED = "No";
            SalesOrderHeaderXML.CUSTOMERPOSTINGPROFILEID = "STD";
            SalesOrderHeaderXML.CUSTOMERTRANSACTIONSETTLEMENTTYPE = "None";
            SalesOrderHeaderXML.DEFAULTSHIPPINGSITEID = "STD";
            SalesOrderHeaderXML.DEFAULTSHIPPINGWAREHOUSEID = "STD";
            SalesOrderHeaderXML.INVENTORYRESERVATIONMETHOD = "None";
            SalesOrderHeaderXML.MBSAUTOPOSTIC = "No";
            SalesOrderHeaderXML.MBSFROMMARITECH = "Yes";
            SalesOrderHeaderXML.SKIPCREATEAUTOCHARGES = "Yes";
            SalesOrderHeaderXML.WILLAUTOMATICINVENTORYRESERVATIONCONSIDERBATCHATTRIBUTES = "No";
            #endregion

            #region Converted values

            SalesOrderHeaderXML.CURRENCYCODE = MTPEntry.CurrencyCode.Trim().ToUpper();
            SalesOrderHeaderXML.FIXEDDUEDATE = MTPEntry.DueDate;
            SalesOrderHeaderXML.FIXEDEXCHANGERATE = postedEntryFixedExchangeRate;
            //NOT VALID ANYMORE: Data sent from Maritech must have ExternalCustomerVendorNo
            //HACK: SalesOrderHeaderXML.INVOICECUSTOMERACCOUNTNUMBER = MTPEntry.ExternalCustomerVendorNo;
            //SalesOrderHeaderXML.INVOICECUSTOMERACCOUNTNUMBER = "000001";
            //Clarification 2022-02-16: Data sent from Maritech will have valid CustomerNo

            //SalesOrderHeaderXML.INVOICECUSTOMERACCOUNTNUMBER = MTPEntry.CustomerNo.Trim().ToUpper();
            SalesOrderHeaderXML.INVOICECUSTOMERACCOUNTNUMBER = $"{CustomerNumber:00000}";
            //Default: SalesOrderHeaderXML.LANGUAGEID = "nb-NO";
            SalesOrderHeaderXML.LANGUAGEID = (CustomersD365.ContainsKey($"{CustomerNumber:00000}") ? CustomersD365[$"{CustomerNumber:00000}"].LanguageId : "nb-NO");
            //Clarification 2022-03-10 SalesOrderHeaderXML.MBSINVOICETOTAL = postedEntryAmount;
            SalesOrderHeaderXML.MBSINVOICETOTAL = postedEntryCurrencyAmount;
            SalesOrderHeaderXML.MBSMARITECHINVOICEDATE = MTPEntry.DocumentDate;

            //Request from Thomas for Migration
            if (ShiftDateOlderThan != null && ShiftDateChangeTo != null)
            {
                try
                {
                    DateTime DocumentDate = DateTime.Parse(MTPEntry.DocumentDate);
                    if (DocumentDate < ShiftDateOlderThan)
                    {
                        SalesOrderHeaderXML.MBSMARITECHINVOICEDATE = ShiftDateChangeTo?.ToString("yyyy-MM-dd HH:mm:ss") ?? SalesOrderHeaderXML.MBSMARITECHINVOICEDATE;
                    }
                }
                catch
                { }
            }

            SalesOrderHeaderXML.MBSMARITECHPAYMID = MTPEntry.KID?.Trim()??"";
            SalesOrderHeaderXML.MBSMARITECHSALESID = DocumentNr.Trim();
            //SalesOrderHeaderXML.MBSQTY = (TotalQTY == 0 ? "" : TotalQTY.ToString());
            SalesOrderHeaderXML.MBSQTY = TotalQTY;
            //NOT VALID Data sent from Maritech must have ExternalCustomerVendorNo
            //Clarification 2022-02-16: Data sent from Maritech will have valid CustomerNo
            //HACK: SalesOrderHeaderXML.ORDERINGCUSTOMERACCOUNTNUMBER = "000001";
            //SalesOrderHeaderXML.ORDERINGCUSTOMERACCOUNTNUMBER = MTPEntry.ExternalCustomerVendorNo;
            SalesOrderHeaderXML.ORDERINGCUSTOMERACCOUNTNUMBER = $"{CustomerNumber:00000}";
            SalesOrderHeaderXML.PAYMENTTERMSNAME = MTPEntry.PaymentConditionNo.Trim();
            SalesOrderHeaderXML.REQUESTEDSHIPPINGDATE = MTPEntry.DocumentDate;

            //Clarification 2022-02-16: Should be Customer name
            //Default: Maritech {DocumentType} {DocumentNr}
            SalesOrderHeaderXML.SALESORDERNAME = (CustomersD365.ContainsKey($"{CustomerNumber:00000}") ? CustomersD365[$"{CustomerNumber:00000}"].OrganizationName : $"Maritech {MTPEntry.DocumentType} {DocumentNr}");
            //Clarification 2022-02-16: SALESTAXGROUPCODE is determined by D365

            //2022-03-27: New fields
            SalesOrderHeaderXML.MBSCURRENCYHEDGE = (MTPEntry.CurrencyHedged == "1" ? "Yes" : "No");
            //SalesOrderHeaderXML.MBSCURRENCYHEDGE = "Yes";
            #endregion

            int LineCounter = 0;

            //Sales Order Lines
            foreach (MTPostedEntry postedEntryLine in mtDoc.postedEntries.Where(p => p.RegistrationType.Trim().ToUpper() == "3" && p.Code.Trim().ToUpper() == "H" && (p.DocumentType.Trim().ToUpper() == "SF" || p.DocumentType.Trim().ToUpper() == "SK")).ToList())
            {
                #region SalesOrderLine
                LineCounter++;
                //decimal.Round(decimal.Parse(postedEntryLine.CurrencyAmount, System.Globalization.CultureInfo.InvariantCulture), 2)
                decimal CurrencyAmount = decimal.Round(decimal.Parse(postedEntryLine.CurrencyAmount, System.Globalization.CultureInfo.InvariantCulture), 2);

                #region Motpart & Baerer

                //DEFAULTLEDGERDIMENSIONDISPLAYVALUE:
                //2022-04-27: Aktivitet-Anleggsstatus-Avdeling-Baerer-Generasjon-Leasinggjeld-Motpart-Prosjekt-Reisekode-Underavdeling
                string Motpart = "";
                string Baerer = "";
                if (CustomersD365.ContainsKey(MTPEntry.CustomerNo) && CustomersD365[MTPEntry.CustomerNo].DefaultDimensionDisplayValue != "")
                {
                    string[] DefaultLedgerDimensionDisplayValues = CustomersD365[MTPEntry.CustomerNo].DefaultDimensionDisplayValue.Split("-");
                    if (DefaultLedgerDimensionDisplayValues.Length > 9)
                    {
                        Motpart = DefaultLedgerDimensionDisplayValues[6];
                        Baerer = DefaultLedgerDimensionDisplayValues[3];
                    }
                }

                #endregion

                #region DEFAULTLEDGERDIMENSIONDISPLAYVALUE

                string DefaultLedgerDimensionDisplayValue = $"--{postedEntryLine.DepartmentNo?.Trim().ToUpper()??""}-{Baerer}---{Motpart}---";
                if (DimensionIntegrationFormats != null && DimensionIntegrationFormats.Keys.Contains("DataEntityDefaultDimensionFormat"))
                {
                    //Aktivitet-Anleggsstatus-Avdeling-Baerer-Generasjon-Leasinggjeld-Motpart-Prosjekt-Reisekode-Underavdeling
                    string[] FinancialDimensionFormatDefinitions = DimensionIntegrationFormats["DataEntityDefaultDimensionFormat"].FinancialDimensionFormat.Split("-");
                    List<string> FinancialDimensionValues = new List<string>();
                    foreach (string FinancialDimension in FinancialDimensionFormatDefinitions)
                    {
                        if (FinancialDimension == "Avdeling")
                        {
                            FinancialDimensionValues.Add(postedEntryLine.DepartmentNo?.Trim().ToUpper()??"");
                        }
                        else if (FinancialDimension == "Baerer")
                        {
                            FinancialDimensionValues.Add(Baerer);
                        }
                        else if (FinancialDimension == "Motpart")
                        {
                            FinancialDimensionValues.Add(Motpart);
                        }
                        else
                        {
                            FinancialDimensionValues.Add("");
                        }
                    }
                    DefaultLedgerDimensionDisplayValue = string.Join("-", FinancialDimensionValues);
                }

                #endregion

                SALESORDERLINEV2ENTITY SalesOrderLineXML = new SALESORDERLINEV2ENTITY
                {
                    #region fixed values
                    GIFTCARDTYPE = "Email",
                    SALESUNITSYMBOL = "stk",
                    SHIPPINGSITEID = "STD",
                    SHIPPINGWAREHOUSEID = "STD",
                    SKIPCREATEAUTOCHARGES = "Yes",
                    WILLAUTOMATICINVENTORYRESERVATIONCONSIDERBATCHATTRIBUTES = "No",
                    WILLREBATECALCULATIONEXCLUDELINE = "No",
                    #endregion

                    #region converted values
                    CURRENCYCODE = postedEntryLine.CurrencyCode.Trim().ToUpper(),
                    //DEFAULTLEDGERDIMENSIONDISPLAYVALUE:
                    //2022-03-29: MainAccount-Avdeling-Generasjon-Aktivitet-Anleggsstatus-Baerer-Leasinggjeld-Motpart-Prosjekt-Reisekode-Underavdeling-Art
                    //2022-03-31: MainAccount-Aktivitet-Anleggsstatus-Avdeling-Baerer-Generasjon-Leasinggjeld-Motpart-Prosjekt-Reisekode-Underavdeling
                    //2022-04-27: Aktivitet-Anleggsstatus-Avdeling-Baerer-Generasjon-Leasinggjeld-Motpart-Prosjekt-Reisekode-Underavdeling


                    //2025-04-16: Dynamic FinancialDimensionFormats
                    ////Aktivitet-Anleggsstatus-Avdeling-Baerer-Generasjon-Leasinggjeld-Motpart-Prosjekt-Reisekode-Underavdeling

                    DEFAULTLEDGERDIMENSIONDISPLAYVALUE = DefaultLedgerDimensionDisplayValue,

                    //ITEMNUMBER = postedEntryLine.AccountNo.Trim().ToUpper(),
                    //2022-05-22: Request from Thomas for migration
                    ITEMNUMBER = (OverrideSalesOrderLineItemNo != "" && OverrideSalesOrderLineItemNo != null ? OverrideSalesOrderLineItemNo : postedEntryLine.AccountNo.Trim().ToUpper()),
                    LINEAMOUNT = (CurrencyAmount * -1),
                    //LINEAMOUNT = (postedEntryLine.DocumentType.Trim().ToUpper() == "SF" ? (CurrencyAmount * -1) : (CurrencyAmount * 1)),
                    LINECREATIONSEQUENCENUMBER = LineCounter.ToString(),
                    LINEDESCRIPTION = $"Maritech {postedEntryLine.DocumentType.Trim().ToUpper()} {DocumentNr.Trim()} - line {LineCounter.ToString()}",
                    //ORDEREDSALESQUANTITY = (postedEntryLine.DocumentType.Trim().ToUpper() == "SF" ? 1 : -1),
                    ORDEREDSALESQUANTITY = ((CurrencyAmount * -1) < 0 ? -1 : 1),
                    REQUESTEDRECEIPTDATE = postedEntryLine.DocumentDate,
                    REQUESTEDSHIPPINGDATE = postedEntryLine.DocumentDate,
                    //SALESPRICE = (postedEntryLine.DocumentType.Trim().ToUpper() == "SF" ? (CurrencyAmount * -1) : (CurrencyAmount)),
                    SALESPRICE = (CurrencyAmount < 0 ? (CurrencyAmount * -1) : (CurrencyAmount)),
                    SALESPRICEQUANTITY = 1,
                    //Clarification 2022-02-16: mapping done in DMF
                    //SALESTAXITEMGROUPCODE = (VATMaps.Where(m => m.MaritechCode == postedEntryLine.VATCode).Any() ? VATMaps.Where(m => m.MaritechCode == postedEntryLine.VATCode).FirstOrDefault().D365Code : "V4") 
                    //SALESTAXITEMGROUPCODE = postedEntryLine.VATCode

                    //SALESTAXITEMGROUPCODE = (VATMaps.Count == 0 ? postedEntryLine.VATCode : (VATMaps.Where(m => m.MaritechCode == postedEntryLine.VATCode).Any() ? VATMaps.Where(m => m.MaritechCode.Trim().ToUpper() == postedEntryLine.VATCode.Trim().ToUpper()).FirstOrDefault().D365Code : postedEntryLine.VATCode))
                    //2022-05-22: Request from Thomas for migration

                    //2026-01-20: postedEntryLine.VATCode null handling
                    //SALESTAXITEMGROUPCODE = (OverrideSalesOrderLineSalesTaxItemGroupCode != "" && OverrideSalesOrderLineSalesTaxItemGroupCode != null ? OverrideSalesOrderLineSalesTaxItemGroupCode : (VATMaps.Count == 0 ? postedEntryLine.VATCode : (VATMaps.Where(m => m.MaritechCode == postedEntryLine.VATCode).Any() ? VATMaps.Where(m => m.MaritechCode.Trim().ToUpper() == postedEntryLine.VATCode.Trim().ToUpper()).FirstOrDefault().D365Code : postedEntryLine.VATCode)))
                    SALESTAXITEMGROUPCODE = (OverrideSalesOrderLineSalesTaxItemGroupCode != "" && OverrideSalesOrderLineSalesTaxItemGroupCode != null ? OverrideSalesOrderLineSalesTaxItemGroupCode : (VATMaps.Count == 0 ? postedEntryLine.VATCode : (VATMaps.Where(m => m.MaritechCode == postedEntryLine.VATCode).Any() ? VATMaps.Where(m => m.MaritechCode.Trim().ToUpper() == postedEntryLine.VATCode.Trim().ToUpper()).FirstOrDefault().D365Code : (postedEntryLine.VATCode ?? ""))))
                    #endregion
                };
                SalesOrderHeaderXML.SalesOrderLineV2Entity.Add(SalesOrderLineXML);
                #endregion
            }
            #endregion

            return SalesOrderHeaderXML;
        }
        private static PURCHPURCHASEORDERHEADERV2ENTITY ConvertPurchaseOrder(string DocumentNr, MTDocument mtDoc, Dictionary<string, VendorD365> VendorsD365, List<VATmap> VATMaps, DateTime? ShiftDateOlderThan, DateTime? ShiftDateChangeTo, Dictionary<string, ExchangeRateD365> ExchangeRatesD365, string PurchaseOrderMultiHeaderPoolId, Dictionary<string, DimensionIntegrationFormat> DimensionIntegrationFormats)
        {
            PURCHPURCHASEORDERHEADERV2ENTITY PurchaseOrderHeaderXML = new PURCHPURCHASEORDERHEADERV2ENTITY();
            PurchaseOrderHeaderXML.PurchaseOrderLineV2Entities = new List<PURCHPURCHASEORDERLINEV2ENTITY>();

            #region KPMGNO-1256 (PO med flere fakturaer)
            List<MTPostedEntry> MTPEntryHeaders = mtDoc.postedEntries.Where(p => p.RegistrationType.Trim().ToUpper() == "1" && p.Code.Trim().ToUpper() == "L" && (p.DocumentType.Trim().ToUpper() == "KF" || p.DocumentType.Trim().ToUpper() == "KK")).ToList();

            PurchaseOrderHeaderXML.PurchaseOrderPoolId = ((MTPEntryHeaders.Count() > 1) ? PurchaseOrderMultiHeaderPoolId : "");

            MTPostedEntry MTPEntryHeaderMaster = MTPEntryHeaders.Where(h => h.RegistrationType.Trim().ToUpper() == "1" && h.Code.Trim().ToUpper() == "L" && h.DocumentType.Trim().ToUpper() == "KF").FirstOrDefault();
            if (MTPEntryHeaderMaster == null)
            {
                MTPEntryHeaderMaster = mtDoc.postedEntries.Where(h => h.RegistrationType.Trim().ToUpper() == "1" && h.Code.Trim().ToUpper() == "L" && (h.DocumentType.Trim().ToUpper() == "KF" || h.DocumentType.Trim().ToUpper() == "KK")).FirstOrDefault();
            }

            decimal HeaderMultiplicationFactor = 1;

            #region Exchange Rate Factor - to be removed when D365 is adjusted KPMGNO-1135 (Column 'MBSFixedExchRate' on purchase order doesn't consider conversion factor) 2022-09-30
            /*
            decimal HeaderMTCurrencyFactor = 1;

            decimal.TryParse(MTPEntryHeaderMaster.CurrencyFactor, out HeaderMTCurrencyFactor);
            decimal HeaderD365CurrencyFactor = 1;
            ExchangeRateD365 HeaderExchRate = ExchangeRatesD365.GetValueOrDefault(MTPEntryHeaderMaster.CurrencyCode.Trim().ToUpper());
            if (HeaderExchRate != null)
            {
                switch (HeaderExchRate.ConversionFactor.Trim().ToUpper())
                {
                    case "ONE":
                        {
                            HeaderD365CurrencyFactor = 1;
                            break;
                        }
                    case "HUNDRED":
                        {
                            HeaderD365CurrencyFactor = 100;
                            break;
                        }
                    case "THOUSAND":
                        {
                            HeaderD365CurrencyFactor = 1000;
                            break;
                        }
                }
            }
            HeaderMultiplicationFactor = HeaderD365CurrencyFactor / HeaderMTCurrencyFactor;
            */
            #endregion

            decimal HeaderPostedEntryFixedExchangeRate = decimal.Round((decimal.Parse(MTPEntryHeaderMaster.ExchangeRate, System.Globalization.CultureInfo.InvariantCulture) * HeaderMultiplicationFactor), 6);
            Int64 HeaderVendorNumber = 0;
            Int64.TryParse(MTPEntryHeaderMaster.VendorNo.Trim().ToUpper(), out HeaderVendorNumber);
            if (!VendorsD365.ContainsKey($"{HeaderVendorNumber:00000}"))
            {
                throw new Exception($"Vendor {HeaderVendorNumber:00000} not found in Dynamics 365");
            }

            #region PurchaseOrderHeader

            #region Fiexd values
            PurchaseOrderHeaderXML.AREPRICESINCLUDINGSALESTAX = 0;
            PurchaseOrderHeaderXML.DEFAULTRECEIVINGSITEID = "STD";
            PurchaseOrderHeaderXML.DEFAULTRECEIVINGWAREHOUSEID = "STD";
            PurchaseOrderHeaderXML.DOCUMENTAPPROVALSTATUS = "40";
            PurchaseOrderHeaderXML.PURCHASEORDERSTATUS = "2";
            #endregion

            #region Coverted values

            //2022-03-29: PurchaseOrderHeaderXML.ACCOUNTINGDATE = MTPEntry.InvoiceDate;
            PurchaseOrderHeaderXML.MBSMARITECHINVOICEDATE = MTPEntryHeaderMaster.InvoiceDate;
            //Request from Thomas for Migration
            if (ShiftDateOlderThan != null && ShiftDateChangeTo != null)
            {
                try
                {
                    DateTime DocumentDate = DateTime.Parse(MTPEntryHeaderMaster.DocumentDate);
                    if (DocumentDate < ShiftDateOlderThan)
                    {
                        PurchaseOrderHeaderXML.MBSMARITECHINVOICEDATE = ShiftDateChangeTo?.ToString("yyyy-MM-dd HH:mm:ss") ?? PurchaseOrderHeaderXML.MBSMARITECHINVOICEDATE;
                    }
                }
                catch
                { }
            }

            PurchaseOrderHeaderXML.CURRENCYCODE = MTPEntryHeaderMaster.CurrencyCode;
            PurchaseOrderHeaderXML.INVOICEVENDORACCOUNTNUMBER = $"{HeaderVendorNumber:00000}"; ; ;
            PurchaseOrderHeaderXML.FIXEDDUEDATE = MTPEntryHeaderMaster.DueDate;
            PurchaseOrderHeaderXML.LANGUAGEID = (VendorsD365.ContainsKey($"{HeaderVendorNumber:00000}") ? VendorsD365[$"{HeaderVendorNumber:00000}"].LanguageId : "nb-NO");
            PurchaseOrderHeaderXML.ORDERVENDORACCOUNTNUMBER = $"{HeaderVendorNumber:00000}"; ;

            PurchaseOrderHeaderXML.REQUESTEDDELIVERYDATE = MTPEntryHeaderMaster.DocumentDate;
            PurchaseOrderHeaderXML.SALESTAXGROUPCODE = VendorsD365[$"{HeaderVendorNumber:00000}"].SalesTaxGroupCode;
            PurchaseOrderHeaderXML.MBSMARITECHPAYMID = (MTPEntryHeaderMaster.KID?.Trim() ?? "");

            PurchaseOrderHeaderXML.MBSCURRENCYHEDGE = (MTPEntryHeaderMaster.CurrencyHedged == "1" ? "Yes" : "No");
            PurchaseOrderHeaderXML.MBSFROMMARITECH = "Yes";

            PurchaseOrderHeaderXML.MBSFIXEDEXCHRATE = HeaderPostedEntryFixedExchangeRate;

            #region KPMGNO-1135 (Column 'MBSFixedExchRate' on purchase order doesn't consider conversion factor) 2022-09-30

            PurchaseOrderHeaderXML.MBSFIXEDEXCHRATE = 0;
            PurchaseOrderHeaderXML.MBSMARITECHEXCHAGERATE = decimal.Round(decimal.Parse(MTPEntryHeaderMaster.ExchangeRate, System.Globalization.CultureInfo.InvariantCulture), 6);
            // MBSMaritechExchageRate
            PurchaseOrderHeaderXML.MBSMARITECHFACTOR = int.Parse(MTPEntryHeaderMaster.CurrencyFactor, System.Globalization.CultureInfo.InvariantCulture);
            // MBSMaritechFactor

            #endregion

            PurchaseOrderHeaderXML.PURCHASEORDERNAME = (VendorsD365.ContainsKey($"{HeaderVendorNumber:00000}") ? VendorsD365[$"{HeaderVendorNumber:00000}"].VendorOrganizationName : $"Maritech {MTPEntryHeaderMaster.DocumentType} {MTPEntryHeaderMaster.DocumentNo}");

            //Multiple headers aggregation
            decimal HeaderPostedEntryAmount = 0;
            decimal HeaderPostedEntryCurrencyAmount = 0;
            string IncomingInvoiceNos = MTPEntryHeaderMaster.IncomingInvoiceNo;
            foreach (MTPostedEntry MTPEntryHeader in MTPEntryHeaders)
            {
                HeaderPostedEntryAmount += decimal.Round(decimal.Parse(MTPEntryHeader.Amount.Trim(), System.Globalization.CultureInfo.InvariantCulture), 2);
                HeaderPostedEntryCurrencyAmount += decimal.Round(decimal.Parse(MTPEntryHeader.CurrencyAmount, System.Globalization.CultureInfo.InvariantCulture), 2);
                if (MTPEntryHeader != MTPEntryHeaderMaster)
                {
                    IncomingInvoiceNos += $"{(IncomingInvoiceNos != "" ? "," : "")}{MTPEntryHeader.IncomingInvoiceNo}";
                }
            }
            PurchaseOrderHeaderXML.MBSMARITECHINVOICEID = IncomingInvoiceNos;
            PurchaseOrderHeaderXML.VENDORORDERREFERENCE = $"{MTPEntryHeaderMaster.DocumentNo}/{IncomingInvoiceNos}";
            PurchaseOrderHeaderXML.MBSINVOICETOTAL = HeaderPostedEntryCurrencyAmount * -1;

            #endregion

            #endregion

            #endregion

            #region OLD: only single header considered
            /*
            MTPostedEntry MTPEntry = mtDoc.postedEntries.Where(p => p.RegistrationType.Trim().ToUpper() == "1" && p.Code.Trim().ToUpper() == "L" && (p.DocumentType.Trim().ToUpper() == "KF" || p.DocumentType.Trim().ToUpper() == "KK")).FirstOrDefault();

            decimal postedEntryAmount = decimal.Round(decimal.Parse(MTPEntry.Amount.Trim(), System.Globalization.CultureInfo.InvariantCulture), 2);
            decimal postedEntryCurrencyAmount = decimal.Round(decimal.Parse(MTPEntry.CurrencyAmount, System.Globalization.CultureInfo.InvariantCulture), 2);

            decimal MultiplicationFactor = 1;
            */
            #region Exchange Rate Factor (removed 2022-06-29)
            /*
            decimal MTCurrencyFactor = 1;
            decimal.TryParse(MTPEntry.CurrencyFactor, out MTCurrencyFactor);
            decimal D365CurrencyFactor = 1;
            ExchangeRateD365 ExchRate = ExchangeRatesD365.GetValueOrDefault(MTPEntry.CurrencyCode.Trim().ToUpper());
            if (ExchRate != null)
            {
                switch (ExchRate.ConversionFactor.Trim().ToUpper())
                {
                    case "ONE":
                        {
                            D365CurrencyFactor = 1;
                            break;
                        }
                    case "HUNDRED":
                        {
                            D365CurrencyFactor = 100;
                            break;
                        }
                    case "THOUSAND":
                        {
                            D365CurrencyFactor = 1000;
                            break;
                        }
                }
            }
            MultiplicationFactor = D365CurrencyFactor / MTCurrencyFactor;
            */
            #endregion
            /*
            decimal postedEntryFixedExchangeRate = decimal.Round((decimal.Parse(MTPEntry.ExchangeRate, System.Globalization.CultureInfo.InvariantCulture) * MultiplicationFactor), 6);

            //decimal postedEntryFixedExchangeRate = decimal.Round(decimal.Parse(MTPEntry.ExchangeRate.Trim(), System.Globalization.CultureInfo.InvariantCulture), 6);

            Int64 VendorNumber = 0;
            Int64.TryParse(MTPEntry.VendorNo.Trim().ToUpper(), out VendorNumber);
            if (!VendorsD365.ContainsKey($"{VendorNumber:00000}"))
            {
                throw new Exception($"Vendor {VendorNumber:00000} not found in Dynamics 365");
            }
            #region PurchaseOrderHeader

            #region Fiexd values
            PurchaseOrderHeaderXML.AREPRICESINCLUDINGSALESTAX = 0;
            PurchaseOrderHeaderXML.DEFAULTRECEIVINGSITEID = "STD";
            PurchaseOrderHeaderXML.DEFAULTRECEIVINGWAREHOUSEID = "STD";
            PurchaseOrderHeaderXML.DOCUMENTAPPROVALSTATUS = "40";
            PurchaseOrderHeaderXML.PURCHASEORDERSTATUS = "2";
            #endregion

            #region Coverted values

            //2022-03-29: PurchaseOrderHeaderXML.ACCOUNTINGDATE = MTPEntry.InvoiceDate;
            PurchaseOrderHeaderXML.MBSMARITECHINVOICEDATE = MTPEntry.InvoiceDate;
            //Request from Thomas for Migration
            if (ShiftDateOlderThan != null && ShiftDateChangeTo != null)
            {
                try
                {
                    DateTime DocumentDate = DateTime.Parse(MTPEntry.DocumentDate);
                    if (DocumentDate < ShiftDateOlderThan)
                    {
                        PurchaseOrderHeaderXML.MBSMARITECHINVOICEDATE = ShiftDateChangeTo?.ToString("yyyy-MM-dd HH:mm:ss") ?? PurchaseOrderHeaderXML.MBSMARITECHINVOICEDATE;
                    }
                }
                catch
                { }
            }
            PurchaseOrderHeaderXML.CURRENCYCODE = MTPEntry.CurrencyCode;
            PurchaseOrderHeaderXML.INVOICEVENDORACCOUNTNUMBER = $"{VendorNumber:00000}"; ; ;
            PurchaseOrderHeaderXML.FIXEDDUEDATE = MTPEntry.DueDate;
            PurchaseOrderHeaderXML.LANGUAGEID = (VendorsD365.ContainsKey($"{VendorNumber:00000}") ? VendorsD365[$"{VendorNumber:00000}"].LanguageId : "nb-NO");
            PurchaseOrderHeaderXML.ORDERVENDORACCOUNTNUMBER = $"{VendorNumber:00000}"; ;
            //PurchaseOrderHeaderXML.PURCHASEORDERNAME = $"Maritech PO (Vendor={MTPEntry.VendorNo}/DocumentNr={MTPEntry.DocumentNo}/Date={MTPEntry.DocumentDate}";
            PurchaseOrderHeaderXML.PURCHASEORDERNAME = (VendorsD365.ContainsKey($"{VendorNumber:00000}") ? VendorsD365[$"{VendorNumber:00000}"].VendorOrganizationName : $"Maritech {MTPEntry.DocumentType} {DocumentNr}");
            PurchaseOrderHeaderXML.REQUESTEDDELIVERYDATE = MTPEntry.DocumentDate;
            PurchaseOrderHeaderXML.SALESTAXGROUPCODE = VendorsD365[$"{VendorNumber:00000}"].SalesTaxGroupCode;
            PurchaseOrderHeaderXML.MBSMARITECHPAYMID = MTPEntry.KID;
            PurchaseOrderHeaderXML.MBSMARITECHINVOICEID = MTPEntry.IncomingInvoiceNo;
            PurchaseOrderHeaderXML.VENDORORDERREFERENCE = $"{MTPEntry.DocumentNo}/{MTPEntry.IncomingInvoiceNo}";

            //2022-03-27: New fields
            PurchaseOrderHeaderXML.MBSCURRENCYHEDGE = (MTPEntry.CurrencyHedged == "1" ? "Yes" : "No");
            //PurchaseOrderHeaderXML.MBSCURRENCYHEDGE = "Yes";
            PurchaseOrderHeaderXML.MBSFROMMARITECH = "Yes";
            PurchaseOrderHeaderXML.MBSFIXEDEXCHRATE = postedEntryFixedExchangeRate;
            //2022-03-29
            PurchaseOrderHeaderXML.MBSINVOICETOTAL = postedEntryCurrencyAmount * -1;



            #endregion

            #endregion
            */
            #endregion

            int LineCounter = 0;
            foreach (MTPostedEntry postedEntryLine in mtDoc.postedEntries.Where(p => p.RegistrationType.Trim().ToUpper() == "3" && p.Code.Trim().ToUpper() == "H" && (p.DocumentType.Trim().ToUpper() == "KF" || p.DocumentType.Trim().ToUpper() == "KK")).ToList())
            {
                #region PurchaseOrderLine
                LineCounter++;
                decimal CurrencyAmount = decimal.Round(decimal.Parse(postedEntryLine.CurrencyAmount, System.Globalization.CultureInfo.InvariantCulture), 2);

                #region Motpart & Baerer
                //-----{Baerer}--{Motpart}----
                string Motpart = "";
                string Baerer = "";
                // KPMGNO-1256 (PO med flere fakturaer)
                //if (VendorsD365.ContainsKey(MTPEntry.VendorNo) && VendorsD365[MTPEntry.VendorNo].DefaultLedgerDimensionDisplayValue != "")
                if (VendorsD365.ContainsKey(MTPEntryHeaderMaster.VendorNo) && VendorsD365[MTPEntryHeaderMaster.VendorNo].DefaultLedgerDimensionDisplayValue != "")
                {
                    //DEFAULTLEDGERDIMENSIONDISPLAYVALUE
                    //2022-04-27: Aktivitet-Anleggsstatus-Avdeling-Baerer-Generasjon-Leasinggjeld-Motpart-Prosjekt-Reisekode-Underavdeling

                    //KPMGNO-1256 (PO med flere fakturaer)
                    //string[] DefaultLedgerDimensionDisplayValues = VendorsD365[MTPEntry.VendorNo].DefaultLedgerDimensionDisplayValue.Split("-");
                    string[] DefaultLedgerDimensionDisplayValues = VendorsD365[MTPEntryHeaderMaster.VendorNo].DefaultLedgerDimensionDisplayValue.Split("-");
                    if (DefaultLedgerDimensionDisplayValues.Length > 9)
                    {
                        Motpart = DefaultLedgerDimensionDisplayValues[6];
                        Baerer = DefaultLedgerDimensionDisplayValues[3];
                    }
                }
                #endregion

                #region DEFAULTLEDGERDIMENSIONDISPLAYVALUE

                string DefaultLedgerDimensionDisplayValue = $"--{postedEntryLine.DepartmentNo?.Trim().ToUpper()??""}-{Baerer}---{Motpart}---";
                if (DimensionIntegrationFormats != null && DimensionIntegrationFormats.Keys.Contains("DataEntityDefaultDimensionFormat"))
                {
                    //Aktivitet-Anleggsstatus-Avdeling-Baerer-Generasjon-Leasinggjeld-Motpart-Prosjekt-Reisekode-Underavdeling
                    string[] FinancialDimensionFormatDefinitions = DimensionIntegrationFormats["DataEntityDefaultDimensionFormat"].FinancialDimensionFormat.Split("-");
                    List<string> FinancialDimensionValues = new List<string>();
                    foreach (string FinancialDimension in FinancialDimensionFormatDefinitions)
                    {
                        if (FinancialDimension == "Avdeling")
                        {
                            FinancialDimensionValues.Add(postedEntryLine.DepartmentNo?.Trim().ToUpper()??"");
                        }
                        else if (FinancialDimension == "Baerer")
                        {
                            FinancialDimensionValues.Add(Baerer);
                        }
                        else if (FinancialDimension == "Motpart")
                        {
                            FinancialDimensionValues.Add(Motpart);
                        }
                        else
                        {
                            FinancialDimensionValues.Add("");
                        }
                    }
                    DefaultLedgerDimensionDisplayValue = string.Join("-", FinancialDimensionValues);
                }

                #endregion

                PURCHPURCHASEORDERLINEV2ENTITY PurchaseOrderLine = new PURCHPURCHASEORDERLINEV2ENTITY
                {
                    #region Fixed values
                    PURCHASEORDERLINESTATUS = "2",
                    PURCHASEUNITSYMBOL = "stk",
                    SALESTAXGROUPCODE = PurchaseOrderHeaderXML.SALESTAXGROUPCODE,
                    #endregion

                    #region Converted values
                    //DEFAULTLEDGERDIMENSIONDISPLAYVALUE
                    //2022-03-29: MainAccount-Avdeling-Generasjon-Aktivitet-Anleggsstatus-Baerer-Leasinggjeld-Motpart-Prosjekt-Reisekode-Underavdeling-Art
                    //2022-03-31: MainAccount-Aktivitet-Anleggsstatus-Avdeling-Baerer-Generasjon-Leasinggjeld-Motpart-Prosjekt-Reisekode-Underavdeling
                    //2022-04-27: Aktivitet-Anleggsstatus-Avdeling-Baerer-Generasjon-Leasinggjeld-Motpart-Prosjekt-Reisekode-Underavdeling
                    //DEFAULTLEDGERDIMENSIONDISPLAYVALUE = $"--{(postedEntryLine.DepartmentNo?.Trim().ToUpper()??"")}-{Baerer}---{Motpart}---",
                    DEFAULTLEDGERDIMENSIONDISPLAYVALUE = DefaultLedgerDimensionDisplayValue,
                    ITEMNUMBER = postedEntryLine.AccountNo,
                    LINEAMOUNT = CurrencyAmount,

                    //KPMGNO-1256 (PO med flere fakturaer)
                    //LINEDESCRIPTION = $"Maritech PO (Vendor={MTPEntry.VendorNo}/DocumentNr={MTPEntry.DocumentNo}/Line={LineCounter}",
                    LINEDESCRIPTION = $"Maritech PO (Vendor={MTPEntryHeaderMaster.VendorNo}/DocumentNr={MTPEntryHeaderMaster.DocumentNo}/Line={LineCounter}",

                    LINENUMBER = LineCounter,
                    //2022-03-12 ORDEREDPURCHASEQUANTITY = (postedEntryLine.DocumentType.Trim().ToUpper() == "KF" ? 1 : -1),
                    ORDEREDPURCHASEQUANTITY = (CurrencyAmount < 0 ? (-1) : (1)),
                    //2022-03-12 PURCHASEPRICE = (postedEntryLine.DocumentType.Trim().ToUpper() == "KF" ? (CurrencyAmount) : (CurrencyAmount * -1)),
                    //PURCHASEPRICE = (CurrencyAmount < 0 ? (-1 * CurrencyAmount) : CurrencyAmount),
                    PURCHASEPRICE = CurrencyAmount,
                    //2022-03-12 PURCHASEPRICEQUANTITY = 1,
                    PURCHASEPRICEQUANTITY = (CurrencyAmount < 0 ? (-1) : (1)),

                    //KPMGNO-1256 (PO med flere fakturaer)
                    //REQUESTEDDELIVERYDATE = MTPEntry.DocumentDate,
                    REQUESTEDDELIVERYDATE = MTPEntryHeaderMaster.DocumentDate,
                    
                    //2026-01-20: postedEntryLine.VATCode null handling
                    //SALESTAXITEMGROUPCODE = (VATMaps.Count == 0 ? postedEntryLine.VATCode : (VATMaps.Where(m => m.MaritechCode == postedEntryLine.VATCode).Any() ? VATMaps.Where(m => m.MaritechCode.Trim().ToUpper() == postedEntryLine.VATCode.Trim().ToUpper()).FirstOrDefault().D365Code : postedEntryLine.VATCode))
                    SALESTAXITEMGROUPCODE = (VATMaps.Count == 0 ? postedEntryLine.VATCode : (VATMaps.Where(m => m.MaritechCode == postedEntryLine.VATCode).Any() ? VATMaps.Where(m => m.MaritechCode.Trim().ToUpper() == postedEntryLine.VATCode.Trim().ToUpper()).FirstOrDefault().D365Code : (postedEntryLine.VATCode ?? "")))
                    #endregion
                };
                PurchaseOrderHeaderXML.PurchaseOrderLineV2Entities.Add(PurchaseOrderLine);
                #endregion
            }

            return PurchaseOrderHeaderXML;
        }
        private static LEDGERJOURNALENTITY ConvertGLEntry(string DocumentNr, MTDocument mtDoc, Dictionary<string, ExchangeRateD365> ExchangeRatesD365, string Description, int LineNumber, Dictionary<string, DimensionIntegrationFormat> DimensionIntegrationFormats)
        {
            List<LEDGERJOURNALENTITY> GLEntries = new List<LEDGERJOURNALENTITY>();
            //int LineNumber = 0;
            LEDGERJOURNALENTITY GLEntry = new LEDGERJOURNALENTITY();
            GLEntry.LedgerJournalEntityLines = new List<LedgerJournalEntityLine>();
            foreach (MTPostedEntry postedEntryLine in mtDoc.postedEntries)
            {
                LineNumber++;
                LedgerJournalEntityLine GLEntryLine = new LedgerJournalEntityLine();
                decimal CurrencyAmount = decimal.Parse(postedEntryLine.CurrencyAmount.Trim());
                decimal ExchangeRate = decimal.Parse(postedEntryLine.ExchangeRate.Trim());

                #region fixed values
                GLEntryLine.ACCOUNTTYPE = "Ledger";
                GLEntryLine.DEFAULTDIMENSIONDISPLAYVALUE = "";
                GLEntryLine.DOCUMENT = "";
                GLEntryLine.INVOICE = "";
                GLEntryLine.JOURNALNAME = "Avdb";
                GLEntryLine.MBSMERGER = "No";
                GLEntryLine.POSTINGLAYER = "Current";
                GLEntryLine.PREPAYMENT = "No";
                GLEntryLine.QUANTITY = 0;
                GLEntryLine.SALESTAXCODE = "";
                GLEntryLine.SALESTAXGROUP = "";
                GLEntryLine.TEXT = "";
                #endregion

                #region Conversion values
                //Definition: [Aktivitet]-[Generasjon]-[Leasinggjeld]-[MAINACCOUNT]-[Prosjekt]-[Reisekode]-[UNDERAVDELNING]-[Anleggsstatus]-[AVDELNING]-[Baerer]-[MOTPART]
                //GLEntryLine.ACCOUNTDISPLAYVALUE = $"---{postedEntryLine.AccountNo.Trim().ToUpper()}-----{postedEntryLine.DepartmentNo.Trim().ToUpper()}--";
                //On export: 30299-104411-
                //2022-03-25: MainAccount-Avdeling-Generasjon-Aktivitet-Anleggsstatus-Baerer-Leasinggjeld-Motpart-Prosjekt-Reisekode-Underavdeling
                //2022-03-25: MainAccount-Avdeling---------
                //2022-03-28: MainAccount-Avdeling-Generasjon-Aktivitet-Anleggsstatus-Baerer-Leasinggjeld-Motpart-Prosjekt-Reisekode-Underavdeling-Art
                // Uses Ledger dimension format (DIM2)
                //GLEntryLine.ACCOUNTDISPLAYVALUE = $"{postedEntryLine.AccountNo.Trim().ToUpper()}-{postedEntryLine.DepartmentNo.Trim().ToUpper()}----------";
                //2022-03-31: MainAccount-Aktivitet-Anleggsstatus-Avdeling-Baerer-Generasjon-Leasinggjeld-Motpart-Prosjekt-Reisekode-Underavdeling
                // Uses Ledger dimension format (DIM2)

                #region DEFAULTLEDGERDIMENSIONDISPLAYVALUE
                //MainAccount-Aktivitet-Anleggsstatus-Avdeling-Baerer-Generasjon-Leasinggjeld-Motpart-Prosjekt-Reisekode-Underavdeling
                string AccountDisplayValue = $"{postedEntryLine.AccountNo.Trim().ToUpper()}---{postedEntryLine.DepartmentNo?.Trim().ToUpper()??""}-------";
                if (DimensionIntegrationFormats != null && DimensionIntegrationFormats.Keys.Contains("DataEntityLedgerDimensionFormat"))
                {
                    //Aktivitet-Anleggsstatus-Avdeling-Baerer-Generasjon-Leasinggjeld-Motpart-Prosjekt-Reisekode-Underavdeling
                    string[] FinancialDimensionFormatDefinitions = DimensionIntegrationFormats["DataEntityLedgerDimensionFormat"].FinancialDimensionFormat.Split("-");
                    List<string> FinancialDimensionValues = new List<string>();
                    foreach (string FinancialDimension in FinancialDimensionFormatDefinitions)
                    {
                        if (FinancialDimension == "MainAccount")
                        {
                            FinancialDimensionValues.Add(postedEntryLine.AccountNo.Trim().ToUpper());
                        }
                        else if (FinancialDimension == "Avdeling")
                        {
                            FinancialDimensionValues.Add(postedEntryLine.DepartmentNo?.Trim().ToUpper()??"");
                        }
                        else
                        {
                            FinancialDimensionValues.Add("");
                        }
                    }
                    AccountDisplayValue = string.Join("-", FinancialDimensionValues);
                }

                #endregion
                
                //GLEntryLine.ACCOUNTDISPLAYVALUE = $"{postedEntryLine.AccountNo.Trim().ToUpper()}---{postedEntryLine.DepartmentNo.Trim().ToUpper()}-------";
                GLEntryLine.ACCOUNTDISPLAYVALUE = AccountDisplayValue;

                //SIT 2022-03-29
                GLEntryLine.CREDITAMOUNT = (CurrencyAmount < 0 ? (-1 * CurrencyAmount) : 0);
                GLEntryLine.DEBITAMOUNT = (CurrencyAmount > 0 ? CurrencyAmount : 0);

                GLEntryLine.CURRENCYCODE = postedEntryLine.CurrencyCode.Trim().ToUpper();
                GLEntryLine.DESCRIPTION = Description; //$"AVDB-{ postedEntryLine.DocumentNo }"; 
                GLEntryLine.DOCUMENTDATE = postedEntryLine.DocumentDate.Trim().ToUpper();
                GLEntryLine.DUEDATE = postedEntryLine.DueDate.Trim().ToUpper();
                GLEntryLine.EXCHANGERATE = ExchangeRate;
                GLEntryLine.LINENUMBER = LineNumber;
                GLEntryLine.TEXT = postedEntryLine.DocumentText;
                GLEntryLine.TRANSDATE = postedEntryLine.InvoiceDate;
                GLEntryLine.VOUCHER = $"{postedEntryLine.DocumentNo}";
                #endregion

                GLEntry.LedgerJournalEntityLines.Add(GLEntryLine);
            }

            return GLEntry;
        }
        private static string XMLStringFromObject(object o)
        {
            string RES = "";
            using (var stringwriter = new System.IO.StringWriter())
            {
                XmlSerializerNamespaces emptyNamespaces = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
                XmlSerializer serializer = new XmlSerializer(o.GetType());
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.OmitXmlDeclaration = true;
                using (var stream = new StringWriter())
                {
                    using (var writer = XmlWriter.Create(stream, settings))
                    {
                        serializer.Serialize(writer, o, emptyNamespaces);
                        string s = stream.ToString();
                        RES = s;
                    }
                }
            }
            return RES;
        }
    }
}
