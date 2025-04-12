using Dapper;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Project.Core.Entities.General;
using Project.Core.Interfaces.IRepositories;
using RestSharp;
using System.Data;
using Project.API.Configuration;
using MySql.Data.MySqlClient;
using System.Data.Common;
using Project.Core.Entities.Business;
using System.Text;
using System.Net; // Add this line

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

                if (api_id == 15)
                {
                    #region amal_get_collectionpoints
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
                                await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "BranchList", entity.user_id, entity.Branch_ID, Client_ID, 0);
                            }



                            string cityNamedefault = "";

                            try
                            {
                                cityNamedefault = result.Beneficiary_City.ToString().ToUpper();
                            }
                            catch (Exception ex)
                            {
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
                        await SaveErrorLogAsync("Get Amal city Collection Points Error exception: <br/> " + ex.ToString(), DateTime.Now, "BranchList", entity.user_id, entity.Branch_ID, Client_ID, 0);
                    }
                    #endregion
                }



                else if (api_id == 45)
                {
                    #region Dynathopia
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
            }

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
    }
}