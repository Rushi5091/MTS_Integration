using Dapper;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Project.Core.Entities.General;
using Project.Core.Interfaces.IRepositories;
using RestSharp;
using System.Data;
using Project.API.Configuration;
using Project.Core.Entities.Business;
using System.Net;
using System.Text;

namespace Project.Infrastructure.Repositories
{
    public class CancelTransactionRepository : BaseRepository<CancelTransaction>, ICancelTransactionRepository
    {
        private readonly AppSettings _appSettings;

        public CancelTransactionRepository(IDbConnection dbConnection, IOptions<AppSettings> appSettings) : base(dbConnection)
        {
            _appSettings = appSettings.Value;
        }

        public async Task<bool> IsExists(string key, int? value)
        {
            var query = $"SELECT COUNT(1) FROM transaction_table WHERE {key} = @value;";
            var result = await _dbConnection.ExecuteScalarAsync<int>(query, new { value });
            return result == 1;
        }

        public async Task<CancelTransactionResponseViewModel> CancelTransaction(CancelTransaction entity)
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

            storedProcedureName = "getproceedtransaction_details";
            var values1 = new
            {
                iClient_ID = Client_ID,
                iTransaction_ID = Transaction_ID,
                iBranchListAPI_ID = api_id
            };

            var results = await _dbConnection.QueryAsync(storedProcedureName, values1, commandType: CommandType.StoredProcedure);
            dynamic result = (dynamic)results.FirstOrDefault();



            if (api_id == 15)
            {
                #region amal
                try
                {
                    string clientkey = "";
                    string secretkey = "";
                    int PaymentDepositType_ID = Convert.ToInt32(result.PaymentDepositType_ID);

                    if (PaymentDepositType_ID == 2)
                    {
                        string Username_api = ""; string password_api = ""; string clientkey_api = ""; string SourceBranchkey_api = "";
                        string transactionidamal = result?.APITransaction_ID;
                        if (api_fields != "" && api_fields != null)
                        {
                            Newtonsoft.Json.Linq.JObject obj12 = Newtonsoft.Json.Linq.JObject.Parse(api_fields);
                            clientkey = Convert.ToString(obj12["clientkey"]);
                            secretkey = Convert.ToString(obj12["secretkey"]);
                            SourceBranchkey_api = Convert.ToString(obj12["SourceBranchkey"]);
                        }
                        Boolean valid = true;
                        string token = "";

                        try
                        {



                            string usernamne = apiuser;
                            string pass = apipass;

                            string cred = Convert.ToBase64String(Encoding.Default.GetBytes(usernamne + ":" + pass));

                            var options1 = new RestClientOptions(apiurl + "/Auth/Token")
                            {
                                MaxTimeout = -1
                            };
                            var client1 = new RestClient(options1);
                            var request1 = new RestRequest()
                            {
                                Method = Method.Get
                            };
                            request1.AddHeader("username", usernamne);
                            request1.AddHeader("secretkey", secretkey);
                            request1.AddHeader("Authorization", "Basic " + cred);
                            ServicePointManager.Expect100Continue = true;
                            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                            System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
                            string req1 = apiurl + "/Auth/Token";
                            await SaveActivityLogTracker("Amal Create Token Request From MTS_Integration: <br/>" + req1 + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.user_id), "TransactionStatus", entity.Branch_ID, Client_ID);
                            RestResponse response1 = client1.Execute(request1);
                            await SaveActivityLogTracker("Amal Create Token Response From MTS_Integration: <br/>" + response1.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.user_id), "TransactionStatus", entity.Branch_ID, Client_ID);
                            var jsonObject = JObject.Parse(response1.Content);
                            token = jsonObject["response"]?["result"]?["token"]?.ToString();

                        }
                        catch (Exception ex)
                        {
                            Message1 = ex.Message;
                            await SaveErrorLogAsync("Amal Token Generation Exception From MTS_Integration: <br/> " + ex.ToString(), DateTime.Now, "TransactionStatus", entity.user_id, entity.Branch_ID, Client_ID, 0);
                        }

                        Newtonsoft.Json.Linq.JObject json = new Newtonsoft.Json.Linq.JObject();

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
    @"	  ""TransactionNo"": """ + Convert.ToString(transactionidamal) + @""",
" + "\n" +
    @"	""requestId"": ""321""
" + "\n" +
    @"}";
                        request.AddParameter("application/json", body, ParameterType.RequestBody);
                        ServicePointManager.Expect100Continue = true;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
                        string req = apiurl + "/Services/GetTransactionStatus" + body;
                        await SaveActivityLogTracker("AmalGetTransactionStatus - request From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.user_id), "TransactionStatus", entity.Branch_ID, Client_ID);

                        var response = client.Execute(request);
                        await SaveActivityLogTracker("Amal GetTransactionStatus - response From MTS_Integration: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.user_id), "TransactionStatus", entity.Branch_ID, Client_ID);

                        json = Newtonsoft.Json.Linq.JObject.Parse(response.Content);
                        if (json["response"]["result"]["Transaction"]["TransactionDetails"]["TransactionStatus"].ToString() == "paid")
                        {

                            Message = "This Transaction is paid and cannot be cancelled";
                            return new CancelTransactionResponseViewModel
                            {
                                Status = "Success",
                                StatusCode = 2,
                                Message = Message,
                                ApiId = Transaction_ID,
                                AgentRate = AgentRateapi,
                                ApiStatus = apistatus,
                                ExtraFields = new List<string> { "", "" }
                            };

                        }
                        else
                            if (json["response"]["result"]["Transaction"]["TransactionDetails"]["TransactionStatus"].ToString() == "unpaid")
                        {
                            try
                            {
                                Newtonsoft.Json.Linq.JObject json1 = new Newtonsoft.Json.Linq.JObject();
                                options = new RestClientOptions(apiurl + "/Services/CancelTransaction")
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
        @"	""clientkey"": """ + clientkey + @""",
" + "\n" +
        @"	""sourceBranchkey"": """ + SourceBranchkey_api + @""",
" + "\n" +
        @"	  ""transactionId"": """ + Convert.ToString(transactionidamal) + @""",
" + "\n" +
        @"	  ""notes"": ""test"",
" + "\n" +
        @"	""requestId"": ""321""
" + "\n" +
        @"}";
                                request.AddParameter("application/json", body, ParameterType.RequestBody);
                                ServicePointManager.Expect100Continue = true;
                                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                                System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
                                req = apiurl + "/Services/CancelTransaction" + body;
                                await SaveActivityLogTracker("Amal CancelTransaction - request From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.user_id), "TransactionStatus", entity.Branch_ID, Client_ID);

                                response = client.Execute(request);
                                await SaveActivityLogTracker("Amal CancelTransaction - response From MTS_Integration: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.user_id), "TransactionStatus", entity.Branch_ID, Client_ID);
                                json = Newtonsoft.Json.Linq.JObject.Parse(response.Content);
                                if (json["response"]["result"]["CancelTransaction"]["Details"]["message"].ToString() == "Transaction Cancelled")
                                {
                                    Message = Convert.ToString(json["response"]["result"]["CancelTransaction"]["Details"]["message"]);
                                    apistatus = 0;

                                }
                                else
                                {
                                    apistatus = 2;
                                }
                            }
                            catch (Exception ex)
                            {
                                Message1 = ex.Message;
                                await SaveErrorLogAsync("Amal Token Generation Exception From MTS_Integration: <br/> " + ex.ToString(), DateTime.Now, "TransactionStatus", entity.user_id, entity.Branch_ID, Client_ID, 0);

                            }


                        }
                        else if (json["response"]["result"]["Transaction"]["TransactionDetails"]["TransactionStatus"].ToString() == "Cancel")
                        {
                            Message = "Transaction has allready cancelled.";
                            return new CancelTransactionResponseViewModel
                            {
                                Status = "Success",
                                StatusCode = 2,
                                Message = Message,
                                ApiId = Transaction_ID,
                                AgentRate = AgentRateapi,
                                ApiStatus = apistatus,
                                ExtraFields = new List<string> { "", "" }
                            };
                        }


                    }
                    else
                    {

                        Message = "Cannot cancel the transaction for this collection type";
                        return new CancelTransactionResponseViewModel
                        {
                            Status = "Success",
                            StatusCode = 2,
                            Message = Message,
                            ApiId = Transaction_ID,
                            AgentRate = AgentRateapi,
                            ApiStatus = apistatus,
                            ExtraFields = new List<string> { "", "" }
                        };
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    Message1 = ex.Message;
                    await SaveErrorLogAsync("Amal Token Generation Exception From MTS_Integration: <br/> " + ex.ToString(), DateTime.Now, "TransactionStatus", entity.user_id, entity.Branch_ID, Client_ID, 0);

                }
            }




            if (apistatus == 0)
            {

                return new CancelTransactionResponseViewModel
                {
                    Status = "Success",
                    StatusCode = 0,
                    Message = Message,
                    ApiId = Transaction_ID,
                    AgentRate = AgentRateapi,
                    ApiStatus = apistatus,
                    ExtraFields = new List<string> { "", "" }
                };
            }
            else
            {
                return new CancelTransactionResponseViewModel
                {
                    Status = "Failed",
                    StatusCode = 2,
                    Message = "Description:" + Message1,
                    ApiId = Transaction_ID,
                    AgentRate = AgentRateapi,
                    ApiStatus = apistatus,
                    ExtraFields = new List<string> { "", "" }
                };

            }

        }
    }
}