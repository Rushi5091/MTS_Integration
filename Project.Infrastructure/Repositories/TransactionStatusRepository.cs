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
using Newtonsoft.Json;


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

            if (api_id == 3)
            {
                #region DataField API

                string Beneficiary_Name = Convert.ToString(result.Beneficiary_Name);

                string company_id = "";
                string Clientid = "";
                string Agentcode = "";
                string FrSubagent = "";
                string Headers = "";

                if (api_id == 3 && api_fields != "" && api_fields != null)
                {
                    Newtonsoft.Json.Linq.JObject obj = Newtonsoft.Json.Linq.JObject.Parse(api_fields);
                    company_id = Convert.ToString(obj["company_id"]);
                    Clientid = Convert.ToString(obj["Clientid"]);
                    Agentcode = Convert.ToString(obj["Agentcode"]);
                    FrSubagent = Convert.ToString(obj["FrSubagent"]);
                    Headers = Convert.ToString(obj["Headers"]);
                }






                string benf_ISO_Code = "";
                string Issue_Datemdy = "";
                string sendernationality = "";
                string SenderID_ExpiryDateymd = "";
                string Purpose = "";
                string ReferenceNo = "";
                string AmountInGBP = "";
                string AmountInPKR = "";
                string Phone_Number = "";
                string Mobile_Number = "";
                string Beneficiary_Name1 = "";
                string Beneficiary_Address = "";
                string Beneficiary_Mobile = "";
                string SenderID_Number = "";
                string Customer_Name = "";
                string Beneficiary_City = "";
                string Beneficiary_Country = "";
                string sender_address = "";
                string City_Name = "";
                string ID_Name = "";
                string FromCurrency_Code = "";
                string Currency_Code = "";
                string sendercountrycode = "";
                var encryptedData = "";
                var bodyJson = "";
                string Customer_ID = result.Customer_ID.ToString();

                Purpose = result.Purpose.ToString();//
                ReferenceNo = result.ReferenceNo.ToString();//
                AmountInGBP = result.AmountInGBP.ToString();//
                AmountInPKR = result.AmountInPKR.ToString();//
                Phone_Number = result.Phone_Number.ToString();//sender
                Mobile_Number = result.Mobile_Number.ToString();//sender
                Beneficiary_Name1 = result.Beneficiary_Name1.ToString();//
                Beneficiary_Address = result.Beneficiary_Address.ToString();//
                Beneficiary_Mobile = result.Beneficiary_Mobile.ToString();//
                Customer_Name = result.Customer_Name.ToString();//
                Beneficiary_City = result.Beneficiary_City.ToString();//
                sender_address = result.sender_address.ToString();//
                City_Name = result.City_Name.ToString();//sender
                SenderID_Number = result.SenderID_Number.ToString();//
                ID_Name = result.ID_Name.ToString();//sender
                FromCurrency_Code = result.FromCurrency_Code.ToString();//sender
                Currency_Code = result.Currency_Code.ToString();//benef
                Issue_Datemdy = result.Issue_Datemdy.ToString();
                sendernationality = result.sendernationality.ToString();
                SenderID_ExpiryDateymd = result.SenderID_ExpiryDateymd.ToString();
                benf_ISO_Code = result.benf_ISO_Code.ToString();
                sendercountrycode = result.sendercountrycode.ToString();


                string username = apiuser;
                string password = apipass;

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
                    double ReceivedAmount_rate = 0.0;
                    string isFixed = "";
                    string isRate = "";
                    double ReceivedComm_rate = 0.0;
                    double buyingRatePay = 0.0;
                    double sellingRateLoc = 0.0;
                    double ratePayRate = 0.0;
                    double buyingRateLoc = 0.0;
                    double sellingRatePay = 0.0;

                    string ToCurrency = Currency_Code;
                    string FrCurrency = FromCurrency_Code;

                    double PayoutAmount = Convert.ToDouble(AmountInPKR);



                    ReceivedAmount_rate = PayoutAmount / ratePayRate;
                    ReceivedAmount_rate = Math.Round(ReceivedAmount_rate, 2);


                    double Ammount = ReceivedAmount_rate * buyingRateLoc;
                    Ammount = Math.Round(Ammount, 2);

                    try
                    {


                        options = new RestClientOptions(apiurl + "/api/RmtStatus")
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

                        var body3 = new
                        {
                            Transno = ReferenceNo,
                        };
                        bodyJson = JsonConvert.SerializeObject(body3);

                        encryptedData = Encrypt(bodyJson);

                        var requestBody4 = new
                        {
                            jsonstring = encryptedData
                        };
                        request.AddParameter("application/json", JsonConvert.SerializeObject(requestBody4), ParameterType.RequestBody);
                        req = apiurl + "/api/RmtStatus" + body3 + "jsonstring :" + requestBody4;
                        await SaveActivityLogTracker("Datafield Check Status Request: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "TransactionStatus", entity.Branch_ID, Client_ID);

                        response = client.Execute(request);
                        json = JsonConvert.DeserializeObject(response.Content);
                        await SaveActivityLogTracker("Datafield Check Status Response: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "TransactionStatus", entity.Branch_ID, Client_ID);

                        string Status1 = (string)json["Status"];
                        string wstransid1 = (string)json["wstransid"];
                        string Rmtno1 = (string)json["Rmtno"];

                        if (Status1 != "")
                        {

                            if (Status1 == "Paid")
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
                            else if (Status1 == "Cancel")
                            {
                                return new TransactionStatusResponseViewModel
                                {
                                    Status = "Success",
                                    StatusCode = 2,
                                    Message = "Transaction Status is Cancelled",
                                    ApiId = Transaction_ID,
                                    AgentRate = AgentRateapi,
                                    ApiStatus = apistatus,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }
                            else if (Status1 == "Stop")
                            {
                                return new TransactionStatusResponseViewModel
                                {
                                    Status = "Success",
                                    StatusCode = 4,
                                    Message = "Transaction status is Stop",
                                    ApiId = Transaction_ID,
                                    AgentRate = AgentRateapi,
                                    ApiStatus = apistatus,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }
                            else if (Status1 == "Ready")
                            {
                                return new TransactionStatusResponseViewModel
                                {
                                    Status = "Success",
                                    StatusCode = 4,
                                    Message = "Transaction status is Ready",
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
                                    StatusCode = 1,
                                    Message = "Transaction Status is " + Status1,
                                    ApiId = Transaction_ID,
                                    AgentRate = AgentRateapi,
                                    ApiStatus = apistatus,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Datafield RmtStatus Call", entity.user_id, entity.Branch_ID, Client_ID, 0);

                    }


                }
                else
                {
                    return new TransactionStatusResponseViewModel
                    {
                        Status = "Success",
                        StatusCode = 3,
                        Message = "Token not generated",
                        ApiId = Transaction_ID,
                        AgentRate = AgentRateapi,
                        ApiStatus = apistatus,
                        ExtraFields = new List<string> { "", "" }
                    };
                }
                #endregion
            }
            else if (api_id == 15)
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
            else if (api_id == 49)// HelloPaisa -- pradip
            {
                string Customer_ID = result.Customer_ID.ToString();
                //await SaveActivityLogTracker("Hello Paisa check Status API start: <br/>", 0, 0, 0, 0, "Hello Paisa Proceed", 0, 0);
                await SaveActivityLogTracker("Hello Paisa check Status API start: < br />", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "TransactionStatus", entity.Branch_ID, Client_ID);

                string RoutingCode = "";
                if (api_fields != "" && api_fields != null)
                {
                    Newtonsoft.Json.Linq.JObject obj = Newtonsoft.Json.Linq.JObject.Parse(api_fields);
                    RoutingCode = Convert.ToString(obj["Routingcode"]);
                }

                try
                {
                    string credentials = $"{apiuser}:{apipass}";
                    string encodedCredentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));

                    DateTime dateTime = DateTime.Now;
                    //c.BranchListAPI_ID = api_id;
                    //c.Is_Procedure = "QUERY";
                    //c.Operation_Name = "Proceed_mail_details";

                    // dt = da;
                    //string TransRefNo = Convert.ToString(dt.Rows[0]["APITransaction_ID"]);
                    //string API_BranchDetails = Convert.ToString(dt.Rows[0]["API_BranchDetails"]);
                    //string ReferenceNo = Convert.ToString(dt.Rows[0]["ReferenceNo"]);

                    string TransRefNo = Convert.ToString(result.APITransaction_ID);
                    string API_BranchDetails = Convert.ToString(result.API_BranchDetails);
                    string ReferenceNo = Convert.ToString(result.ReferenceNo);


                    //string ReferenceNo = Convert.ToString(result.ReferenceNo);
                    //string scountry = Convert.ToString(result.ISO_Code);
                    //string Customer_ID = result.Customer_ID.ToString();

                    string DCode = "";
                    string BankRoute = "";
                    if (!string.IsNullOrWhiteSpace(TransRefNo) && TransRefNo.Contains("-"))
                    {
                        string[] parts = TransRefNo.Split('-');
                        if (parts.Length == 2)
                        {
                            DCode = parts[0];
                            BankRoute = parts[1];
                        }
                    }

                    string Temp_url = apiurl + "/checkStatus";// https://dcmtest.nvizible.co.za/api/checkStatus
                    //var clientcheckStatus = new RestClient(Temp_url);
                    //clientcheckStatus.Timeout = -1;
                    //var requestcheckStatus = new RestRequest();
                    //requestcheckStatus.Method = Method.POST;

                    var options = new RestClientOptions(Temp_url)
                    {
                        MaxTimeout = -1
                    };
                    var clientcheckStatus = new RestClient(options);
                    var requestcheckStatus = new RestRequest()
                    {
                        Method = Method.Post
                    };


                    requestcheckStatus.AddHeader("Authorization", "Basic " + encodedCredentials);
                    requestcheckStatus.AddHeader("Content-Type", "application/json");
                    var body1 = @"{
" + "\n" +
@"  ""DcmRequest"": [
" + "\n" +
@"    {
" + "\n" +
@"      ""ReferenceNo"": """ + API_BranchDetails + @""",
" + "\n" +
@"      ""RoutingCode"": """ + RoutingCode + @""",
" + "\n" +
@"      ""BankRoute"": """ + BankRoute + @""",
" + "\n" +
@"      ""Action"": ""checkStatus"",
" + "\n" +
@"      ""DCode"": """ + DCode + @""",
" + "\n" +
@"      ""DcmLogin"": {
" + "\n" +
@"        ""userId"": """ + accesscode + @"""
" + "\n" +
@"      }
" + "\n" +
@"    }
" + "\n" +
                    @"  ]
"
                    + "\n" +
                    @"}";

                    // mtsmethods.InsertActivityLogDetails("Hello Paisa check Status response post: <br/>" + body1 + "", 0, 0, 0, 0, "Hello Paisa Proceed", 0, 0);
                    await SaveActivityLogTracker("Hello Paisa check Status response post: <br/>" + body1 + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "TransactionStatus", entity.Branch_ID, Client_ID);

                    requestcheckStatus.AddParameter("application/json", body1, ParameterType.RequestBody);
                    RestResponse responsecheckStatus = clientcheckStatus.Execute(requestcheckStatus);
                    //mtsmethods.InsertActivityLogDetails("Hello Paisa check Status response post: <br/>" + responsecheckStatus.Content + "", 0, 0, 0, 0, "Hello Paisa Proceed", 0, 0);
                    await SaveActivityLogTracker("Hello Paisa check Status response post: <br/>" + responsecheckStatus.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "TransactionStatus", entity.Branch_ID, Client_ID);

                    dynamic dJson = Newtonsoft.Json.JsonConvert.DeserializeObject(responsecheckStatus.Content);
                    var responseCode = dJson.DcmResponse.responseCode;
                    string message = dJson.DcmResponse.responseMessage;
                    //if (responseCode != null)
                    //{
                    //    if (responseCode == 200) { ds.Rows.Add(0, "Paid", message); return ds; }
                    //    else if (message.StartsWith("fail", StringComparison.OrdinalIgnoreCase) || message.IndexOf("fail", StringComparison.OrdinalIgnoreCase) >= 0) { ds.Rows.Add(5, "PAID", 0); return ds; }
                    //    else if (responseCode == 449 || responseCode == 206) { ds.Rows.Add(4, " Transaction Status is Pending : " + message, ""); return ds; }
                    //    else { ds.Rows.Add(3, message, status); return ds; }
                    //}
                    //else { ds.Rows.Add(3, message + " " + responseCode, "NO_STATUS"); }
                    TransactionStatusResponseViewModel response = new TransactionStatusResponseViewModel();

                    if (responseCode != null)
                    {
                        if (responseCode == 200)
                        {
                            response.Status = "Paid";
                            response.StatusCode = 0;
                            response.Message = "Transaction Paid Successfully";
                        }
                        else
                        {
                            // return whatever status comes from API
                            response.Status = "Un-Paid";
                            response.StatusCode = 5; // or map based on apistatus if needed
                            response.Message = message;
                        }
                    }
                    else
                    {
                        // responseCode null or empty → error
                        response.Status = "Error";
                        response.StatusCode = 3;
                        response.Message = "Invalid or missing response code from API";
                    }

                    // common fields
                    response.ApiId = Transaction_ID;
                    response.AgentRate = AgentRateapi;
                    response.ApiStatus = apistatus;
                    response.ExtraFields = new List<string> { "", "" };

                    return response;



                }
                catch (Exception ex)
                {
                    await SaveErrorLogAsync("API Hello Paisa transaction Status ERROR " + ex.ToString() + " " + entity.Transaction_ID, DateTime.Now, "CheckStatus", entity.user_id, entity.Branch_ID, Client_ID, 0);
                }
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