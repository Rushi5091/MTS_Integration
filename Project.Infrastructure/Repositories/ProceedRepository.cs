using Dapper;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Project.Core.Entities.General;
using Project.Core.Interfaces.IRepositories;
using RestSharp;
using System.Data;
using Project.API.Configuration;
using System.Net;
using System.Text;
using System.Linq;

namespace Project.Infrastructure.Repositories
{
    public class ProceedRepository : BaseRepository<Proceed>, IProceedRepository
    {
        private readonly AppSettings _appSettings;

        public ProceedRepository(IDbConnection dbConnection, IOptions<AppSettings> appSettings) : base(dbConnection)
        {
            _appSettings = appSettings.Value;
        }

        public async Task<bool> IsExists(string key, int? value)
        {
            var query = $"SELECT COUNT(1) FROM transaction_table WHERE {key} = @value;";
            var result = await _dbConnection.ExecuteScalarAsync<int>(query, new { value });
            return result == 1;
        }

        public async Task<ProceedResponseViewModel> Proceed(Proceed entity)
        {
            int? api_id = entity.BranchListAPI_ID;
            int? Client_ID = entity.Client_ID;
            int? Transaction_ID = entity.Transaction_ID;
            int? apistatus = 1;
            double? AgentRateapi = 0;
            string Message = "";
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
            dynamic apidetail = apidetails.FirstOrDefault();


            api_id = Convert.ToInt32(apidetail.ID);
            apibankname = Convert.ToString(apidetail.Bank_Name);
            apiurl = Convert.ToString(apidetail.API_URL);
            apiuser = Convert.ToString(apidetail.APIUser_ID);
            apipass = Convert.ToString(apidetail.Password);
            accesscode = Convert.ToString(apidetail.APIAccess_Code);
            apicompany_id = Convert.ToString(apidetail.APICompany_ID);
            api_fields = Convert.ToString(apidetail.api_Fields);

            storedProcedureName = "getproceedtransaction_details";
            var values1 = new
            {
                iClient_ID = Client_ID,
                iTransaction_ID = Transaction_ID,
                iBranchListAPI_ID = api_id
            };

            var results = await _dbConnection.QueryAsync(storedProcedureName, values1, commandType: CommandType.StoredProcedure);

            // Make sure to cast or assign to dynamic
            dynamic result = (dynamic)results.FirstOrDefault();



            if (api_id == 15)
            {
                #region amal_single_proceed
                Newtonsoft.Json.Linq.JObject json = new Newtonsoft.Json.Linq.JObject();
                Newtonsoft.Json.Linq.JObject jsongetServices = new Newtonsoft.Json.Linq.JObject();
                Newtonsoft.Json.Linq.JObject jsongetServiceOperators = new Newtonsoft.Json.Linq.JObject();
                Newtonsoft.Json.Linq.JObject jsonGetCitiesByCountryId = new Newtonsoft.Json.Linq.JObject();
                string proceedMethod = " Single proceed  ";
                 string Username_api = ""; string password_api = ""; string clientkey_api = ""; string SourceBranchkey_api = "";
                string url = "", amalTimeZone = "";
                string clientkey = "";
                string secretkey = "";
                string Transactionreference = "";
                string proceed_flag = "";
                string Customer_ID = result.Customer_ID.ToString();
                try
                {
                    proceed_flag = Convert.ToString(entity.Processing_Flag);//Convert.ToString(dictObjMain["processed_type"]);
                }
                catch { }
                if (api_fields != "" && api_fields != null)
                {
                    url = apiurl;

                    if (api_id == 15 && api_fields != "" && api_fields != null)
                    {
                        Newtonsoft.Json.Linq.JObject obj12 = Newtonsoft.Json.Linq.JObject.Parse(api_fields);
                        clientkey = Convert.ToString(obj12["clientkey"]);
                        secretkey = Convert.ToString(obj12["secretkey"]);
                        SourceBranchkey_api = Convert.ToString(obj12["SourceBranchkey"]);
                    }
                }




                string Apitrans_id = "";
                Apitrans_id = result.APITransaction_ID;
                Transactionreference = result.ReferenceNo;
                string token = "";
                int ServiceId = 0;

                try
                {



                    string usernamne = apiuser;
                    string pass = apipass;

                    string cred = Convert.ToBase64String(Encoding.Default.GetBytes(usernamne + ":" + pass));

                    var options = new RestClientOptions(url + "/Auth/Token")
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
                    string req = url + "/Auth/Token";
                    await SaveActivityLogTracker("Amal Create Token Request From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);
                    RestResponse response = client.Execute(request);
                    await SaveActivityLogTracker("Amal Create Token Response From MTS_Integration: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);
                    var jsonObject = JObject.Parse(response.Content);
                    token = jsonObject["response"]?["result"]?["token"]?.ToString();

                }
                catch (Exception ex)
                {
                    await SaveErrorLogAsync("Amal Token Generation Exception From MTS_Integration: <br/> " + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                }

                if (Apitrans_id != "" && Apitrans_id != "0" && proceed_flag != "reprocessed" && proceed_flag == "")
                {

                    var options = new RestClientOptions(apiurl + "/Services/GetTransactionStatus")
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
@"	""clientkey"": """ + clientkey + @""",
" + "\n" +
@"	  ""TransactionNo"": """ + Convert.ToString(Apitrans_id) + @""",
" + "\n" +
@"	""requestId"": ""321""
" + "\n" +
@"}";
                    request.AddParameter("application/json", body, ParameterType.RequestBody);
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
                    string req = apiurl + "/Services/GetTransactionStatus" + body;
                    await SaveActivityLogTracker("Get Amal Transaction status Request From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);
                    var response = client.Execute(request);
                    await SaveActivityLogTracker("Get Amal Transaction status Response From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);
                    json = Newtonsoft.Json.Linq.JObject.Parse(response.Content);
                    string Tr_status = json["response"]["result"]["Transaction"]["TransactionDetails"]["TransactionStatus"].ToString();

                    if (Tr_status == "Fail")
                    {
                        Message = "This Transaction is Failed From Amal Side, Did You Want Processed it Again ?";
                        return new ProceedResponseViewModel
                        {
                            Status = "Failed",
                            StatusCode = 2,
                            Message = Message,
                            ApiId = api_id,
                            AgentRate = AgentRateapi,
                            ApiStatus = apistatus,
                            ExtraFields = new List<string> { "", "" }
                        };
                    }
                }
                else if (proceed_flag == "reprocessed")
                {
                    if (Transactionreference.EndsWith("A"))
                    {
                        Transactionreference = Transactionreference + "B";
                    }
                    else if (Transactionreference.EndsWith("AB"))
                    {
                        Transactionreference = Transactionreference + "C";
                    }
                    else if (Transactionreference.EndsWith("ABC"))
                    {
                        Message = "You Have Reached Your Limit For Reprocessing The Transaction.";
                        return new ProceedResponseViewModel
                        {
                            Status = "Failed",
                            StatusCode = 2,
                            Message = Message,
                            ApiId = api_id,
                            AgentRate = AgentRateapi,
                            ApiStatus = apistatus,
                            ExtraFields = new List<string> { "", "" }
                        };
                    }
                    else
                    {
                        Transactionreference = Transactionreference + "A";
                    }
                }

                storedProcedureName = "GetCountryCodes";
                var values2 = new
                {
                    country_code = result.Country_ID,
                    api_id = api_id,
                };

                var GetCountryCodes = await _dbConnection.QueryAsync(storedProcedureName, values2, commandType: CommandType.StoredProcedure);
                dynamic dt_Basecountry = GetCountryCodes.First();

                Boolean valid = true;



                //getServices(dtt, t);

                storedProcedureName = "GetCountryCodes";
                var values3 = new
                {
                    country_code = result.Beneficiary_Country_ID,
                    api_id = api_id,
                };

                var GetCountryCodes1 = await _dbConnection.QueryAsync(storedProcedureName, values3, commandType: CommandType.StoredProcedure);
                dynamic dt_country = GetCountryCodes1.First();



                // Sender Country API Id get here
                storedProcedureName = "GetCountryCodes";
                var values4 = new
                {
                    country_code = result.Country_ID,
                    api_id = api_id,
                };

                var GetCountryCodes2 = await _dbConnection.QueryAsync(storedProcedureName, values4, commandType: CommandType.StoredProcedure);
                dynamic dt_sendercountry = GetCountryCodes2.First();


                await SaveActivityLogTracker(proceedMethod + "Sender Country Id : <br/>" + result.Country_ID + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);


                try
                {
                    int countryId = Convert.ToInt32(dt_Basecountry.country_code);
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
                            @"  ""username"": """ + Username_api + @""",
                    " + "\n" +
                            @"	""password"": """ + password_api + @""",
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
                    await SaveActivityLogTracker("getCityId Request Request From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                    var response = client.Execute(request);
                    await SaveActivityLogTracker("getCityId Request Response From MTS_Integration: <br/>" + response.Content.Replace("'", "\"") + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                    jsonGetCitiesByCountryId = Newtonsoft.Json.Linq.JObject.Parse(response.Content);

                }
                catch (Exception ex)
                {
                    await SaveErrorLogAsync("Get Amal city Collection Points exception: <br/> " + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                }


                Newtonsoft.Json.Linq.JObject json4sendercity = jsonGetCitiesByCountryId;

                int senderCityID = 0;
                var senderCity = (dynamic)null;
                try
                {
                    senderCity = json4sendercity["response"]["result"]["Cities"]["City"];
                    if (senderCity.GetType().Name == "JArray")
                    {
                        foreach (Newtonsoft.Json.Linq.JObject item in senderCity) // <-- Note that here we used JObject instead of usual JProperty
                        {
                            string senderCityName = item["CityName"].ToString().ToLower().Trim();
                            if (senderCityName == result.City_Name.ToString().ToLower())
                            {
                                senderCityID = Convert.ToInt32(item["CityId"]);
                            }

                        }
                    }
                    else
                    {
                        try
                        {
                            string senderCityName = (senderCity["CityName"]).ToString().ToLower().Trim();
                            if (senderCityName == result.City_Name.ToString().ToLower())
                            {
                                senderCityID = Convert.ToInt32(senderCity["CityId"]);
                            }

                        }
                        catch (Exception ex)
                        {
                            await SaveErrorLogAsync(proceedMethod + "Sender City name error: <br/> " + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                        }
                    }
                }
                catch (Exception ex)
                {
                    await SaveErrorLogAsync(proceedMethod + "Sender City name error2: <br/> " + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                }
                //End Sender City
                await SaveActivityLogTracker(proceedMethod + "Sender City : <br/>" + senderCityID + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                if (senderCityID == 0)
                { senderCityID = 12008; }

                string senderRemark = result.Comment.Trim();
                if (senderRemark == "" || senderRemark == null)
                {
                    senderRemark = result.Purpose.Trim();
                    if (senderRemark == "" || senderRemark == null)
                    {
                        senderRemark = "Sending money for support.";
                    }
                }
                try
                {
                    int countryId = Convert.ToInt32(dt_country.country_code);

                    var options = new RestClientOptions(apiurl + "/Services/getServices")
                    {
                        MaxTimeout = -1
                    };
                    var client = new RestClient(options);
                    var request = new RestRequest()
                    {
                        Method = Method.Post
                    };
                    request.AddHeader("Authorization", "Bearer " + token);
                    request.AddHeader("Content-Type", "application/json");
                    request.AddHeader("username", apiuser);
                    request.AddHeader("secretkey", secretkey);
                    var body = @"{
" + "\n" +
                    @"    ""username"":""" + Username_api + @""",
" + "\n" +
                    @"    ""password"":""" + password_api + @""",
" + "\n" +
                    @"    ""clientkey"":""" + clientkey + @""",
" + "\n" +
                    @"    ""requestId"":""202121819242333"",
" + "\n" +
                    @"    ""countryId"":" + countryId + @"
" + "\n" +
                    @"    
" + "\n" +
                    @"}";
                    request.AddParameter("application/json", body, ParameterType.RequestBody);
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
                    string req = apiurl + "/Services/getServices" + body;
                    await SaveActivityLogTracker("getServices Request From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                    var response = client.Execute(request);
                    await SaveActivityLogTracker("getServices Response: <br/>" + response.Content.Replace("'", "\"") + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);


                    jsongetServices = Newtonsoft.Json.Linq.JObject.Parse(response.Content);
                    var arr1 = (dynamic)null;

                    if (result.PaymentDepositType_ID == 1)
                    {
                        try
                        {

                            arr1 = (jsongetServices["response"]["result"]["Service"]);


                            if (arr1.GetType().Name == "JArray")
                            {
                                foreach (Newtonsoft.Json.Linq.JObject item in arr1) // <-- Note that here we used JObject instead of usual JProperty
                                {
                                    string ServiceName = item["ServiceName"].ToString();
                                    if (ServiceName == "Bank-Transfer" || ServiceName == "Bank Transfer")
                                    {
                                        ServiceId = Convert.ToInt32(item["ServiceId"]);
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                try
                                {
                                    string ServiceName = (arr1["ServiceName"]).ToString();
                                    if (ServiceName == "Bank-Transfer" || ServiceName == "Bank Transfer")
                                    {
                                        ServiceId = Convert.ToInt32(arr1["ServiceId"]);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                        }
                    }
                    else if(result.PaymentDepositType_ID == 2)
                    {
                        try
                        {

                            arr1 = (jsongetServices["response"]["result"]["Service"]);


                            if (arr1.GetType().Name == "JArray")
                            {
                                foreach (Newtonsoft.Json.Linq.JObject item in arr1) // <-- Note that here we used JObject instead of usual JProperty
                                {
                                    string ServiceName = item["ServiceName"].ToString();
                                    if (ServiceName == "Cash Pickup" || ServiceName == "Cash-Pickup")
                                    {
                                        ServiceId = Convert.ToInt32(item["ServiceId"]);
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                try
                                {
                                    string ServiceName = (arr1["ServiceName"]).ToString();
                                    if (ServiceName == "Cash Pickup" || ServiceName == "Cash-Pickup")
                                    {
                                        ServiceId = Convert.ToInt32(arr1["ServiceId"]);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);


                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                        }
                    }
                    else if(result.PaymentDepositType_ID == 3)
                    {
                        try
                        {

                            arr1 = (jsongetServices["response"]["result"]["Service"]);


                            if (arr1.GetType().Name == "JArray")
                            {
                                foreach (Newtonsoft.Json.Linq.JObject item in arr1) // <-- Note that here we used JObject instead of usual JProperty
                                {
                                    string ServiceName = item["ServiceName"].ToString();
                                    if (ServiceName == "Mobile-Wallet" || ServiceName == "Mobile Wallet")
                                    {
                                        ServiceId = Convert.ToInt32(item["ServiceId"]); break;

                                    }
                                }
                            }
                            else
                            {
                                try
                                {
                                    string ServiceName = (arr1["ServiceName"]).ToString();
                                    if (ServiceName == "Mobile-Wallet" || ServiceName == "Mobile Wallet")
                                    {
                                        ServiceId = Convert.ToInt32(arr1["ServiceId"]);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);


                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                        }
                    }
                    //arr1 = (json["response"]["result"]["Service"]);
                }
                catch (Exception ex)
                {
                    await SaveErrorLogAsync("amal Exception  parameters <br/> " + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                }
                try
                {
                    int countryId = Convert.ToInt32(dt_country.country_code);
                    var options = new RestClientOptions(apiurl + "/Services/getServiceOperators")
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
        @"	""username"": """ + Username_api + @""",
" + "\n" +
        @"	""password"": """ + password_api + @""",
" + "\n" +
        @"	""clientkey"": """ + clientkey + @""",
" + "\n" +
        @"	""requestId"": ""1"",
" + "\n" +
        @"	""serviceId"": " + ServiceId.ToString() + @",
" + "\n" +
        @"	""countryId"": " + countryId + @"
" + "\n" +
        @"}";
                    request.AddParameter("application/json", body, ParameterType.RequestBody);
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
                    string req = apiurl + "/Services/getServiceOperators" + body;
                    await SaveActivityLogTracker("getServicesOPeraters Requeste From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                    var response = client.Execute(request);
                    await SaveActivityLogTracker("getServicesOPeraters Response From MTS_Integration: <br/>" + response.Content.Replace("'", "\"") + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                    jsongetServiceOperators = Newtonsoft.Json.Linq.JObject.Parse(response.Content);

                }
                catch (Exception ex)
                {
                    await SaveErrorLogAsync("Get Amal getServiceOperators exception: <br/> " + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                }

                try
                {
                    int countryId = Convert.ToInt32(dt_country.country_code);
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
                            @"  ""username"": """ + Username_api + @""",
                    " + "\n" +
                            @"	""password"": """ + password_api + @""",
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
                    await SaveActivityLogTracker("getCityId Request Request From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                    var response = client.Execute(request);
                    await SaveActivityLogTracker("getCityId Request Response From MTS_Integration: <br/>" + response.Content.Replace("'", "\"") + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                    jsonGetCitiesByCountryId = Newtonsoft.Json.Linq.JObject.Parse(response.Content);

                }
                catch (Exception ex)
                {
                    await SaveErrorLogAsync("Get Amal city Collection Points exception: <br/> " + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                }

                if (result.PaymentDepositType_ID == 1)//Bank Tranfers
                {
                    #region bankprocess

                    if (ServiceId != 0)
                    {
                        //



                        Newtonsoft.Json.Linq.JObject json3 = jsongetServiceOperators;

                        int OperatorId = 0;
                        var arr2 = (dynamic)null;
                        try
                        {
                            arr2 = json3["response"]["result"]["ServiceOperator"];
                            if (arr2.GetType().Name == "JArray")
                            {
                                foreach (Newtonsoft.Json.Linq.JObject item in arr2) // <-- Note that here we used JObject instead of usual JProperty
                                {
                                    string OperatorName = item["OperatorName"].ToString();
                                    if (result.Provider_name != null)
                                    {
                                        if (OperatorName.ToLower().Trim() == Convert.ToString(result.Provider_name).ToLower().Trim())
                                        {
                                            OperatorId = Convert.ToInt32(item["OperatorId"]);
                                            break;
                                        }
                                    }
                                    if (OperatorName.ToLower().Trim().Contains("amal bank"))
                                    {
                                        OperatorId = Convert.ToInt32(item["OperatorId"]);
                                        break;
                                    }
                                    OperatorId = Convert.ToInt32(item["OperatorId"]);
                                }
                            }
                            else
                            {
                                try
                                {
                                    string OperatorName = (arr2["OperatorName"]).ToString();
                                    if (OperatorName.ToLower().Trim() == Convert.ToString(result.Bank_Name).ToLower().Trim())
                                    {
                                        OperatorId = Convert.ToInt32(arr2["OperatorId"]);
                                    }
                                    OperatorId = Convert.ToInt32(arr2["OperatorId"]);
                                }
                                catch (Exception ex)
                                {
                                    await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                        }







                        if (OperatorId != 0)
                        {


                            Newtonsoft.Json.Linq.JObject json4 = jsonGetCitiesByCountryId;
                            int CityId = 0;
                            var arr3 = (dynamic)null;
                            try
                            {
                                arr3 = json4["response"]["result"]["Cities"]["City"];
                                if (arr3.GetType().Name == "JArray")
                                {
                                    foreach (Newtonsoft.Json.Linq.JObject item in arr3) // <-- Note that here we used JObject instead of usual JProperty
                                    {
                                        string CityName = item["CityName"].ToString();
                                        if (CityName.ToLower().Trim() == result.Beneficiary_City.ToString().ToLower().Trim())
                                        {
                                            CityId = Convert.ToInt32(item["CityId"]);

                                        }
                                    }
                                }
                                else
                                {
                                    try
                                    {
                                        string CityName = (arr3["CityName"]).ToString();
                                        if (CityName.ToLower().Trim() == result.City_Name.ToString().ToLower().Trim())
                                        {
                                            CityId = Convert.ToInt32(arr3["CityId"]);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                            }



                            if (CityId != 0)
                            {
                                //
                                string bname = Convert.ToString(result.Beneficiary_Name); string bfname = ""; string bmname = ""; string blname = "";

                                var n = bname.Split(' ');
                                if (n.Length == 1)
                                {
                                    bfname = n[0];
                                    bmname = "";
                                    blname = "";
                                }
                                else if (n.Length == 2)
                                {
                                    bfname = n[0];
                                    bmname = "";
                                    blname = n[1];
                                }
                                else if (n.Length == 3)
                                {
                                    bfname = n[0];
                                    bmname = n[1];
                                    blname = n[2];
                                }
                                else if (n.Length > 3)
                                {
                                    var lastname = "";
                                    for (var i = 0; i < n.Length; i++)
                                    {
                                        if (i == 0) { bfname = n[i]; }
                                        if (i == 1) { bmname = n[i]; }
                                        if (i > 1) { lastname = lastname + " " + n[i]; }
                                    }
                                    blname = lastname.Trim();
                                }
                                else
                                {
                                    bfname = bname;
                                    bmname = "";
                                    blname = "";
                                }
                                Newtonsoft.Json.Linq.JObject json5 = new Newtonsoft.Json.Linq.JObject();

                                try
                                {
                                    string destinationCountryId = Convert.ToString(dt_country?.country_code);
                                    string amountToSend = Convert.ToString(result?.AmountInGBP);
                                    string sourceCurrency = Convert.ToString(result?.FromCurrency_Code);
                                    string destinationCurrency = Convert.ToString(result?.Currency_Code);

                                    var options = new RestClientOptions(apiurl + "/Services/GetCommissionCharges")
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
                        @"  ""sourceBranchkey"": """ + SourceBranchkey_api + @""",
" + "\n" +
                        @" ""amountToSend"": """ + amountToSend + @""",
" + "\n" +
                        @"	""serviceId"": """ + ServiceId.ToString() + @""",
" + "\n" +
                        @"	""serviceOperatorId"": """ + OperatorId.ToString() + @""",
" + "\n" +
                        @"	""destinationCountryId"": """ + destinationCountryId + @""",
" + "\n" +
                        @"	""sourceCurrency"": """ + sourceCurrency + @""",
" + "\n" +
                        @"	""destinationCurrency"": """ + destinationCurrency + @""",
" + "\n" +
                        @"	""requestId"": ""0123456987"",
" + "\n" +
                        @"	""destinationCityId"": " + CityId + @"
" + "\n" +
                        @"}";
                                    request.AddParameter("application/json", body, ParameterType.RequestBody);
                                    ServicePointManager.Expect100Continue = true;
                                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                                    System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
                                    string req = apiurl + "/Services/GetCommissionCharges" + body;
                                    await SaveActivityLogTracker("GetCommissionCharges Request From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                                    var response = client.Execute(request);
                                    await SaveActivityLogTracker("GetCommissionCharges Response From MTS_Integration: <br/>" + response.Content.Replace("'", "\"") + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                                    json5 = Newtonsoft.Json.Linq.JObject.Parse(response.Content);
                                    var arr4 = (dynamic)null;
                                    string commisionCharges = "";
                                    string servicetax = "";
                                    string AmountToReceive = "";
                                    string MaxCommissionAmountInUSD = "", MinCommissionAmountInUSD = "";

                                    try
                                    {
                                        try
                                        {
                                            if ((json5["response"]["STATUS"]).ToString() == "Success")
                                            {
                                                if ((json5["response"]["result"]).ToString() != "null") { }
                                                try
                                                {
                                                    arr4 = json5["response"]["result"]["CommissionSlab"]["CommissionCharge"];
                                                    commisionCharges = arr4["CommissionChargesInSourceCurrency"];
                                                    servicetax = arr4["ServiceChargesInUSD"];
                                                    AmountToReceive = arr4["AmountToReceive"];
                                                    double base_cal_amt = Convert.ToDouble(result.AmountInPKR);
                                                    base_cal_amt = base_cal_amt / Convert.ToDouble(arr4["SourceToDestinationCurrencyExchangeRate"]);
                                                    MaxCommissionAmountInUSD = arr4["MaxCommissionAmountInUSD"];
                                                    MinCommissionAmountInUSD = arr4["MinCommissionAmountInUSD"];
                                                    try
                                                    {

                                                        options = new RestClientOptions(apiurl + "/Services/GetCommissionCharges")
                                                        {
                                                            MaxTimeout = -1
                                                        };
                                                        client = new RestClient(options);
                                                        request = new RestRequest()
                                                        {
                                                            Method = Method.Post
                                                        };
                                                        request.AddHeader("Authorization", "Bearer " + token);
                                                        request.AddHeader("Content-Type", "application/json");
                                                        request.AddHeader("username", apiuser);
                                                        request.AddHeader("secretkey", secretkey);
                                                        body = @"{
" + "\n" +
                                            @"  ""username"": """ + apiuser + @""",
" + "\n" +
                                            @"	""password"": """ + apipass + @""",
" + "\n" +
                                            @"	""clientkey"": """ + clientkey + @""",
" + "\n" +
                                            @"  ""sourceBranchkey"": """ + SourceBranchkey_api + @""",
" + "\n" +
                                            @" ""amountToSend"": """ + base_cal_amt.ToString() + @""",
" + "\n" +
                                            @"	""serviceId"": """ + ServiceId.ToString() + @""",
" + "\n" +
                                            @"	""serviceOperatorId"": """ + OperatorId.ToString() + @""",
" + "\n" +
                                            @"	""destinationCountryId"": """ + destinationCountryId + @""",
" + "\n" +
                                            @"	""sourceCurrency"": """ + sourceCurrency + @""",
" + "\n" +
                                            @"	""destinationCurrency"": """ + destinationCurrency + @""",
" + "\n" +
                                            @"	""requestId"": ""0123456987"",
" + "\n" +
                                            @"	""destinationCityId"": " + CityId + @"" + "\n" +
                                            @"}";
                                                        request.AddParameter("application/json", body, ParameterType.RequestBody);
                                                        ServicePointManager.Expect100Continue = true;
                                                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                                                        System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
                                                        req = apiurl + "/Services/GetCommissionCharges" + body;
                                                        await SaveActivityLogTracker("GetCommissionCharges2 Request From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);
                                                        response = client.Execute(request);
                                                        await SaveActivityLogTracker("GetCommissionCharges2 Response From MTS_Integration: <br/>" + response.Content.Replace("'", "\"") + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);
                                                        json5 = Newtonsoft.Json.Linq.JObject.Parse(response.Content);
                                                        arr4 = (dynamic)null;
                                                        //
                                                        if ((json5["response"]["STATUS"]).ToString() == "Success")
                                                        {
                                                            arr4 = json5["response"]["result"]["CommissionSlab"]["CommissionCharge"];
                                                            commisionCharges = arr4["CommissionChargesInSourceCurrency"];
                                                            servicetax = arr4["ServiceChargesInUSD"];
                                                            AmountToReceive = arr4["AmountToReceive"];
                                                            MaxCommissionAmountInUSD = arr4["MaxCommissionAmountInUSD"];
                                                            MinCommissionAmountInUSD = arr4["MinCommissionAmountInUSD"];
                                                            await SaveActivityLogTracker(proceedMethod + "The Amount Sent to amal is " + base_cal_amt.ToString() + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        await SaveActivityLogTracker(" GetCommissionCharges2 Error" + ex.ToString() + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    await SaveErrorLogAsync("GetCommissionCharges Error exception: <br/> " + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                                }
                                            }
                                            else
                                            {
                                                Message = " Description : " + json5["response"]["result"]["message"].ToString();
                                                return new ProceedResponseViewModel
                                                {
                                                    Status = "Failed",
                                                    StatusCode = 2,
                                                    Message = Message,
                                                    ApiId = api_id,
                                                    AgentRate = AgentRateapi,
                                                    ApiStatus = apistatus,
                                                    ExtraFields = new List<string> { "", "" }
                                                };
                                            }
                                            if (commisionCharges != "")
                                            {
                                                if (Convert.ToDouble(commisionCharges) < Convert.ToDouble(MinCommissionAmountInUSD))
                                                {
                                                    commisionCharges = MinCommissionAmountInUSD;
                                                }
                                                else if (Convert.ToDouble(commisionCharges) > Convert.ToDouble(MaxCommissionAmountInUSD))
                                                {
                                                    commisionCharges = MaxCommissionAmountInUSD;
                                                }
                                                else
                                                {
                                                    commisionCharges = result.Transfer_Fees.ToString();
                                                }

                                                try
                                                {
                                                    try
                                                    {
                                                        if (string.IsNullOrEmpty(servicetax.Trim()))
                                                        {
                                                            servicetax = "0.0000";
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        servicetax = "0.0000";
                                                    }

                                                    if (result.ID_Name.ToString() == "Passport")
                                                    {
                                                        result.ID_Name = "P";
                                                    }
                                                    else if (result.ID_Name.ToString() == "Driving License" || result.ID_Name.ToString() == "UK Driving License")
                                                    {
                                                        result.ID_Name = "D";
                                                    }
                                                    else if (result.ID_Name.ToString() == "EU Nationality Card" || result.ID_Name.ToString() == "Identification ID")
                                                    {
                                                        result.ID_Name = "NID";
                                                    }
                                                    else
                                                    {
                                                        result.ID_Name = "NID";
                                                    }
                                                    string Gender = result.Gender.ToString().Trim();
                                                    string genders = Gender;
                                                    if (Gender == "Male")
                                                    {
                                                        Gender = "M";
                                                    }
                                                    else if (Gender == "Female")
                                                    {
                                                        Gender = "F";
                                                    }
                                                    else
                                                    {
                                                        Gender = "O";
                                                    }
                                                    //
                                                    Newtonsoft.Json.Linq.JObject json6 = new Newtonsoft.Json.Linq.JObject();
                                                    options = new RestClientOptions(apiurl + "/Services/GenerateBankTransaction")
                                                    {
                                                        MaxTimeout = -1
                                                    };
                                                    client = new RestClient(options);
                                                    request = new RestRequest()
                                                    {
                                                        Method = Method.Post
                                                    };
                                                    request.AddHeader("Authorization", "Bearer " + token);
                                                    request.AddHeader("username", apiuser);
                                                    request.AddHeader("secretkey", secretkey);
                                                    string sourceamt = Convert.ToString(arr4["SourceAmount"]);
                                                    request.AddHeader("Content-Type", "application/json");
                                                    body = @"{
" + "\n" +
                            @"	""username"": """ + apiuser + @""",
" + "\n" +
                            @"	""password"": """ + apipass + @""",
" + "\n" +
                            @"	""clientkey"": """ + clientkey + @""",
" + "\n" +
                            @"	""sourceBranchkey"": """ + SourceBranchkey_api + @""",
" + "\n" +
                            @"	""sourceCountry"": """ + Convert.ToString(dt_Basecountry.country_code) + @""",
" + "\n" +
                            @"	""senderFName"": """ + Convert.ToString(result.First_Name) + @""",
" + "\n" +
                            @"	""senderMName"": """ + Convert.ToString(result.Middle_Name) + @""",
" + "\n" +
                            @"	""senderLName"": """ + Convert.ToString(result.Last_Name) + @""",
" + "\n" +
                            @"	""senderMobileCountryCode"": """ + Convert.ToString(result.BCountry_Code) + @""",
" + "\n" +
                            @"	""senderMobile"": """ + Convert.ToString(result.Mobile_Number) + @""",
" + "\n" +
                            @"	""senderTelephone"": """ + Convert.ToString(result.Phone_Number) + @""",
" + "\n" +
                            @"	""senderAddress"": """ + Convert.ToString(result.sender_address) + @""",
" + "\n" +
                            @"	""serviceId"": """ + ServiceId + @""",
" + "\n" +
                            @"	""serviceOperatorId"": """ + OperatorId + @""",
" + "\n" +
                            @"	""destinationCountryId"": """ + Convert.ToString(dt_country.country_code) + @""",
" + "\n" +
                            @"	""sendercity"": """ + senderCityID + @""",
" + "\n" +
                            @"	""senderpostalcode"": """ + Convert.ToString(result.Post_Code) + @""",
" + "\n" +
                            @"	""sendernationality"": """ + Convert.ToString(result.Nationality_Country) + @""",
" + "\n" +
                            @"	""SenderEmail"": """ + Convert.ToString(result.Email_ID) + @""",
" + "\n" +
                            @"	""sendergender"": """ + Gender + @""",
" + "\n" +
                            @"	""senderDOB"": """ + Convert.ToString(result.Sender_DOBmdy) + @""",
" + "\n" +
                            @"	""SenderOccupation"": """ + Convert.ToString(result.Profession) + @""",
" + "\n" +
                            @"	""destinationCityId"": """ + CityId + @""",
" + "\n" +
                            @"	""beneficiaryFirstName"": """ + bfname + @""",
" + "\n" +
                            @"	""beneficiaryMiddleName"": """ + bmname + @""",
" + "\n" +
                            @"	""beneficiaryLastName"": """ + blname + @""",
" + "\n" +
                            @"	""beneficiaryMobileCountryCode"": """ + Convert.ToString(result.BCountry_Code) + @""",
" + "\n" +
                            @"	""beneficiaryMobile"": """ + (Convert.ToString(result.Beneficiary_Mobile)).Substring(result.BCountry_Code.ToString().Length) + @""",


" + "\n" +
                            @"	""beneficiaryTelephone"": """",
" + "\n" +
                            @"	""beneficiaryAddress"": """ + Convert.ToString(result.Beneficiary_Address) + @""",
" + "\n" +
                            @"	""totalAmount"": """ + sourceamt + @""",
" + "\n" +
                            @"	""totalCommission"": """ + commisionCharges + @""",
" + "\n" +
                            @"	""serviceTax"": """ + servicetax + @""",
" + "\n" +
                            @"	""sendingCurrency"": """ + Convert.ToString(result.FromCurrency_Code) + @""",
" + "\n" +
                            @"	""payoutMethod"": ""banktransfer"",
" + "\n" +
                            @"	""currencyToReceive"": """ + Convert.ToString(result.Currency_Code) + @""",
" + "\n" +
                            @"	""amountToReceive"": """ + AmountToReceive + @""",
" + "\n" +
                            @"	""purpose"": """ + Convert.ToString(result.Purpose) + @""",
" + "\n" +
                            @"	""sourceofIncome"": ""Individual"",
" + "\n" +
                            @"	""senderRemarks"": """ + senderRemark + @""",
" + "\n" +
                            @"	""transactionDate"": """ + Convert.ToString(result.current_date1) + @""",
" + "\n" +
                            @"	""userCreated"": """ + Convert.ToString(result.First_Name)+ @""",
" + "\n" +
                            @"	""transactionReferenceNo"": """ + Convert.ToString(Transactionreference) + @""",
" + "\n" +
                            @"	""collectionMode"": ""Cash"",
" + "\n" +
                            @"	""msisdn"": ""1"",
" + "\n" +
                            @"	""requestId"": ""012345698745"",
" + "\n" +
                            @"	""sendernationality"": """ + Convert.ToString(result.Nationality_Country) + @""",   
" + "\n" +
                            @"	""documentName"": """ + Convert.ToString(result.ID_Name) + @""", 
" + "\n" +
                            @"	""documentNumber"": """ + Convert.ToString(result.SenderID_Number) + @""",  
" + "\n" +
                            @"	""Issuer"": """ + Convert.ToString(result.Country_Name) + @""", 
" + "\n" +
                            @"	""dateofIssue"": """ + Convert.ToString(result.Issue_Datemdy) + @""",  
" + "\n" +
                            @"	""dateofExpire"": """ + Convert.ToString(result.SenderID_ExpiryDatemdy) + @""",   
" + "\n" +
                            @"	""documentFront"": """",
" + "\n" +
                            @"	""documentBack"": """",
" + "\n" +
                            @" ""Bankname"": """ + Convert.ToString(result.Bank_Name) + @""",
" + "\n" +
                            @"  ""beneficiaryBankAccNo"": """ + Convert.ToString(result.Account_Number) + @""" 
" + "\n" +
                            @"}";



                                                    request.AddParameter("application/json", body, ParameterType.RequestBody);
                                                    ServicePointManager.Expect100Continue = true;
                                                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                                                    System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
                                                    req = apiurl + "/Services/GenerateBankTransaction" + body;
                                                    await SaveActivityLogTracker("GenerateBankTransaction Request From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);
                                                    response = client.Execute(request);
                                                    await SaveActivityLogTracker("GenerateBankTransaction Response From MTS_Integration: <br/>" + response.Content.Replace("'", "\"") + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);
                                                    json6 = Newtonsoft.Json.Linq.JObject.Parse(response.Content);
                                                    var arr5 = json6["response"]["ResponseCode"];
                                                    Message = " Description : " + json6["response"]["result"]["message"].ToString();



                                                    if (arr5.ToString() == "M400")
                                                    {
                                                        //
                                                        string APIBranch_Details = "";
                                                        APIBranch_Details = Convert.ToString(entity.payerId_datafull);

                                                        apistatus = 0;
                                                        string trn_referenceNo = json6["response"]["result"]["TransactionNo"].ToString();
                                                        int? BranchListAPI_ID = api_id; APIBranch_Details = entity.payerId_datafull;
                                                        int mappingid = Convert.ToInt32(result.TransMap_ID);
                                                        if (mappingid > 0)
                                                        {
                                                            try
                                                            {
                                                                AgentRateapi = 0;
                                                            }
                                                            catch { }
                                                            try
                                                            {
                                                                var parameters2 = new
                                                                {
                                                                    _ReferenceNo = Transactionreference,
                                                                    _Transaction_ID = entity.Transaction_ID
                                                                };

                                                                var rowsAffected2 = await _dbConnection.ExecuteAsync("Update_reprocessed_Transaction_ref", parameters2, commandType: CommandType.StoredProcedure);





                                                                string refer = Convert.ToString(result.ReferenceNo);
                                                                await SaveActivityLogTracker("Checking All values for sp  : <br/>" + api_id + " " + refer + " " + refer + " " + APIBranch_Details, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Amal update transactiom mapping values checking ", entity.Branch_ID, Client_ID);

                                                                if (mappingid > 0)
                                                                {
                                                                    try
                                                                    {
                                                                        AgentRateapi = 0;
                                                                    }
                                                                    catch { }
                                                                    try
                                                                    {
                                                                        var parameters = new
                                                                        {
                                                                            _BranchListAPI_ID = api_id,
                                                                            _APIBranch_Details = entity.APIBranch_Details,
                                                                            _TransactionRef = Transactionreference,
                                                                            _trn_referenceNo = trn_referenceNo,
                                                                            _APITransaction_Alert = 0,
                                                                            _Transaction_ID = entity.Transaction_ID,
                                                                            _Client_ID = entity.Client_ID,
                                                                            _payout_partner_rate = AgentRateapi,
                                                                        };

                                                                        var rowsAffected = await _dbConnection.ExecuteAsync("Update_TransactionDetails", parameters, commandType: CommandType.StoredProcedure);



                                                                    }
                                                                    catch (Exception ex)
                                                                    {
                                                                        await SaveErrorLogAsync("Update_TransactionDetails SP exception: <br/> " + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                                                    }
                                                                }


                                                            }
                                                            catch (Exception ex) { }

                                                        }


                                                    }
                                                    else
                                                    {
                                                        Message = " Description : " + json6["response"]["result"]["message"].ToString();
                                                        return new ProceedResponseViewModel
                                                        {
                                                            Status = "Failed",
                                                            StatusCode = 2,
                                                            Message = Message,
                                                            ApiId = api_id,
                                                            AgentRate = AgentRateapi,
                                                            ApiStatus = apistatus,
                                                            ExtraFields = new List<string> { "", "" }
                                                        };
                                                    }
                                                    //
                                                }
                                                catch (Exception ex)
                                                {
                                                    await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);


                                                    Message = " Description : GenerateBankTransaction error";
                                                    return new ProceedResponseViewModel
                                                    {
                                                        Status = "Failed",
                                                        StatusCode = 2,
                                                        Message = Message,
                                                        ApiId = api_id,
                                                        AgentRate = AgentRateapi,
                                                        ApiStatus = apistatus,
                                                        ExtraFields = new List<string> { "", "" }
                                                    };



                                                }
                                            }
                                            else
                                            {
                                                Message = " Description : Commission charges is not defined.";
                                                return new ProceedResponseViewModel
                                                {
                                                    Status = "Failed",
                                                    StatusCode = 2,
                                                    Message = Message,
                                                    ApiId = api_id,
                                                    AgentRate = AgentRateapi,
                                                    ApiStatus = apistatus,
                                                    ExtraFields = new List<string> { "", "" }
                                                };
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                        }


                                    }
                                    catch (Exception ex)
                                    {
                                        await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                }

                            }
                            else
                            {
                                Message = " Description : Service Provider is not available for this city.";
                                return new ProceedResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = Message,
                                    ApiId = api_id,
                                    AgentRate = AgentRateapi,
                                    ApiStatus = apistatus,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }

                        }
                        else
                        {

                            Message = " Description : Bank is not available for this country.";
                            return new ProceedResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = Message,
                                ApiId = api_id,
                                AgentRate = AgentRateapi,
                                ApiStatus = apistatus,
                                ExtraFields = new List<string> { "", "" }
                            };
                        }


                    }
                    else
                    {
                        Message = " Description : Service Provider is not available for this country.";
                        return new ProceedResponseViewModel
                        {
                            Status = "Failed",
                            StatusCode = 2,
                            Message = Message,
                            ApiId = api_id,
                            AgentRate = AgentRateapi,
                            ApiStatus = apistatus,
                            ExtraFields = new List<string> { "", "" }
                        };
                    }
                    #endregion
                }
                else if (result.PaymentDepositType_ID == 2)//cash pickup
                {
                    #region amalcash_process


                    Newtonsoft.Json.Linq.JObject json2 = jsongetServices;
                    
                    var arr1 = (dynamic)null;

                    try
                    {

                        arr1 = (json2["response"]["result"]["Service"]);


                        if (arr1.GetType().Name == "JArray")
                        {
                            foreach (Newtonsoft.Json.Linq.JObject item in arr1) // <-- Note that here we used JObject instead of usual JProperty
                            {
                                string ServiceName = item["ServiceName"].ToString();
                                if (ServiceName == "Cash Pickup" || ServiceName == "Cash-Pickup")
                                {
                                    ServiceId = Convert.ToInt32(item["ServiceId"]);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            try
                            {
                                string ServiceName = (arr1["ServiceName"]).ToString();
                                if (ServiceName == "Cash Pickup" || ServiceName == "Cash-Pickup")
                                {
                                    ServiceId = Convert.ToInt32(arr1["ServiceId"]);
                                }
                            }
                            catch (Exception ex)
                            {
                                await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                    }


                    if (ServiceId != 0)
                    {
                        //
                        Newtonsoft.Json.Linq.JObject json3 = jsongetServiceOperators;


                        int OperatorId = 0;
                        var arr2 = (dynamic)null;
                        try
                        {
                            arr2 = json3["response"]["result"]["ServiceOperator"];
                            if (arr2.GetType().Name == "JArray")
                            {
                                foreach (Newtonsoft.Json.Linq.JObject item in arr2) // <-- Note that here we used JObject instead of usual JProperty
                                {
                                    string OperatorName = item["OperatorName"].ToString();
                                    if (OperatorName == "Amal Express")
                                    {
                                        OperatorId = Convert.ToInt32(item["OperatorId"]);
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                try
                                {
                                    string OperatorName = (arr2["OperatorName"]).ToString();
                                    if (OperatorName == "Amal Express")
                                    {
                                        OperatorId = Convert.ToInt32(arr2["OperatorId"]);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                        }


                        if (OperatorId != 0)
                        {
                            Newtonsoft.Json.Linq.JObject json4 = jsonGetCitiesByCountryId;
                            int CityId = 0;
                            var arr3 = (dynamic)null;
                            try
                            {

                                string payerIdValue = Convert.ToString(entity.APIBranch_Details);


                                string[] words = payerIdValue.Split('-');
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
                                if (digitsOnly)
                                {
                                    CityId = Convert.ToInt32(cityCodes);
                                }
                            }
                            catch (Exception ex)
                            {
                                await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                            }





                            if (CityId != 0)
                            {
                                //
                                string bname = Convert.ToString(result.Beneficiary_Name); string bfname = ""; string bmname = ""; string blname = "";

                                var n = bname.Split(' ');
                                if (n.Length == 1)
                                {
                                    bfname = n[0];
                                    bmname = "";
                                    blname = "";
                                }
                                else if (n.Length == 2)
                                {
                                    bfname = n[0];
                                    bmname = "";
                                    blname = n[1];
                                }
                                else if (n.Length == 3)
                                {
                                    bfname = n[0];
                                    bmname = n[1];
                                    blname = n[2];
                                }
                                else if (n.Length > 3)
                                {
                                    var lastname = "";
                                    for (var i = 0; i < n.Length; i++)
                                    {
                                        if (i == 0) { bfname = n[i]; }
                                        if (i == 1) { bmname = n[i]; }
                                        if (i > 1) { lastname = lastname + " " + n[i]; }
                                    }
                                    blname = lastname.Trim();
                                }
                                else
                                {
                                    bfname = bname;
                                    bmname = "";
                                    blname = "";
                                }
                                Newtonsoft.Json.Linq.JObject json5 = new Newtonsoft.Json.Linq.JObject();

                                try
                                {
                                    string destinationCountryId = Convert.ToString(dt_country?.country_code);
                                    string amountToSend = Convert.ToString(result?.AmountInGBP);
                                    string sourceCurrency = Convert.ToString(result?.FromCurrency_Code);
                                    string destinationCurrency = Convert.ToString(result?.Currency_Code);

                                    var options = new RestClientOptions(apiurl + "/Services/GetCommissionCharges")
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
                        @"  ""sourceBranchkey"": """ + SourceBranchkey_api + @""",
" + "\n" +
                        @" ""amountToSend"": """ + amountToSend + @""",
" + "\n" +
                        @"	""serviceId"": """ + ServiceId.ToString() + @""",
" + "\n" +
                        @"	""serviceOperatorId"": """ + OperatorId.ToString() + @""",
" + "\n" +
                        @"	""destinationCountryId"": """ + destinationCountryId + @""",
" + "\n" +
                        @"	""sourceCurrency"": """ + sourceCurrency + @""",
" + "\n" +
                        @"	""destinationCurrency"": """ + destinationCurrency + @""",
" + "\n" +
                        @"	""requestId"": ""0123456987"",
" + "\n" +
                        @"	""destinationCityId"": " + CityId + @"
" + "\n" +
                        @"}";
                                    request.AddParameter("application/json", body, ParameterType.RequestBody);
                                    ServicePointManager.Expect100Continue = true;
                                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                                    System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
                                    string req = apiurl + "/Services/GetCommissionCharges" + body;
                                    await SaveActivityLogTracker(proceedMethod + "GetCommissionCharges Request From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                                    var response = client.Execute(request);
                                    await SaveActivityLogTracker(proceedMethod + "GetCommissionChargesResponse From MTS_Integration: <br/>" + response.Content.Replace("'", "\"") + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                                    json5 = Newtonsoft.Json.Linq.JObject.Parse(response.Content);
                                    var arr4 = (dynamic)null;
                                    string commisionCharges = "";
                                    string servicetax = "";
                                    string AmountToReceive = "";
                                    string MaxCommissionAmountInUSD = "", MinCommissionAmountInUSD = "";
                                    try
                                    {
                                        try
                                        {
                                            double base_cal_amt = 0.00;
                                            if ((json5["response"]["STATUS"]).ToString() == "Success")
                                            {
                                                if ((json5["response"]["result"]).ToString() != "null") { }
                                                try
                                                {
                                                    arr4 = json5["response"]["result"]["CommissionSlab"]["CommissionCharge"];
                                                    //commisionCharges = arr4["CommissionChargesInSourceCurrency"];
                                                    //servicetax = arr4["ServiceChargesInUSD"];
                                                    //AmountToReceive = arr4["AmountToReceive"];
                                                    base_cal_amt = Convert.ToDouble(result.AmountInPKR);
                                                    base_cal_amt = base_cal_amt / Convert.ToDouble(arr4["SourceToDestinationCurrencyExchangeRate"]);
                                                    try
                                                    {

                                                        options = new RestClientOptions(apiurl + "/Services/GetCommissionCharges")
                                                        {
                                                            MaxTimeout = -1
                                                        };
                                                        client = new RestClient(options);
                                                        request = new RestRequest()
                                                        {
                                                            Method = Method.Post
                                                        };
                                                        request.AddHeader("Authorization", "Bearer " + token);
                                                        request.AddHeader("username", apiuser);
                                                        request.AddHeader("secretkey", secretkey);
                                                        request.AddHeader("Content-Type", "application/json");
                                                        body = @"{
" + "\n" +
                                            @"  ""username"": """ + apiuser + @""",
" + "\n" +
                                            @"	""password"": """ + apipass + @""",
" + "\n" +
                                            @"	""clientkey"": """ + clientkey + @""",
" + "\n" +
                                            @"  ""sourceBranchkey"": """ + SourceBranchkey_api + @""",
" + "\n" +
                                            @" ""amountToSend"": """ + base_cal_amt.ToString() + @""",
" + "\n" +
                                            @"	""serviceId"": """ + ServiceId.ToString() + @""",
" + "\n" +
                                            @"	""serviceOperatorId"": """ + OperatorId.ToString() + @""",
" + "\n" +
                                            @"	""destinationCountryId"": """ + destinationCountryId + @""",
" + "\n" +
                                            @"	""sourceCurrency"": """ + sourceCurrency + @""",
" + "\n" +
                                            @"	""destinationCurrency"": """ + destinationCurrency + @""",
" + "\n" +
                                            @"	""requestId"": ""0123456987"",
" + "\n" +
                                            @"	""destinationCityId"": " + CityId + @"
" + "\n" +
                                            @"}";
                                                        request.AddParameter("application/json", body, ParameterType.RequestBody);
                                                        ServicePointManager.Expect100Continue = true;
                                                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                                                        System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
                                                        req = apiurl + "/Services/GetCommissionCharges" + body;
                                                        await SaveActivityLogTracker(proceedMethod + "GetCommissionCharges2 Request From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                                                        response = client.Execute(request);
                                                        await SaveActivityLogTracker(proceedMethod + "GetCommissionCharges2 Response From MTS_Integration: <br/>" + response.Content.Replace("'", "\"") + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);
                                                        json5 = Newtonsoft.Json.Linq.JObject.Parse(response.Content);
                                                        arr4 = (dynamic)null;
                                                        //
                                                        if ((json5["response"]["STATUS"]).ToString() == "Success")
                                                        {
                                                            arr4 = json5["response"]["result"]["CommissionSlab"]["CommissionCharge"];
                                                            commisionCharges = arr4["CommissionChargesInSourceCurrency"];
                                                            servicetax = arr4["ServiceChargesInUSD"];
                                                            AmountToReceive = arr4["AmountToReceive"];
                                                            MaxCommissionAmountInUSD = arr4["MaxCommissionAmountInUSD"];
                                                            MinCommissionAmountInUSD = arr4["MinCommissionAmountInUSD"];
                                                            await SaveActivityLogTracker(proceedMethod + "The Amount Sent to amal is " + base_cal_amt.ToString() + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {

                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                                }
                                            }
                                            else
                                            {
                                                Message = "Description : " + json5["response"]["result"]["message"].ToString();
                                                return new ProceedResponseViewModel
                                                {
                                                    Status = "Failed",
                                                    StatusCode = 2,
                                                    Message = Message,
                                                    ApiId = api_id,
                                                    AgentRate = AgentRateapi,
                                                    ApiStatus = apistatus,
                                                    ExtraFields = new List<string> { "", "" }
                                                };

                                            }
                                            if (commisionCharges != "")
                                            {
                                                if (Convert.ToDouble(commisionCharges) < Convert.ToDouble(MinCommissionAmountInUSD))
                                                {
                                                    commisionCharges = MinCommissionAmountInUSD;
                                                }
                                                else if (Convert.ToDouble(commisionCharges) > Convert.ToDouble(MaxCommissionAmountInUSD))
                                                {
                                                    commisionCharges = MaxCommissionAmountInUSD;
                                                }
                                                else
                                                {
                                                    commisionCharges = result.Transfer_Fees.ToString();
                                                }

                                                try
                                                {
                                                    try
                                                    {
                                                        if (string.IsNullOrEmpty(servicetax.Trim()))
                                                        {
                                                            servicetax = "0.0000";
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        servicetax = "0.0000";
                                                    }

                                                    if (result.ID_Name.ToString() == "Passport")
                                                    {
                                                        result.ID_Name = "P";
                                                    }
                                                    else if (result.ID_Name.ToString() == "Driving License" || result.ID_Name.ToString() == "UK Driving License")
                                                    {
                                                        result.ID_Name = "D";
                                                    }
                                                    else if (result.ID_Name.ToString() == "EU Nationality Card" || result.ID_Name.ToString() == "Identification ID")
                                                    {
                                                        result.ID_Name = "NID";
                                                    }
                                                    else
                                                    {
                                                        result.ID_Name = "NID";
                                                    }
                                                    string Gender = result.Gender.ToString().Trim();
                                                    string genders = Gender;
                                                    if (Gender == "Male")
                                                    {
                                                        Gender = "M";
                                                    }
                                                    else if (Gender == "Female")
                                                    {
                                                        Gender = "F";
                                                    }
                                                    else
                                                    {
                                                        Gender = "O";
                                                    }
                                                    //
                                                    Newtonsoft.Json.Linq.JObject json6 = new Newtonsoft.Json.Linq.JObject();
                                                    options = new RestClientOptions(apiurl + "/Services/GenerateCashPickupTransaction")
                                                    {
                                                        MaxTimeout = -1
                                                    };
                                                    client = new RestClient(options);
                                                    request = new RestRequest()
                                                    {
                                                        Method = Method.Post
                                                    };


                                                    request.AddHeader("Authorization", "Bearer " + token);
                                                    request.AddHeader("Content-Type", "application/json");
                                                    request.AddHeader("username", apiuser);
                                                    request.AddHeader("secretkey", secretkey);
                                                    string sourceamt = Convert.ToString(arr4["SourceAmount"]);
                                                    await SaveActivityLogTracker(proceedMethod + " Document: " + base_cal_amt.ToString() + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);
                                                    body = @"{
" + "\n" +
                            @"	""username"": """ + apiuser + @""",
" + "\n" +
                            @"	""password"": """ + apipass + @""",
" + "\n" +
                            @"	""clientkey"": """ + clientkey + @""",
" + "\n" +
                            @"	""sourceBranchkey"": """ + SourceBranchkey_api + @""",
" + "\n" +
                            @"	""sourceCountry"": """ + Convert.ToString(dt_Basecountry.country_code) + @""",
" + "\n" +
                            @"	""senderFName"": """ + Convert.ToString(result.First_Name) + @""",
" + "\n" +
                            @"	""senderMName"": """ + Convert.ToString(result.Middle_Name) + @""",
" + "\n" +
                            @"	""senderLName"": """ + Convert.ToString(result.Last_Name) + @""",
" + "\n" +
                            @"	""senderMobileCountryCode"": """ + Convert.ToString(result.BCountry_Code) + @""",
" + "\n" +
                            @"	""senderMobile"": """ + Convert.ToString(result.Mobile_Number) + @""",
" + "\n" +
                            @"	""senderTelephone"": """ + Convert.ToString(result.Phone_Number) + @""",
" + "\n" +
                            @"	""senderAddress"": """ + Convert.ToString(result.sender_address) + @""",
" + "\n" +
                            @"	""serviceId"": """ + ServiceId + @""",
" + "\n" +
                            @"	""serviceOperatorId"": """ + OperatorId + @""",
" + "\n" +
                            @"	""destinationCountryId"": """ + Convert.ToString(dt_country.country_code) + @""",
" + "\n" +
                            @"	""sendercity"": """ + senderCityID + @""",
" + "\n" +
                            @"	""senderpostalcode"": """ + Convert.ToString(result.Post_Code) + @""",
" + "\n" +
                            @"	""sendernationality"": """ + Convert.ToString(result.Nationality_Country) + @""",
" + "\n" +
                            @"	""SenderEmail"": """ + Convert.ToString(result.Email_ID) + @""",
" + "\n" +
                            @"	""sendergender"": """ + Gender + @""",
" + "\n" +
                            @"	""SenderDOB"": """ + Convert.ToString(result.Sender_DOBmdy) + @""",
" + "\n" +
                            @"	""SenderOccupation"": """ + Convert.ToString(result.Profession) + @""",                    
" + "\n" +
                            @"	""destinationCityId"": """ + CityId + @""",
" + "\n" +
                            @"	""beneficiaryFirstName"": """ + bfname + @""",
" + "\n" +
                            @"	""beneficiaryMiddleName"": """ + bmname + @""",
" + "\n" +
                            @"	""beneficiaryLastName"": """ + blname + @""",
" + "\n" +
                            @"	""beneficiaryMobileCountryCode"": """ + Convert.ToString(result.BCountry_Code) + @""",
" + "\n" +
                            @"	""beneficiaryMobile"": """ + (result.Beneficiary_Mobile.ToString()).Substring(result.BCountry_Code.ToString().Length) + @""",
" + "\n" +
                            @"	""beneficiaryTelephone"": "" "",
" + "\n" +
                            @"	""beneficiaryAddress"": """ + Convert.ToString(result.Beneficiary_Address) + @""",
" + "\n" +
                            @"	""totalAmount"": """ + sourceamt + @""",
" + "\n" +
                            @"	""totalCommission"": """ + commisionCharges + @""",
" + "\n" +
                            @"	""serviceTax"": """ + servicetax + @""",
" + "\n" +
                            @"	""sendingCurrency"": """ + Convert.ToString(result.FromCurrency_Code) + @""",
" + "\n" +
                            @"	""payoutMethod"": ""cashpickup"",
" + "\n" +
                            @"	""currencyToReceive"": """ + Convert.ToString(result.Currency_Code) + @""",
" + "\n" +
                            @"	""amountToReceive"": """ + AmountToReceive + @""",
" + "\n" +
                            @"	""purpose"": """ + Convert.ToString(result.Purpose) + @""",
" + "\n" +
                            @"	""sourceofIncome"": ""Individual"",
" + "\n" +
                            @"	""senderRemarks"": """ + senderRemark + @""",
" + "\n" +
                            @"	""transactionDate"": """ + Convert.ToString(result.current_date1) + @""",
" + "\n" +
                            @"	""userCreated"": """ + Convert.ToString(result.First_Name) + @""",
" + "\n" +
                                @"	""transactionReferenceNo"": """ + Transactionreference.ToString() + @""",
" + "\n" +
                            @"	""collectionMode"": ""cash"",
" + "\n" +
                            @"	""msisdn"": ""1"",
" + "\n" +
                            @"	""requestId"": ""012345698745"",


" + "\n" +
                            @"	""sendernationality"": """ + Convert.ToString(result.Nationality_Country) + @""",  
                  
" + "\n" +
                            @"	""documentName"": """ + Convert.ToString(result.ID_Name) + @""", 
" + "\n" +
                            @"	""documentNumber"": """ + Convert.ToString(result.SenderID_Number) + @""",  
" + "\n" +
                            @"	""Issuer"": """ + Convert.ToString(result.Country_Name) + @""",
" + "\n" +
                            @"	""dateofIssue"": """ + Convert.ToString(result.Issue_Datemdy) + @""",  
" + "\n" +
                            @"	""dateofExpire"": """ + Convert.ToString(result.SenderID_ExpiryDatemdy) + @""", 
" + "\n" +
                            @"	""documentFront"": """",
" + "\n" +
                            @"	""documentBack"": """"
" + "\n" +
                            @"}";
                                                    request.AddParameter("application/json", body, ParameterType.RequestBody);
                                                    ServicePointManager.Expect100Continue = true;
                                                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                                                    System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
                                                    req = apiurl + "/Services/GenerateCashPickupTransaction" + body;
                                                    await SaveActivityLogTracker(proceedMethod + "GenerateCashPickupTransaction Request From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);
                                                    response = client.Execute(request);
                                                    await SaveActivityLogTracker(proceedMethod + "GenerateCashPickupTransaction Response From MTS_Integration: <br/>" + response.Content.Replace("'", "\"") + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);
                                                    json6 = Newtonsoft.Json.Linq.JObject.Parse(response.Content);
                                                    var arr5 = json6["response"]["ResponseCode"];
                                                    if (arr5.ToString() == "M400")
                                                    {
                                                        string tras_ref = json6["response"]["result"]["TransactionRefNo"].ToString();
                                                        try
                                                        {
                                                            Newtonsoft.Json.Linq.JObject json7 = new Newtonsoft.Json.Linq.JObject();

                                                            options = new RestClientOptions(apiurl + "/Services/ConfirmTransaction")
                                                            {
                                                                MaxTimeout = -1
                                                            };
                                                            client = new RestClient(options);
                                                            request = new RestRequest()
                                                            {
                                                                Method = Method.Post
                                                            };
                                                            request.AddHeader("Authorization", "Bearer " + token);
                                                            request.AddHeader("username", apiuser);
                                                            request.AddHeader("secretkey", secretkey);
                                                            request.AddHeader("Content-Type", "application/json");
                                                            body = @"{
" + "\n" +
@"	""username"": """ + apiuser + @""",
" + "\n" +
@"	""password"": """ + apipass + @""",
" + "\n" +
@"	""clientkey"": """ + clientkey + @""",
" + "\n" +
@"	  ""TransactionRefNo"": """ + tras_ref + @""",
" + "\n" +
@"	""requestId"": ""321""
" + "\n" +
@"}";
                                                            request.AddParameter("application/json", body, ParameterType.RequestBody);
                                                            ServicePointManager.Expect100Continue = true;
                                                            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                                                            System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
                                                            req = apiurl + "/Services/ConfirmTransaction" + body;
                                                            await SaveActivityLogTracker(proceedMethod + "ConfirmTransaction Request From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                                                            response = client.Execute(request);
                                                            await SaveActivityLogTracker(proceedMethod + "ConfirmTransaction Response From MTS_Integration: <br/>" + response.Content.Replace("'", "\"") + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                                                            json7 = Newtonsoft.Json.Linq.JObject.Parse(response.Content);

                                                            Message = "Description : " + json7["response"]["result"]["message"].ToString();


                                                            if (json7["response"]["ResponseCode"].ToString() == "M400")
                                                            {

                                                                //
                                                                string APIBranch_Details = "";

                                                                APIBranch_Details = Convert.ToString(entity.payerId_datafull);
                                                                apistatus = 0;
                                                                string trn_referenceNo = json7["response"]["result"]["TransactionNo"].ToString();
                                                                int? BranchListAPI_ID = api_id; APIBranch_Details = entity.payerId_datafull;
                                                                int mappingid = Convert.ToInt32(result.TransMap_ID);
                                                                if (mappingid > 0)
                                                                {
                                                                    try
                                                                    {
                                                                        AgentRateapi = 0;
                                                                    }
                                                                    catch { }
                                                                    try
                                                                    {
                                                                        var parameters2 = new
                                                                        {
                                                                            _ReferenceNo = Transactionreference,
                                                                            _Transaction_ID = entity.Transaction_ID
                                                                        };

                                                                        var rowsAffected2 = await _dbConnection.ExecuteAsync("Update_reprocessed_Transaction_ref", parameters2, commandType: CommandType.StoredProcedure);





                                                                        string refer = Convert.ToString(result.ReferenceNo);
                                                                        await SaveActivityLogTracker("Checking All values for sp  : <br/>" + api_id + " " + refer + " " + refer + " " + APIBranch_Details, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Amal update transactiom mapping values checking ", entity.Branch_ID, Client_ID);

                                                                        if (mappingid > 0)
                                                                        {
                                                                            try
                                                                            {
                                                                                AgentRateapi = 0;
                                                                            }
                                                                            catch { }
                                                                            try
                                                                            {
                                                                                var parameters = new
                                                                                {
                                                                                    _BranchListAPI_ID = api_id,
                                                                                    _APIBranch_Details = entity.APIBranch_Details,
                                                                                    _TransactionRef = Transactionreference,
                                                                                    _trn_referenceNo = trn_referenceNo,
                                                                                    _APITransaction_Alert = 0,
                                                                                    _Transaction_ID = entity.Transaction_ID,
                                                                                    _Client_ID = entity.Client_ID,
                                                                                    _payout_partner_rate = AgentRateapi,
                                                                                };

                                                                                var rowsAffected = await _dbConnection.ExecuteAsync("Update_TransactionDetails", parameters, commandType: CommandType.StoredProcedure);



                                                                            }
                                                                            catch (Exception ex)
                                                                            {
                                                                                await SaveErrorLogAsync("Update_TransactionDetails SP exception: <br/> " + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                                                            }
                                                                        }


                                                                    }
                                                                    catch (Exception ex) { }

                                                                }

                                                                //
                                                            }
                                                            else
                                                            {
                                                                Message = "Description :  " + json7["response"]["result"]["message"].ToString();
                                                                return new ProceedResponseViewModel
                                                                {
                                                                    Status = "Failed",
                                                                    StatusCode = 2,
                                                                    Message = Message,
                                                                    ApiId = api_id,
                                                                    AgentRate = AgentRateapi,
                                                                    ApiStatus = apistatus,
                                                                    ExtraFields = new List<string> { "", "" }
                                                                };
                                                            }

                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            await SaveErrorLogAsync(proceedMethod + "ConfirmTransaction Error  exception: <br/> " + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);

                                                        }
                                                    }
                                                    else
                                                        {
                                                        Message = "Description :  " + json6["response"]["result"]["message"].ToString();
                                                        return new ProceedResponseViewModel
                                                        {
                                                            Status = "Failed",
                                                            StatusCode = 2,
                                                            Message = Message,
                                                            ApiId = api_id,
                                                            AgentRate = AgentRateapi,
                                                            ApiStatus = apistatus,
                                                            ExtraFields = new List<string> { "", "" }
                                                        };
                                                    }

                                                    //
                                                }
                                                catch (Exception ex)
                                                {
                                                    await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);



                                                    Message = "Description : CashPickup API Transaction Error.";
                                                    return new ProceedResponseViewModel
                                                    {
                                                        Status = "Failed",
                                                        StatusCode = 2,
                                                        Message = Message,
                                                        ApiId = api_id,
                                                        AgentRate = AgentRateapi,
                                                        ApiStatus = apistatus,
                                                        ExtraFields = new List<string> { "", "" }
                                                    };
                                                }
                                            }
                                            else
                                            {

                                                Message = "Description : Commission charges is not defined.";
                                                return new ProceedResponseViewModel
                                                {
                                                    Status = "Failed",
                                                    StatusCode = 2,
                                                    Message = Message,
                                                    ApiId = api_id,
                                                    AgentRate = AgentRateapi,
                                                    ApiStatus = apistatus,
                                                    ExtraFields = new List<string> { "", "" }
                                                };
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);

                                        }


                                    }
                                    catch (Exception ex)
                                    {
                                        await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);

                                    }


                                    Console.WriteLine(response.Content);
                                }
                                catch (Exception ex)
                                {
                                    await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);

                                }

                            }
                            else
                            {

                                Message = "Description : Service Provider is not available for this city.";
                                return new ProceedResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = Message,
                                    ApiId = api_id,
                                    AgentRate = AgentRateapi,
                                    ApiStatus = apistatus,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }

                        }
                        else
                        {

                            Message = "Description : Operator is not available for this country.";
                            return new ProceedResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = Message,
                                ApiId = api_id,
                                AgentRate = AgentRateapi,
                                ApiStatus = apistatus,
                                ExtraFields = new List<string> { "", "" }
                            };
                        }


                    }
                    else
                    {

                        Message = "Description : Service Provider is not available for this country.";
                        return new ProceedResponseViewModel
                        {
                            Status = "Failed",
                            StatusCode = 2,
                            Message = Message,
                            ApiId = api_id,
                            AgentRate = AgentRateapi,
                            ApiStatus = apistatus,
                            ExtraFields = new List<string> { "", "" }
                        };

                    }
                    #endregion
                }
                else if (result.PaymentDepositType_ID == 3)// mobile wallet
                {
                    #region mobilewallet_process
                    Newtonsoft.Json.Linq.JObject json2 = jsongetServices;


                    if (ServiceId != 0)
                    {
                        //
                        Newtonsoft.Json.Linq.JObject json3 = jsongetServiceOperators;
                        int OperatorId = 0;
                        var arr2 = (dynamic)null;
                        try
                        {
                            arr2 = json3["response"]["result"]["ServiceOperator"];
                            if (arr2.GetType().Name == "JArray")
                            {
                                foreach (Newtonsoft.Json.Linq.JObject item in arr2) // <-- Note that here we used JObject instead of usual JProperty
                                {
                                    string OperatorName = item["OperatorName"].ToString();
                                    OperatorName = new string(OperatorName.ToString()
                                     .Where(c => char.IsLetterOrDigit(c))
                                     .ToArray())
                                     .ToLower();
                                    if (Convert.ToString(result.Provider_name).Trim().ToLower().Contains(OperatorName.Trim().ToLower()))
                                    {
                                        OperatorId = Convert.ToInt32(item["OperatorId"]); break;

                                    }
                                }
                            }
                            else
                            {
                                try
                                {
                                    string OperatorName = (arr2["OperatorName"]).ToString();
                                    if (Convert.ToString(result.Provider_name).Trim().ToLower().Contains(OperatorName.Trim().ToLower()))
                                    {
                                        OperatorId = Convert.ToInt32(arr2["OperatorId"]);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);

                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);

                        }

                        if (OperatorId != 0)
                        {
                            Newtonsoft.Json.Linq.JObject json4 = jsonGetCitiesByCountryId;
                            int CityId = 0;
                            var arr3 = (dynamic)null;
                            try
                            {
                                arr3 = json4["response"]["result"]["Cities"]["City"];
                                if (arr3.GetType().Name == "JArray")
                                {
                                    foreach (Newtonsoft.Json.Linq.JObject item in arr3) // <-- Note that here we used JObject instead of usual JProperty
                                    {
                                        string CityName = item["CityName"].ToString().ToLower().Trim();
                                        if (CityName == result.Beneficiary_City.ToString().ToLower())
                                        {
                                            CityId = Convert.ToInt32(item["CityId"]);

                                        }
                                    }
                                }
                                else
                                {
                                    try
                                    {
                                        string CityName = (arr3["CityName"]).ToString().ToLower().Trim();
                                        if (CityName == result.City_Name.ToString().ToLower())
                                        {
                                            CityId = Convert.ToInt32(arr3["CityId"]);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);

                                    }
                                }


                                string payerIdValue = Convert.ToString(entity.APIBranch_Details);


                                string[] words = payerIdValue.Split('-');
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
                                if (digitsOnly)
                                {
                                    CityId = Convert.ToInt32(cityCodes);
                                }
                            }
                            catch (Exception ex)
                            {
                                await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);

                            }



                            if (CityId != 0)
                            {
                                //
                                string bname = Convert.ToString(result.Beneficiary_Name); string bfname = ""; string bmname = ""; string blname = "";

                                var n = bname.Split(' ');
                                if (n.Length == 1)
                                {
                                    bfname = n[0];
                                    bmname = "";
                                    blname = "";
                                }
                                else if (n.Length == 2)
                                {
                                    bfname = n[0];
                                    bmname = "";
                                    blname = n[1];
                                }
                                else if (n.Length == 3)
                                {
                                    bfname = n[0];
                                    bmname = n[1];
                                    blname = n[2];
                                }
                                else if (n.Length > 3)
                                {
                                    var lastname = "";
                                    for (var i = 0; i < n.Length; i++)
                                    {
                                        if (i == 0) { bfname = n[i]; }
                                        if (i == 1) { bmname = n[i]; }
                                        if (i > 1) { lastname = lastname + " " + n[i]; }
                                    }
                                    blname = lastname.Trim();
                                }
                                else
                                {
                                    bfname = bname;
                                    bmname = "";
                                    blname = "";
                                }
                                Newtonsoft.Json.Linq.JObject json5 = new Newtonsoft.Json.Linq.JObject();

                                try
                                {
                                    string destinationCountryId = Convert.ToString(dt_country?.country_code);
                                    string amountToSend = Convert.ToString(result?.AmountInGBP);
                                    string sourceCurrency = Convert.ToString(result?.FromCurrency_Code);
                                    string destinationCurrency = Convert.ToString(result?.Currency_Code);
                                    var options = new RestClientOptions(apiurl + "/Services/GetCommissionCharges")
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
                        @"  ""sourceBranchkey"": """ + SourceBranchkey_api + @""",
" + "\n" +
                        @" ""amountToSend"": """ + amountToSend + @""",
" + "\n" +
                        @"	""serviceId"": """ + ServiceId.ToString() + @""",
" + "\n" +
                        @"	""serviceOperatorId"": """ + OperatorId.ToString() + @""",
" + "\n" +
                        @"	""destinationCountryId"": """ + destinationCountryId + @""",
" + "\n" +
                        @"	""sourceCurrency"": """ + sourceCurrency + @""",
" + "\n" +
                        @"	""destinationCurrency"": """ + destinationCurrency + @""",
" + "\n" +
                        @"	""requestId"": ""0123456987"",
" + "\n" +
                        @"	""destinationCityId"": " + CityId + @"
" + "\n" +
                        @"}";
                                    request.AddParameter("application/json", body, ParameterType.RequestBody);
                                    ServicePointManager.Expect100Continue = true;
                                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                                    System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
                                    string req = apiurl + "/Services/GetCommissionCharges" + body;
                                    await SaveActivityLogTracker(proceedMethod + "GetCommissionCharges Request From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);
                                    var response = client.Execute(request);
                                    await SaveActivityLogTracker(proceedMethod + "GetCommissionCharges Response From MTS_Integration: <br/>" + response.Content.Replace("'", "\"") + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);
                                    json5 = Newtonsoft.Json.Linq.JObject.Parse(response.Content);
                                    var arr4 = (dynamic)null;
                                    string commisionCharges = "";
                                    string servicetax = "";
                                    string AmountToReceive = "";
                                    string MaxCommissionAmountInUSD = "", MinCommissionAmountInUSD = "";
                                    try
                                    {
                                        try
                                        {
                                            if ((json5["response"]["STATUS"]).ToString() == "Success")
                                            {
                                                if ((json5["response"]["result"]).ToString() != "null") { }
                                                try
                                                {
                                                    arr4 = json5["response"]["result"]["CommissionSlab"]["CommissionCharge"];
                                                    commisionCharges = arr4["CommissionChargesInSourceCurrency"];
                                                    servicetax = arr4["ServiceChargesInUSD"];
                                                    AmountToReceive = arr4["AmountToReceive"];
                                                    MaxCommissionAmountInUSD = arr4["MaxCommissionAmountInUSD"];
                                                    MinCommissionAmountInUSD = arr4["MinCommissionAmountInUSD"];
                                                    double base_cal_amt = Convert.ToDouble(result.AmountInPKR);
                                                    base_cal_amt = base_cal_amt / Convert.ToDouble(arr4["SourceToDestinationCurrencyExchangeRate"]);
                                                    try
                                                    {

                                                        options = new RestClientOptions(apiurl + "/Services/GetCommissionCharges")
                                                        {
                                                            MaxTimeout = -1
                                                        };
                                                        client = new RestClient(options);
                                                        request = new RestRequest()
                                                        {
                                                            Method = Method.Post
                                                        };
                                                        request.AddHeader("Authorization", "Bearer " + token);
                                                        request.AddHeader("username", apiuser);
                                                        request.AddHeader("secretkey", secretkey);
                                                        request.AddHeader("Content-Type", "application/json");
                                                        body = @"{
" + "\n" +
                                            @"  ""username"": """ + apiuser + @""",
" + "\n" +
                                            @"	""password"": """ + apipass + @""",
" + "\n" +
                                            @"	""clientkey"": """ + clientkey + @""",
" + "\n" +
                                            @"  ""sourceBranchkey"": """ + SourceBranchkey_api + @""",
" + "\n" +
                                            @" ""amountToSend"": """ + base_cal_amt.ToString() + @""",
" + "\n" +
                                            @"	""serviceId"": """ + ServiceId.ToString() + @""",
" + "\n" +
                                            @"	""serviceOperatorId"": """ + OperatorId.ToString() + @""",
" + "\n" +
                                            @"	""destinationCountryId"": """ + destinationCountryId + @""",
" + "\n" +
                                            @"	""sourceCurrency"": """ + sourceCurrency + @""",
" + "\n" +
                                            @"	""destinationCurrency"": """ + destinationCurrency + @""",
" + "\n" +
                                            @"	""requestId"": ""0123456987"",
" + "\n" +
                                            @"	""destinationCityId"": " + CityId + @"
" + "\n" +
                                            @"}";
                                                        request.AddParameter("application/json", body, ParameterType.RequestBody);
                                                        ServicePointManager.Expect100Continue = true;
                                                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                                                        System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
                                                        req = apiurl + "/Services/GetCommissionCharges" + body;
                                                        await SaveActivityLogTracker(proceedMethod + "GetCommissionCharges2 Request From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                                                        response = client.Execute(request);
                                                        await SaveActivityLogTracker(proceedMethod + "GetCommissionCharges2 Response From MTS_Integration: <br/>" + response.Content.Replace("'", "\"") + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                                                        json5 = Newtonsoft.Json.Linq.JObject.Parse(response.Content);
                                                        arr4 = (dynamic)null;
                                                        //
                                                        if ((json5["response"]["STATUS"]).ToString() == "Success")
                                                        {
                                                            arr4 = json5["response"]["result"]["CommissionSlab"]["CommissionCharge"];
                                                            commisionCharges = arr4["CommissionChargesInSourceCurrency"];
                                                            servicetax = arr4["ServiceChargesInUSD"];
                                                            AmountToReceive = arr4["AmountToReceive"];
                                                            MaxCommissionAmountInUSD = arr4["MaxCommissionAmountInUSD"];
                                                            MinCommissionAmountInUSD = arr4["MinCommissionAmountInUSD"];
                                                            await SaveActivityLogTracker(proceedMethod + " The Amount Sent to amal is " + base_cal_amt.ToString() + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {

                                                    }

                                                }
                                                catch (Exception ex)
                                                {
                                                    await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);


                                                }
                                            }
                                            else
                                            {
                                                Message = "Description : " + json5["response"]["result"]["message"].ToString();
                                                return new ProceedResponseViewModel
                                                {
                                                    Status = "Failed",
                                                    StatusCode = 2,
                                                    Message = Message,
                                                    ApiId = api_id,
                                                    AgentRate = AgentRateapi,
                                                    ApiStatus = apistatus,
                                                    ExtraFields = new List<string> { "", "" }
                                                };
                                            }
                                            if (commisionCharges != "")
                                            {

                                                if (Convert.ToDouble(commisionCharges) < Convert.ToDouble(MinCommissionAmountInUSD))
                                                {
                                                    commisionCharges = MinCommissionAmountInUSD;
                                                }
                                                else if (Convert.ToDouble(commisionCharges) > Convert.ToDouble(MaxCommissionAmountInUSD))
                                                {
                                                    commisionCharges = MaxCommissionAmountInUSD;
                                                }
                                                else
                                                {
                                                    commisionCharges = result.Transfer_Fees.ToString();
                                                }

                                                try
                                                {
                                                    try
                                                    {
                                                        if (string.IsNullOrEmpty(servicetax.Trim()))
                                                        {
                                                            servicetax = "0.0000";
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        servicetax = "0.0000";
                                                    }

                                                    if (result.ID_Name.ToString() == "Passport")
                                                    {
                                                        result.ID_Name = "P";
                                                    }
                                                    else if (result.ID_Name.ToString() == "Driving License" || result.ID_Name.ToString() == "UK Driving License")
                                                    {
                                                        result.ID_Name = "D";
                                                    }
                                                    else if (result.ID_Name.ToString() == "EU Nationality Card" || result.ID_Name.ToString() == "Identification ID")
                                                    {
                                                        result.ID_Name = "NID";
                                                    }
                                                    else
                                                    {
                                                        result.ID_Name = "NID";
                                                    }
                                                    string Gender = result.Gender.ToString().Trim();
                                                    string genders = Gender;
                                                    if (Gender == "Male")
                                                    {
                                                        Gender = "M";
                                                    }
                                                    else if (Gender == "Female")
                                                    {
                                                        Gender = "F";
                                                    }
                                                    else
                                                    {
                                                        Gender = "O";
                                                    }
                                                    //
                                                    Newtonsoft.Json.Linq.JObject json6 = new Newtonsoft.Json.Linq.JObject();

                                                    options = new RestClientOptions(apiurl + "/Services/GenerateMobileWalletTransaction")
                                                    {
                                                        MaxTimeout = -1
                                                    };
                                                    client = new RestClient(options);
                                                    request = new RestRequest()
                                                    {
                                                        Method = Method.Post
                                                    };

                                                    request.AddHeader("Authorization", "Bearer " + token);
                                                    request.AddHeader("username", apiuser);
                                                    request.AddHeader("secretkey", secretkey);
                                                    string sourceamt = Convert.ToString(arr4["SourceAmount"]);
                                                    request.AddHeader("Content-Type", "application/json");
                                                    body = @"{
                    " + "\n" +
                                                @"	""username"": """ + apiuser + @""",
                    " + "\n" +
                                                @"	""password"": """ + apipass + @""",
                    " + "\n" +
                                                @"	""clientkey"": """ + clientkey + @""",
                    " + "\n" +
                                                @"	""sourceBranchkey"": """ + SourceBranchkey_api + @""",
                    " + "\n" +
                                                @"	""sourceCountry"": """ + Convert.ToString(dt_Basecountry.country_code) + @""",
                    " + "\n" +
                                                @"	""senderFName"": """ + Convert.ToString(result.First_Name) + @""",
                    " + "\n" +
                                                @"	""senderMName"": """ + Convert.ToString(result.Middle_Name) + @""",
                    " + "\n" +
                                                @"	""senderLName"": """ + Convert.ToString(result.Last_Name) + @""",
                    " + "\n" +
                                                @"	""senderMobileCountryCode"": """ + Convert.ToString(result.BCountry_Code) + @""",
                    " + "\n" +
                                                @"	""senderMobile"": """ + Convert.ToString(result.Mobile_Number) + @""",
                    " + "\n" +
                                                @"	""senderTelephone"": """ + Convert.ToString(result.Phone_Number) + @""",
                    " + "\n" +
                                                @"	""senderAddress"": """ + Convert.ToString(result.sender_address) + @""",
                    " + "\n" +
                                                @"	""serviceId"": """ + ServiceId + @""",
                    " + "\n" +
                                                @"	""serviceOperatorId"": """ + OperatorId + @""",
                    " + "\n" +
                                                @"	""destinationCountryId"": """ + Convert.ToString(dt_country.country_code) + @""",
                    " + "\n" +
                                                @"	""destinationCityId"": """ + CityId + @""",
                    " + "\n" +
                                                @"	""beneficiaryFirstName"": """ + bfname + @""",
                    " + "\n" +
                                                @"	""beneficiaryMiddleName"": """ + bmname + @""",
                    " + "\n" +
                                                @"	""beneficiaryLastName"": """ + blname + @""",
                    " + "\n" +
                                                @"	""beneficiaryMobileCountryCode"": """ + Convert.ToString(result.BCountry_Code) + @""",
                    " + "\n" +
                                                @"	""beneficiaryMobile"": """ + (result.Beneficiary_Mobile.ToString()).Substring(result.BCountry_Code.ToString().Length) + @""",
                    " + "\n" +
                                                @"	""beneficiaryTelephone"": "" "",
                    " + "\n" +
                                                @"	""beneficiaryAddress"": """ + Convert.ToString(result.Beneficiary_Address) + @""",
                    " + "\n" +
                                                @"	""totalAmount"": """ + sourceamt + @""",
                    " + "\n" +
                                                @"	""totalCommission"": """ + commisionCharges + @""",
                    " + "\n" +
                                                @"	""serviceTax"": """ + servicetax + @""",
                    " + "\n" +
                                                @"	""sendingCurrency"": """ + Convert.ToString(result.FromCurrency_Code) + @""",
                    " + "\n" +
                                                @"	""payoutMethod"": ""mobilewallet"",
                    " + "\n" +
                                                @"	""currencyToReceive"": """ + Convert.ToString(result.Currency_Code) + @""",
                    " + "\n" +
                                                @"	""amountToReceive"": """ + AmountToReceive + @""",
                    " + "\n" +
                                                @"	""purpose"": """ + Convert.ToString(result.Purpose) + @""",
                    " + "\n" +
                                                @"	""sourceofIncome"": ""Individual"",
                    " + "\n" +
                                                @"	""senderRemarks"": """ + senderRemark + @""",
                    " + "\n" +
                                                @"	""transactionDate"": """ + Convert.ToString(result.current_date1) + @""",
                    " + "\n" +
                                                @"	""userCreated"": """ + Convert.ToString(result.First_Name) + @""",
                    " + "\n" +
                                                    @"	""transactionReferenceNo"": """ + Transactionreference.ToString() + @""",
                    " + "\n" +
                                                @"	""collectionMode"": ""Cash"",
                    " + "\n" +
                                                @"	""msisdn"": ""1"",
                    " + "\n" +
                                                @"	""requestId"": ""012345698745"",
                    " + "\n" +
                                                @"	""documentName"": """ + Convert.ToString(result.ID_Name) + @""",    
                    " + "\n" +
                                                @"	""documentNumber"": """ + Convert.ToString(result.SenderID_Number) + @""",    
                    " + "\n" +
                                                @"	""Issuer"": """ + Convert.ToString(result.Country_Name) + @""",  
                    " + "\n" +
                                                @"	""dateofIssue"": """ + Convert.ToString(result.Issue_Datedmy) + @""",       
                    " + "\n" +
                                                @"	""dateofExpire"": """ + Convert.ToString(result.SenderID_ExpiryDatedmy) + @""", 

                    " + "\n" +
                                                @"	""sendercity"": """ + senderCityID + @""",
                    " + "\n" +
                                                @"	""senderpostalcode"": """ + Convert.ToString(result.Post_Code) + @""",
                    " + "\n" +
                                                @"	""sendernationality"": """ + Convert.ToString(result.Nationality_Country) + @""",
                    " + "\n" +
                                                @"	""SenderEmail"": """ + Convert.ToString(result.Email_ID) + @""",
                    " + "\n" +
                                                @"	""sendergender"": """ + Gender + @""",
                    " + "\n" +
                                                @"	""SenderOccupation"": """ + Convert.ToString(result.Profession) + @""",
                    " + "\n" +
                                                @"	""dateofIssue"": """ + Convert.ToString(result.Issue_Datemdy) + @""",       
                    " + "\n" +
                                                @"	""dateofExpire"": """ + Convert.ToString(result.SenderID_ExpiryDatemdy) + @""",
                    " + "\n" +
                                                @"	""sendernationality"": """ + Convert.ToString(result.Nationality_Country) + @""",  
                    " + "\n" +
                                                @"	""senderDOB"": """ + Convert.ToString(result.Sender_DOBmdy) + @""",       
                    " + "\n" +

                                                @"	""documentFront"": """",
                    " + "\n" +
                                                @"	""documentBack"": """"
                    " + "\n" +
                                                @"}";
                                                    request.AddParameter("application/json", body, ParameterType.RequestBody);
                                                    ServicePointManager.Expect100Continue = true;
                                                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                                                    System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
                                                    req = apiurl + "/Services/GenerateMobileWalletTransaction" + body;
                                                    await SaveActivityLogTracker(proceedMethod + "GenerateMobileWalletTransaction Request From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                                                    response = client.Execute(request);
                                                    await SaveActivityLogTracker(proceedMethod + "GenerateMobileWalletTransaction Response From MTS_Integration: <br/>" + response.Content.Replace("'", "\"") + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);
                                                    json6 = Newtonsoft.Json.Linq.JObject.Parse(response.Content);
                                                    var arr5 = json6["response"]["ResponseCode"];
                                                    Message = "Description : " + json6["response"]["result"]["message"].ToString();

                                                    if (arr5.ToString() == "M400")
                                                    {
                                                        string APIBranch_Details = "";
                                                        APIBranch_Details = Convert.ToString(entity.payerId_datafull);

                                                        apistatus = 0;
                                                        string trn_referenceNo = json6["response"]["result"]["TransactionNo"].ToString();
                                                        int? BranchListAPI_ID = api_id; APIBranch_Details = entity.payerId_datafull;
                                                        int mappingid = Convert.ToInt32(result.TransMap_ID);
                                                        if (mappingid > 0)
                                                        {
                                                            try
                                                            {
                                                                AgentRateapi = 0;
                                                            }
                                                            catch { }
                                                            try
                                                            {
                                                                var parameters2 = new
                                                                {
                                                                    _ReferenceNo = Transactionreference,
                                                                    _Transaction_ID = entity.Transaction_ID
                                                                };

                                                                var rowsAffected2 = await _dbConnection.ExecuteAsync("Update_reprocessed_Transaction_ref", parameters2, commandType: CommandType.StoredProcedure);





                                                                string refer = Convert.ToString(result.ReferenceNo);
                                                                await SaveActivityLogTracker("Checking All values for sp  : <br/>" + api_id + " " + refer + " " + refer + " " + APIBranch_Details, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Amal update transactiom mapping values checking ", entity.Branch_ID, Client_ID);

                                                                if (mappingid > 0)
                                                                {
                                                                    try
                                                                    {
                                                                        AgentRateapi = 0;
                                                                    }
                                                                    catch { }
                                                                    try
                                                                    {
                                                                        var parameters = new
                                                                        {
                                                                            _BranchListAPI_ID = api_id,
                                                                            _APIBranch_Details = entity.APIBranch_Details,
                                                                            _TransactionRef = Transactionreference,
                                                                            _trn_referenceNo = trn_referenceNo,
                                                                            _APITransaction_Alert = 0,
                                                                            _Transaction_ID = entity.Transaction_ID,
                                                                            _Client_ID = entity.Client_ID,
                                                                            _payout_partner_rate = AgentRateapi,
                                                                        };

                                                                        var rowsAffected = await _dbConnection.ExecuteAsync("Update_TransactionDetails", parameters, commandType: CommandType.StoredProcedure);



                                                                    }
                                                                    catch (Exception ex)
                                                                    {
                                                                        await SaveErrorLogAsync("Update_TransactionDetails SP exception: <br/> " + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                                                    }
                                                                }


                                                            }
                                                            catch (Exception ex) { }

                                                        }
                                                    }
                                                    else
                                                    {

                                                        Message = "Description : " + json6["response"]["result"]["message"].ToString();
                                                        return new ProceedResponseViewModel
                                                        {
                                                            Status = "Failed",
                                                            StatusCode = 2,
                                                            Message = Message,
                                                            ApiId = api_id,
                                                            AgentRate = AgentRateapi,
                                                            ApiStatus = apistatus,
                                                            ExtraFields = new List<string> { "", "" }
                                                        };
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);


                                                    Message = "Description : Generate Mobile Wallet Transaction API Error.";
                                                    return new ProceedResponseViewModel
                                                    {
                                                        Status = "Failed",
                                                        StatusCode = 2,
                                                        Message = Message,
                                                        ApiId = api_id,
                                                        AgentRate = AgentRateapi,
                                                        ApiStatus = apistatus,
                                                        ExtraFields = new List<string> { "", "" }
                                                    };

                                                }
                                            }
                                            else
                                            {
                                                Message = "Description : Commission charges is not defined.";
                                                return new ProceedResponseViewModel
                                                {
                                                    Status = "Failed",
                                                    StatusCode = 2,
                                                    Message = Message,
                                                    ApiId = api_id,
                                                    AgentRate = AgentRateapi,
                                                    ApiStatus = apistatus,
                                                    ExtraFields = new List<string> { "", "" }
                                                };
                                            }
                                        }
                                        catch (Exception ex)
                                        {

                                            await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);


                                        }


                                    }
                                    catch (Exception ex)
                                    {
                                        await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);


                                    }


                                    Console.WriteLine(response.Content);
                                }
                                catch (Exception ex)
                                {

                                    await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);

                                }
                                //
                                // Newtonsoft.Json.Linq.JObject json4 = GetCommissionCharges(json.response.result.token, ServiceId, OperatorId);

                            }
                            else
                            {
                                Message = "Description :Service Provider is not available for this city.";
                                return new ProceedResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = Message,
                                    ApiId = api_id,
                                    AgentRate = AgentRateapi,
                                    ApiStatus = apistatus,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }

                        }
                        else
                        {

                            Message = "Description : Operator is not available for this country.";
                            return new ProceedResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = Message,
                                ApiId = api_id,
                                AgentRate = AgentRateapi,
                                ApiStatus = apistatus,
                                ExtraFields = new List<string> { "", "" }
                            };
                        }


                    }
                    else
                    {
                        Message = "Description : Service Provider is not available for this country.";
                        return new ProceedResponseViewModel
                        {
                            Status = "Failed",
                            StatusCode = 2,
                            Message = Message,
                            ApiId = api_id,
                            AgentRate = AgentRateapi,
                            ApiStatus = apistatus,
                            ExtraFields = new List<string> { "", "" }
                        };
                    }
                    #endregion
                }
                else
                {
                    Message = "Description : This Collection Type is not available for this collection point";
                    return new ProceedResponseViewModel
                    {
                        Status = "Failed",
                        StatusCode = 2,
                        Message = Message,
                        ApiId = api_id,
                        AgentRate = AgentRateapi,
                        ApiStatus = apistatus,
                        ExtraFields = new List<string> { "", "" }
                    };
                }
                #endregion
            }



            if (api_id == 45) // Rushikesh
            {
                #region Dynathopia


                string beneficiary_bank_code = Convert.ToString(result.bank_code);
                string Beneficiary_Name = Convert.ToString(result.Beneficiary_Name);
                string bname = Beneficiary_Name;
                string BankCode = "", benf_ISO_Code = "", bank_code = "", Purpose = "", ReferenceNo = "", AmountInGBP = "", AmountInPKR = "", Exchange_Rate = "", Transfer_Fees = "", Customer_ID = "", Phone_Number = "", Mobile_Number = "", Beneficiary_Mobile = "", Customer_Name = "", Bank_Name = "", AccountHolderName = "", Account_Number = "";


                BankCode = result.BankCode.ToString();
                benf_ISO_Code = result.benf_ISO_Code.ToString();
                bank_code = result.bank_code.ToString();
                Purpose = result.Purpose.ToString();
                ReferenceNo = result.ReferenceNo.ToString();
                AmountInGBP = result.AmountInGBP.ToString();
                AmountInPKR = result.AmountInPKR.ToString();
                Exchange_Rate = result.Exchange_Rate.ToString();
                Transfer_Fees = result.Transfer_Fees.ToString();
                Customer_ID = result.Customer_ID.ToString();
                Beneficiary_Mobile = result.Beneficiary_Mobile.ToString();
                Customer_Name = result.Customer_Name.ToString();
                Bank_Name = result.Bank_Name.ToString();
                AccountHolderName = result.AccountHolderName.ToString();
                Account_Number = result.Account_Number.ToString();

                if (Mobile_Number == "")
                {
                    Mobile_Number = Phone_Number;
                }
                if (BankCode == "")
                {
                    BankCode = bank_code;
                }

                if (BankCode != "")
                {
                    try
                    {
                        string callbackurl = "http://response.com/test";
                        var options = new RestClientOptions(apiurl + "/fxwalletapi/v1/service/feluwaaddai/transfer/")
                        {
                            MaxTimeout = -1
                        };
                        var client = new RestClient(options);
                        var request = new RestRequest()
                        {
                            Method = Method.Post
                        };
                        request.AddHeader("Content-Type", "application/json");
                        request.AddHeader("accesskey", apiuser);
                        request.AddHeader("secrete", apipass);
                        var body = @" {
                                " + "\n" +
                                             @" ""phonenumber"": """ + Beneficiary_Mobile + @""",
                                " + "\n" +
                                             @" ""amount"": """ + AmountInPKR + @""",
                                " + "\n" +
                                             @" ""description"": """ + Purpose + @""",
                                " + "\n" +
                                             @" ""sender"": """ + AccountHolderName + @""",
                                " + "\n" +
                                             @" ""reciever"": """ + Beneficiary_Name + @""",
                                " + "\n" +
                                             @" ""providerrefrence"": """ + ReferenceNo + @""",
                                " + "\n" +
                                             @" ""accountnumber"": """ + Account_Number + @""",
                                " + "\n" +
                                             @" ""callbackurl"": """ + callbackurl + @""",
                                " + "\n" +
                                             @" ""bank"": """ + BankCode + @""",
                                " + "\n" +
                                             @" ""externalref"": """ + ReferenceNo + @"""
                                " + "\n" +
                        @" } ";
                        request.AddParameter("application/json", body, ParameterType.RequestBody);
                        string req = apiurl + "/fxwalletapi/v1/service/feluwaaddai/transfer/" + body;
                        await SaveActivityLogTracker("Dynathopia Create Transaction Request: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);
                        RestResponse response = client.Execute(request);
                        await SaveActivityLogTracker("Dynathopia Create Transaction Response: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);
                        var jsonObject = JObject.Parse(response.Content);
                        string? code = jsonObject["code"]?.ToString();
                        string? externalRef = jsonObject["externalref"]?.ToString();
                        string? description = jsonObject["description"]?.ToString();
                        Message = description;
                        if (code == "02")
                        {
                            string APIBranch_Details = "";
                            apistatus = 0;

                            string refer = Convert.ToString(result.ReferenceNo);
                            await SaveActivityLogTracker("Checking All values for sp  : <br/>" + api_id + " " + refer + " " + refer + " " + APIBranch_Details, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Dynathopia update transactiom mapping values checking ", entity.Branch_ID, Client_ID);
                            int mappingid = Convert.ToInt32(entity.Transaction_ID);
                            if (mappingid > 0)
                            {
                                try
                                {
                                    AgentRateapi = 0;
                                }
                                catch { }
                                try
                                {
                                    var parameters = new
                                    {
                                        _BranchListAPI_ID = api_id,
                                        _APIBranch_Details = entity.APIBranch_Details,
                                        _TransactionRef = externalRef,
                                        _trn_referenceNo = externalRef,
                                        _APITransaction_Alert = 0,
                                        _Transaction_ID = entity.Transaction_ID,
                                        _Client_ID = entity.Client_ID,
                                        _payout_partner_rate = AgentRateapi,
                                    };

                                    var rowsAffected = await _dbConnection.ExecuteAsync("Update_TransactionDetails", parameters, commandType: CommandType.StoredProcedure);

                                }
                                catch (Exception ex)
                                {
                                    // mtsmethods.SaveActivityLogTracker(ex.ToString(), t.User_ID, "Dynathopia Update transaction mapping table sp error", t.CB_ID, t.Client_ID);
                                    await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                }
                            }

                        }
                        else
                        {
                            return new ProceedResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = Message,
                                ApiId = api_id,
                                AgentRate = AgentRateapi,
                                ApiStatus = apistatus,
                                ExtraFields = new List<string> { "", "" }
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);

                        return new ProceedResponseViewModel
                        {
                            Status = "Failed",
                            StatusCode = 2,
                            Message = "Failed To Processed Transaction.",
                            ApiId = api_id,
                            AgentRate = AgentRateapi,
                            ApiStatus = apistatus,
                            ExtraFields = new List<string> { "", "" }
                        };
                    }
                }
                else
                {
                    return new ProceedResponseViewModel
                    {
                        Status = "Failed",
                        StatusCode = 2,
                        Message = "Bank Code Not Found.",
                        ApiId = api_id,
                        AgentRate = AgentRateapi,
                        ApiStatus = apistatus,
                        ExtraFields = new List<string> { "", "" }
                    };
                }
                #endregion Dynathopia
            }
            else if (api_id == 47)//Rushikesh
            {
                #region Budpay

                string beneficiary_bank_code = Convert.ToString(result.bank_code);
                string Beneficiary_Name = Convert.ToString(result.Beneficiary_Name);
                string bname = Beneficiary_Name;
                string BankCode = "", benf_ISO_Code = "", bank_code = "", Purpose = "", ReferenceNo = "", AmountInGBP = "", AmountInPKR = "", Exchange_Rate = "", Transfer_Fees = "", Customer_ID = "", Phone_Number = "", Mobile_Number = "", Beneficiary_Mobile = "", Customer_Name = "", Bank_Name = "", AccountHolderName = "", Account_Number = "";

                bool success = false; string BISO_Code_three = "";
                string Country_Code = ""; string Currency_Code = ""; string sender_address = "";

                BankCode = result.BankCode.ToString();
                benf_ISO_Code = result.benf_ISO_Code.ToString();
                bank_code = result.bank_code.ToString();
                Purpose = result.Purpose.ToString();
                ReferenceNo = result.ReferenceNo.ToString();
                AmountInGBP = result.AmountInGBP.ToString();
                AmountInPKR = result.AmountInPKR.ToString();
                Exchange_Rate = result.Exchange_Rate.ToString();
                Transfer_Fees = result.Transfer_Fees.ToString();
                Customer_ID = result.Customer_ID.ToString();
                Beneficiary_Mobile = result.Beneficiary_Mobile.ToString();
                Customer_Name = result.Customer_Name.ToString();
                Bank_Name = result.Bank_Name.ToString();
                AccountHolderName = result.AccountHolderName.ToString();
                Account_Number = result.Account_Number.ToString();
                Country_Code = result.Country_Code.ToString();
                Currency_Code = result.Currency_Code.ToString();
                sender_address = result.sender_address.ToString();
                BISO_Code_three = result.BISO_Code_Three.ToString();

                if (Mobile_Number == "")
                {
                    Mobile_Number = Phone_Number;
                }
                if (BankCode == "")
                {
                    BankCode = bank_code;
                }
                if (BankCode != "")
                {
                    try
                    {

                        var options = new RestClientOptions(apiurl + "/api/v1/bank_list/" + Currency_Code)
                        {
                            MaxTimeout = -1
                        };
                        var client = new RestClient(options);
                        var request = new RestRequest()
                        {
                            Method = Method.Get
                        };

                        request.AddHeader("Content-Type", "application/json");
                        request.AddHeader("Authorization", "Bearer " + accesscode);

                        string req = apiurl + "/api/v2/bank_list/" + Country_Code;
                        await SaveActivityLogTracker("Budpay Get Banks Request: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Budpay Proceed", entity.Branch_ID, Client_ID);

                        RestResponse response = client.Execute(request);
                        await SaveActivityLogTracker("Budpay Get Banks Response: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Budpay Proceed", entity.Branch_ID, Client_ID);

                        var jsonObject = JObject.Parse(response.Content);
                        if (response.StatusCode == HttpStatusCode.BadRequest)
                        {
                            bool status = jsonObject["status"].Value<bool>();
                            Message = jsonObject["message"].ToString();
                        }
                        foreach (var bank in jsonObject["data"])
                        {
                            if (bank["bank_code"]?.ToString() == BankCode)
                            {
                                BankCode = bank["bank_code"].ToString();
                                Bank_Name = bank["bank_name"].ToString();
                                break;
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                    }
                    if (BankCode != "" && Bank_Name != "")
                    {
                        try
                        {

                            var options = new RestClientOptions(apiurl + "/api/v1/account_name_verify")
                            {
                                MaxTimeout = -1
                            };
                            var client = new RestClient(options);
                            var request = new RestRequest()
                            {
                                Method = Method.Post
                            };
                            request.AddHeader("Content-Type", "application/json");
                            request.AddHeader("Authorization", "Bearer " + accesscode);
                            var body = @"{
               " + "\n" +
                            @"    ""bank_code"": """ + BankCode + @""",
               " + "\n" +
                            @"    ""account_number"": """ + Account_Number + @"""
               " + "\n" +
                            @"}";
                            request.AddParameter("application/json", body, ParameterType.RequestBody);
                            string req = apiurl + "/api/v2/account_name_verify " + body;
                            await SaveActivityLogTracker("Budpay Verify Account Request: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Budpay Proceed", entity.Branch_ID, Client_ID);

                            RestResponse response = client.Execute(request);
                            await SaveActivityLogTracker("Budpay Verify Account Response: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Budpay Proceed", entity.Branch_ID, Client_ID);

                            var jsonObject = JObject.Parse(response.Content);
                            if (response.StatusCode == HttpStatusCode.BadRequest)
                            {
                                bool status = jsonObject["status"].Value<bool>();
                                Message = jsonObject["message"].ToString();
                            }

                            success = jsonObject["success"].Value<bool>();
                            string message = jsonObject["message"].ToString();
                            string data = jsonObject["data"].ToString();

                        }
                        catch (Exception ex)
                        {
                            await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Budpay Verify Account Error", entity.user_id, entity.Branch_ID, Client_ID, 0);
                        }
                        if (success == true)
                        {
                            try
                            {

                                var options = new RestClientOptions(apiurl + "/api/v1/bank_transfer")
                                {
                                    MaxTimeout = -1
                                };
                                var client = new RestClient(options);
                                var request = new RestRequest()
                                {
                                    Method = Method.Post
                                };
                                request.AddHeader("Content-Type", "application/json");
                                request.AddHeader("Authorization", "Bearer " + accesscode);
                                var body = @"{
               " + "\n" +
                                 @"    ""currency"": """ + Currency_Code + @""",
               " + "\n" +
                                 @"    ""amount"": """ + AmountInPKR + @""",
               " + "\n" +
                                 @"    ""bank_code"": """ + BankCode + @""",
               " + "\n" +
                                 @"    ""bank_name"": """ + Bank_Name + @""",
               " + "\n" +
                                 @"    ""account_number"": """ + Account_Number + @""",
               " + "\n" +
                                 @"    ""narration"": ""Test transfer"",
               " + "\n" +
                                 @"    ""reference"": """ + ReferenceNo + @""",
               " + "\n" +
                                 @"    ""meta_data"":[
               " + "\n" +
                                 @"        {
               " + "\n" +
                                 @"            ""sender_name"":""" + Customer_Name + @""",
               " + "\n" +
                                 @"            ""sender_address"":""" + sender_address + @"""
               " + "\n" +
                                 @"        }
               " + "\n" +
                                 @"    ]
               " + "\n" +
                                 @"}";
                                request.AddParameter("application/json", body, ParameterType.RequestBody);
                                string req = apiurl + "/api/v2/bank_transfer" + body;
                                await SaveActivityLogTracker("Budpay Bank Transfer Request: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Budpay Proceed", entity.Branch_ID, Client_ID);

                                RestResponse response = client.Execute(request);
                                await SaveActivityLogTracker("Budpay Bank Transfer Response: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Budpay Proceed", entity.Branch_ID, Client_ID);

                                var jsonObject = JObject.Parse(response.Content);
                                if (response.StatusCode == HttpStatusCode.BadRequest)
                                {
                                    Message = jsonObject["message"].ToString();
                                }
                                string status = jsonObject["data"]["status"].ToString();
                                string reference = jsonObject["data"]["reference"].ToString();
                                if (status == "success" || status == "pending")
                                {
                                    string APIBranch_Details = entity.APIBranch_Details;
                                    apistatus = 0;
                                    string refer = Convert.ToString(result.ReferenceNo);
                                    await SaveActivityLogTracker("Checking All values for sp  : <br/>" + api_id + " " + refer + " " + refer + " " + APIBranch_Details, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Budpay update transactiom mapping values checking", entity.Branch_ID, Client_ID);

                                    int mappingid = Convert.ToInt32(entity.Transaction_ID);
                                    if (mappingid > 0)
                                    {
                                        try
                                        {
                                            AgentRateapi = 0;
                                        }
                                        catch { }
                                        try
                                        {

                                            var parameters = new
                                            {
                                                _BranchListAPI_ID = api_id,
                                                _APIBranch_Details = entity.APIBranch_Details,
                                                _TransactionRef = reference,
                                                _trn_referenceNo = reference,
                                                _APITransaction_Alert = 0,
                                                _Transaction_ID = entity.Transaction_ID,
                                                _Client_ID = entity.Client_ID,
                                                _payout_partner_rate = AgentRateapi,
                                            };

                                            var rowsAffected = await _dbConnection.ExecuteAsync("Update_TransactionDetails", parameters, commandType: CommandType.StoredProcedure);
                                        }
                                        catch (Exception ex)
                                        {
                                            await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Budpay Update transaction mapping table sp error", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                        }
                                    }

                                }
                                else
                                {
                                    if (Message == "")
                                    {
                                        Message = "Proceed Transaction Error.";
                                    }
                                    return new ProceedResponseViewModel
                                    {
                                        Status = "Failed",
                                        StatusCode = 2,
                                        Message = Message,
                                        ApiId = api_id,
                                        AgentRate = AgentRateapi,
                                        ApiStatus = apistatus,
                                        ExtraFields = new List<string> { "", "" }
                                    };
                                }
                            }
                            catch (Exception ex)
                            {
                                await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Budpay Transaction Procced Error", entity.user_id, entity.Branch_ID, Client_ID, 0);
                            }
                        }
                        else
                        {
                            if (Message == "")
                            {
                                Message = "Bank Account Validation Error.";
                            }
                            return new ProceedResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = Message,
                                ApiId = api_id,
                                AgentRate = AgentRateapi,
                                ApiStatus = apistatus,
                                ExtraFields = new List<string> { "", "" }
                            };
                        }
                    }
                    else
                    {
                        if (Message == "")
                        {
                            Message = "Bank Not Found In API.";
                        }
                        return new ProceedResponseViewModel
                        {
                            Status = "Failed",
                            StatusCode = 2,
                            Message = Message,
                            ApiId = api_id,
                            AgentRate = AgentRateapi,
                            ApiStatus = apistatus,
                            ExtraFields = new List<string> { "", "" }
                        };
                    }
                }
                else
                {
                    if (Message == "")
                    {
                        Message = "Bank Code Not Found.";
                    }
                    return new ProceedResponseViewModel
                    {
                        Status = "Failed",
                        StatusCode = 2,
                        Message = Message,
                        ApiId = api_id,
                        AgentRate = AgentRateapi,
                        ApiStatus = apistatus,
                        ExtraFields = new List<string> { "", "" }
                    };
                }
                #endregion Budpay
            }

            if (apistatus == 0)
            {
                return new ProceedResponseViewModel
                {
                    Status = "Success",
                    StatusCode = 0,
                    Message = Message,
                    ApiId = api_id,
                    AgentRate = AgentRateapi,
                    ApiStatus = apistatus,
                    ExtraFields = new List<string> { "", "" }
                };
            }
            else
            {
                Message = "Description : Something wents wrong.";
                return new ProceedResponseViewModel
                {
                    Status = "Failed",
                    StatusCode = 2,
                    Message = Message,
                    ApiId = api_id,
                    AgentRate = AgentRateapi,
                    ApiStatus = apistatus,
                    ExtraFields = new List<string> { "", "" }
                };
            }
        }
    }
}