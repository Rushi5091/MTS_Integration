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
using Bogus.Bson;

namespace Project.Infrastructure.Repositories
{
    public class TransactionStatusRepository : BaseRepository<TransactionStatus>, ITransactionStatusRepository
    {
        private readonly AppSettings _appSettings;

        public TransactionStatusRepository(IDbConnection dbConnection, IOptions<AppSettings> appSettings) : base(dbConnection)
        {
            _appSettings = appSettings.Value;
        }

        public async Task<bool> IsExists(string key, int? value)
        {
            var query = $"SELECT COUNT(1) FROM transaction_table WHERE {key} = @value;";
            var result = await _dbConnection.ExecuteScalarAsync<int>(query, new { value });
            return result == 1;
        }

        public async Task<TransactionStatusResponseViewModel> TransactionStatus(TransactionStatus entity)
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

            storedProcedureName = "getproceedtransaction_details";
            var values1 = new
            {
                iClient_ID = Client_ID,
                iTransaction_ID = Transaction_ID,
                iBranchListAPI_ID = api_id
            };

            var results = await _dbConnection.QueryAsync(storedProcedureName, values1, commandType: CommandType.StoredProcedure);
            dynamic result = results.First();

            storedProcedureName = "get_transaction_Ref";
            var values2 = new
            {
                TransactionRef = Transaction_ID
            };
            var results_ref = await _dbConnection.QueryAsync(storedProcedureName, values2, commandType: CommandType.StoredProcedure);
            dynamic result_ref = (dynamic)results_ref.FirstOrDefault();

            if (api_id == 15)
            {
                #region amal

        
                int PaymentDepositType_ID = Convert.ToInt32(result.PaymentDepositType_ID);

                string clientkey = "";
                string secretkey = "";
                if (PaymentDepositType_ID == 1 || PaymentDepositType_ID == 2 || PaymentDepositType_ID == 3)
                {
                     string Username_api = ""; string password_api = ""; string clientkey_api = ""; string SourceBranchkey_api = "";
                  
                    if (api_fields != "" && api_fields != null)
                    {
                        Newtonsoft.Json.Linq.JObject obj12 = Newtonsoft.Json.Linq.JObject.Parse(api_fields);
                        clientkey = Convert.ToString(obj12["clientkey"]);
                        secretkey = Convert.ToString(obj12["secretkey"]);
                        SourceBranchkey_api = Convert.ToString(obj12["SourceBranchkey"]);
                    }
                    Boolean valid = true;
                    //string token = TokenGenrationAmal(dt);

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
                        await SaveErrorLogAsync("Amal Token Generation Exception From MTS_Integration: <br/> " + ex.ToString(), DateTime.Now, "TransactionStatus", entity.user_id, entity.Branch_ID, Client_ID, 0);
                    }

                    string transactionidamal = result_ref?.APITransaction_ID;
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
                    await SaveActivityLogTracker("Get Amal Transaction status Request From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.user_id), "TransactionStatus", entity.Branch_ID, Client_ID);
                    var response = client.Execute(request);
                    await SaveActivityLogTracker("Get Amal Transaction status Response From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.user_id), "TransactionStatus", entity.Branch_ID, Client_ID);
                    Newtonsoft.Json.Linq.JObject json = Newtonsoft.Json.Linq.JObject.Parse(response.Content);
                    string Tr_status = json["response"]["result"]["Transaction"]["TransactionDetails"]["TransactionStatus"].ToString();
                    if (json["response"]["result"]["Transaction"]["TransactionDetails"]["TransactionStatus"].ToString() == "paid")
                    {

                        
                        return new TransactionStatusResponseViewModel
                        {
                            Status = "Success",
                            StatusCode = 0,
                            Message = "Transaction Status is Paid",
                            ApiId = Transaction_ID,
                            AgentRate = AgentRateapi,
                            ApiStatus = apistatus,
                            ExtraFields = new List<string> { "", "" }
                        };
                    }
                    else
                        if (json["response"]["result"]["Transaction"]["TransactionDetails"]["TransactionStatus"].ToString() == "unpaid")
                    {
                        return new TransactionStatusResponseViewModel
                        {
                            Status = "Success",
                            StatusCode = 4,
                            Message = " Transaction Status is : " + Tr_status,
                            ApiId = Transaction_ID,
                            AgentRate = AgentRateapi,
                            ApiStatus = apistatus,
                            ExtraFields = new List<string> { "", "" }
                        };
                    }
                    else
                            if (json["response"]["result"]["Transaction"]["TransactionDetails"]["TransactionStatus"].ToString() == "Cancel")
                    {
                        return new TransactionStatusResponseViewModel
                        {
                            Status = "Success",
                            StatusCode = 4,
                            Message = "Transaction Status Is: " + Tr_status,
                            ApiId = Transaction_ID,
                            AgentRate = AgentRateapi,
                            ApiStatus = apistatus,
                            ExtraFields = new List<string> { "", "" }
                        };
                    }
                    else
                    {
                        return new TransactionStatusResponseViewModel
                        {
                            Status = "Success",
                            StatusCode = 4,
                            Message = "Transaction Status Is: " + Tr_status,
                            ApiId = Transaction_ID,
                            AgentRate = AgentRateapi,
                            ApiStatus = apistatus,
                            ExtraFields = new List<string> { "", "" }
                        };
                    }


                }
             
                #endregion
            }
            else if (api_id == 45)
            {
                #region Dynathopia

                string refer = "";
                string ReferenceNo = Convert.ToString(result.ReferenceNo);
                string scountry = Convert.ToString(result.ISO_Code);
                string Customer_ID = result.Customer_ID.ToString();
                refer = Convert.ToString(result_ref.TransactionRef);
                await SaveActivityLogTracker(apiurl + "insde the checkstatus transaction ref" + ReferenceNo, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "TransactionStatus", entity.Branch_ID, Client_ID);

                try
                {
                    var options = new RestClientOptions(apiurl + "/fxwalletapi/v1/service/feluwaaddai/transfer/" + refer)
                    {
                        MaxTimeout = -1
                    };
                    var client = new RestClient(options);
                    var request = new RestRequest()
                    {
                        Method = Method.Get
                    };
                    request.AddHeader("Content-Type", "application/json");
                    request.AddHeader("accesskey", apiuser);
                    request.AddHeader("secrete", apipass);


                    string req = apiurl + "/fxwalletapi/v1/service/feluwaaddai/transfer/" + refer;
                    await SaveActivityLogTracker("Dynathopia Check Status Request: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "TransactionStatus", entity.Branch_ID, Client_ID);

                    RestResponse response = client.Execute(request);
                    await SaveActivityLogTracker("Dynathopia Check Status Response: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "TransactionStatus", entity.Branch_ID, Client_ID);

                    var jsonObject = JObject.Parse(response.Content);
                    string code = jsonObject["code"]?.ToString();
                    string externalRef = jsonObject["externalref"]?.ToString();
                    string description = jsonObject["description"]?.ToString();
                    if (code == "00")
                    {
                        return new TransactionStatusResponseViewModel
                        {
                            Status = "Success",
                            StatusCode = 0,
                            Message = "Transaction Status is Paid",
                            ApiId = Transaction_ID,
                            AgentRate = AgentRateapi,
                            ApiStatus = apistatus,
                            ExtraFields = new List<string> { "", "" }
                        };
                    }
                    if (code == "02")
                    {
                        return new TransactionStatusResponseViewModel
                        {
                            Status = "Success",
                            StatusCode = 4,
                            Message = " Transaction Status is Pending: " + description,
                            ApiId = Transaction_ID,
                            AgentRate = AgentRateapi,
                            ApiStatus = apistatus,
                            ExtraFields = new List<string> { "", "" }
                        };
                    }
                    else
                    {
                        return new TransactionStatusResponseViewModel
                        {
                            Status = "Success",
                            StatusCode = 4,
                            Message = "Transaction Status Is: " + description,
                            ApiId = Transaction_ID,
                            AgentRate = AgentRateapi,
                            ApiStatus = apistatus,
                            ExtraFields = new List<string> { "", "" }
                        };
                    }
                }
                catch (Exception ex)
                {
                    await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "TransactionStatus", entity.user_id, entity.Branch_ID, Client_ID, 0);
                }
                #endregion Dynathopia
            }
            else if (api_id == 47)//Rushikesh
            {
                #region Budpay

                string refer = "";
                string ReferenceNo = Convert.ToString(result.ReferenceNo);
                string scountry = Convert.ToString(result.ISO_Code);
                string Customer_ID = result.Customer_ID.ToString();


                refer = Convert.ToString(result_ref.TransactionRef);

                await SaveActivityLogTracker(apiurl + "insde the checkstatus transaction ref" + ReferenceNo, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "TransactionStatus", entity.Branch_ID, Client_ID);
                try
                {
                    var options = new RestClientOptions(apiurl + "/api/v1/payout/:" + refer)
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
                    string req = apiurl + "/api/v2/payout/:" + refer;
                    await SaveActivityLogTracker("Budpay Check Status Request: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "TransactionStatus", entity.Branch_ID, Client_ID);

                    RestResponse response = client.Execute(request);
                    await SaveActivityLogTracker("Budpay Check Status Response: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "TransactionStatus", entity.Branch_ID, Client_ID);

                    var jsonObject = JObject.Parse(response.Content);
                    JObject dataObject = (JObject)jsonObject["data"];
                    string transaction_status = dataObject["status"].ToString();
                    string processingStatus = dataObject["processing_status"].ToString();
                    if (transaction_status == "success")
                    {
                        return new TransactionStatusResponseViewModel
                        {
                            Status = "Success",
                            StatusCode = 0,
                            Message = "Transaction Status is Paid",
                            ApiId = Transaction_ID,
                            AgentRate = AgentRateapi,
                            ApiStatus = apistatus,
                            ExtraFields = new List<string> { "", "" }
                        };
                    }
                    if (transaction_status == "pending")
                    {
                        return new TransactionStatusResponseViewModel
                        {
                            Status = "Success",
                            StatusCode = 4,
                            Message = "Transaction Status is Pending:" + transaction_status,
                            ApiId = Transaction_ID,
                            AgentRate = AgentRateapi,
                            ApiStatus = apistatus,
                            ExtraFields = new List<string> { "", "" }
                        };
                    }
                    else
                    {
                        return new TransactionStatusResponseViewModel
                        {
                            Status = "Success",
                            StatusCode = 4,
                            Message = "Transaction Status Is: " + transaction_status,
                            ApiId = Transaction_ID,
                            AgentRate = AgentRateapi,
                            ApiStatus = apistatus,
                            ExtraFields = new List<string> { "", "" }
                        };
                    }
                }
                catch (Exception ex)
                {
                    await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "TransactionStatus", entity.user_id, entity.Branch_ID, Client_ID, 0);
                }
                #endregion Budpay
            }
            else
            {
                return new TransactionStatusResponseViewModel
                {
                    Status = "Failed",
                    StatusCode = 3,
                    Message = "No API Method",
                    ApiId = Transaction_ID,
                    AgentRate = AgentRateapi,
                    ApiStatus = apistatus,
                    ExtraFields = new List<string> { "", "" }
                };
            }


            return new TransactionStatusResponseViewModel
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
    }
}