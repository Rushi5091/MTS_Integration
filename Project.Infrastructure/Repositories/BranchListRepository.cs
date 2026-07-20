using Bogus.DataSets;
using Dapper;
using Microsoft.Extensions.Options;
using Newtonsoft.Json; // Add this line
using Newtonsoft.Json.Linq;
using Project.API.Configuration;
using Project.Core.Entities.Business;
using Project.Core.Entities.General;
using Project.Core.Interfaces.IRepositories;
using RestSharp;
using System.Data;
using System.Net;
using System.Text;
using System.Xml;
namespace Project.Infrastructure.Repositories
{
    public class BranchListRepository : BaseRepository<BranchList>, IBranchListRepository
    {
        private readonly AppSettings _appSettings;

        public BranchListRepository(IDbConnection dbConnection, IOptions<AppSettings> appSettings) : base(dbConnection)
        {
            _appSettings = appSettings.Value;
        }

        public async Task<bool> IsExists(string key, int? value)
        {
            var query = $"SELECT COUNT(1) FROM transaction_table WHERE {key} = @value;";
            var result = await _dbConnection.ExecuteScalarAsync<int>(query, new { value });
            return result == 1;
        }

        public async Task<BranchListResponseViewModel> BranchList(BranchList entity)
        {
            int? api_id = entity.BranchListAPI_ID;
            int? Client_ID = entity.Client_ID;
            int? Transaction_ID = entity.Transaction_ID;
            int? apistatus = 1;
            double? AgentRateapi = 0;
            string Message = ""; string Message1 = "";
            string apibankname = "", apiurl = "", apiuser = "", apipass = "", accesscode = "", apicompany_id = "", api_fields = "";
            string? SecurityKey = _appSettings.SecurityKey;
            string whereclause = " and a.ID=" + api_id;
            var storedProcedureName = "Get_APIDetails";
            var values = new
            {
                _whereclause = whereclause,
                _security_key = SecurityKey
            };

            var apidetails = await _dbConnection.QueryAsync(storedProcedureName, values, commandType: CommandType.StoredProcedure);
            dynamic apidetail = apidetails.First();

            api_id = Convert.ToInt32(apidetail.ID);
            apibankname = Convert.ToString(apidetail.Bank_Name);
            apiurl = Convert.ToString(apidetail.API_URL);
            apiuser = Convert.ToString(apidetail.APIUser_ID);
            apipass = Convert.ToString(apidetail.Password);
            accesscode = Convert.ToString(apidetail.APIAccess_Code);
            apicompany_id = Convert.ToString(apidetail.APICompany_ID);
            api_fields = Convert.ToString(apidetail.api_Fields);

            var branchDetails = new List<BranchDetailViewModel>();


            storedProcedureName = "getproceedtransaction_details";
            var values1 = new
            {
                iClient_ID = Client_ID,
                iTransaction_ID = Transaction_ID,
                iBranchListAPI_ID = api_id
            };

            var results = await _dbConnection.QueryAsync(storedProcedureName, values1, commandType: CommandType.StoredProcedure);


            dynamic result = (dynamic)results.FirstOrDefault();


            if (api_id > 0)
            {

                if (api_id == 3)
                {

                    apistatus = 0;
                    string company_id = "";
                    string Clientid = "";
                    string Agentcode = "";
                    string FrSubagent = "";
                    string Headers = "";
                    try
                    {
                        if (api_id == 3 && api_fields != "" && api_fields != null)
                        {
                            Newtonsoft.Json.Linq.JObject obj = Newtonsoft.Json.Linq.JObject.Parse(api_fields);
                            company_id = Convert.ToString(obj["company_id"]);
                            Clientid = Convert.ToString(obj["Clientid"]);
                            Agentcode = Convert.ToString(obj["Agentcode"]);
                            FrSubagent = Convert.ToString(obj["FrSubagent"]);
                            Headers = Convert.ToString(obj["Headers"]);
                        }
                    }
                    catch (Exception ex)
                    {
                        Message1 = ex.Message;
                        await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "BranchList", entity.user_id, entity.Branch_ID, Client_ID, 0);
                    }

                    string country_code = "";
                    string coutry_name = "";
                    string username = apiuser;
                    string password = apipass;
                    company_id = company_id;
                    string Customer_ID = result.Customer_ID.ToString();

                    var options = new RestClientOptions(apiurl + "/api/Token")
                    {
                        MaxTimeout = -1
                    };
                    var client = new RestClient(options);
                    var request = new RestRequest()
                    {
                        Method = Method.Post
                    };
                    string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
                    request.AddHeader("Authorization", $"Basic {credentials}");
                    string req = apiurl + "/api/Token" + credentials;
                    await SaveActivityLogTracker("Datafield Create Token Request From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                    var response = client.Execute(request);
                    await SaveActivityLogTracker("Datafield Create Token Response From MTS_Integration: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        dynamic json = JsonConvert.DeserializeObject(response.Content);
                        string token = json.Token;


                        options = new RestClientOptions(apiurl + "/api/CountryList")
                        {
                            MaxTimeout = -1
                        };
                        client = new RestClient(options);
                        request = new RestRequest()
                        {
                            Method = Method.Post
                        };

                        request.AddHeader(Headers, token);
                        request.AddHeader("Content-Type", "application/json");
                        request.AddHeader("Authorization", $"Basic {credentials}");

                        var body = new
                        {
                            UserId = username,
                            CompanyID = company_id
                        };

                        string bodyJson = JsonConvert.SerializeObject(body);

                        string encryptedData = Encrypt(bodyJson);


                        var requestBody = new
                        {
                            jsonstring = encryptedData
                        };
                        request.AddParameter("application/json", JsonConvert.SerializeObject(requestBody), ParameterType.RequestBody);
                        req = apiurl + "/api/CountryList" + body + "jsonstring:" + requestBody;
                        await SaveActivityLogTracker("Datafield CountryList Request From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.user_id), "BranchList", entity.Branch_ID, Client_ID);

                        response = client.Execute(request);
                        json = JsonConvert.DeserializeObject(response.Content);
                        await SaveActivityLogTracker("Datafield CountryList Response From MTS_Integration: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.user_id), "BranchList", entity.Branch_ID, Client_ID);

                        string targetCountryCode = entity.Country_Code;


                        foreach (var country5 in json.CountryList)
                        {
                            if (country5.CountryCode == targetCountryCode)
                            {
                                coutry_name = country5.Country;
                                country_code = country5.CountryCode;
                            }
                        }





                        options = new RestClientOptions(apiurl + "/api/BranchList")
                        {
                            MaxTimeout = -1
                        };
                        client = new RestClient(options);
                        request = new RestRequest()
                        {
                            Method = Method.Post
                        };

                        request.AddHeader(Headers, token);
                        request.AddHeader("Content-Type", "application/json");
                        request.AddHeader("Authorization", $"Basic {credentials}");

                        var body4 = new
                        {
                            UserId = username,
                            CompanyID = company_id,
                            CountryCode = country_code,
                            type = "Cash"
                        };

                        bodyJson = JsonConvert.SerializeObject(body4);

                        encryptedData = Encrypt(bodyJson);


                        requestBody = new
                        {
                            jsonstring = encryptedData
                        };
                        request.AddParameter("application/json", JsonConvert.SerializeObject(requestBody), ParameterType.RequestBody);
                        req = apiurl + "/api/BranchList" + body4 + "jsonstring: " + requestBody;
                        await SaveActivityLogTracker("Datafield BranchList Request From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.user_id), "BranchList", entity.Branch_ID, Client_ID);

                        response = client.Execute(request);
                        json = JsonConvert.DeserializeObject(response.Content);
                        await SaveActivityLogTracker("Datafield BranchList Response From MTS_Integration: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.user_id), "BranchList", entity.Branch_ID, Client_ID);

                        var obj1 = Newtonsoft.Json.Linq.JObject.Parse(response.Content);
                        var arr = obj1["Branch_List"];

                        foreach (var branch in arr)
                        {

                            string branch_code = Convert.ToString(branch["Branchcode"]);
                            string city = Convert.ToString(branch["City"]);
                            string Address = Convert.ToString(branch["Address"]);
                            branchDetails.Add(new BranchDetailViewModel
                            {
                                BranchCode = branch_code,
                                City = city,
                                Country = entity.Country_Name,
                                Address = Address,
                                ApiId = api_id.Value
                            });


                        }




                    }
                }
                else if (api_id == 2) // GCC Remit Transaction
                {
                    #region gcc_get_branchlist

                    apistatus = 0;
                    string custCountryName = "";

                    string countrycode = entity.Country_Code;

                    try
                    {
                        await SaveActivityLogTracker("Get GCC Remit Branch List 1", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.user_id), "BranchList", entity.Branch_ID, Client_ID);

                        storedProcedureName = "Country_Search";
                        var valuesCountry = new
                        {
                            ISO_Code = Convert.ToString(countrycode).Trim()
                        };
                        var countryResult = await _dbConnection.QueryAsync(storedProcedureName, valuesCountry, commandType: CommandType.StoredProcedure);
                        dynamic dt_country = countryResult.FirstOrDefault();
                        if (dt_country != null)
                        {
                            custCountryName = Convert.ToString(dt_country.Country_Name).ToUpper().Trim();
                        }

                        // Country Code Check Here
                        if (countrycode == "FR" || countrycode == "DE" || countrycode == "BE" || countrycode == "BG" || countrycode == "DK" || countrycode == "CH"
                        || countrycode == "SE" || countrycode == "ES" || countrycode == "SI" || countrycode == "NL" || countrycode == "IT"
                        || countrycode == "LV" || countrycode == "LI" || countrycode == "LT" || countrycode == "LU" || countrycode == "MT" || countrycode == "MC" || countrycode == "NO"
                        || countrycode == "PL" || countrycode == "PT" || countrycode == "RO" || countrycode == "SM" || countrycode == "SK" || countrycode == "CZ" || countrycode == "AT"
                        || countrycode == "HR" || countrycode == "CY" || countrycode == "EE" || countrycode == "FI" || countrycode == "GR" || countrycode == "HU" || countrycode == "IS" || countrycode == "IE")
                        {
                            countrycode = "EU";
                        }

                        // Gat PaymentMode Here New Update from GCC team
                        string countryCurrency = "";
                        if ("EU" == countrycode) { countryCurrency = "EUR"; }

                        int Transfer_ID = 0;
                        try
                        {
                            Transfer_ID = Convert.ToInt32(result.Transfer_ID);
                        }
                        catch { }

                        if (Transfer_ID > 0)
                        {
                            storedProcedureName = "View_Transfer";
                            var valuesTransfer = new
                            {
                                Transaction_ID = Transfer_ID
                            };
                            var transferResult = await _dbConnection.QueryAsync(storedProcedureName, valuesTransfer, commandType: CommandType.StoredProcedure);
                            dynamic dt_transfer = transferResult.FirstOrDefault();
                            if (dt_transfer != null && countryCurrency == "")
                            {
                                countryCurrency = Convert.ToString(dt_transfer.Currency_Code).ToUpper().Trim();
                            }
                        }
                        else
                        {
                            try
                            {
                                if (countryCurrency == "")
                                    countryCurrency = Convert.ToString(result.currencyTitle).ToUpper().Trim();
                            }
                            catch { }
                        }

                        var optionsPaymentMode = new RestClientOptions(apiurl)
                        {
                            MaxTimeout = -1
                        };
                        var clientPaymentMode = new RestClient(optionsPaymentMode);
                        var requestPaymentMode = new RestRequest()
                        {
                            Method = Method.Post
                        };
                        requestPaymentMode.AddHeader("Content-Type", "text/xml; charset=utf-8");
                        requestPaymentMode.AddHeader("SOAPAction", "http://tempuri.org/ISendAPI/GetPaymentModeList");

                        var bodyPaymentMode = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:tem=""http://tempuri.org/"" xmlns:grem=""http://schemas.datacontract.org/2004/07/GRemitWCFService.Send"">" + "\n" +
                            @"   <soapenv:Header/>" + "\n" +
                            @"   <soapenv:Body>" + "\n" +
                            @"      <tem:GetPaymentModeList>" + "\n" +
                            @"         <tem:req>" + "\n" +
                            @"            <grem:CountryCode>" + countrycode + "</grem:CountryCode>" + "\n" +
                            @"            <grem:CurrencyCode>" + countryCurrency + "</grem:CurrencyCode>" + "\n" +
                            @"            <grem:Password>" + apipass + "</grem:Password>" + "\n" +
                            @"            <grem:SecurityKey>" + accesscode + "</grem:SecurityKey>" + "\n" +
                            @"            <grem:UniqueID>" + apiuser + "</grem:UniqueID>" + "\n" +
                            @"         </tem:req>" + "\n" +
                            @"      </tem:GetPaymentModeList>" + "\n" +
                            @"   </soapenv:Body>" + "\n" +
                            @"</soapenv:Envelope>";

                        requestPaymentMode.AddParameter("text/xml; charset=utf-8", bodyPaymentMode, ParameterType.RequestBody);

                        await SaveActivityLogTracker("Get GCC GetPaymentModeList request parameter: <br/>" + bodyPaymentMode + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.user_id), "BranchList", entity.Branch_ID, Client_ID);

                        RestResponse responsePaymentMode = clientPaymentMode.Execute(requestPaymentMode);

                        await SaveActivityLogTracker("Get GCC GetPaymentModeList response parameter: <br/>" + responsePaymentMode.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.user_id), "BranchList", entity.Branch_ID, Client_ID);

                        XmlDocument xmlDocPaymentMode = new XmlDocument();
                        xmlDocPaymentMode.LoadXml(responsePaymentMode.Content);
                        XmlNodeList nodeListPaymentMode = xmlDocPaymentMode.GetElementsByTagName("Table");

                        foreach (XmlNode node1 in nodeListPaymentMode)
                        {
                            string json = Newtonsoft.Json.JsonConvert.SerializeXmlNode(node1);
                            var obj1 = Newtonsoft.Json.Linq.JObject.Parse(json);
                            string PaymentModeName = Convert.ToString(obj1["Table"]["PaymentModeName"]).ToUpper().Trim();
                            string PaymentModeCode = Convert.ToString(obj1["Table"]["PaymentModeCode"]).Trim();

                            // In Calyx system 1 for Bank, 2 for Cash, 3 for Mobile Wallet
                            if (result.PaymentDepositType_ID == 1) // BANK
                            {
                                if (PaymentModeName.Contains("MOBILE") || PaymentModeName.Contains("CASH"))
                                {
                                    continue;
                                }
                            }
                            else if (result.PaymentDepositType_ID == 2) // CASH
                            {
                                if (PaymentModeName.Contains("MOBILE") || PaymentModeName.Contains("CREDIT") || PaymentModeName.Contains("IMPS") || PaymentModeName.Contains("RTGS"))
                                {
                                    continue;
                                }
                            }
                            else if (result.PaymentDepositType_ID == 3) // Mobile Wallet
                            {
                                if (PaymentModeName.Contains("Home") || PaymentModeName.Contains("Pickup") || PaymentModeName.Contains("CREDIT") || PaymentModeName.Contains("IMPS") || PaymentModeName.Contains("RTGS"))
                                {
                                    continue;
                                }
                            }

                            try
                            {
                                var optionsBranchList = new RestClientOptions(apiurl)
                                {
                                    MaxTimeout = -1
                                };
                                var clientBranchList = new RestClient(optionsBranchList);
                                var requestBranchList = new RestRequest()
                                {
                                    Method = Method.Post
                                };
                                requestBranchList.AddHeader("Content-Type", "text/xml; charset=utf-8");
                                requestBranchList.AddHeader("SOAPAction", "http://tempuri.org/ISendAPI/GetBranchList");

                                var bodyBranchList = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:tem=""http://tempuri.org/"" xmlns:grem=""http://schemas.datacontract.org/2004/07/GRemitWCFService.Send"">" + "\n" +
                                    @"   <soapenv:Header/>" + "\n" +
                                    @"   <soapenv:Body>" + "\n" +
                                    @"      <tem:GetBranchList>" + "\n" +
                                    @"         <tem:req>" + "\n" +
                                    @"            <grem:CityCode>0</grem:CityCode>" + "\n" +
                                    @"            <grem:CountryCode>" + countrycode + "</grem:CountryCode>" + "\n" +
                                    @"            <grem:CurrencyCode>" + countryCurrency + "</grem:CurrencyCode>" + "\n" +
                                    @"            <grem:Password>" + apipass + "</grem:Password>" + "\n" +
                                    @"            <grem:PaymentModeCode>" + PaymentModeCode + "</grem:PaymentModeCode>" + "\n" +
                                    @"            <grem:Search></grem:Search>" + "\n" +
                                    @"            <grem:SecurityKey>" + accesscode + "</grem:SecurityKey>" + "\n" +
                                    @"            <grem:UniqueID>" + apiuser + "</grem:UniqueID>" + "\n" +
                                    @"         </tem:req>" + "\n" +
                                    @"      </tem:GetBranchList>" + "\n" +
                                    @"   </soapenv:Body>" + "\n" +
                                    @"</soapenv:Envelope>";

                                requestBranchList.AddParameter("text/xml; charset=utf-8", bodyBranchList, ParameterType.RequestBody);

                                await SaveActivityLogTracker("Get GCC GetBranchList request parameter: <br/>" + bodyBranchList + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.user_id), "BranchList", entity.Branch_ID, Client_ID);

                                RestResponse responseBranchList = clientBranchList.Execute(requestBranchList);

                                await SaveActivityLogTracker("Get GCC GetBranchList response parameter: <br/>" + responseBranchList.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.user_id), "BranchList", entity.Branch_ID, Client_ID);

                                XmlDocument xmlDocBranchList = new XmlDocument();
                                xmlDocBranchList.LoadXml(responseBranchList.Content);
                                XmlNodeList nodeListBranch = xmlDocBranchList.GetElementsByTagName("Table");

                                foreach (XmlNode node2 in nodeListBranch)
                                {
                                    string json2 = Newtonsoft.Json.JsonConvert.SerializeXmlNode(node2);
                                    var obj2 = Newtonsoft.Json.Linq.JObject.Parse(json2);
                                    string paymentMode = Convert.ToString(obj2["Table"]["PaymentModeName"]).Trim();
                                    string branchCode = Convert.ToString(obj2["Table"]["BranchCode"]).Trim();
                                    string bankNM = Convert.ToString(obj2["Table"]["BranchName"]).Trim();
                                    string branchAddress = Convert.ToString(obj2["Table"]["BranchAddress"]).Trim().ToLower();

                                    branchDetails.Add(new BranchDetailViewModel
                                    {
                                        BranchCode = branchCode,
                                        City = custCountryName,
                                        Country = custCountryName,
                                        Address = "(" + bankNM + " : " + paymentMode + " : " + branchAddress + ")",
                                        ApiId = api_id.Value
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                Message1 = ex.Message;
                                await SaveErrorLogAsync("Get GCC GetBranchList exception: <br/> " + ex.ToString(), DateTime.Now, "BranchList", entity.user_id, entity.Branch_ID, Client_ID, 0);
                            }
                        }

                        // Filter down to the beneficiary's last used collection point, if one exists
                        try
                        {
                            if (Convert.ToInt32(result.Beneficiary_ID) != 0)
                            {
                                storedProcedureName = "GetLastBenf_POC";
                                var valuesLastBenf = new
                                {
                                    _Beneficiary_ID = Convert.ToInt32(result.Beneficiary_ID),
                                    _ApiId = api_id
                                };
                                var lastBenfResult = await _dbConnection.QueryAsync(storedProcedureName, valuesLastBenf, commandType: CommandType.StoredProcedure);
                                dynamic dt_CollPoint = lastBenfResult.FirstOrDefault();

                                if (dt_CollPoint != null)
                                {
                                    string[] parts = Convert.ToString(dt_CollPoint.bank_code).Split(new string[] { " - " }, StringSplitOptions.None);
                                    string apibranchCode = parts.Length > 0 ? parts[0].Trim() : string.Empty;

                                    var existingBranch = branchDetails.FirstOrDefault(b => b.BranchCode == apibranchCode);
                                    if (existingBranch != null)
                                    {
                                        branchDetails = new List<BranchDetailViewModel> { existingBranch };
                                    }
                                    else
                                    {
                                        await SaveActivityLogTracker("check existingRow " + apibranchCode + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.user_id), "BranchList", entity.Branch_ID, Client_ID);
                                    }
                                }
                                else
                                {
                                    await SaveActivityLogTracker("get data of API_BranchDetails ", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.user_id), "BranchList", entity.Branch_ID, Client_ID);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Message1 = ex.Message;
                            await SaveErrorLogAsync("GetLastBenf_POC block Error: <br/> " + ex.ToString(), DateTime.Now, "BranchList", entity.user_id, entity.Branch_ID, Client_ID, 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        Message1 = ex.Message;
                        await SaveErrorLogAsync("Get GCC Remit Branch List Error exception: <br/> " + ex.ToString(), DateTime.Now, "BranchList", entity.user_id, entity.Branch_ID, Client_ID, 0);
                    }

                    #endregion
                }

                else if (api_id == 15)
                {
                    #region amal_get_collectionpoints
                    apistatus = 0;
                    Newtonsoft.Json.Linq.JObject jsongetServices = new Newtonsoft.Json.Linq.JObject();
                    Newtonsoft.Json.Linq.JObject jsongetServiceOperators = new Newtonsoft.Json.Linq.JObject();
                    Newtonsoft.Json.Linq.JObject jsonGetCitiesByCountryId = new Newtonsoft.Json.Linq.JObject();
                    string clientkey = "";
                    string secretkey = "";
                    string SourceBranchkey_api = "";
                    if (api_id == 15 && api_fields != "" && api_fields != null)
                    {
                        Newtonsoft.Json.Linq.JObject obj12 = Newtonsoft.Json.Linq.JObject.Parse(api_fields);
                        clientkey = Convert.ToString(obj12["clientkey"]);
                        secretkey = Convert.ToString(obj12["secretkey"]);
                        SourceBranchkey_api = Convert.ToString(obj12["SourceBranchkey"]);
                    }
                    try
                    {
                        await SaveActivityLogTracker("Get Amal city Collection Points 1", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.user_id), "BranchList", entity.Branch_ID, Client_ID);
                        if (result.PaymentDepositType_ID == 1)
                        {
                            string benf_id = Convert.ToString(result.Beneficiary_ID);
                            string cityNamedefault = "";
                            string defaultcityId = "";
                            try
                            {
                                defaultcityId = result.Beneficiary_City_ID.ToString();
                                cityNamedefault = result.Beneficiary_City.ToString().ToUpper();
                            }
                            catch (Exception ex)
                            {
                                Message1 = ex.Message;
                                await SaveErrorLogAsync("Get Amal city Collection Points Bank Error: <br/> " + ex.ToString(), DateTime.Now, "BranchList", entity.user_id, entity.Branch_ID, Client_ID, 0);

                            }

                            branchDetails.Add(new BranchDetailViewModel
                            {
                                BranchCode = cityNamedefault,
                                City = defaultcityId,
                                Country = entity.Country_Name,
                                Address = "",
                                ApiId = api_id.Value
                            });
                        }
                        else
                        {

                            storedProcedureName = "GetCountryCodes";
                            var values2 = new
                            {
                                country_code = entity.Country_ID,
                                api_id = api_id,
                            };

                            var GetCountryCodes = await _dbConnection.QueryAsync(storedProcedureName, values2, commandType: CommandType.StoredProcedure);
                            dynamic dt_country = (dynamic)GetCountryCodes.FirstOrDefault();

                            string countryCodeAmal = dt_country.country_code.ToString();
                            string benf_id = Convert.ToString(result.Beneficiary_ID);
                            string collectionPoint_ID = "", transactionId = "";
                            try
                            {
                                //collectionPoint_ID = "Convert.ToString(dictObjMain["CollectionPoint_ID"])";
                                transactionId = Convert.ToString(Transaction_ID);

                            }
                            catch (Exception ex)
                            {
                                Message1 = ex.Message;
                                await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "BranchList", entity.user_id, entity.Branch_ID, Client_ID, 0);
                            }



                            string cityNamedefault = "";

                            try
                            {
                                cityNamedefault = result.Beneficiary_City.ToString().ToUpper();
                            }
                            catch (Exception ex)
                            {
                                Message1 = ex.Message;
                                await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "BranchList", entity.user_id, entity.Branch_ID, Client_ID, 0);
                            }


                            string savedCollectionPointId = "";
                            if (transactionId != "")
                            {
                                try
                                {


                                    storedProcedureName = "GetAPI_BranchDetails";
                                    var values3 = new
                                    {
                                        transactionId = Transaction_ID,

                                    };

                                    var API_BranchDetailsds = await _dbConnection.QueryAsync(storedProcedureName, values3, commandType: CommandType.StoredProcedure);
                                    dynamic API_BranchDetailsd = API_BranchDetailsds.FirstOrDefault();

                                    savedCollectionPointId = API_BranchDetailsd.API_BranchDetails.ToString().Trim();
                                    string[] words = savedCollectionPointId.Split('-');
                                    int v = 0; string cityCodes = "";
                                    if (words.Count() == 1)
                                    {
                                        cityCodes = words[0].Trim();
                                    }
                                    else
                                    {
                                        foreach (var word in words)
                                        {
                                            if (v == 1)
                                            {
                                                cityCodes = word.Trim();
                                                string[] tokens = cityCodes.Split(' ');
                                                cityCodes = tokens[0];
                                                break;
                                            }
                                            v++;
                                        }
                                    }
                                    bool digitsOnly = cityCodes.All(char.IsDigit);
                                    if (!digitsOnly)
                                    {
                                        cityCodes = "";
                                    }
                                    savedCollectionPointId = cityCodes.Trim();
                                }
                                catch (Exception ex)
                                {
                                    Message1 = ex.Message;
                                    await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "BranchList", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                }
                            }


                            string token = "";

                            try
                            {



                                string usernamne = apiuser;
                                string pass = apipass;

                                string cred = Convert.ToBase64String(Encoding.Default.GetBytes(usernamne + ":" + pass));

                                var options = new RestClientOptions(apiurl + "/Auth/Token")
                                {
                                    MaxTimeout = -1
                                };
                                var client = new RestClient(options);
                                var request = new RestRequest()
                                {
                                    Method = Method.Get
                                };
                                request.AddHeader("username", usernamne);
                                request.AddHeader("secretkey", secretkey);
                                request.AddHeader("Authorization", "Basic " + cred);
                                ServicePointManager.Expect100Continue = true;
                                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                                System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
                                string req = apiurl + "/Auth/Token";
                                await SaveActivityLogTracker("Amal Create Token Request From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.user_id), "BranchList", entity.Branch_ID, Client_ID);
                                RestResponse response = client.Execute(request);
                                await SaveActivityLogTracker("Amal Create Token Response From MTS_Integration: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.user_id), "BranchList", entity.Branch_ID, Client_ID);
                                var jsonObject = JObject.Parse(response.Content);
                                token = jsonObject["response"]?["result"]?["token"]?.ToString();

                            }
                            catch (Exception ex)
                            {
                                Message1 = ex.Message;
                                await SaveErrorLogAsync("Amal Token Generation Exception From MTS_Integration: <br/> " + ex.ToString(), DateTime.Now, "BranchList", entity.user_id, entity.Branch_ID, Client_ID, 0);
                            }




                            try
                            {
                                int countryId = Convert.ToInt32(countryCodeAmal);
                                var options = new RestClientOptions(apiurl + "/Services/GetCitiesByCountryId")
                                {
                                    MaxTimeout = -1
                                };
                                var client = new RestClient(options);
                                var request = new RestRequest()
                                {
                                    Method = Method.Post
                                };
                                request.AddHeader("Authorization", "Bearer " + token);
                                request.AddHeader("username", apiuser);
                                request.AddHeader("secretkey", secretkey);
                                request.AddHeader("Content-Type", "application/json");
                                var body = @"{
                    " + "\n" +
                                        @"  ""username"": """ + apiuser + @""",
                    " + "\n" +
                                        @"	""password"": """ + apipass + @""",
                    " + "\n" +
                                        @"	""clientkey"": """ + clientkey + @""",
                    " + "\n" +
                                        @"    ""countryId"": " + countryId + @",
                    " + "\n" +
                                        @"  ""sourceBranchkey"": """ + SourceBranchkey_api + @"""
                    " + "\n" +
                                        @"
                    " + "\n" +
                                        @" 
                    " + "\n" +
                                        @"}";


                                request.AddParameter("application/json", body, ParameterType.RequestBody);
                                ServicePointManager.Expect100Continue = true;
                                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                                System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
                                string req = apiurl + "/Services/GetCitiesByCountryId" + body;
                                await SaveActivityLogTracker("getCityId Request Request From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.user_id), "BranchList", entity.Branch_ID, Client_ID);

                                var response = client.Execute(request);
                                await SaveActivityLogTracker("getCityId Request Response From MTS_Integration: <br/>" + response.Content.Replace("'", "\"") + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.user_id), "BranchList", entity.Branch_ID, Client_ID);

                                jsonGetCitiesByCountryId = Newtonsoft.Json.Linq.JObject.Parse(response.Content);

                            }
                            catch (Exception ex)
                            {
                                Message1 = ex.Message;
                                await SaveErrorLogAsync("Get Amal city Collection Points exception: <br/> " + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                            }

                            Newtonsoft.Json.Linq.JObject json = jsonGetCitiesByCountryId;
                            await SaveActivityLogTracker("Get Amal city Collection Points 2 From MTS_Integration: <br/>" + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.user_id), "BranchList", entity.Branch_ID, Client_ID);

                            var arr1 = (json["response"]["result"]["Cities"]["City"]);
                            int foundCityId = 0; ;

                            if (collectionPoint_ID == "")
                            {
                                foreach (Newtonsoft.Json.Linq.JObject item in arr1)
                                {
                                    string CityId = item.GetValue("CityId").ToString().Trim();
                                    string CityName = item.GetValue("CityName").ToString().ToUpper();
                                    if (cityNamedefault == CityName || savedCollectionPointId == CityId)
                                    {
                                        foundCityId = 1;
                                        branchDetails.Add(new BranchDetailViewModel
                                        {
                                            BranchCode = CityName,
                                            City = CityId,
                                            Country = entity.Country_Name,
                                            Address = "",
                                            ApiId = api_id.Value
                                        });
                                        break;
                                    }
                                }
                                foreach (Newtonsoft.Json.Linq.JObject item in arr1)
                                {
                                    string CityId = item.GetValue("CityId").ToString().Trim(); ;
                                    string CityName = item.GetValue("CityName").ToString().ToUpper();
                                    if (CityId.Trim() != collectionPoint_ID.Trim())
                                    {
                                        if (savedCollectionPointId == CityId) { }
                                        else
                                        {
                                            foundCityId = 1;
                                            branchDetails.Add(new BranchDetailViewModel
                                            {
                                                BranchCode = CityName,
                                                City = CityId,
                                                Country = entity.Country_Name,
                                                Address = "",
                                                ApiId = api_id.Value
                                            });
                                        }
                                    }
                                }
                            }
                            else
                            {
                                foreach (Newtonsoft.Json.Linq.JObject item in arr1)
                                {
                                    string CityId = item.GetValue("CityId").ToString().Trim(); ;
                                    string CityName = item.GetValue("CityName").ToString().ToUpper();
                                    if (collectionPoint_ID.Trim() == CityId.Trim() || savedCollectionPointId == CityId)
                                    {
                                        foundCityId = 1;
                                        branchDetails.Add(new BranchDetailViewModel
                                        {
                                            BranchCode = CityName,
                                            City = CityId,
                                            Country = entity.Country_Name,
                                            Address = "",
                                            ApiId = api_id.Value
                                        }); break;
                                    }
                                }
                                foreach (Newtonsoft.Json.Linq.JObject item in arr1)
                                {
                                    string CityId = item.GetValue("CityId").ToString().Trim();
                                    string CityName = item.GetValue("CityName").ToString().ToUpper();
                                    if (CityId.Trim() != collectionPoint_ID.Trim())
                                    {
                                        if (savedCollectionPointId == CityId) { }
                                        else
                                        {
                                            foundCityId = 1;
                                            branchDetails.Add(new BranchDetailViewModel
                                            {
                                                BranchCode = CityName,
                                                City = CityId,
                                                Country = entity.Country_Name,
                                                Address = "",
                                                ApiId = api_id.Value
                                            });
                                        }
                                    }
                                }
                            }
                            if (foundCityId == 0)
                            {
                                foreach (Newtonsoft.Json.Linq.JObject item in arr1)
                                {
                                    string CityId = item.GetValue("CityId").ToString().Trim(); ;
                                    string CityName = item.GetValue("CityName").ToString().ToUpper();
                                    branchDetails.Add(new BranchDetailViewModel
                                    {
                                        BranchCode = CityName,
                                        City = CityId,
                                        Country = entity.Country_Name,
                                        Address = "",
                                        ApiId = api_id.Value
                                    });
                                }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        Message1 = ex.Message;
                        await SaveErrorLogAsync("Get Amal city Collection Points Error exception: <br/> " + ex.ToString(), DateTime.Now, "BranchList", entity.user_id, entity.Branch_ID, Client_ID, 0);
                    }
                    #endregion
                }



                else if (api_id == 45)
                {
                    #region Dynathopia
                    apistatus = 0;
                    branchDetails.Add(new BranchDetailViewModel
                    {
                        BranchCode = "Dynathopia",
                        City = "",
                        Country = entity.Country_Name,
                        Address = "",
                        ApiId = api_id.Value
                    });
                    #endregion Dynathopia
                }
                else if (api_id == 47)
                {
                    #region Budpay
                    apistatus = 0;
                    branchDetails.Add(new BranchDetailViewModel
                    {
                        BranchCode = "Budpay",
                        City = "",
                        Country = entity.Country_Name,
                        Address = "",
                        ApiId = api_id.Value
                    });
                    #endregion Budpay
                }
                else if (api_id == 48)
                {
                    #region HelloPaisa
                    if (result.PaymentDepositType_ID == 2)
                    {
         
                        try
                        {
                            string clause = "apacc.Collection_Type = " + Convert.ToInt32(result.PaymentDepositType_ID) +
                                           " AND apacc.Country_ID = " + Convert.ToInt32(entity.Country_ID);
                            var storedProcedureName125 = "Get_Provider_Details_By_Id";
                            var values125 = new
                            {
                                in_Client_ID = Convert.ToInt32(Client_ID),
                                WhereClause = clause,
                            };

                            var Get_Provider_Details_By_Id = await _dbConnection.QueryAsync(storedProcedureName125, values125, commandType: CommandType.StoredProcedure);
                            dynamic dtp = Get_Provider_Details_By_Id.FirstOrDefault();

                            if (dtp!= "")
                            {
                               
                                foreach (DataRow row in dtp.Rows)
                                {
                                    string Provider_name = Convert.ToString(row["Provider_name"]);
                                    string BankRoute = Convert.ToString(row["ProviderPayerID"]);

                                    branchDetails.Add(new BranchDetailViewModel
                                    {
                                        BranchCode = Provider_name,
                                        City = BankRoute,
                                        Country = entity.Country_Name,
                                        Address = BankRoute,
                                        ApiId = api_id.Value
                                    });

                                }

                            }
                        }
                        catch (Exception ex) { }
                    }
                    else
                    {
                        apistatus = 0;
                        branchDetails.Add(new BranchDetailViewModel
                        {
                            BranchCode = "Hello Paisa",
                            City = "",
                            Country = entity.Country_Name,
                            Address = "",
                            ApiId = api_id.Value
                        });
                    }
                    #endregion HelloPaisa
                }

            }
            if (apistatus == 0)
            {
                return new BranchListResponseViewModel
                {
                    Status = "Success",
                    StatusCode = 0,
                    Message = Message,
                    ApiId = Transaction_ID,
                    AgentRate = AgentRateapi,
                    ApiStatus = apistatus,
                    ExtraFields = new List<string> { "", "" },
                    BranchDetails = branchDetails // Add this line
                };
            }
            else
            {
                Message = "Description :" + Message1;
                return new BranchListResponseViewModel
                {
                    Status = "Failed",
                    StatusCode = 2,
                    Message = Message,
                    ApiId = Transaction_ID,
                    AgentRate = AgentRateapi,
                    ApiStatus = apistatus,
                    ExtraFields = new List<string> { "", "" },
                    BranchDetails = branchDetails // Add this line
                };
            }
        }
    }
}