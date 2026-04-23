using Azure;
using Bogus.Bson;
using Dapper;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Project.API.Configuration;
using Project.Core.Entities.General;
using Project.Core.Interfaces.IRepositories;
using RestSharp;
using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;

using System.Xml;
using TransferZero.Sdk.Api;
using TransferZero.Sdk.Model;


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

            if (api_id == 3)
            {
                #region DataField
                try
                {
                    int? BranchListAPI_ID = api_id;
                    string APIBranch_Details = Convert.ToString(entity.APIBranch_Details);
                    //apiurl = "https://calyxuat.tayotransfer.com";
                    string username = apiuser;
                    string password = apipass;
                    string company_id = "";
                    string country_code = "";
                    string coutry_name = "";
                    string token = "";
                    double ReceivedAmount_rate = 0.0;
                    string isFixed = "";
                    string isRate = "";
                    double ReceivedComm_rate = 0.0;
                    double buyingRatePay = 0.0;
                    double sellingRateLoc = 0.0;
                    double ratePayRate = 0.0;
                    double buyingRateLoc = 0.0;
                    double sellingRatePay = 0.0;
                    string Clientid = "";
                    string Agentcode = "";
                    string FrSubagent = "";
                    string Headers = "";
                    string RMTno = "";
                    string SID = "";
                    string Status = "";
                    double exchangesrate = 0.0;
                    double transferfee = 0.0;
                    double USDComm = 0.0;
                    double ReceivedAmount = 0.0;
                    double Received_com = 0.0;
                    double PayoutAmount = 0.0;
                    double Localexchangerate = 0.0;
                    double Payoutexchangerate = 0.0;
                    string bank_name = "";
                    string account_number = "";
                    string Customer_ID = result.Customer_ID.ToString();
                    if (api_id == 3 && api_fields != "" && api_fields != null)
                    {
                        Newtonsoft.Json.Linq.JObject obj = Newtonsoft.Json.Linq.JObject.Parse(api_fields);
                        company_id = Convert.ToString(obj["company_id"]);
                        Clientid = Convert.ToString(obj["Clientid"]);
                        Agentcode = Convert.ToString(obj["Agentcode"]);
                        FrSubagent = Convert.ToString(obj["FrSubagent"]);
                        Headers = Convert.ToString(obj["Headers"]);
                    }



                    string Beneficiary_Name = Convert.ToString(result.Beneficiary_Name);

                    int benef_currencyid = 0;

                    string foreignamt = Convert.ToString(result.AmountInPKR);
                    int Deliverytype_Id = Convert.ToInt32(result.Deliverytype_Id);


                    storedProcedureName = "Currency_Search";
                    var values2 = new
                    {
                        _Currency_Code = result.Currency_Code,
                    };

                    var results1 = await _dbConnection.QueryAsync(storedProcedureName, values2, commandType: CommandType.StoredProcedure);

                    // Make sure to cast or assign to dynamic
                    dynamic dtcurrency = (dynamic)results1.FirstOrDefault();

                    benef_currencyid = Convert.ToInt32(dtcurrency.Currency_ID);

                    await SaveActivityLogTracker(" Datafields benef_currencyid  : <br/>" + "benef_currencyid :" + benef_currencyid + "Currency_Code :" + result.Currency_Code, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);


                    storedProcedureName = "Rates_Search_for_juba_usd_etb";
                    var values3 = new
                    {
                        p_Client_ID = Client_ID,
                        p_Deliverytype_Id = Deliverytype_Id,
                        p_PaymentDepositTypeID = result.PaymentDepositType_ID,
                        p_PaymentType_ID = result.PaymentType_ID,
                        p_Branch_ID = result.Branch_ID,
                        p_Beneficiary_Country_ID = result.Beneficiary_Country_ID,
                        p_CurrencyID = benef_currencyid,
                        p_foreignamt = foreignamt,
                        p_Agent_ID = entity.user_id,
                    };

                    var results2 = await _dbConnection.QueryAsync(storedProcedureName, values3, commandType: CommandType.StoredProcedure);

                    // Make sure to cast or assign to dynamic
                    dynamic dtusd_rate = (dynamic)results2.FirstOrDefault();


                    double etbRate = 0, usdRate = 1; double totalUSDAmt = 0;
                    try
                    {
                        exchangesrate = Convert.ToDouble(result.Exchange_Rate);
                        transferfee = Convert.ToDouble(result.Transfer_Fees);
                        etbRate = Convert.ToDouble(dtusd_rate.Agent_Rate);
                        if (etbRate > 0 && etbRate != null)
                            totalUSDAmt = Math.Round((Convert.ToDouble(result.AmountInPKR) * usdRate) / etbRate, 2);
                        USDComm = 0.00; //Math.Round((Convert.ToDouble(transferfee) * exchangesrate) / etbRate, 2);
                        ReceivedAmount = Math.Round((Convert.ToDouble(result.AmountInGBP)), 2);
                        Received_com = 0.00; //Math.Round((Convert.ToDouble(transferfee)));
                        PayoutAmount = Math.Round((Convert.ToDouble(result.AmountInPKR)), 2);





                        Localexchangerate = Math.Round((Convert.ToDouble(totalUSDAmt) / ReceivedAmount), 2);
                        Payoutexchangerate = Math.Round((Convert.ToDouble(exchangesrate)), 2);
                        await SaveActivityLogTracker(" Datafields rate found : <br/>" + "totalUSDAmt :" + totalUSDAmt + "ReceivedAmount :" + ReceivedAmount + "PayoutAmount :" + PayoutAmount + "Localexchangerate :" + Localexchangerate + "Payoutexchangerate :" + Payoutexchangerate, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);


                    }
                    catch (Exception ex)
                    {
                        Message1 = ex.Message;
                        await SaveErrorLogAsync(" Datafields rate found error: <br/>" + ex.ToString() + "", DateTime.Now, "Datafields Rate", entity.user_id, entity.Branch_ID, Client_ID, 0);

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
                    double Ammount = 0.0;
                    double comm = 0.0;
                    double RDAmount = 0.0;



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
                    if (result.PaymentDepositType_ID == 1)
                    {
                        bank_name = result.Bank_Name.ToString();
                        account_number = result.Account_Number.ToString();
                    }











                    //Call Add Remittance                            


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
                        token = json.Token;


                        string ToCurrency = Currency_Code;
                        string FrCurrency = FromCurrency_Code;

                        if (Agentcode == "RDS")
                        {

                            RDAmount = totalUSDAmt;

                        }
                        else
                        {
                            try
                            {

                                options = new RestClientOptions(apiurl + "/api/GetExchangeRate")
                                {
                                    MaxTimeout = -1
                                };
                                client = new RestClient(options);
                                request = new RestRequest()
                                {
                                    Method = Method.Post
                                };
                                credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
                                request.AddHeader(Headers, token);
                                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                                request.AddHeader("Authorization", $"Basic {credentials}");

                                var body4 = new
                                {
                                    FrCurrency = FrCurrency,
                                    ToCurrency = ToCurrency
                                };

                                bodyJson = JsonConvert.SerializeObject(body4);

                                encryptedData = Encrypt(bodyJson);


                                var requestBody1 = new
                                {
                                    jsonstring = encryptedData
                                };
                                request.AddParameter("application/json", JsonConvert.SerializeObject(requestBody1), ParameterType.RequestBody);
                                req = apiurl + "/api/GetExchangeRate" + body4 + " jsonstring : " + requestBody1;
                                await SaveActivityLogTracker("Datafiled GetExchangeRate Request From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                                response = client.Execute(request);
                                json = JsonConvert.DeserializeObject(response.Content);

                                await SaveActivityLogTracker("Datafiled GetExchangeRate Response From MTS_Integration: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);


                                try
                                {
                                    buyingRatePay = Convert.ToDouble(json.BuyingRatePay.ToString());        //USD TO KES Rate
                                }
                                catch (Exception ex) { }
                                try
                                {
                                    sellingRateLoc = Convert.ToDouble(json.SellingRateLoc.ToString());     //USD TO GBP Rate
                                }
                                catch (Exception ex) { }
                                try
                                {
                                    ratePayRate = Convert.ToDouble(json.RatePayRate.ToString());          //GBP TO KES Rate
                                }

                                catch (Exception ex) { }
                                try
                                {
                                    buyingRateLoc = Convert.ToDouble(json.BuyingRateLoc.ToString());     //USD TO GBP Rate
                                }
                                catch (Exception ex) { }
                                try
                                {
                                    sellingRatePay = Convert.ToDouble(json.SellingRatePay.ToString());  //USD TO KES Rate
                                }
                                catch (Exception ex) { }


                            }
                            catch (Exception ex)
                            {
                                Message1 = ex.Message;
                                await SaveErrorLogAsync("Datafield GetExchangeRate Call From MTS_Integration: <br/> " + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);

                            }

                            PayoutAmount = Convert.ToDouble(AmountInPKR);



                            ReceivedAmount_rate = PayoutAmount / ratePayRate;
                            ReceivedAmount_rate = Math.Round(ReceivedAmount_rate, 2);

                            string fromcountry = sendercountrycode;
                            string tocountry = benf_ISO_Code;

                            try
                            {



                                options = new RestClientOptions(apiurl + "/api/CommList")
                                {
                                    MaxTimeout = -1
                                };
                                client = new RestClient(options);
                                request = new RestRequest()
                                {
                                    Method = Method.Post
                                };
                                credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
                                request.AddHeader(Headers, token);
                                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                                request.AddHeader("Authorization", $"Basic {credentials}");

                                var body5 = new
                                {
                                    fromcountry = fromcountry,
                                    tocountry = tocountry,
                                    amount = ReceivedAmount_rate

                                };

                                bodyJson = JsonConvert.SerializeObject(body5);

                                encryptedData = Encrypt(bodyJson);


                                var requestBody2 = new
                                {
                                    jsonstring = encryptedData
                                };
                                request.AddParameter("application/json", JsonConvert.SerializeObject(requestBody2), ParameterType.RequestBody);
                                req = apiurl + "/api/CommList" + body5 + "jsonstring :" + requestBody2;
                                await SaveActivityLogTracker("Datafiled CommList Request From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                                response = client.Execute(request);
                                json = JsonConvert.DeserializeObject(response.Content);

                                await SaveActivityLogTracker("Datafiled CommList Response From MTS_Integration: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);



                                var commList = json.Comm_List;

                                // Iterate through the Comm_List array and print the values
                                foreach (var item in commList)
                                {
                                    ReceivedComm_rate = item.Rate;
                                    isFixed = item.IsFixed;
                                    isRate = item.IsRate;
                                }


                            }
                            catch (Exception ex)
                            {
                                Message1 = ex.Message;
                                await SaveErrorLogAsync("Datafield CommList Call From MTS_Integration: <br/> " + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);

                            }

                            Ammount = ReceivedAmount_rate / buyingRateLoc;
                            //Ammount = ReceivedAmount_rate * buyingRateLoc;
                            Ammount = Math.Round(Ammount, 2);
                            //comm = ReceivedComm_rate * buyingRateLoc;                           
                            comm = ReceivedComm_rate / buyingRateLoc;
                            comm = Math.Round(comm, 4);

                        }

                        if (Ammount > 0.0 && comm > 0.0 && ReceivedComm_rate > 0.0 || (RDAmount > 0.0))//|| (RDAmount > 0.0)
                        {
                            if (Agentcode == "RDS")
                            {

                                Ammount = RDAmount;
                                comm = USDComm;
                                ReceivedAmount_rate = ReceivedAmount;
                                ReceivedComm_rate = Received_com;
                                sellingRateLoc = Localexchangerate;
                                //PayoutAmount = PayoutAmount;
                                ratePayRate = Payoutexchangerate;
                            }

                            try
                            {



                                options = new RestClientOptions(apiurl + "/api/AddRemittanceWithRateV1")
                                {
                                    MaxTimeout = -1
                                };
                                client = new RestClient(options);
                                request = new RestRequest()
                                {
                                    Method = Method.Post
                                };
                                request.AddHeader(Headers, token);
                                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                                request.AddHeader("Authorization", $"Basic {credentials}");
                                var body1 = new
                                {
                                    Agentcode = Agentcode,
                                    FrSubagent = FrSubagent,
                                    Clientid = Clientid,
                                    Sender = Customer_Name,
                                    Sendertel = Mobile_Number,
                                    Senderaddress = sender_address,
                                    Sendercity = City_Name,
                                    Sendernationality = sendernationality,
                                    Doctype = ID_Name,
                                    Docno = SenderID_Number,
                                    Issuedate = Issue_Datemdy,
                                    Expdate = SenderID_ExpiryDateymd,
                                    Beneficiary = Beneficiary_Name1,
                                    Benefcity = Beneficiary_City,
                                    Beneftel = Beneficiary_Mobile,
                                    Benefaddress = Beneficiary_Address,
                                    Amount = Ammount,
                                    Comm = comm,
                                    purpose = Purpose,
                                    Source = "Business",
                                    Wstransid = ReferenceNo,
                                    ToSubAgent = APIBranch_Details,
                                    ReceivedAmount = ReceivedAmount_rate,
                                    ReceivedComm = ReceivedComm_rate,
                                    LocalExchagerate = sellingRateLoc,
                                    LocalCurrency = FromCurrency_Code,
                                    PayoutAmount = PayoutAmount,
                                    PayoutCurrency = Currency_Code,
                                    PayoutExrate = ratePayRate



                                };

                                var finalBody = body1.GetType()
                                                     .GetProperties()
                                                     .ToDictionary(prop => prop.Name, prop => prop.GetValue(body1));

                                if (result.PaymentDepositType_ID == 1)
                                {
                                    if (!string.IsNullOrEmpty(bank_name))
                                    {
                                        finalBody["BankName"] = bank_name;
                                    }

                                    if (!string.IsNullOrEmpty(account_number))
                                    {
                                        finalBody["AccountNumber"] = account_number;
                                    }
                                }
                                if (result.PaymentDepositType_ID == 2)
                                {

                                    finalBody["BankName"] = "NA";




                                    finalBody["AccountNumber"] = "NA";

                                }
                                bodyJson = JsonConvert.SerializeObject(finalBody);
                                encryptedData = Encrypt(bodyJson);

                                var requestBody3 = new
                                {
                                    jsonstring = encryptedData
                                };
                                request.AddParameter("application/json", JsonConvert.SerializeObject(requestBody3), ParameterType.RequestBody);
                                req = apiurl + "/api/AddRemittance" + body1 + "jsonstring:" + requestBody3;
                                await SaveActivityLogTracker("Datafield AddRemittance Request From MTS_Integration: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                                response = client.Execute(request);
                                json = JsonConvert.DeserializeObject(response.Content);
                                await SaveActivityLogTracker("Datafield AddRemittance Response From MTS_Integration: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                                JObject jsonObject = JObject.Parse(response.Content);

                                // Accessing the values
                                string message = (string)jsonObject["Result"][0]["Message"];
                                string code = (string)jsonObject["Result"][0]["Code"];


                                if (code == "200")
                                {
                                    RMTno = (string)jsonObject["Rmtno"];
                                    SID = (string)jsonObject["Sid"];
                                    Status = (string)jsonObject["Status"];



                                    if (Status == "Ready")
                                    {


                                        //APIBranch_Details = "";
                                        apistatus = 0;

                                        string refer = Convert.ToString(result.ReferenceNo);
                                        await SaveActivityLogTracker("Checking All values for sp  : <br/>" + api_id + " " + refer + " " + refer + " " + APIBranch_Details, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Datafiled update transactiom mapping values checking ", entity.Branch_ID, Client_ID);
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
                                                    _TransactionRef = RMTno,
                                                    _trn_referenceNo = RMTno,
                                                    _APITransaction_Alert = 0,
                                                    _Transaction_ID = entity.Transaction_ID,
                                                    _Client_ID = entity.Client_ID,
                                                    _payout_partner_rate = AgentRateapi,
                                                };

                                                var rowsAffected = await _dbConnection.ExecuteAsync("Update_TransactionDetails", parameters, commandType: CommandType.StoredProcedure);

                                            }
                                            catch (Exception ex)
                                            {
                                                Message1 = ex.Message;
                                                await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                            }
                                        }


                                    }






                                }





                            }
                            catch (Exception ex)
                            {
                                Message1 = ex.Message;
                                await SaveErrorLogAsync("Datafield AddRemittance Call From MTS_Integration: <br/> " + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);

                            }



                        }





                    }










                }
                catch (Exception ex)
                {
                    Message1 = ex.Message;
                    await SaveErrorLogAsync("Datafield Proceed Transaction Exception From MTS_Integration: <br/> " + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);

                }



                #endregion DataField
            }

            else if (api_id == 15)
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
                    Message1 = ex.Message;
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
                    Message1 = ex.Message;
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
                            Message1 = ex.Message;
                            await SaveErrorLogAsync(proceedMethod + "Sender City name error: <br/> " + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Message1 = ex.Message;
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
                            Message1 = ex.Message;
                            await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                        }
                    }
                    else if (result.PaymentDepositType_ID == 2)
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
                                    Message1 = ex.Message;
                                    await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);


                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            Message1 = ex.Message;
                            await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                        }
                    }
                    else if (result.PaymentDepositType_ID == 3)
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
                                    Message1 = ex.Message;
                                    await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);


                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Message1 = ex.Message;
                            await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                        }
                    }
                    //arr1 = (json["response"]["result"]["Service"]);
                }
                catch (Exception ex)
                {
                    Message1 = ex.Message;
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
                    Message1 = ex.Message;
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
                    Message1 = ex.Message;
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
                                    Message1 = ex.Message;
                                    await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            Message1 = ex.Message;
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
                                        Message1 = ex.Message;
                                        await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Message1 = ex.Message;
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
                                                        Message1 = ex.Message;
                                                        await SaveActivityLogTracker(" GetCommissionCharges2 Error" + ex.ToString() + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    Message1 = ex.Message;
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
                            @"	""userCreated"": """ + Convert.ToString(result.First_Name) + @""",
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
                                                                        Message1 = ex.Message;
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
                                            Message1 = ex.Message;
                                            await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                        }


                                    }
                                    catch (Exception ex)
                                    {
                                        Message1 = ex.Message;
                                        await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Message1 = ex.Message;
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
                                Message1 = ex.Message;
                                await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        Message1 = ex.Message;
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
                                    Message1 = ex.Message;
                                    await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            Message1 = ex.Message;
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
                                Message1 = ex.Message;
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
                                                        Message1 = ex.Message;
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    Message1 = ex.Message;
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
                                                                                Message1 = ex.Message;
                                                                                await SaveErrorLogAsync("Update_TransactionDetails SP exception: <br/> " + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                                                            }
                                                                        }


                                                                    }
                                                                    catch (Exception ex) { Message1 = ex.Message; }

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
                                                            Message1 = ex.Message;
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
                                            Message1 = ex.Message;
                                            await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);

                                        }


                                    }
                                    catch (Exception ex)
                                    {
                                        Message1 = ex.Message;
                                        await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);

                                    }



                                }
                                catch (Exception ex)
                                {
                                    Message1 = ex.Message;
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
                                    Message1 = ex.Message;
                                    await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);

                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            Message1 = ex.Message;
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
                                        Message1 = ex.Message;
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
                                Message1 = ex.Message;
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
                                                        Message1 = ex.Message;
                                                    }

                                                }
                                                catch (Exception ex)
                                                {
                                                    Message1 = ex.Message;
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
                                                                        Message1 = ex.Message;
                                                                        await SaveErrorLogAsync("Update_TransactionDetails SP exception: <br/> " + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                                                    }
                                                                }


                                                            }
                                                            catch (Exception ex) { Message1 = ex.Message; }

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
                                            Message1 = ex.Message;
                                            await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);


                                        }


                                    }
                                    catch (Exception ex)
                                    {
                                        Message1 = ex.Message;
                                        await SaveErrorLogAsync(proceedMethod + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);


                                    }


                                    Console.WriteLine(response.Content);
                                }
                                catch (Exception ex)
                                {
                                    Message1 = ex.Message;
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
            else if (api_id == 34)
            {
                #region Waverlite 
                try
                {

                    var body = "";

                    string Customer_ID = result.Customer_ID.ToString();

                    string beneficiary_name = Convert.ToString(result.Beneficiary_Name);
                    string beneficiary_account_number = Convert.ToString(result.Account_Number);
                    string beneficiary_country = Convert.ToString(result.Beneficiary_Country);
                    string beneficiary_phone = Convert.ToString(result.Beneficiary_Mobile);
                    string beneficiary_bank_code = Convert.ToString(result.bank_code);
                    string Reference_Id = Convert.ToString(result.ReferenceNo);
                    string naration = Convert.ToString(result.Comment);
                    string sender_name = Convert.ToString(result.SenderNameOnID);
                    string Currency_Code = Convert.ToString(result.Currency_Code);
                    string transaction_ammount = Convert.ToString(result.AmountInPKR);
                    string sourceId = "";
                    string status = "";
                    string beneficieary_country_code = Convert.ToString(result.benf_ISO_Code);
                    string method = "";
                    string wallet_key = "";
                    string waveToken = "";
                    string balance = "";
                    dynamic json = "";
                    string CreateFileUrl = "";
                    string BaseDirectoryPath = "";
                    string Hook = "";
                    string walletKey5 = "";
                    if (api_id == 34 && api_fields != "" && api_fields != null)
                    {
                        Newtonsoft.Json.Linq.JObject obj = Newtonsoft.Json.Linq.JObject.Parse(api_fields);
                        CreateFileUrl = Convert.ToString(obj["CreateFileUrl"]);
                        BaseDirectoryPath = Convert.ToString(obj["BaseDirectoryPath"]);
                        Hook = Convert.ToString(obj["Hook"]);
                        walletKey5 = Convert.ToString(obj["walletKey"]);
                    }

                    if (api_fields != "" && api_fields != null)
                    {
                        Newtonsoft.Json.Linq.JObject obj = Newtonsoft.Json.Linq.JObject.Parse(api_fields);
                        sourceId = Convert.ToString(obj["defaultgateway"]);
                    }
                    string bname = Convert.ToString(result.Beneficiary_Name).Trim(); string bfname = bname; string blname = " ";
                    if (bname.Contains(" "))
                    {
                        string[] spli = bname.Split(' ');
                        if (spli.Length > 1) { bfname = bname.Substring(0, (bname.Length - spli[spli.Length - 1].Length)); blname = spli[spli.Length - 1]; }
                    }


                    //  var options = apiurl;//"https://api.live.waverlite.com";
                    string privatekey = accesscode;//"WLT_PRI_LIVE_ZKM3j9kZ41CUufsjXCnOqt876RHcYi6sxtJmaPNl2yRa2BDSp0GVx8MnFJMmlCJbTf93Lg30sWEloiM8OSTE72c";


                    if (beneficieary_country_code == "NG")
                    {
                        beneficieary_country_code = "NGA";
                    }

                    //string privatekey = accesscode; //"WLT_PRI_LIVE_1TRYVJrlXH7AoQqnD5WIUgKre32tujcVwJxMV6jv16F5ZdCSbYIZ0vKl1GIrsfZ0uZZfAjzjrl3kZQocCGvnB5L";


                    // Get Banks details
                    try
                    {
                        var options = new RestClientOptions(apiurl + "/1.0/resources/institutions/list?country_code=" + beneficieary_country_code)
                        {
                            MaxTimeout = -1
                        };
                        var client = new RestClient(options);
                        var request = new RestRequest()
                        {
                            Method = Method.Get
                        };
                        request.AddHeader("Private-Key", privatekey);
                        string req = options + "/1.0/resources/institutions/list?country_code=" + beneficieary_country_code + privatekey;
                        await SaveActivityLogTracker("Get banks wawerlite Request: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                        RestResponse response = client.Execute(request);

                        json = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
                        await SaveActivityLogTracker("Get banks wawerlite Response: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);



                        dynamic resultwaver = json["data"];
                        dynamic result1 = json["data"][0]; ;
                        method = result1["method"];
                        foreach (var item in result)
                        {

                            dynamic institutions = item["institutions"];

                            foreach (var institution in institutions)
                            {

                                string identifier = institution["identifier"];


                                if (beneficiary_bank_code == identifier)
                                {

                                    beneficiary_bank_code = identifier;
                                    break;
                                }
                            }

                        }

                    }
                    catch (Exception ex)
                    {
                        Message1 = ex.Message;
                        await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Wawerlite Proceed Transaction 1st call", entity.user_id, entity.Branch_ID, Client_ID, 0);
                    }





                    bool invert = true;

                    string service = "DIRECT_DEPOSIT";
                    string walletKey = walletKey5;//"tIhbDk1sWiacBNDqijYCKfkX5eRF3u";//wallet_key;
                    string countryCode = beneficieary_country_code;
                    string currencyCode = Currency_Code;




                    //Get Wave Token
                    try
                    {



                        var options = new RestClientOptions(apiurl + "/1.0/wallet/wave-token/create")
                        {
                            MaxTimeout = -1
                        };
                        var client = new RestClient(options);
                        var request = new RestRequest()
                        {
                            Method = Method.Post
                        };
                        request.AddHeader("Private-Key", privatekey);
                        request.AddHeader("Content-Type", "application/json");
                        body = @"
                                    {
                                        ""invert"": " + invert.ToString().ToLower() + @",
                                        ""amount"": " + transaction_ammount + @",
                                        ""service"": """ + service + @""",
                                        ""method"": """ + method + @""",
                                        ""source"": {
                                            ""wallet_key"": """ + walletKey + @"""
                                        },
                                        ""target"": {
                                            ""country_code"": """ + countryCode + @""",
                                            ""currency_code"": """ + currencyCode + @"""
                                        }
                                    }";


                        request.AddParameter("application/json", body, ParameterType.RequestBody);

                        string req = options + "/1.0/wallet/wave-token/create" + privatekey + body;
                        await SaveActivityLogTracker("Wawerlite create wave_token API call reqest: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Wawerlite create wave_token req", entity.Branch_ID, Client_ID);


                        RestResponse response = client.Execute(request);


                        json = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
                        await SaveActivityLogTracker("Wawerlite create wave_token API call response: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Wawerlite create wave_token res", entity.Branch_ID, Client_ID);





                        waveToken = json["data"]["wave_token"];

                    }
                    catch (Exception ex)
                    {
                        Message1 = ex.Message;
                        await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Wawerlite Proceed Transaction 2nd call", entity.user_id, entity.Branch_ID, Client_ID, 0);

                    }




                    // Check available balance in account
                    try
                    {



                        var options = new RestClientOptions(apiurl + "/1.0/wallet/balance?wallet_key=" + walletKey)
                        {
                            MaxTimeout = -1
                        };
                        var client = new RestClient(options);
                        var request = new RestRequest()
                        {
                            Method = Method.Get
                        };

                        request.AddHeader("Private-Key", privatekey);

                        string req = options + "/ 1.0 / wallet / balance ? wallet_key = " + walletKey + privatekey;
                        await SaveActivityLogTracker("Wawerlite get balance API call reqest: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "wawerlite check balance request", entity.Branch_ID, Client_ID);

                        RestResponse response = client.Execute(request);
                        json = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
                        await SaveActivityLogTracker("Wawerlite get balance API call response: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Wawerlte check balance response", entity.Branch_ID, Client_ID);




                        balance = json["data"]["balances"]["available"];





                    }
                    catch (Exception ex)
                    {
                        Message1 = ex.Message;
                        await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Wawerlite Proceed Transaction 3rd call", entity.user_id, entity.Branch_ID, Client_ID, 0);

                    }



                    decimal balanceDecimal;
                    decimal transactionAmountDecimal;

                    if (decimal.TryParse(balance, out balanceDecimal) && decimal.TryParse(transaction_ammount, out transactionAmountDecimal))
                    {
                        if (balanceDecimal >= transactionAmountDecimal)
                        {

                            ;

                            // create file using transaction refnum for 3D-Authentication


                            //"https://localhost:7109/BankOfLondon";
                            apiurl = CreateFileUrl; //"https://currencyexchangesoftware.eu/waverlite/waverfilecreate/bankoflondon";
                            string url = $"{apiurl}?transactionReference={Reference_Id}";
                            //string url = apiurl + "?PrivateKey=" + PrivateKey + "&keyId=" + keyId + "&apiUrl=" + apiUrl + "&flagg=" + flagg + "&bodyy=" + bodyy + "&request_url=" + request_url + "&method=" + method;// "&paymentId=" + refNumber + "&redirect_success_url=" + redirect_success_url + "&payment_amount=" + payment_amount + "&payment_currency=" + payment_currency + "&customer_id=" + Customer_Reference + "&redirect_fail_url=" + redirect_fail_url + "&force_payment_method=" + force_payment_method + "&payment_description=" + payment_description + "&customer_account_id=" + customer_account_id + "&customer_country=" + customer_country + "&customer_city=" + customer_city + "&customer_first_name=" + customer_first_name + "&customer_last_name=" + customer_last_name + "&customer_ip_address=" + customer_ip_address + "&customer_address=" + customer_address + "&recepient_wallet_owner=" + recepient_wallet_owner + "&recepient_wallet_id=" + recepient_wallet_id + "&recipient_country=" + recipient_country + "&ProjectIdforBank=" + ProjectIdforBank;


                            var options = new RestClientOptions(url)
                            {
                                MaxTimeout = -1
                            };
                            var client10 = new RestClient(options);
                            var request10 = new RestRequest()
                            {
                                Method = Method.Get
                            };


                            request10.AddParameter("application/json", string.Empty, ParameterType.RequestBody);
                            string req = url;
                            await SaveActivityLogTracker("Waverlite create file using transaction refnum for 3D-Authentication Request: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Wawerlite create create transaction file", entity.Branch_ID, Client_ID);

                            RestResponse response10 = client10.Execute(request10);

                            await SaveActivityLogTracker("Waverlite create file using transaction refnum for 3D-Authentication Response: <br/>" + response10.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Wawerlite create create transaction file", entity.Branch_ID, Client_ID);

                            string baseDirectoryPath = BaseDirectoryPath;// @"C:\inetpub\wwwroot\waverlite";
                            string hookDirectoryName = Hook; //"WLT_HOOK_0SW0LG5vya2xRZK1JxXdOUMNCjyRAobc93oMiq4WuR3e62Uq5Ias33CfIP6HronFhest2O1pvIKoDU8j7s6OG4UOzm5";  
                            string transactionReference = Reference_Id;//"ABC12345"; 

                            string hookDirectoryPath = Path.Combine(baseDirectoryPath, hookDirectoryName);
                            string pointerFilePath = Path.Combine(hookDirectoryPath, transactionReference);


                            if (!Directory.Exists(baseDirectoryPath))
                            {
                                Directory.CreateDirectory(baseDirectoryPath);
                            }


                            if (!Directory.Exists(hookDirectoryPath))
                            {
                                Directory.CreateDirectory(hookDirectoryPath);
                            }

                            // Write the transaction reference to the pointer file
                            File.WriteAllText(pointerFilePath, transactionReference);


                            string publicBaseUrl = "https://currencyexchangesoftware.eu/waverlite";
                            string publicHookUrl = $"{publicBaseUrl}/{hookDirectoryName}/{transactionReference}";
                            await SaveActivityLogTracker("Waverlite Pointer file created at: <br/>" + publicHookUrl + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Wawerlite create create transaction file", entity.Branch_ID, Client_ID);




                            //Check file is created or not


                            var httpClient = new System.Net.Http.HttpClient();
                            var response5 = httpClient.GetAsync(publicHookUrl).Result;


                            if (response5.IsSuccessStatusCode)
                            {

                                await SaveActivityLogTracker("Wawerlite File is created and publicly accessible at location.: <br/>" + publicHookUrl + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Wawerlite create create transaction file", entity.Branch_ID, Client_ID);


                                string walletKey1 = walletKey; //"z3JBVj3hnLWRkyGhbm9g423R5VCz2J";
                                string countryCode1 = countryCode;//"CAN";
                                string waveToken1 = waveToken;//"9d0e5f0534e33d23470cbbae84635dac";
                                string institutionIdentifier1 = beneficiary_bank_code; //"mgPR9x1HBmbdZs38X5fUUCcbe74uk2";
                                string institutionEmail1 = "temytaio@yahoo.com";
                                string secretQuestion1 = "How are you?";
                                string secretAnswer1 = "I am fine";
                                string firstName1 = bfname;//"TAYO";
                                string surname1 = blname; //"AKILOSOSE";
                                string phoneNumber1 = "+2348131533096";//beneficiary_phone;//"+2348131533096";
                                string narration1 = "Financial support"; //naration;//"Financial support";
                                string reference1 = Reference_Id;//"sRuXQWWQ1gXYTFDDD46Wted";
                                string account_name = firstName1 + " " + surname1;
                                string account_number = beneficiary_account_number;
                                string method1 = "BANK_TRANSFER";

                                string msg3 = "";

                                string requestBody1 = $@"
{{
    ""country_code"": ""{countryCode1}"",
    ""currency_code"": ""{currencyCode}"",
    ""method"": ""{method1}"",
    ""recipient"": {{
        ""institution"": {{
            ""identifier"": ""{institutionIdentifier1}""
        }},
        ""account_number"": ""{account_number}""
    }}
}}";

                                // Verify account API call 
                                try
                                {



                                    options = new RestClientOptions(apiurl + "/1.0/payout/direct-deposit/recipient/verify")
                                    {
                                        MaxTimeout = -1
                                    };
                                    var client = new RestClient(options);
                                    var request = new RestRequest()
                                    {
                                        Method = Method.Post
                                    };



                                    request.AddHeader("Private-Key", privatekey);
                                    request.AddHeader("Content-Type", "application/json");

                                    request.AddParameter("application/json", requestBody1, ParameterType.RequestBody);
                                    req = options + "/1.0/payout/direct-deposit/recipient/verify" + privatekey + requestBody1;
                                    await SaveActivityLogTracker("Wawerlite verify account details  API call reqest: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Wawerlite verify account details  req", entity.Branch_ID, Client_ID);

                                    RestResponse response = client.Execute(request);

                                    json = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
                                    await SaveActivityLogTracker("Wawerlite verify account details  API call response: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Wawerlite verify account details   response", entity.Branch_ID, Client_ID);


                                    msg3 = json["message"];

                                }
                                catch (Exception ex)
                                {
                                    await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Wawerlite Proceed Transaction 4th api call", entity.user_id, entity.Branch_ID, Client_ID, 0);

                                    Message1 = ex.Message;

                                }

                                // Construct the JSON request body using string interpolation
                                string requestBody = $@"
{{
    ""wallet_key"": ""{walletKey1}"",
    ""country_code"": ""{countryCode1}"",
    ""wave_token"": ""{waveToken1}"",
    ""recipient"": {{
        ""institution"": {{
            ""identifier"": ""{institutionIdentifier1}"",
            ""account_name"": ""{account_name}"",
            ""account_number"": ""{account_number}""
        }},
        ""narration"": ""{narration1}""
    }},
    ""reference"": ""{reference1}""
}}";
                                if (msg3 == "Successful.")
                                {
                                    // Transaction API call 
                                    try
                                    {




                                        options = new RestClientOptions(apiurl + "/1.0/payout/direct-deposit/send")
                                        {
                                            MaxTimeout = -1
                                        };
                                        var client = new RestClient(options);
                                        var request = new RestRequest()
                                        {
                                            Method = Method.Post
                                        };




                                        request.AddHeader("Private-Key", privatekey);
                                        request.AddHeader("Content-Type", "application/json");

                                        request.AddParameter("application/json", requestBody, ParameterType.RequestBody);
                                        req = options + "/1.0/payout/direct-deposit/send" + privatekey + requestBody;
                                        await SaveActivityLogTracker("Wawerlite create send transaction API call reqest: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Wawerlite create transaction req", entity.Branch_ID, Client_ID);

                                        RestResponse response = client.Execute(request);

                                        json = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
                                        await SaveActivityLogTracker("Wawerlite create send transaction API call response: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Wawerlite create transaction  response", entity.Branch_ID, Client_ID);




                                    }
                                    catch (Exception ex)
                                    {
                                        Message1 = ex.Message;

                                        await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Wawerlite Proceed Transaction 5th api call", entity.user_id, entity.Branch_ID, Client_ID, 0);

                                    }




                                    // Check transaction success or not
                                    try
                                    {





                                        options = new RestClientOptions(apiurl + "/1.0/payout/direct-deposit/transactions/verify")
                                        {
                                            MaxTimeout = -1
                                        };
                                        var client = new RestClient(options);
                                        var request = new RestRequest()
                                        {
                                            Method = Method.Post
                                        };



                                        request.AddHeader("Private-Key", privatekey);
                                        request.AddHeader("Content-Type", "application/json");
                                        body = @"{" + "\n" +
                                        @"    ""reference"": """ + Reference_Id + @"""" + "\n" +
                                        @"}";

                                        request.AddParameter("application/json", body, ParameterType.RequestBody);
                                        req = options + "/1.0/payout/direct-deposit/transactions/verify" + privatekey + body;
                                        await SaveActivityLogTracker("Wawerlite Verify transaction API call reqest: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Wawerlite Verify transaction req", entity.Branch_ID, Client_ID);

                                        RestResponse response = client.Execute(request);

                                        json = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
                                        await SaveActivityLogTracker("Wawerlite Verify transaction API call response: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Wawerlite Verify transaction  response", entity.Branch_ID, Client_ID);




                                        status = json.data.status;






                                    }
                                    catch (Exception ex)
                                    {
                                        await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Wawerlite Proceed Transaction 6th api call", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                        Message1 = ex.Message;
                                    }






                                    if (status == "SUCCESSFUL")
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
                                                    _TransactionRef = Reference_Id,
                                                    _trn_referenceNo = Reference_Id,
                                                    _APITransaction_Alert = 0,
                                                    _Transaction_ID = entity.Transaction_ID,
                                                    _Client_ID = entity.Client_ID,
                                                    _payout_partner_rate = AgentRateapi,
                                                };

                                                var rowsAffected = await _dbConnection.ExecuteAsync("Update_TransactionDetails", parameters, commandType: CommandType.StoredProcedure);

                                            }
                                            catch (Exception ex)
                                            {
                                                Message1 = ex.Message;
                                                await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                            }
                                        }






                                    }
                                }


                                else
                                {


                                    return new ProceedResponseViewModel
                                    {
                                        Status = "Failed",
                                        StatusCode = 2,
                                        Message = "Error Message: " + json.responseDescription,
                                        ApiId = api_id,
                                        AgentRate = AgentRateapi,
                                        ApiStatus = apistatus,
                                        ExtraFields = new List<string> { "", "" }
                                    };

                                }

                            }
                            else
                            {
                                await SaveActivityLogTracker("Wawerlite File is not created and publicly not  accessible at location.: <br/>" + publicHookUrl + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Wawerlite create transaction req", entity.Branch_ID, Client_ID);

                                Console.WriteLine($"Failed to access file publicly. HTTP status: {response5.StatusCode}");



                                return new ProceedResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = "Error Message: Failed to access file publicly.HTTP status" + response5.StatusCode,
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
                                Message = "Error Message: Insufficient balance ",
                                ApiId = api_id,
                                AgentRate = AgentRateapi,
                                ApiStatus = apistatus,
                                ExtraFields = new List<string> { "", "" }
                            };

                        }

                    }



                }
                catch (Exception ex)
                {
                    Message1 = ex.Message;
                    await SaveErrorLogAsync("Wawerlite Proceed Error: <br/>" + ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);

                }
                #endregion Waverlite
            }


            else if (api_id == 45) // Rushikesh
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
                                    Message1 = ex.Message;
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
                        Message1 = ex.Message;
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
                            Message1 = ex.Message;
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
                                if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Unauthorized)
                                {
                                    Message = jsonObject["message"].ToString();
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
                                else if(response.StatusCode != HttpStatusCode.BadRequest || response.StatusCode != HttpStatusCode.Unauthorized) {
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
                                                Message1 = ex.Message;
                                                await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Budpay Update transaction mapping table sp error", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                            }
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
                                Message1 = ex.Message;
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

            else if (api_id == 4600)//pradip NepalRemit
            {
                #region Nepal Remit
                string jsonResponse = "";
                JObject responseObject = null;
                int AmountInGBPAsInt = 0;
                try
                {
                    string Customer_ID = result.Customer_ID.ToString();
                    string locationId = "";
                    string Beneficiary_Name = "";
                    try
                    {
                        Beneficiary_Name = result.Beneficiary_Name.ToString();
                    }
                    catch (Exception ex) { }

                    string Account_Number = "";
                    string AmountInGBP = result.AmountInPKR.ToString();
                    AmountInGBPAsInt = Convert.ToInt32(Convert.ToDecimal(AmountInGBP));
                    string refer = result.ReferenceNo.ToString();

                    string senderFirstName = result.First_Name.ToString();
                    string senderMiddleName = result.Middle_Name.ToString();
                    string senderLastName = result.Last_Name.ToString();
                    string senderAddress = result.Country_Name.ToString();
                    string senderCity = result.City_Name.ToString();

                    string senderCountry = result.ISO_Code_Three.ToString();
                    string senderMobile = result.Mobile_Number.ToString();
                    string senderIdNumber = result.SenderID_Number.ToString();
                    string senderDateOfBirth = result.Sender_DOBmdy.ToString();
                    string senderIdExpireDate = result.SenderID_ExpiryDatemdy.ToString();
                    string senderIdIssueDate = result.Issue_Datemdy.ToString();
                    string senderIdIssueCountry = result.NISO_Code_Three.ToString();
                    string senderZipCode = result.Post_Code.ToString();

                    string receiverAddress = result.Beneficiary_Address.ToString();
                    string receiverContactNumber = result.Beneficiary_Mobile.ToString();
                    string receiverCity = result.Beneficiary_City.ToString();
                    string receiverCountry = result.BISO_Code_Three.ToString();
                    string payoutCurrency = result.Currency_Code.ToString();
                    string transferAmount = result.AmountInGBP.ToString();
                    int PaymentDepositType_ID = Convert.ToInt32(result.PaymentDepositType_ID);
                    string AccountHolderName = "";

                    try
                    {
                        if (result.AccountHolderName != null && result.AccountHolderName.ToString() != "")
                            AccountHolderName = result.AccountHolderName.ToString();
                        else if (result.Beneficiary_Name != null)
                            AccountHolderName = result.Beneficiary_Name.ToString();
                        else
                            AccountHolderName = "";
                    }
                    catch (Exception ex) { }
                    string[] nameParts = AccountHolderName.Split(' ');
                    string receiverFirstName = nameParts[0]; // First name before space
                    string receiverLastName = nameParts.Length > 1 ? nameParts[1] : "";

                    string senderGender = result.sendergender.ToString().ToLower();

                    string dateTime = "\"" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "\"";
                    string agentSessionId = ComputeSha256Hash(dateTime);
                    string senderNationality = "";
                    try
                    {
                        string WhereClause = "Country_Name = '" + result.Nationality.ToString() + "'";

                        var dttcmdSenderNationality = await _dbConnection.QueryAsync("Sp_GetCountryMaster",
                            new { WhereClause },
                            commandType: CommandType.StoredProcedure);

                        if (dttcmdSenderNationality.Any())
                        {
                            senderNationality = result.ISO_Code_Three.ToString();
                        }
                    }
                    catch (Exception ex) { }

                    string paymentMode = "";
                    if (PaymentDepositType_ID == 1) { paymentMode = "B"; locationId = result.bank_code.ToString(); Account_Number = result.Account_Number.ToString(); }
                    if (PaymentDepositType_ID == 2) { paymentMode = "C"; locationId = "CASHNPKA"; }
                    if (PaymentDepositType_ID == 3)
                    {
                        paymentMode = "W";
                        try
                        {
                            string Provider_name = result.Provider_name.ToString();
                            if (Provider_name == "KHALTI Digital N") { locationId = "KHALTI"; }
                            if (Provider_name == "IMEPAY") { locationId = "IMEPAY"; }
                            if (Provider_name == "CellPay") { locationId = "CELLPAY"; }

                            Account_Number = senderMobile;
                        }
                        catch (Exception ex) { }
                    }

                    var options = new RestClientOptions(apiurl + "/SendTransaction")
                    {
                        MaxTimeout = -1
                    };
                    var clientSendTransaction = new RestClient(options);
                    var requestSendTransaction = new RestRequest()
                    {
                        Method = Method.Post
                    };
                    requestSendTransaction.AddHeader("Authorization", accesscode);

                    var body = @"{
" + "\n" +
@"    ""agentSessionId"": """ + agentSessionId + @""",
" + "\n" +
@"    ""agentTxnId"": """ + refer + @""",
" + "\n" +
@"    ""locationId"": """ + locationId + @""",
" + "\n" +
@"    ""senderFirstName"": """ + senderFirstName + @""",
" + "\n" +
@"    ""senderMiddleName"": """ + senderMiddleName + @""",
" + "\n" +
@"    ""senderLastName"": """ + senderLastName + @""",
" + "\n" +
@"    ""senderGender"": """ + senderGender + @""",
" + "\n" +
@"    ""senderAddress"": """ + senderAddress + @" State-01"",
" + "\n" +
@"    ""senderCity"": """ + senderCity + @""",
" + "\n" +
@"    ""senderState"": ""East London"",
" + "\n" +
@"    ""senderZipCode"": """ + senderZipCode + @""",
" + "\n" +
@"    ""senderCountry"": """ + senderCountry + @""",
" + "\n" +
@"    ""senderMobile"": ""0" + senderMobile + @""",
" + "\n" +
@"    ""senderNationality"": """ + senderNationality + @""",
" + "\n" +
@"    ""senderIdType"": ""02"",
" + "\n" +
@"    ""senderIdNumber"": """ + senderIdNumber + @""",
" + "\n" +
@"    ""senderIdIssueCountry"": """ + senderIdIssueCountry + @""",
" + "\n" +
@"    ""senderIdIssueDate"": """ + senderIdIssueDate + @""",
" + "\n" +
@"    ""senderIdExpireDate"": """ + senderIdExpireDate + @""",
" + "\n" +
@"    ""senderDateOfBirth"": """ + senderDateOfBirth + @""",
" + "\n" +
@"    ""senderOccupation"": ""09"",
" + "\n" +
@"    ""senderSourceOfFund"": ""2"",
" + "\n" +
@"    ""senderSecondaryIdType"": null,
" + "\n" +
@"    ""senderSecondaryIdNumber"": null,
" + "\n" +
@"    ""senderEmail"": null,
" + "\n" +
@"    ""senderBeneficiaryRelationship"": ""19"",
" + "\n" +
@"    ""purposeOfRemittance"": ""18"",
" + "\n" +
@"    ""receiverFirstName"": """ + receiverFirstName + @""",
" + "\n" +
@"    ""receiverMiddleName"": """",
" + "\n" +
@"    ""receiverLastName"": """ + receiverLastName + @""",
" + "\n" +
@"    ""receiverAddress"": """ + receiverAddress + @""",
" + "\n" +
@"    ""receiverContactNumber"": """ + receiverContactNumber + @""",
" + "\n" +
@"    ""receiverCity"": """ + receiverCity + @""",
" + "\n" +
@"    ""receiverCountry"": """ + receiverCountry + @""",
" + "\n" +
@"    ""receiverIdType"": ""99"",
" + "\n" +
@"    ""receiverIdNumber"": null,
" + "\n" +
@"    ""calcBy"": ""P"",
" + "\n" +
@"    ""transferAmount"": """ + transferAmount + @""",
" + "\n" +
@"    ""remitCurrency"": """ + payoutCurrency + @""",
" + "\n" +
@"    ""payoutCurrency"": """ + payoutCurrency + @""",
" + "\n" +
@"    ""paymentMode"": """ + paymentMode + @""",
" + "\n" +
@"    ""bankName"": """",
" + "\n" +
@"    ""bankBranchName"": null,
" + "\n" +
@"    ""bankBranchCode"": null,
" + "\n" +
@"    ""bankAccountNumber"": """ + Account_Number + @""",
" + "\n" +
@"    ""promotionCode"": null,
" + "\n" +
@"    ""routePartner"": null,
" + "\n" +
@"    ""dynamicFields"": [    
" + "\n" +
@"    ]
" + "\n" +
@"}";

                    requestSendTransaction.AddParameter("application/json", body, ParameterType.RequestBody);

                    await SaveActivityLogTracker("NepalRemit Create Transaction Request: <br/>" + body + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Dynathopia Proceed", entity.Branch_ID, Client_ID);
                    RestResponse response = clientSendTransaction.Execute(requestSendTransaction);
                    await SaveActivityLogTracker("NepalRemit Create Transaction Response: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Dynathopia Proceed", entity.Branch_ID, Client_ID);

                    dynamic dJson = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
                    string code = dJson.code;
                    if (dJson.code == "0")
                    {
                        agentSessionId = dJson.agentSessionId;
                        string message = dJson.message;
                        string confirmationId = dJson.confirmationId;
                        string pinNumber = dJson.pinNumber;
                        string agentTxnId = dJson.agentTxnId;
                        string collectAmount = dJson.collectAmount;
                        string collectCurrency = dJson.collectCurrency;
                        string serviceCharge = dJson.serviceCharge;
                        string gstCharge = string.IsNullOrEmpty((string)dJson.gstCharge) ? "0" : dJson.gstCharge;
                        transferAmount = dJson.transferAmount;
                        string exchangeRate = dJson.exchangeRate;
                        string payoutAmount = dJson.payoutAmount;
                        payoutCurrency = dJson.payoutCurrency;
                        string feeDiscount = dJson.feeDiscount;
                        string additionalPremiumRate = dJson.additionalPremiumRate;
                        string txnDate = dJson.txnDate;
                        string settlementRate = dJson.settlementRate;
                        string sendCommission = dJson.sendCommission;
                        string settlementAmount = dJson.settlementAmount;

                        // CommitTransaction
                        if (confirmationId != null && confirmationId != "")
                        {
                            await SaveActivityLogTracker("NepalRemit CommitTransaction start confirmationId: < br /> " + confirmationId + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "NepalRemit Proceed", entity.Branch_ID, Client_ID);
                            var optionsCommitTransaction = new RestClientOptions(apiurl + "/CommitTransaction")
                            {
                                MaxTimeout = -1
                            };
                            var clientCommitTransaction = new RestClient(optionsCommitTransaction);
                            var requestCommitTransaction = new RestRequest()
                            {
                                Method = Method.Post
                            };
                            requestCommitTransaction.AddHeader("Authorization", accesscode);
                            body = @"{
" + "\n" +
@"    ""agentSessionId"": """ + agentSessionId + @""",
" + "\n" +
@"    ""confirmationId"": """ + confirmationId + @"""
" + "\n" +
@"}";
                            requestCommitTransaction.AddParameter("application/json", body, ParameterType.RequestBody);
                            await SaveActivityLogTracker("NepalRemit CommitTransaction Request  : < br /> " + confirmationId + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "NepalRemit Proceed", entity.Branch_ID, Client_ID);
                            RestResponse responseCommit = clientCommitTransaction.Execute(requestCommitTransaction);
                            await SaveActivityLogTracker("NepalRemit CommitTransaction response : < br /> " + responseCommit.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "NepalRemit Proceed", entity.Branch_ID, Client_ID);

                            dJson = null;
                            dJson = Newtonsoft.Json.JsonConvert.DeserializeObject(responseCommit.Content);
                            string codeCommit = dJson.code;
                            if (codeCommit == "0")
                            {
                                string APIBranch_Details = "";
                                apistatus = 0;

                                refer = Convert.ToString(result.ReferenceNo);
                                await SaveActivityLogTracker("Checking All values for sp  : <br/>" + api_id + " " + refer + " " + refer + " " + APIBranch_Details, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "NepalRemit update transactiom mapping values checking ", entity.Branch_ID, Client_ID);
                                int mappingid = Convert.ToInt32(result.TransMap_ID);
                                if (mappingid > 0)
                                {
                                    try
                                    { AgentRateapi = 0; }
                                    catch (Exception etx) { }

                                    try
                                    {
                                        var parameters = new
                                        {
                                            _BranchListAPI_ID = api_id,
                                            _APIBranch_Details = entity.APIBranch_Details,
                                            _TransactionRef = pinNumber,
                                            _trn_referenceNo = confirmationId,
                                            _APITransaction_Alert = 0,
                                            _Transaction_ID = entity.Transaction_ID,
                                            _Client_ID = entity.Client_ID,
                                            _payout_partner_rate = AgentRateapi,
                                        };

                                        var rowsAffected = await _dbConnection.ExecuteAsync("Update_TransactionDetails", parameters, commandType: CommandType.StoredProcedure);

                                    }
                                    catch (Exception ex)
                                    {
                                        Message1 = ex.Message;
                                        await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                    }
                                }
                            }

                        }
                    }

                }
                catch (Exception ex)
                {

                    return new ProceedResponseViewModel
                    {
                        Status = "fail",
                        StatusCode = 2,
                        Message = " Error Message: " + ex.Message.ToString(),
                        ApiId = Transaction_ID,
                        AgentRate = AgentRateapi,
                        ApiStatus = apistatus,
                        ExtraFields = new List<string> { "", "" }
                    };
                }
                #endregion Nepal Remit

            }
            else if (api_id == 2)
            {
                #region GCCRemit_Send_Transfer
                await SaveActivityLogTracker("GCC Transaction start: <br/>", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "GCC Proceed", entity.Branch_ID, Client_ID);

                try
                {
                    
                 
                    string bname = result.Beneficiary_Name.ToString();
                    string bfname = bname; string blname = ".";
                    if (bname.Contains(" "))
                    {
                        string[] spli = bname.Split(' ');
                        if (spli.Length > 1) { bfname = bname.Substring(0, (bname.Length - spli[spli.Length - 1].Length)); blname = spli[spli.Length - 1]; } //if (spli.Length > 1) { bfname = spli[0]; blname = spli[1]; }
                    }
                    string ph_no = "";
                
                    if (result.Mobile_Number.ToString() != "")
                    {
                        ph_no = result.Mobile_Number.ToString();
                    }
                    else if (result.Phone_Number.ToString() != "")
                    {
                        ph_no = result.Phone_Number.ToString();
                    }
                    string Bph_no = "";
                    if (result.Beneficiary_Mobile.ToString() != "")
                    {
                        Bph_no = result.Beneficiary_Mobile.ToString();
                    }
                   
                    int PaymentDepositType_ID = Convert.ToInt32(result.PaymentDepositType_ID);
                    int senderidtype = 1;
                  
                    if (result.ID_Name.ToString() == "Passport")
                        senderidtype = 2;
                    else if (result.ID_Name.ToString() == "Work Permit")
                        senderidtype = 3;
                    else if (result.ID_Name.ToString() == "Driving License")
                        senderidtype = 4;
                    else if (result.ID_Name.ToString() == "EU Nationality Card")
                        senderidtype = 7;

                    int sendTransferPurpose = 1;
                
                    if (!string.IsNullOrEmpty(result.Purpose_Code?.ToString()))
                    {
                        if (result.Purpose_Code.ToString() == "medical")
                            sendTransferPurpose = 2;
                        else if (result.Purpose_Code.ToString() == "Other purposes" || result.Purpose_Code.ToString() == "Holiday")
                            sendTransferPurpose = 3;
                        else if (result.Purpose_Code.ToString() == "Education loan repayment")
                            sendTransferPurpose = 4;
                        else if (result.Purpose_Code.ToString() == "saving")
                            sendTransferPurpose = 6;
                        else if (result.Purpose_Code.ToString() == "Investment")
                            sendTransferPurpose = 8;
                        else if (result.Purpose_Code.ToString() == "Other loan repayment")
                            sendTransferPurpose = 9;
                    }
                    else { sendTransferPurpose = 3; }
                   
                    string branchcodeData = entity.APIBranch_Details.ToString();
                    string[] words = branchcodeData.Split(' ');
                    string branchcode = "", branchcodeValue = "";
                    foreach (var word in words)
                    {
                        if (word.Length >= 5)
                        {
                            branchcode = word;
                            branchcodeValue = branchcode.Substring(0, 5);
                        }
                        else
                        {
                            branchcode = word;
                            branchcodeValue = branchcode; 
                        }
                        break;
                    }
               
                    string costRate = "";
                    string APITransaction_ID = result.APITransaction_ID.ToString();

                    if (result.APITransaction_ID.ToString() != "0" && result.APITransaction_ID.ToString() != "")
                    {
                        var options = new RestClientOptions()
                        {
                            MaxTimeout = -1
                        };
                        var client = new RestClient(options);
                        var request = new RestRequest()
                        {
                            Method = Method.Post
                        };
                        //client.Timeout = -1;
                        //var request = new RestRequest(Method.POST);
                        request.AddHeader("Content-Type", "text/xml; charset=utf-8");
                        request.AddHeader("SOAPAction", "http://tempuri.org/ISendAPI/ApproveTransfer");
                        var body = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:tem=""http://tempuri.org/"" xmlns:grem=""http://schemas.datacontract.org/2004/07/GRemitWCFService.Send"">" + "\n" +
                        @"   <soapenv:Header/>" + "\n" +
                        @"   <soapenv:Body>" + "\n" +
                        @"      <tem:ApproveTransfer>" + "\n" +
                        @"         <!--Optional:-->" + "\n" +
                        @"         <tem:req>" + "\n" +
                        @"            <!--Optional:-->" + "\n" +
                        @"            <grem:Password>" + apipass + "</grem:Password>" + "\n" +
                        @"            <!--Optional:-->" + "\n" +
                        @"            <grem:SecurityKey>" + accesscode + "</grem:SecurityKey>" + "\n" +
                        @"            <!--Optional:-->" + "\n" +
                        @"            <grem:TransactionNo>" + APITransaction_ID + "</grem:TransactionNo>" + "\n" +
                        @"            <!--Optional:-->" + "\n" +
                        @"            <grem:UniqueID>" + apiuser + "</grem:UniqueID>" + "\n" +
                        @"         </tem:req>" + "\n" +
                        @"      </tem:ApproveTransfer>" + "\n" +
                        @"   </soapenv:Body>" + "\n" +
                        @"</soapenv:Envelope>";

                        await SaveActivityLogTracker("Confirm gcc remittance request transaction number:" + body + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "GCC Proceed", entity.Branch_ID, Client_ID);


                        request.AddParameter("text/xml; charset=utf-8", body, ParameterType.RequestBody);
                        RestResponse response_ = client.Execute(request);
                        await SaveActivityLogTracker("Confirm gcc remittance response transaction number: <br/>" + response_.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "GCC Proceed", entity.Branch_ID, Client_ID);

                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(response_.Content);
                        XmlNodeList nodeList = xmlDoc.GetElementsByTagName("ApproveTransferResult");
                        string responseCode = "", messageResponse = "";
                        foreach (XmlNode node12 in nodeList)
                        {
                            string json2 = Newtonsoft.Json.JsonConvert.SerializeXmlNode(node12);
                            var obj12 = Newtonsoft.Json.Linq.JObject.Parse(json2);
                            messageResponse = Convert.ToString(obj12["ApproveTransferResult"]["a:ResponseMessage"]);
                            responseCode = Convert.ToString(obj12["ApproveTransferResult"]["a:ResponseCode"]);
                            break;
                        }
                        if (responseCode == "001")
                        {
                            await SaveActivityLogTracker("Confirm gcc remittance response transaction number in responseCode: <br/>" + response_.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "GCC Proceed", entity.Branch_ID, Client_ID);

                            string APIBranch_Details = entity.APIBranch_Details.ToString();
                            apistatus = 0;
                            string trn_referenceNo = result.APITransaction_ID.ToString();


                            #region gccrebateamount

                            options = new RestClientOptions(apiurl)
                            {
                                MaxTimeout = -1
                            };
                            client = new RestClient(options);
                            request = new RestRequest()
                            {
                                Method = Method.Post
                            };

                            request.AddHeader("Content-Type", "text/xml; charset=utf-8");
                            string transaction_date = result.transaction_date.ToString();
                            string PayinRebateShareAmount = "0";
                            request.AddHeader("SOAPAction", "http://tempuri.org/ISendAPI/GetTransferFinancialsList");
                            var bodyrebate = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:tem=""http://tempuri.org/"" xmlns:grem=""http://schemas.datacontract.org/2004/07/GRemitWCFService.Send"">" + "\n" +
                                        @"   <soapenv:Header/>" + "\n" +
                                        @"   <soapenv:Body>" + "\n" +
                                        @"      <tem:GetTransferFinancialsList>" + "\n" +
                                        @"         <tem:req>" + "\n" +
                                        @"<grem:EndDate></grem:EndDate>" + "\n" +
                                        @"            <grem:Password>" + apipass + "</grem:Password>" + "\n" +
                                        @"            <grem:SecurityKey>" + accesscode + "</grem:SecurityKey>" + "\n" +
                                        @"            <grem:StartDate></grem:StartDate>" + "\n" +
                                        @"            <grem:TransactionNo>" + trn_referenceNo + "</grem:TransactionNo>" + "\n" +
                                        @"            <grem:UniqueID>" + apiuser + "</grem:UniqueID>" + "\n" +
                                        @"         </tem:req>" + "\n" +
                                        @"      </tem:GetTransferFinancialsList>" + "\n" +
                                        @"   </soapenv:Body>" + "\n" +
                                        @"</soapenv:Envelope>" + "\n" +
                                        @"";
                            request.AddParameter("text/xml", bodyrebate, ParameterType.RequestBody);
                            await SaveActivityLogTracker("Backofc GetTransferFinancialsList request transaction number:" + bodyrebate + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "GCC Proceed", entity.Branch_ID, Client_ID);

                            RestResponse responserebate = client.Execute(request);
                            await SaveActivityLogTracker("Backofc GetTransferFinancialsList response transaction number:" + responserebate.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "GCC Proceed", entity.Branch_ID, Client_ID);

                          
                            XmlDocument xmlDoc_rebate = new XmlDocument();
                            xmlDoc_rebate = new XmlDocument();
                            xmlDoc_rebate.LoadXml(responserebate.Content);
                            double payoutpartnerrate = 0;
                            double payoutpartnercommission = 0;
                            double payoutpartnerRebate_Amt = 0;
                            double payoutpartnerRebate_calculatedAmt = 0;
                            double partnerpayamount = Convert.ToDouble(result.payout_pay_amt);
                            string rebateType = "", payinCurrencyForRebate = "";
                            XmlNodeList nodeList_Rebate = xmlDoc_rebate.GetElementsByTagName("dtTransferFinancialsList");
                            foreach (XmlNode node12 in nodeList_Rebate)
                            {
                                string json2 = Newtonsoft.Json.JsonConvert.SerializeXmlNode(node12);
                                var obj12 = Newtonsoft.Json.Linq.JObject.Parse(json2);
                                payoutpartnerrate = Convert.ToDouble(obj12["dtTransferFinancialsList"]["CostRate"]);
                                payoutpartnercommission = Convert.ToDouble(obj12["dtTransferFinancialsList"]["Commission"]);
                                string payoutpartnerrebate_type = rebateType = Convert.ToString(obj12["dtTransferFinancialsList"]["PayinRebateShareType"]);
                                string payoutpartnerRebate_currency = payinCurrencyForRebate = Convert.ToString(obj12["dtTransferFinancialsList"]["PayinCurrency"]);
                                Double payinAmount = Convert.ToDouble(obj12["dtTransferFinancialsList"]["PayinAmount"]);


                                string fromCurrencyCode = result.FromCurrency_Code.ToString();
                                string toCurrencyCode = result.Currency_Code.ToString();
                                string rabateSetupCurrency = "";
                                if (api_fields != "" && api_fields != null)
                                {
                                    Newtonsoft.Json.Linq.JObject obj = Newtonsoft.Json.Linq.JObject.Parse(api_fields);
                                    try
                                    {
                                        rabateSetupCurrency = Convert.ToString(obj["rebateSetupCurrency"]);
                                    }
                                    catch (Exception ex)
                                    {
                                        rabateSetupCurrency = "";
                                    }
                                }

                                if (rebateType == "Fixed")
                                {
                                    payoutpartnerRebate_Amt = Convert.ToDouble(obj12["dtTransferFinancialsList"]["PayinRebateShareAmount"]);
                                    Double rebateCalculatedAmt = 0;

                                    if (rabateSetupCurrency == payinCurrencyForRebate)
                                    {
                                        rebateCalculatedAmt = payoutpartnerRebate_Amt;
                                    }
                                    else
                                    {
                                        #region rebateGBPrate

                                        options = new RestClientOptions(apiurl)
                                        {
                                            MaxTimeout = -1
                                        };
                                        client = new RestClient(options);
                                        request = new RestRequest()
                                        {
                                            Method = Method.Post
                                        };

                                        request.AddHeader("Content-Type", "text/xml; charset=utf-8");
                                        request.AddHeader("SOAPAction", "http://tempuri.org/ISendAPI/GetExchangeRateList");
                                        body = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:tem=""http://tempuri.org/"" xmlns:grem=""http://schemas.datacontract.org/2004/07/GRemitWCFService.Send"">" + "\n" +
                                        @"   <soapenv:Header/>" + "\n" +
                                        @"   <soapenv:Body>" + "\n" +
                                        @"      <tem:GetExchangeRateList>" + "\n" +
                                        @"         <!--Optional:-->" + "\n" +
                                        @"         <tem:req>" + "\n" +
                                        @"            <!--Optional:-->" + "\n" +
                                        @"            <grem:Password>" + apipass + "</grem:Password>" + "\n" +
                                        @"            <!--Optional:-->" + "\n" +
                                        @"            <grem:PaymentModeType></grem:PaymentModeType>" + "\n" +
                                        @"            <!--Optional:-->" + "\n" +
                                        @"            <grem:PayoutCountryCode>" + "" + "</grem:PayoutCountryCode>" + "\n" +
                                        @"            <!--Optional:-->" + "\n" +
                                        @"            <grem:PayoutCurrencyCode>" + "GBP" + "</grem:PayoutCurrencyCode>" + "\n" +
                                        @"            <!--Optional:-->" + "\n" +
                                        @"            <grem:SecurityKey>" + accesscode + "</grem:SecurityKey>" + "\n" +
                                        @"            <!--Optional:-->" + "\n" +
                                        @"            <grem:UniqueID>" + apiuser + "</grem:UniqueID>" + "\n" +
                                        @"         </tem:req>" + "\n" +
                                        @"      </tem:GetExchangeRateList>" + "\n" +
                                        @"   </soapenv:Body>" + "\n" +
                                        @"</soapenv:Envelope>";
                                        request.AddParameter("text/xml", body, ParameterType.RequestBody);
                                        await SaveActivityLogTracker("Transaction proceed Confirm gcc remittance request parameters for GetExchangeRateList :" + body + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "GCC Proceed", entity.Branch_ID, Client_ID);

                                        RestResponse response = client.Execute(request);
                                        await SaveActivityLogTracker("Transaction proceed Confirm gcc remittance response parameters for GetExchangeRateList :" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "GCC Proceed", entity.Branch_ID, Client_ID);

                                        xmlDoc = new XmlDocument();
                                        xmlDoc.LoadXml(response.Content);
                                        nodeList = xmlDoc.GetElementsByTagName("Table");

                                        double AgentRate = 0;
                                        foreach (XmlNode node1 in nodeList)
                                        {
                                            string json = Newtonsoft.Json.JsonConvert.SerializeXmlNode(node1);

                                            var obj1 = Newtonsoft.Json.Linq.JObject.Parse(json);
                                            Double costRateGCC = Convert.ToDouble(obj1["Table"]["CostRate"]);
                                            string paymentMode = Convert.ToString(obj1["Table"]["PaymentMode"]).Trim();
                                            if ((paymentMode == "Cash Pickup" || paymentMode.Contains("Cash To Home")) && PaymentDepositType_ID == 2)
                                            {
                                                AgentRate = costRateGCC; break;
                                            }
                                            else if ((paymentMode.Contains("RTGS/NEFT") || paymentMode.Contains("IMPS") || paymentMode.Contains("Credit To Account") || paymentMode.Contains("Instant Credit To Account")) && PaymentDepositType_ID == 1)
                                            {
                                                AgentRate = costRateGCC; break;
                                            }
                                            else if (paymentMode.Contains("Mobile Wallet") && PaymentDepositType_ID == 3)
                                            {
                                                AgentRate = costRateGCC; break;
                                            }
                                            else
                                            {
                                                AgentRate = 0;
                                            }
                                        }

                                        if (AgentRate == 0)
                                        {
                                            foreach (XmlNode node1 in nodeList)
                                            {
                                                string json = Newtonsoft.Json.JsonConvert.SerializeXmlNode(node1);

                                                var obj1 = Newtonsoft.Json.Linq.JObject.Parse(json);
                                                Double costRateGCC = Convert.ToDouble(obj1["Table"]["CostRate"]);
                                                AgentRate = costRateGCC; break;
                                            }
                                        }

                                        rebateCalculatedAmt = (payoutpartnerRebate_Amt * AgentRate);
                                        #endregion
                                    }

                                    payoutpartnerRebate_calculatedAmt = rebateCalculatedAmt;
                                }
                                else if (rebateType == "Percentage")
                                {
                                    Double rebatePercentage = payoutpartnerRebate_Amt = Convert.ToDouble(obj12["dtTransferFinancialsList"]["PayinRebateShareAmount"]);
                                    Double rebateCalculatedAmt = 0;

                                    if (rabateSetupCurrency == payinCurrencyForRebate)
                                    {
                                        rebateCalculatedAmt = (payinAmount * rebatePercentage) / 100;
                                    }
                                    else
                                    {
                                        rebateCalculatedAmt = (payinAmount * rebatePercentage) / 100;
                                        #region rebateGBPrate For Percentage
                                        options = new RestClientOptions(apiurl)
                                        {
                                            MaxTimeout = -1
                                        };
                                        client = new RestClient(options);
                                        request = new RestRequest()
                                        {
                                            Method = Method.Post
                                        };

                                        request.AddHeader("Content-Type", "text/xml; charset=utf-8");
                                        request.AddHeader("SOAPAction", "http://tempuri.org/ISendAPI/GetExchangeRateList");
                                        body = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:tem=""http://tempuri.org/"" xmlns:grem=""http://schemas.datacontract.org/2004/07/GRemitWCFService.Send"">" + "\n" +
                                        @"   <soapenv:Header/>" + "\n" +
                                        @"   <soapenv:Body>" + "\n" +
                                        @"      <tem:GetExchangeRateList>" + "\n" +
                                        @"         <!--Optional:-->" + "\n" +
                                        @"         <tem:req>" + "\n" +
                                        @"            <!--Optional:-->" + "\n" +
                                        @"            <grem:Password>" + apipass + "</grem:Password>" + "\n" +
                                        @"            <!--Optional:-->" + "\n" +
                                        @"            <grem:PaymentModeType></grem:PaymentModeType>" + "\n" +
                                        @"            <!--Optional:-->" + "\n" +
                                        @"            <grem:PayoutCountryCode>" + "" + "</grem:PayoutCountryCode>" + "\n" +
                                        @"            <!--Optional:-->" + "\n" +
                                        @"            <grem:PayoutCurrencyCode>" + "GBP" + "</grem:PayoutCurrencyCode>" + "\n" +
                                        @"            <!--Optional:-->" + "\n" +
                                        @"            <grem:SecurityKey>" + accesscode + "</grem:SecurityKey>" + "\n" +
                                        @"            <!--Optional:-->" + "\n" +
                                        @"            <grem:UniqueID>" + apiuser + "</grem:UniqueID>" + "\n" +
                                        @"         </tem:req>" + "\n" +
                                        @"      </tem:GetExchangeRateList>" + "\n" +
                                        @"   </soapenv:Body>" + "\n" +
                                        @"</soapenv:Envelope>";
                                        request.AddParameter("text/xml", body, ParameterType.RequestBody);
                                        await SaveActivityLogTracker("Transaction proceed Confirm gcc remittance request parameters for GetExchangeRateList :" + body + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "GCC Proceed", entity.Branch_ID, Client_ID);

                                        RestResponse response = client.Execute(request);
                                        await SaveActivityLogTracker("Transaction proceed Confirm gcc remittance response parameters for GetExchangeRateList :" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "GCC Proceed", entity.Branch_ID, Client_ID);

                                        xmlDoc = new XmlDocument();
                                        xmlDoc.LoadXml(response.Content);
                                        nodeList = xmlDoc.GetElementsByTagName("Table");

                                        double AgentRate = 0;
                                        foreach (XmlNode node1 in nodeList)
                                        {
                                            string json = Newtonsoft.Json.JsonConvert.SerializeXmlNode(node1);

                                            var obj1 = Newtonsoft.Json.Linq.JObject.Parse(json);
                                            Double costRateGCC = Convert.ToDouble(obj1["Table"]["CostRate"]);
                                            string paymentMode = Convert.ToString(obj1["Table"]["PaymentMode"]).Trim();
                                            if ((paymentMode == "Cash Pickup" || paymentMode.Contains("Cash To Home")) && PaymentDepositType_ID == 2)
                                            {
                                                AgentRate = costRateGCC; break;
                                            }
                                            else if ((paymentMode.Contains("RTGS/NEFT") || paymentMode.Contains("IMPS") || paymentMode.Contains("Credit To Account") || paymentMode.Contains("Instant Credit To Account")) && PaymentDepositType_ID == 1)
                                            {
                                                AgentRate = costRateGCC; break;
                                            }
                                            else if (paymentMode.Contains("Mobile Wallet") && PaymentDepositType_ID == 3)
                                            {
                                                AgentRate = costRateGCC; break;
                                            }
                                            else
                                            {
                                                AgentRate = 0;
                                            }
                                        }

                                        if (AgentRate == 0)
                                        {
                                            foreach (XmlNode node1 in nodeList)
                                            {
                                                string json = Newtonsoft.Json.JsonConvert.SerializeXmlNode(node1);

                                                var obj1 = Newtonsoft.Json.Linq.JObject.Parse(json);
                                                Double costRateGCC = Convert.ToDouble(obj1["Table"]["CostRate"]);
                                                AgentRate = costRateGCC; break;
                                            }
                                        }

                                        rebateCalculatedAmt = rebateCalculatedAmt * AgentRate;
                                        #endregion
                                    }
                                    payoutpartnerRebate_calculatedAmt = rebateCalculatedAmt;
                                }

                                break;
                            }
                            #endregion gccrebateamount

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
                                    var parameters = new
                                    {
                                        _BranchListAPI_ID = api_id,
                                        _APIBranch_Details = entity.APIBranch_Details,
                                        _TransactionRef = result.APITransaction_ID.ToString(),
                                        _trn_referenceNo = result.APITransaction_ID.ToString(),
                                        _APITransaction_Alert = 0,
                                        _Transaction_ID = entity.Transaction_ID,
                                        _Client_ID = entity.Client_ID,
                                        _payout_partner_rate = AgentRateapi,
                                    };

                                    var rowsAffected = await _dbConnection.ExecuteAsync("Update_TransactionDetails", parameters, commandType: CommandType.StoredProcedure);

                                }
                                catch (Exception ex)
                                {
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
                                Message = messageResponse + " Error Message: " + responseCode,
                                ApiId = api_id,
                                AgentRate = AgentRateapi,
                                ApiStatus = apistatus,
                                ExtraFields = new List<string> { "", "" }
                            };

                        }

                    }
                    else
                    {
                        // Get rate from GCC
                        var options = new RestClientOptions(apiurl)
                        {
                            MaxTimeout = -1
                        };
                        var client = new RestClient(options);
                        var request = new RestRequest()
                        {
                            Method = Method.Post
                        };
                        RestResponse response = new RestResponse();
                        string Currency_Code = result.Currency_Code.ToString();
                   
                        request.AddHeader("Content-Type", "text/xml; charset=utf-8");
                        request.AddHeader("SOAPAction", "http://tempuri.org/ISendAPI/GetExchangeRateList");
                        var body = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:tem=""http://tempuri.org/"" xmlns:grem=""http://schemas.datacontract.org/2004/07/GRemitWCFService.Send"">" + "\n" +
                        @"   <soapenv:Header/>" + "\n" +
                        @"   <soapenv:Body>" + "\n" +
                        @"      <tem:GetExchangeRateList>" + "\n" +
                        @"         <!--Optional:-->" + "\n" +
                        @"         <tem:req>" + "\n" +
                        @"            <!--Optional:-->" + "\n" +
                        @"            <grem:Password>" + apipass + "</grem:Password>" + "\n" +
                        @"            <!--Optional:-->" + "\n" +
                        @"            <grem:PaymentModeType></grem:PaymentModeType>" + "\n" +
                        @"            <!--Optional:-->" + "\n" +
                        @"            <grem:PayoutCountryCode></grem:PayoutCountryCode>" + "\n" +
                        @"            <!--Optional:-->" + "\n" +
                        @"            <grem:PayoutCurrencyCode>" + Currency_Code + "</grem:PayoutCurrencyCode>" + "\n" +
                        @"            <!--Optional:-->" + "\n" +
                        @"            <grem:SecurityKey>" + accesscode + "</grem:SecurityKey>" + "\n" +
                        @"            <!--Optional:-->" + "\n" +
                        @"            <grem:UniqueID>" + apiuser + "</grem:UniqueID>" + "\n" +
                        @"         </tem:req>" + "\n" +
                        @"      </tem:GetExchangeRateList>" + "\n" +
                        @"   </soapenv:Body>" + "\n" +
                        @"</soapenv:Envelope>";
                        try
                        {
                            request.AddParameter("text/xml", body, ParameterType.RequestBody);
                            await SaveActivityLogTracker("GCC Remit send transfer get rate request :" + body + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "GCC Proceed", entity.Branch_ID, Client_ID);

                            response = client.Execute(request);
                            await SaveActivityLogTracker("GCC Remit send transfer get rate response :" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "GCC Proceed", entity.Branch_ID, Client_ID);

                        }
                        catch (Exception esd) { }
                        Console.WriteLine(response.Content);
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(response.Content);
                        XmlNodeList nodeList = xmlDoc.GetElementsByTagName("Table");


                        // This request for Get Rates******************
                        
                        var request_ = new RestRequest()
                        {
                            Method = Method.Post
                        };

                        request_.AddHeader("Content-Type", "text/xml; charset=utf-8");
                        request_.AddHeader("SOAPAction", "http://tempuri.org/ISendAPI/GetExchangeRate");
                        var body_ = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:tem=""http://tempuri.org/"" xmlns:grem=""http://schemas.datacontract.org/2004/07/GRemitWCFService.Send"">" + "\n" +
                        @"   <soapenv:Header/>" + "\n" +
                        @"   <soapenv:Body>" + "\n" +
                        @"      <tem:GetExchangeRate>" + "\n" +
                        @"         <!--Optional:-->" + "\n" +
                        @"         <tem:req>" + "\n" +
                        @"            <!--Optional:-->" + "\n" +
                        @"            <grem:Password>" + apipass + "</grem:Password>" + "\n" +
                        @"            <!--Optional:-->" + "\n" +
                        @"            <grem:PayoutBranchCode>" + branchcode + "</grem:PayoutBranchCode>" + "\n" +
                        @"            <!--Optional:-->" + "\n" +
                        @"            <grem:SecurityKey>" + accesscode + "</grem:SecurityKey>" + "\n" +
                        @"            <!--Optional:-->" + "\n" +
                        @"            <grem:UniqueID>" + apiuser + "</grem:UniqueID>" + "\n" +
                        @"         </tem:req>" + "\n" +
                        @"      </tem:GetExchangeRate>" + "\n" +
                        @"   </soapenv:Body>" + "\n" +
                        @"</soapenv:Envelope>";
                        request_.AddParameter("text/xml", body_, ParameterType.RequestBody);
                        await SaveActivityLogTracker("GCC Remit send transfer get rate request_ :" + body_ + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "GCC Proceed", entity.Branch_ID, Client_ID);

                        RestResponse response_rate = client.Execute(request_);
                        await SaveActivityLogTracker("GCC Remit send transfer get rate response_rate :" + response_rate.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "GCC Proceed", entity.Branch_ID, Client_ID);

                        try
                        {
                            XmlDocument xmlDoc_ = new XmlDocument();
                            xmlDoc_ = new XmlDocument();
                            xmlDoc_.LoadXml(response_rate.Content);
                            XmlNodeList nodeList_ = xmlDoc_.GetElementsByTagName("GetExchangeRateResult");
                            foreach (XmlNode node12 in nodeList_)
                            {
                                string json2 = Newtonsoft.Json.JsonConvert.SerializeXmlNode(node12);
                                var obj12 = Newtonsoft.Json.Linq.JObject.Parse(json2);
                                costRate = Convert.ToString(obj12["GetExchangeRateResult"]["a:CostRate"]);
                                break;
                            }

                        }
                        catch (Exception EXT) { }

                        string formattedAmountInPKR = "0";

                        try
                        {
                            double amountInPKR = Convert.ToDouble(result.AmountInPKR);
                            double rate = 0;
                            if (result != null &&
         result.AmountInPKR != null &&
         double.TryParse(result.AmountInPKR.ToString(), out amountInPKR) &&
         double.TryParse(costRate.ToString(), out rate) &&
         rate != 0)
                            {
                                formattedAmountInPKR = string.Format("{0:0.##}", amountInPKR / rate);
                            }
                        }
                        catch (Exception ex)
                        {
                            formattedAmountInPKR = "0";
                        }
                        string AvailableBalance = Gcc_getAvailableBalance(apiurl, apipass, accesscode, apiuser);
                        if (Convert.ToDouble(AvailableBalance) >= Convert.ToDouble(formattedAmountInPKR))
                        {
                            string payMethod_type = "";
                            if (PaymentDepositType_ID == 1)
                            {
                                payMethod_type = "Bank";
                            }
                            else if (PaymentDepositType_ID == 2)
                            {
                                payMethod_type = "Pickup";
                            }
                            else if (PaymentDepositType_ID == 3)
                            {
                                payMethod_type = "Wallet";
                            }
                            client = new RestClient(apiurl);
                            string Bankdetails = "";
                            string Beneficiary_Address = "";
                            string Ifsc_Code = "";
                            if (PaymentDepositType_ID == 1)
                            {
                                if (String.IsNullOrEmpty(Convert.ToString(result.Beneficiary_Address.ToString())))
                                {
                                    Beneficiary_Address = result.Beneficiary_Address.ToString().Trim();
                                }
                                else { Beneficiary_Address = result.Beneficiary_Address.ToString().Trim(); }

                                string bank_ac_no = "";
                                if (result.Iban_ID.ToString().Trim() != "")
                                {
                                    if (result.Iban_ID.ToString().Trim() != result.benf_ISO_Code.ToString().Trim())
                                        bank_ac_no = result.Iban_ID.ToString();
                                }
                                if (result.Account_Number.ToString().Trim() != "" && bank_ac_no == "")
                                {
                                    bank_ac_no = result.Account_Number.ToString().Trim();
                                }
                                if (result.Ifsc_Code.ToString().Trim() == "")
                                {
                                    Ifsc_Code = result.bic_code.ToString();
                                }
                                else { Ifsc_Code = result.Ifsc_Code.ToString().Trim(); }

                                Bankdetails = @"<grem:BankAccountNo>" + bank_ac_no + "</grem:BankAccountNo>" + "\n" +
                            @"<grem:BankAddress>" + "" + "</grem:BankAddress>" + "\n" +
                            @"<grem:BankBranchCode>" + Ifsc_Code + "</grem:BankBranchCode>" + "\n" +
                            @"<grem:BankBranchName>" + result.Bank_Name.ToString() + "</grem:BankBranchName>" + "\n" +
                            @"<grem:BankCity></grem:BankCity>" + "\n" +
                            @"<grem:BankCountry>" + result.benf_ISO_Code.ToString() + "</grem:BankCountry>" + "\n" +
                            @"<grem:BankName>" + result.Bank_Name.ToString() + "</grem:BankName>" + "\n" +
                            @"<grem:BankState></grem:BankState>" + "\n" +
                            @"<grem:BankZipCode></grem:BankZipCode>" + "\n";
                            }
                            if (PaymentDepositType_ID == 3)
                            {
                                if (result.benf_ISO_Code.ToString().Trim() == "PH")
                                {
                                    Bph_no = Bph_no.Substring(Bph_no.Length - 11, 11);
                                }

                                Bankdetails = @"<grem:BankAccountNo>" + Bph_no + "</grem:BankAccountNo>" + "\n" +
                            @"<grem:BankAddress>" + "" + "</grem:BankAddress>" + "\n" +
                            @"<grem:BankBranchCode>" + Convert.ToString("") + "</grem:BankBranchCode>" + "\n" +
                            @"<grem:BankBranchName>" + Convert.ToString("") + "</grem:BankBranchName>" + "\n" +
                            @"<grem:BankCity></grem:BankCity>" + "\n" +
                            @"<grem:BankCountry>" + Convert.ToString("") + "</grem:BankCountry>" + "\n" +
                            @"<grem:BankName>" + Convert.ToString("") + "</grem:BankName>" + "\n" +
                            @"<grem:BankState></grem:BankState>" + "\n" +
                            @"<grem:BankZipCode></grem:BankZipCode>" + "\n";
                            }

                            string relationwithBen = "OTHERS";

                            string ISO_Code = "";
                            string PayinAmount = "";
                            string benef_DOB_ymd = "";
                            string AmountInPKR = "";
                            string benf_ISO_Code = "";
                            string ReferenceNo = "";
                            string sender_address = "";
                            string Sender_DateOfBirth = "";
                            string Email_ID = "";
                            string First_Name = "";
                            string SenderID_ExpiryDate = "";
                            string SenderId_Number = "";
                            string Last_Name = "";
                            string Middle_Name = "";
                            string sendercountrycode = "";
                            string Post_Code = "";
                            try
                            {
                                ISO_Code = result.ISO_Code.ToString();
                                benef_DOB_ymd = result.benef_DOB_ymd.ToString();
                                AmountInPKR = result.AmountInPKR.ToString();
                                benf_ISO_Code = result.benf_ISO_Code.ToString();
                                ReferenceNo = result.ReferenceNo.ToString();
                                sender_address = result.sender_address.ToString();
                                Sender_DateOfBirth = result.Sender_DateOfBirth.ToString();
                                Email_ID = result.Email_ID.ToString();
                                First_Name = result.First_Name.ToString();
                                SenderID_ExpiryDate = result.SenderID_ExpiryDate.ToString();
                                SenderId_Number = result.SenderID_Number.ToString();
                                Last_Name = result.Last_Name.ToString();
                                Middle_Name = result.Middle_Name.ToString();
                                sendercountrycode = result.sendercountrycode.ToString();
                                Post_Code = result.Post_Code.ToString();
                                PayinAmount = String.Format("{0:0.##}", (Convert.ToDouble(result.AmountInPKR) / Convert.ToDouble(costRate)));

                            }
                            catch (Exception ex)
                            {
                                Message1 = ex.Message;
                                await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                            }
                          

                            options = new RestClientOptions(apiurl)
                            {
                                MaxTimeout = -1
                            };
                            client = new RestClient(options);
                            request = new RestRequest()
                            {
                                Method = Method.Post
                            };
                            
                            request.AddHeader("Content-Type", "text/xml; charset=utf-8");
                            request.AddHeader("SOAPAction", "http://tempuri.org/ISendAPI/SendTransfer");
                            body = @"<?xml version=""1.0"" encoding=""utf-8""?>" + "\n" +
                            @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:tem=""http://tempuri.org/"" xmlns:grem=""http://schemas.datacontract.org/2004/07/GRemitWCFService.Send"">" + "\n" +
                            @"   <soapenv:Header/>" + "\n" +
                            @"" + "\n" +
                            @"<soapenv:Body>" + "\n" +
                            @"<tem:SendTransfer>" + "\n" +
                            @"<tem:req>" + "\n" +

                            Bankdetails +
                            @"<grem:CostRate> " + costRate + " </grem:CostRate>" + "\n" +
                            @"<grem:IncomeSourceCode>2</grem:IncomeSourceCode>" + "\n" +
                            @"<grem:OriginCountryCode>" + ISO_Code + "</grem:OriginCountryCode>" + "\n" +
                            @"<grem:Password>" + apipass + "</grem:Password>" + "\n" +
                            @"<grem:PayinAmount>" + PayinAmount + "</grem:PayinAmount>" + "\n" +
                            @"<grem:PayoutAmount>" + AmountInPKR + "</grem:PayoutAmount>" + "\n" +
                            @"<grem:PayoutBranchCode>" + branchcode + "</grem:PayoutBranchCode>" + "\n" +
                            @"<grem:PayoutCurrencyCode>" + Currency_Code + "</grem:PayoutCurrencyCode>" + "\n" +
                            @"<grem:PurposeCode>" + sendTransferPurpose + "</grem:PurposeCode>" + "\n" +

                            @"<grem:ReceiverAddress> " + Beneficiary_Address + "</grem:ReceiverAddress>" + "\n" +
                            @"<grem:ReceiverDOB>" + benef_DOB_ymd + "</grem:ReceiverDOB>" + "\n" +
                            @"<grem:ReceiverFirstName>" + bfname + "</grem:ReceiverFirstName>" + "\n" +
                            @"<grem:ReceiverFourthName></grem:ReceiverFourthName>" + "\n" +
                            @"<grem:ReceiverLastName>" + blname + "</grem:ReceiverLastName>" + "\n" +
                            @"<grem:ReceiverMessage></grem:ReceiverMessage>" + "\n" +
                            @"<grem:ReceiverMiddleName></grem:ReceiverMiddleName>" + "\n" +
                            @"<grem:ReceiverMobileNo>" + Bph_no + "</grem:ReceiverMobileNo>" + "\n" +
                            @"<grem:ReceiverNationality>" + benf_ISO_Code + "</grem:ReceiverNationality>" + "\n" +
                            @"<grem:ReceiverRelationship>" + relationwithBen + "</grem:ReceiverRelationship>" + "\n" +
                            @"<grem:ReceiverTelephoneNo></grem:ReceiverTelephoneNo>" + "\n" +
                            @"<grem:ReceiverZipCode></grem:ReceiverZipCode>" + "\n" +

                            @"<grem:ReferenceNo>" + ReferenceNo + "</grem:ReferenceNo>" + "\n" +
                            @"<grem:SecurityKey>" + accesscode + "</grem:SecurityKey>" + "\n" +
                            @"<grem:SenderAddress>" + sender_address + "</grem:SenderAddress>" + "\n" +
                            @"<grem:SenderDOB>" + Sender_DateOfBirth + "</grem:SenderDOB>" + "\n" +
                            @"<grem:SenderEmail>" + Email_ID + "</grem:SenderEmail>" + "\n" +
                            @"<grem:SenderFirstName>" + First_Name + "</grem:SenderFirstName>" + "\n" +
                            @"<grem:SenderFourthName></grem:SenderFourthName>" + "\n" +
                            @"<grem:SenderIDExpiryDate>" + SenderID_ExpiryDate + "</grem:SenderIDExpiryDate>" + "\n" +
                            @"<grem:SenderIDNumber>" + SenderId_Number + "</grem:SenderIDNumber>" + "\n" +
                            @"<grem:SenderIDPlaceOfIssue>" + ISO_Code + "</grem:SenderIDPlaceOfIssue>" + "\n" +
                            @"<grem:SenderIDType>" + senderidtype + "</grem:SenderIDType>" + "\n" +
                            @"<grem:SenderLastName>" + Last_Name + "</grem:SenderLastName>" + "\n" +
                            @"<grem:SenderMiddleName>" + Middle_Name + "</grem:SenderMiddleName>" + "\n" +
                            @"<grem:SenderMobileNo>" + ph_no + "</grem:SenderMobileNo>" + "\n" +
                            @"<grem:SenderNationality>" + sendercountrycode + "</grem:SenderNationality>" + "\n" +
                            @"<grem:SenderTelephoneNo>" + ph_no + "</grem:SenderTelephoneNo>" + "\n" +
                            @"<grem:SenderZipCode>" + Post_Code + "</grem:SenderZipCode>" + "\n" +

                            @"<grem:TransactionNo></grem:TransactionNo>" + "\n" +
                            @"<grem:UniqueID>" + apiuser + "</grem:UniqueID>" + "\n" +
                            @"</tem:req>" + "\n" +
                            @"</tem:SendTransfer>" + "\n" +
                            @"</soapenv:Body>" + "\n" +
                            @"  " + "\n" +
                            @"</soapenv:Envelope>" + "\n" +
                            @"";
                            request.AddParameter("text/xml", body, ParameterType.RequestBody);
                            await SaveActivityLogTracker("Confirm gcc remittance All parameter send transfer request:" + body + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "GCC Proceed", entity.Branch_ID, Client_ID);

                            response = client.Execute(request);
                            await SaveActivityLogTracker("Confirm gcc remittance All parameter send transfer response:" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "GCC Proceed", entity.Branch_ID, Client_ID);


                            xmlDoc = new XmlDocument();

                            xmlDoc.LoadXml(response.Content);
                            nodeList = xmlDoc.GetElementsByTagName("SendTransferResult");
                            double partnerpayamount = 0;
                            string responseCode = "", messageResponse = "", resultTransactionNum = "";
                            foreach (XmlNode node1 in nodeList)
                            {
                                string json = Newtonsoft.Json.JsonConvert.SerializeXmlNode(node1);
                                var obj1 = Newtonsoft.Json.Linq.JObject.Parse(json);
                                messageResponse = Convert.ToString(obj1["SendTransferResult"]["a:ResponseMessage"]);
                                responseCode = Convert.ToString(obj1["SendTransferResult"]["a:ResponseCode"]);
                                resultTransactionNum = Convert.ToString(obj1["SendTransferResult"]["a:TransactionNo"]);
                                partnerpayamount = Convert.ToDouble(obj1["SendTransferResult"]["a:PayinAmount"]);
                            }

                            if (responseCode == "001")
                            {

                                options = new RestClientOptions(apiurl)
                                {
                                    MaxTimeout = -1
                                };
                                client = new RestClient(options);
                                request = new RestRequest()
                                {
                                    Method = Method.Post
                                };

                                
                                request.AddHeader("Content-Type", "text/xml; charset=utf-8");
                                request.AddHeader("SOAPAction", "http://tempuri.org/ISendAPI/ApproveTransfer");
                                body = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:tem=""http://tempuri.org/"" xmlns:grem=""http://schemas.datacontract.org/2004/07/GRemitWCFService.Send"">" + "\n" +
                                @"   <soapenv:Header/>" + "\n" +
                                @"   <soapenv:Body>" + "\n" +
                                @"      <tem:ApproveTransfer>" + "\n" +
                                @"         <!--Optional:-->" + "\n" +
                                @"         <tem:req>" + "\n" +
                                @"            <!--Optional:-->" + "\n" +
                                @"            <grem:Password>" + apipass + "</grem:Password>" + "\n" +
                                @"            <!--Optional:-->" + "\n" +
                                @"            <grem:SecurityKey>" + accesscode + "</grem:SecurityKey>" + "\n" +
                                @"            <!--Optional:-->" + "\n" +
                                @"            <grem:TransactionNo>" + resultTransactionNum + "</grem:TransactionNo>" + "\n" +
                                @"            <!--Optional:-->" + "\n" +
                                @"            <grem:UniqueID>" + apiuser + "</grem:UniqueID>" + "\n" +
                                @"         </tem:req>" + "\n" +
                                @"      </tem:ApproveTransfer>" + "\n" +
                                @"   </soapenv:Body>" + "\n" +
                                @"</soapenv:Envelope>";


                                request.AddParameter("text/xml", body, ParameterType.RequestBody);
                                await SaveActivityLogTracker("Confirm gcc remittance request transaction number:" + body + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "GCC Proceed", entity.Branch_ID, Client_ID);

                                RestResponse response_ = client.Execute(request);
                                await SaveActivityLogTracker("Confirm gcc remittance request transaction number:" + response_.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "GCC Proceed", entity.Branch_ID, Client_ID);

                                string APIBranch_Details = entity.APIBranch_Details.ToString();
                                apistatus = 0;
                                string trn_referenceNo = resultTransactionNum;
                                #region gccrebateamount
                                options = new RestClientOptions(apiurl)
                                {
                                    MaxTimeout = -1
                                };
                                client = new RestClient(options);
                                request = new RestRequest()
                                {
                                    Method = Method.Post
                                };


                                request.AddHeader("Content-Type", "text/xml; charset=utf-8");

                                string transaction_date = result.transaction_date.ToString();
                                string PayinRebateShareAmount = "0";
                                request.AddHeader("SOAPAction", "http://tempuri.org/ISendAPI/GetTransferFinancialsList");
                                var bodyrebate = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:tem=""http://tempuri.org/"" xmlns:grem=""http://schemas.datacontract.org/2004/07/GRemitWCFService.Send"">" + "\n" +
                                            @"   <soapenv:Header/>" + "\n" +
                                            @"   <soapenv:Body>" + "\n" +
                                            @"      <tem:GetTransferFinancialsList>" + "\n" +
                                            @"         <tem:req>" + "\n" +
                                            @"<grem:EndDate></grem:EndDate>" + "\n" +
                                            @"            <grem:Password>" + apipass + "</grem:Password>" + "\n" +
                                            @"            <grem:SecurityKey>" + accesscode + "</grem:SecurityKey>" + "\n" +
                                            @"            <grem:StartDate></grem:StartDate>" + "\n" +
                                            @"            <grem:TransactionNo>" + trn_referenceNo + "</grem:TransactionNo>" + "\n" +
                                            @"            <grem:UniqueID>" + apiuser + "</grem:UniqueID>" + "\n" +
                                            @"         </tem:req>" + "\n" +
                                            @"      </tem:GetTransferFinancialsList>" + "\n" +
                                            @"   </soapenv:Body>" + "\n" +
                                            @"</soapenv:Envelope>" + "\n" +
                                            @"";
                                request.AddParameter("text/xml", bodyrebate, ParameterType.RequestBody);
                                await SaveActivityLogTracker("Backofc GetTransferFinancialsList request transaction number:" + bodyrebate + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "GCC Proceed", entity.Branch_ID, Client_ID);

                                RestResponse responserebate = client.Execute(request);
                                await SaveActivityLogTracker("Backofc GetTransferFinancialsList response transaction number:" + responserebate.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "GCC Proceed", entity.Branch_ID, Client_ID);

                                XmlDocument xmlDoc_rebate = new XmlDocument();
                                xmlDoc_rebate = new XmlDocument();
                                xmlDoc_rebate.LoadXml(responserebate.Content);
                                double payoutpartnerrate = 0;
                                double payoutpartnercommission = 0;
                                double payoutpartnerRebate_Amt = 0; double payoutpartnerRebate_calculatedAmt = 0;
                                string rebateType = "", payinCurrencyForRebate = "";
                                string agentcommission_type = "";
                                string payoutpartnerrebate_type = "";
                                string payoutpartnerRebate_currency = "";
                                XmlNodeList nodeList_Rebate = xmlDoc_rebate.GetElementsByTagName("dtTransferFinancialsList");
                                double AgentRate = 0;
                                foreach (XmlNode node12 in nodeList_Rebate)
                                {
                                    string json2 = Newtonsoft.Json.JsonConvert.SerializeXmlNode(node12);
                                    var obj12 = Newtonsoft.Json.Linq.JObject.Parse(json2);
                                    payoutpartnerrate = Convert.ToDouble(obj12["dtTransferFinancialsList"]["CostRate"]);
                                    payoutpartnercommission = Convert.ToDouble(obj12["dtTransferFinancialsList"]["Commission"]);
                                    agentcommission_type = Convert.ToString(obj12["dtTransferFinancialsList"]["PayinCommShareType"]);
                                    payoutpartnerrebate_type = rebateType = Convert.ToString(obj12["dtTransferFinancialsList"]["PayinRebateShareType"]);
                                    payoutpartnerRebate_currency = payinCurrencyForRebate = Convert.ToString(obj12["dtTransferFinancialsList"]["PayinCurrency"]);
                                    Double payinAmount = Convert.ToDouble(obj12["dtTransferFinancialsList"]["PayinAmount"]);
                                    AgentRateapi = Convert.ToDouble(obj12["dtTransferFinancialsList"]["CostRate"]);


                                    string fromCurrencyCode = result.FromCurrency_Code.ToString();

                                    string toCurrencyCode = result.Currency_Code.ToString();
                                    string rabateSetupCurrency = "";
                                    if (api_fields != "" && api_fields != null)
                                    {
                                        Newtonsoft.Json.Linq.JObject obj = Newtonsoft.Json.Linq.JObject.Parse(api_fields);
                                        try
                                        {
                                            rabateSetupCurrency = Convert.ToString(obj["rebateSetupCurrency"]);
                                        }
                                        catch (Exception ex)
                                        {
                                            rabateSetupCurrency = "";
                                        }
                                    }

                                    if (rebateType == "Fixed")
                                    {
                                        payoutpartnerRebate_Amt = Convert.ToDouble(obj12["dtTransferFinancialsList"]["PayinRebateShareAmount"]);
                                        Double rebateCalculatedAmt = 0;

                                        if (rabateSetupCurrency == payinCurrencyForRebate)
                                        {
                                            rebateCalculatedAmt = payoutpartnerRebate_Amt;
                                        }
                                        else
                                        {
                                            #region rebateGBPrate

                                            options = new RestClientOptions(apiurl)
                                            {
                                                MaxTimeout = -1
                                            };
                                            client = new RestClient(options);
                                            request = new RestRequest()
                                            {
                                                Method = Method.Post
                                            };

                                            request.AddHeader("Content-Type", "text/xml");
                                            request.AddHeader("SOAPAction", "http://tempuri.org/ISendAPI/GetExchangeRateList");
                                            body = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:tem=""http://tempuri.org/"" xmlns:grem=""http://schemas.datacontract.org/2004/07/GRemitWCFService.Send"">" + "\n" +
                                            @"   <soapenv:Header/>" + "\n" +
                                            @"   <soapenv:Body>" + "\n" +
                                            @"      <tem:GetExchangeRateList>" + "\n" +
                                            @"         <!--Optional:-->" + "\n" +
                                            @"         <tem:req>" + "\n" +
                                            @"            <!--Optional:-->" + "\n" +
                                            @"            <grem:Password>" + apipass + "</grem:Password>" + "\n" +
                                            @"            <!--Optional:-->" + "\n" +
                                            @"            <grem:PaymentModeType></grem:PaymentModeType>" + "\n" +
                                            @"            <!--Optional:-->" + "\n" +
                                            @"            <grem:PayoutCountryCode>" + "" + "</grem:PayoutCountryCode>" + "\n" +
                                            @"            <!--Optional:-->" + "\n" +
                                            @"            <grem:PayoutCurrencyCode>" + "GBP" + "</grem:PayoutCurrencyCode>" + "\n" +
                                            @"            <!--Optional:-->" + "\n" +
                                            @"            <grem:SecurityKey>" + accesscode + "</grem:SecurityKey>" + "\n" +
                                            @"            <!--Optional:-->" + "\n" +
                                            @"            <grem:UniqueID>" + apiuser + "</grem:UniqueID>" + "\n" +
                                            @"         </tem:req>" + "\n" +
                                            @"      </tem:GetExchangeRateList>" + "\n" +
                                            @"   </soapenv:Body>" + "\n" +
                                            @"</soapenv:Envelope>";
                                            request.AddParameter("text/xml", body, ParameterType.RequestBody);
                                            await SaveActivityLogTracker("Transaction proceed Confirm gcc remittance request parameters for GetExchangeRateList :" + body + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "GCC Proceed", entity.Branch_ID, Client_ID);

                                            response = client.Execute(request);
                                            await SaveActivityLogTracker("Transaction proceed Confirm gcc remittance response parameters for GetExchangeRateList :" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "GCC Proceed", entity.Branch_ID, Client_ID);

                                            xmlDoc = new XmlDocument();
                                            xmlDoc.LoadXml(response.Content);
                                            nodeList = xmlDoc.GetElementsByTagName("Table");

                                            AgentRate = 0;
                                            foreach (XmlNode node1 in nodeList)
                                            {
                                                string json = Newtonsoft.Json.JsonConvert.SerializeXmlNode(node1);

                                                var obj1 = Newtonsoft.Json.Linq.JObject.Parse(json);
                                                Double costRateGCC = Convert.ToDouble(obj1["Table"]["CostRate"]);
                                                string paymentMode = Convert.ToString(obj1["Table"]["PaymentMode"]).Trim();
                                                if ((paymentMode == "Cash Pickup" || paymentMode.Contains("Cash To Home")) && PaymentDepositType_ID == 2)
                                                {
                                                    AgentRate = costRateGCC; break;
                                                }
                                                else if ((paymentMode.Contains("RTGS/NEFT") || paymentMode.Contains("IMPS") || paymentMode.Contains("Credit To Account") || paymentMode.Contains("Instant Credit To Account")) && PaymentDepositType_ID == 1)
                                                {
                                                    AgentRate = costRateGCC; break;
                                                }
                                                else if (paymentMode.Contains("Mobile Wallet") && PaymentDepositType_ID == 3)
                                                {
                                                    AgentRate = costRateGCC; break;
                                                }
                                                else
                                                {
                                                    AgentRate = 0;
                                                }
                                            }

                                            if (AgentRate == 0)
                                            {
                                                foreach (XmlNode node1 in nodeList)
                                                {
                                                    string json = Newtonsoft.Json.JsonConvert.SerializeXmlNode(node1);

                                                    var obj1 = Newtonsoft.Json.Linq.JObject.Parse(json);
                                                    Double costRateGCC = Convert.ToDouble(obj1["Table"]["CostRate"]);
                                                    AgentRate = costRateGCC; break;
                                                }
                                            }

                                            rebateCalculatedAmt = (payoutpartnerRebate_Amt * AgentRate);
                                            #endregion
                                        }

                                        payoutpartnerRebate_calculatedAmt = rebateCalculatedAmt;
                                    }
                                    else if (rebateType == "Percentage")
                                    {
                                        Double rebatePercentage = payoutpartnerRebate_Amt = Convert.ToDouble(obj12["dtTransferFinancialsList"]["PayinRebateShareAmount"]);
                                        Double rebateCalculatedAmt = 0;

                                        if (rabateSetupCurrency == payinCurrencyForRebate)
                                        {
                                            rebateCalculatedAmt = (payinAmount * rebatePercentage) / 100;
                                        }
                                        else
                                        {
                                            rebateCalculatedAmt = (payinAmount * rebatePercentage) / 100;
                                            #region rebateGBPrate For Percentage

                                            options = new RestClientOptions(apiurl)
                                            {
                                                MaxTimeout = -1
                                            };
                                            client = new RestClient(options);
                                            request = new RestRequest()
                                            {
                                                Method = Method.Post
                                            };

                                            request.AddHeader("Content-Type", "text/xml; charset=utf-8");
                                            request.AddHeader("SOAPAction", "http://tempuri.org/ISendAPI/GetExchangeRateList");
                                            body = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:tem=""http://tempuri.org/"" xmlns:grem=""http://schemas.datacontract.org/2004/07/GRemitWCFService.Send"">" + "\n" +
                                            @"   <soapenv:Header/>" + "\n" +
                                            @"   <soapenv:Body>" + "\n" +
                                            @"      <tem:GetExchangeRateList>" + "\n" +
                                            @"         <!--Optional:-->" + "\n" +
                                            @"         <tem:req>" + "\n" +
                                            @"            <!--Optional:-->" + "\n" +
                                            @"            <grem:Password>" + apipass + "</grem:Password>" + "\n" +
                                            @"            <!--Optional:-->" + "\n" +
                                            @"            <grem:PaymentModeType></grem:PaymentModeType>" + "\n" +
                                            @"            <!--Optional:-->" + "\n" +
                                            @"            <grem:PayoutCountryCode>" + "" + "</grem:PayoutCountryCode>" + "\n" +
                                            @"            <!--Optional:-->" + "\n" +
                                            @"            <grem:PayoutCurrencyCode>" + "GBP" + "</grem:PayoutCurrencyCode>" + "\n" +
                                            @"            <!--Optional:-->" + "\n" +
                                            @"            <grem:SecurityKey>" + accesscode + "</grem:SecurityKey>" + "\n" +
                                            @"            <!--Optional:-->" + "\n" +
                                            @"            <grem:UniqueID>" + apiuser + "</grem:UniqueID>" + "\n" +
                                            @"         </tem:req>" + "\n" +
                                            @"      </tem:GetExchangeRateList>" + "\n" +
                                            @"   </soapenv:Body>" + "\n" +
                                            @"</soapenv:Envelope>";
                                            request.AddParameter("text/xml", body, ParameterType.RequestBody);
                                            await SaveActivityLogTracker("Transaction proceed Confirm gcc remittance request parameters for GetExchangeRateList : " + body + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "GCC Proceed", entity.Branch_ID, Client_ID);

                                            response = client.Execute(request);
                                            await SaveActivityLogTracker("Transaction proceed Confirm gcc remittance response parameters for GetExchangeRateList : " + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "GCC Proceed", entity.Branch_ID, Client_ID);

                                            xmlDoc = new XmlDocument();
                                            xmlDoc.LoadXml(response.Content);
                                            nodeList = xmlDoc.GetElementsByTagName("Table");

                                            AgentRate = 0;
                                            foreach (XmlNode node1 in nodeList)
                                            {
                                                string json = Newtonsoft.Json.JsonConvert.SerializeXmlNode(node1);

                                                var obj1 = Newtonsoft.Json.Linq.JObject.Parse(json);
                                                Double costRateGCC = Convert.ToDouble(obj1["Table"]["CostRate"]);
                                                string paymentMode = Convert.ToString(obj1["Table"]["PaymentMode"]).Trim();
                                                if ((paymentMode == "Cash Pickup" || paymentMode.Contains("Cash To Home")) && PaymentDepositType_ID == 2)
                                                {
                                                    AgentRate = costRateGCC; break;
                                                }
                                                else if ((paymentMode.Contains("RTGS/NEFT") || paymentMode.Contains("IMPS") || paymentMode.Contains("Credit To Account") || paymentMode.Contains("Instant Credit To Account")) && PaymentDepositType_ID == 1)
                                                {
                                                    AgentRate = costRateGCC; break;
                                                }
                                                else if (paymentMode.Contains("Mobile Wallet") && PaymentDepositType_ID == 3)
                                                {
                                                    AgentRate = costRateGCC; break;
                                                }
                                                else
                                                {
                                                    AgentRate = 0;
                                                }
                                            }

                                            if (AgentRate == 0)
                                            {
                                                foreach (XmlNode node1 in nodeList)
                                                {
                                                    string json = Newtonsoft.Json.JsonConvert.SerializeXmlNode(node1);

                                                    var obj1 = Newtonsoft.Json.Linq.JObject.Parse(json);
                                                    Double costRateGCC = Convert.ToDouble(obj1["Table"]["CostRate"]);
                                                    AgentRate = costRateGCC; break;
                                                }
                                            }

                                            rebateCalculatedAmt = rebateCalculatedAmt * AgentRate;
                                            #endregion
                                        }
                                        payoutpartnerRebate_calculatedAmt = rebateCalculatedAmt;
                                    }

                                    break;
                                }
                                #endregion gccrebateamount
                                // Rebate Amount and Commission and Rate GCC  End
                                //New code
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
                                        var parameters = new
                                        {
                                            _BranchListAPI_ID = api_id,
                                            _APIBranch_Details = entity.APIBranch_Details,
                                            _TransactionRef = result.APITransaction_ID.ToString(),
                                            _trn_referenceNo = result.APITransaction_ID.ToString(),
                                            _APITransaction_Alert = 0,
                                            _Transaction_ID = entity.Transaction_ID,
                                            _Client_ID = entity.Client_ID,
                                            _payout_partner_rate = AgentRateapi,
                                        };

                                        var rowsAffected = await _dbConnection.ExecuteAsync("Update_TransactionDetails", parameters, commandType: CommandType.StoredProcedure);

                                    }
                                    catch (Exception ex)
                                    {
                                        Message1 = ex.Message;
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
                                    Message = messageResponse + " Error Message: " + responseCode,
                                    ApiId = api_id,
                                    AgentRate = AgentRateapi,
                                    ApiStatus = apistatus,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }
                        }
                        else
                        {
                            await SaveActivityLogTracker("Gcc AvailableBalance response: " + AvailableBalance + " : " + formattedAmountInPKR + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "GCC Proceed", entity.Branch_ID, Client_ID);
                        }
                    }
                }
                catch (Exception ex)
                {

                    return new ProceedResponseViewModel
                    {
                        Status = "Failed",
                        StatusCode = 2,
                        Message = " Error Message: " + ex.Message.ToString(),
                        ApiId = api_id,
                        AgentRate = AgentRateapi,
                        ApiStatus = apistatus,
                        ExtraFields = new List<string> { "", "" }
                    };

                }

                #endregion
            }
            else if (api_id == 31)
            {
                #region Providus
                try
                {
                 

                    string providusBankCode = "", debitAccountNo = "", requestLink = "";
                 
                    if (api_fields != "" && api_fields != null)
                    {
                        Newtonsoft.Json.Linq.JObject obj = Newtonsoft.Json.Linq.JObject.Parse(api_fields);
                        providusBankCode = Convert.ToString(obj["providusBankCode"]);
                        debitAccountNo = Convert.ToString(obj["debitAccountNo"]);
                        requestLink = Convert.ToString(obj["requestlink"]);
                    }
                    var body = "";
                    DateTime dateTime = DateTime.Now;
                   
                    string payerIdValue = entity.APIBranch_Details.ToString();
                    string[] elements = payerIdValue.Split('-');
                    string bankCode = string.IsNullOrEmpty(result.bank_code?.ToString()) ? "" : result.bank_code.ToString();
                    string requestedAmount = result.AmountInPKR.ToString();
                    string requestedCurrency = result.Currency_Code.ToString();
                    string Comment = result.ReferenceNo.ToString();

                    if (!string.IsNullOrEmpty(result.Comment?.ToString()))
                    {
                        Comment = result.Comment.ToString();
                    }

                  
                    string ReferenceNo = result.ReferenceNo.ToString();
                  
                    string bAccNum = string.IsNullOrEmpty(result.Account_Number?.ToString()) ? "" : result.Account_Number.ToString();
                   
                    string bname = result.Beneficiary_Name.ToString().Trim();
                    string bfname = bname; string blname = " ";
                    if (bname.Contains(" "))
                    {
                        string[] spli = bname.Split(' ');
                        if (spli.Length > 1) { bfname = bname.Substring(0, (bname.Length - spli[spli.Length - 1].Length)); blname = spli[spli.Length - 1]; }
                    }
                    string responseCode = "", responseMessage = "";
                    if (bankCode == providusBankCode)
                    {
                        await SaveActivityLogTracker("Backofc Providus request parameters Providus inside ProvidusFundTransfer : " + bankCode + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "Get Providus Proceed Transaction", entity.Branch_ID, Client_ID);
                        var options = new RestClientOptions(apiurl)
                        {
                            MaxTimeout = -1
                        };
                        var client = new RestClient(options);
                        var request = new RestRequest()
                        {
                            Method = Method.Post
                        };
           
                        request.AddHeader("Content-Type", "application/xml");
                        body = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:prov=""http://providus.com/"">" + "\n" +
                                    @"   <soapenv:Header/> " + "\n" +
                                    @"   <soapenv:Body> " + "\n" +
                                    @"  <prov:ProvidusFundTransfer> " + "\n" +
                                    @"	<currency>" + requestedCurrency + "</currency> " + "\n" +
                                    @"	<amount>" + requestedAmount + "</amount> " + "\n" +
                                    @"	<credit_account>" + bAccNum + "</credit_account>" + "\n" +
                                    @"	<debit_account>" + debitAccountNo + "</debit_account> " + "\n" +
                                    @"	<narration>" + Comment + "</narration> " + "\n" +
                                    @"	<transaction_reference>" + ReferenceNo + "</transaction_reference> " + "\n" +
                                    @"	<username>" + apiuser + "</username> " + "\n" +
                                    @"	<password>" + apipass + "</password> " + "\n" +
                                    @"	</prov:ProvidusFundTransfer> " + "\n" +
                                    @"   </soapenv:Body> " + "\n" +
                                    @"</soapenv:Envelope>";
                        request.AddParameter("text/xml", body, ParameterType.RequestBody);
                        await SaveActivityLogTracker("Backofc Providus request parameters Providus inside ProvidusFundTransfer : " + body + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "Get Providus Proceed Transaction", entity.Branch_ID, Client_ID);

                        RestResponse response1 = client.Execute(request);
                        await SaveActivityLogTracker("Backofc Providus response parameters Providus inside ProvidusFundTransfer : " + response1.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "Get Providus Proceed Transaction", entity.Branch_ID, Client_ID);
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(response1.Content);

                        XmlNodeList nodeList = doc.GetElementsByTagName("return");
                        foreach (XmlNode node in nodeList)
                        {
                            string jsonContent = node.InnerText;

                            try
                            {
                                JObject jsonObject = JObject.Parse(jsonContent);

                                responseCode = (string)jsonObject["responseCode"];
                                responseMessage = (string)jsonObject["responseMessage"];
                            }
                            catch (Exception ex)
                            {
                                Message1 = ex.Message;
                            }
                        }

                        if (responseCode == "00")
                        {
                            //string APIBranch_Details = Convert.ToString(dictObjMain["APIBranch_Details"]);
                            string APIBranch_Details = entity.APIBranch_Details.ToString();
                            apistatus = 0;

                            //New code
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
                                    var parameters = new
                                    {
                                        _BranchListAPI_ID = api_id,
                                        _APIBranch_Details = APIBranch_Details,
                                        _TransactionRef = result.APITransaction_ID.ToString(),
                                        _trn_referenceNo = ReferenceNo,
                                        _APITransaction_Alert = 0,
                                        _Transaction_ID = entity.Transaction_ID,
                                        _Client_ID = entity.Client_ID,
                                        _payout_partner_rate = AgentRateapi,
                                    };

                                    var rowsAffected = await _dbConnection.ExecuteAsync("Update_TransactionDetails", parameters, commandType: CommandType.StoredProcedure);

                                }
                                catch (Exception ex)
                                {
                                    Message1 = ex.Message;
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
                                Message = "Error Message: " + responseMessage,
                                ApiId = api_id,
                                AgentRate = AgentRateapi,
                                ApiStatus = apistatus,
                                ExtraFields = new List<string> { "", "" }
                            };

                        }
                    }
                    else
                    {
                        if (bankCode == "" || bankCode == null)
                        {
                            return new ProceedResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = " Message: " + "Bank code is require.",
                                ApiId = api_id,
                                AgentRate = AgentRateapi,
                                ApiStatus = apistatus,
                                ExtraFields = new List<string> { "", "" }
                            };
                          
                        }

                        // Account Validation Providus
                        var options = new RestClientOptions(apiurl)
                        {
                            MaxTimeout = -1
                        };
                        var clientbankaccount = new RestClient(options);
                        var requestbankaccount = new RestRequest(requestLink)
                        {
                            Method = Method.Post
                        };

                       
                        requestbankaccount.AddHeader("Content-Type", "text/xml; charset=utf-8");
                        var bodybankaccount = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:prov=""http://providus.com/"">
                                        " + "\n" +
                                    @"   <soapenv:Header/>
                                        " + "\n" +
                                    @"   <soapenv:Body>
                                        " + "\n" +
                                    @"      <prov:GetNIPAccount>
                                        " + "\n" +
                                    @"         <!--Optional:-->
                                        " + "\n" +
                                    @"         <account_number>" + bAccNum + "</account_number>" + "\n" +
                                    @"         <!--Optional:-->
                                        " + "\n" +
                                    @"         <bank_code>" + bankCode + "</bank_code>" + "\n" +
                                    @"         <!--Optional:-->
                                        " + "\n" +
                                    @"         <username>" + apiuser + "</username>" + "\n" +
                                    @"         <!--Optional:-->
                                        " + "\n" +
                                    @"         <password>" + apipass + "</password>" + "\n" +
                                    @"      </prov:GetNIPAccount>
                                        " + "\n" +
                                    @"   </soapenv:Body>
                                        " + "\n" +
                            @"</soapenv:Envelope>";

                        requestbankaccount.AddParameter("text/xml", bodybankaccount, ParameterType.RequestBody);
                        await SaveActivityLogTracker("Backofc Providus request parameters GetNIPAccount: <br/>" + bodybankaccount + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "Get Providus Proceed Transaction", entity.Branch_ID, Client_ID);

                        RestResponse response1bankaccount = clientbankaccount.Execute(requestbankaccount);
                        await SaveActivityLogTracker("Backofc Providus response parameters GetNIPAccount: <br/>" + response1bankaccount.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "Get Providus Proceed Transaction", entity.Branch_ID, Client_ID);

                        XmlDocument docbankaccount = new XmlDocument();
                        docbankaccount.LoadXml(response1bankaccount.Content);
                        XmlNodeList nodeListbankaccount = docbankaccount.GetElementsByTagName("return");
                        string accountName = "";
                        foreach (XmlNode node in nodeListbankaccount)
                        {
                            string jsonContent = node.InnerText;
                            try
                            {
                                JObject jsonObject = JObject.Parse(jsonContent);
                                responseCode = (string)jsonObject["responseCode"];

                                if (responseCode == "00")
                                { accountName = (string)jsonObject["accountName"]; break; }
                            }
                            catch (Exception ex)
                            {
                                Message1 = ex.Message;
                            }
                        }
                        if (accountName == "" || accountName == null)
                        {
                            return new ProceedResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = " Message: " + "Account Name Mismatch",
                                ApiId = api_id,
                                AgentRate = AgentRateapi,
                                ApiStatus = apistatus,
                                ExtraFields = new List<string> { "", "" }
                            };

                          
                        }
                        // End Account Validation

                        // Providus balance check
                        double availableProvidusBalance = 0;
                        dynamic balanceresponse = providusBalance(apiuser, apipass, apiurl, debitAccountNo, requestLink);
                        try
                        {
                            foreach (var item in balanceresponse)
                            {
                                try
                                {
                                    availableProvidusBalance = Convert.ToDouble(balanceresponse["availableBalance"]); break;
                                }
                                catch (Exception exx) { }
                            }

                        }
                        catch (Exception ex)
                        {
                            availableProvidusBalance = 0;
                        }

                        if (availableProvidusBalance < Convert.ToDouble(requestedAmount))
                        {
                            return new ProceedResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = " Message: " + "Fund is not enough.",
                                ApiId = api_id,
                                AgentRate = AgentRateapi,
                                ApiStatus = apistatus,
                                ExtraFields = new List<string> { "", "" }
                            };
                           
                        }

                        // End Providus balance check
                        options = new RestClientOptions(apiurl)
                        {
                            MaxTimeout = -1
                        };
                        var client = new RestClient(options);
                        var request = new RestRequest(requestLink)
                        {
                            Method = Method.Post
                        };


                        
                        request.AddHeader("Content-Type", "application/xml");
                        body = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:prov=""http://providus.com/""> " + "\n" +
                            @"   <soapenv:Header/> " + "\n" +
                            @"   <soapenv:Body> " + "\n" +
                            @"   <prov:NIPFundTransfer> " + "\n" +
                            @"	<amount>" + requestedAmount + "</amount> " + "\n" +
                            @"	<currency>" + requestedCurrency + "</currency> " + "\n" +
                            @"	<narration>" + Comment + "</narration> " + "\n" +
                            @"	<transaction_reference>" + ReferenceNo + "</transaction_reference> " + "\n" +
                            @"	<recipient_account_number>" + bAccNum + "</recipient_account_number> " + "\n" +
                            @"	<recipient_bank_code>" + bankCode + "</recipient_bank_code> " + "\n" +
                            @"	<account_name>" + bname + "</account_name> " + "\n" +
                            @"	<originator_name>" + bname + "</originator_name> " + "\n" +
                            @"	<username>" + apiuser + "</username> " + "\n" +
                            @"	<password>" + apipass + "</password> " + "\n" +
                            @"	</prov:NIPFundTransfer>   " + "\n" +
                            @"   </soapenv:Body> " + "\n" +
                            @"</soapenv:Envelope>";
                        request.AddParameter("text/xml", body, ParameterType.RequestBody);
                        await SaveActivityLogTracker("Backofc Providus request parameters NIPFundTransfer: <br/>" + body + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "Get Providus Proceed Transaction", entity.Branch_ID, Client_ID);

                        RestResponse response1 = client.Execute(request);
                        await SaveActivityLogTracker("Backofc Providus request parameters NIPFundTransfer: <br/>" + response1.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "Get Providus Proceed Transaction", entity.Branch_ID, Client_ID);

                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(response1.Content);

                        XmlNodeList nodeList = doc.GetElementsByTagName("return");
                        foreach (XmlNode node in nodeList)
                        {
                            string jsonContent = node.InnerText;

                            try
                            {
                                JObject jsonObject = JObject.Parse(jsonContent);

                                responseCode = (string)jsonObject["responseCode"];
                                responseMessage = (string)jsonObject["responseMessage"];
                            }
                            catch (Exception ex)
                            {
                                Message1 = ex.Message;
                            }
                        }

                        if (responseCode == "00")
                        {
                            string APIBranch_Details = entity.APIBranch_Details.ToString();
                            apistatus = 0;

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
                                    var parameters = new
                                    {
                                        _BranchListAPI_ID = api_id,
                                        _APIBranch_Details = APIBranch_Details,
                                        _TransactionRef = result.APITransaction_ID.ToString(),
                                        _trn_referenceNo = ReferenceNo,
                                        _APITransaction_Alert = 0,
                                        _Transaction_ID = entity.Transaction_ID,
                                        _Client_ID = entity.Client_ID,
                                        _payout_partner_rate = AgentRateapi,
                                    };

                                    var rowsAffected = await _dbConnection.ExecuteAsync("Update_TransactionDetails", parameters, commandType: CommandType.StoredProcedure);

                                }
                                catch (Exception ex)
                                {
                                    Message1 = ex.Message;
                                    await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                }
                            }
                            //End New code

                           
                        }
                        else
                        {
                            return new ProceedResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = " Error Message: " + responseMessage,
                                ApiId = api_id,
                                AgentRate = AgentRateapi,
                                ApiStatus = apistatus,
                                ExtraFields = new List<string> { "", "" }
                            };

                         
                        }
                    }
                }
                catch (Exception ex)
                {
                    Message1 = ex.Message;
                    await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                }
                #endregion Providus
            }
            else if (api_id == 38)//anchor pradip
            {
                #region Anchor
                string counterPartyid = "";
                if (api_fields != "" && api_fields != null)
                {
                    Newtonsoft.Json.Linq.JObject objAPI = Newtonsoft.Json.Linq.JObject.Parse(api_fields);
                    counterPartyid = Convert.ToString(objAPI["id"]);
                }
                string jsonResponse = "";
                JObject responseObject = null;
                int AmountInGBPAsInt = 0;
                try
                {
                    string bank_code = result.bank_code.ToString();
                    string Beneficiary_Name = result.Beneficiary_Name.ToString();
                    string Account_Number = result.Account_Number.ToString();
                    string AmountInGBP = result.AmountInPKR.ToString();
                    AmountInGBPAsInt = Convert.ToInt32(Convert.ToDecimal(AmountInGBP) * 100);

                    string Temp_url = apiurl + "/counterparties";
                    var options = new RestClientOptions(Temp_url)
                    {
                        MaxTimeout = -1
                    };
                    var client = new RestClient(options);
                    var request = new RestRequest()
                    {
                        Method = Method.Post
                    };

                    request.AddHeader("accept", "application/json");
                    //request.AddHeader("x-anchor-key", "qa20S.ff67793a71d5698a21e5b2f1a1466c2ec6420795a59c7997b46849a5b8c36300ffda541b0a00034e776b73b523f727750177");
                    request.AddHeader("x-anchor-key", accesscode); //RestResponse response = await client.ExecuteAsync(request);

                    var body = @"" + "\n" +
                  @"{" + "\n" +
                  @"  ""data"": {" + "\n" +
                  @"    ""type"": ""CounterParty""," + "\n" +
                  @"    ""attributes"": {" + "\n" +
                  @"      ""bankCode"": """ + bank_code + @"""," + "\n" +
                  @"      ""accountName"": """ + Beneficiary_Name + @"""," + "\n" +
                  @"      ""accountNumber"": """ + Account_Number + @"""," + "\n" +
                  @"      ""verifyName"": true" + "\n" +
                  @"    }" + "\n" +
                  @"  }" + "\n" +
                  @"}";
                    request.AddParameter("application/json", body, ParameterType.RequestBody);
                    await SaveActivityLogTracker("Anchor Create Transaction counterparties Request: <br/>" + body + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Anchor Proceed", entity.Branch_ID, Client_ID);

                    RestResponse response = client.Execute(request);
                    await SaveActivityLogTracker("Anchor Create Transaction counterparties Response: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Anchor Proceed", entity.Branch_ID, Client_ID);
                    jsonResponse = response.Content;

                    responseObject = JObject.Parse(jsonResponse);
                }
                catch (Exception ex)
                {
                    Message1 = ex.Message;
                    await SaveActivityLogTracker("Anchor Create T transaction  ERROR: <br/>" + ex.ToString() + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Anchor Proceed", entity.Branch_ID, Client_ID);
                }
                if (responseObject["errors"] != null)
                {
                    string errorTitle = responseObject["errors"][0]["title"].ToString();
                    string errorStatus = responseObject["errors"][0]["status"].ToString();
                    string errorDetail = responseObject["errors"][0]["detail"].ToString();
                }
                else if (responseObject["data"] != null)
                {
                    string bankId = responseObject["data"]["id"].ToString();
                    string accountNumber = responseObject["data"]["attributes"]["accountNumber"].ToString();
                    string accountHolderName = responseObject["data"]["attributes"]["accountName"].ToString();
                    string refer = result.ReferenceNo.ToLower();
                    if (bankId != null && bankId != "")
                    {
                        string Temp_url = apiurl + "/transfers";

                        var options = new RestClientOptions(Temp_url)
                        {
                            MaxTimeout = -1
                        };
                        var client1 = new RestClient(options);
                        var request1 = new RestRequest()
                        {
                            Method = Method.Post
                        };

                        request1.AddHeader("Content-Type", "application/json");
                        request1.AddHeader("x-anchor-key", accesscode);
                        var body = @"{
                                    " + "\n" +
                       @"    ""data"": {
                                    " + "\n" +
                       @"        ""type"": ""NIPTransfer"",
                                    " + "\n" +
                       @"        ""attributes"": {
                                    " + "\n" +
                       @"            ""amount"": " + AmountInGBPAsInt + @",
                                    " + "\n" +
                       @"            ""currency"": ""NGN"",
                                    " + "\n" +
                       @"            ""reason"": ""Sample NIP test transfer"",
                                    " + "\n" +
                       @"            ""reference"": """ + refer + @"""
                                    " + "\n" +
                       @"        },
                                    " + "\n" +
                       @"        ""relationships"": {
                                    " + "\n" +
                       @"            ""account"": {
                                    " + "\n" +
                       @"                ""data"": {
                                    " + "\n" +
                       @"                    ""id"": """ + counterPartyid + @""",
                                    " + "\n" +
                       @"                    ""type"": ""DepositAccount""
                                    " + "\n" +
                       @"                }
                                    " + "\n" +
                       @"            },
                                    " + "\n" +
                       @"            ""counterParty"": {
                                    " + "\n" +
                       @"                ""data"": {
                                    " + "\n" +
                       @"                    ""id"": """ + bankId + @""",
                                    " + "\n" +
                       @"                    ""type"": ""CounterParty""
                                    " + "\n" +
                       @"                }
                                    " + "\n" +
                       @"            }
                                    " + "\n" +
                       @"        }
                                    " + "\n" +
                       @"    }
                                    " + "\n" +
                       @"}";
                        //string body1 = "{ \"data\": { \"type\": \"NIPTransfer\", \"attributes\": { \"amount\": \"" + AmountInGBP + "\", \"currency\": \"NGN\", \"reason\": \"Sample NIP test transfer\", \"reference\": \"tthwubtvwt\" }, \"relationships\": { \"account\": { \"data\": { \"id\": \"" + bankId + "\", \"type\": \"DepositAccount\" } }, \"counterParty\": { \"data\": { \"id\": \""+ counterPartyid + "\", \"type\": \"CounterParty\" } } } } }";
                        await SaveActivityLogTracker("Anchor Create Transaction transfers Request: <br/>" + body + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Anchor Proceed", entity.Branch_ID, Client_ID);

                        request1.AddParameter("application/json", body, ParameterType.RequestBody);

                        RestResponse response1 = client1.Execute(request1);
                        await SaveActivityLogTracker("Anchor Create Transaction transfers Request: <br/>" + response1.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Anchor Proceed", entity.Branch_ID, Client_ID);

                        string jsonResponse1 = response1.Content;
                        JObject responseObject1 = JObject.Parse(jsonResponse1);
                        if (responseObject1["data"] != null)
                        {
                            //string bankId = responseObject1["data"]["attributes"]["bank"]["id"].ToString();                                        
                            //FAILED - PENDING - COMPLETED
                            string responseObject1id = responseObject1["data"]["id"].ToString();
                            string responseObject1reference = responseObject1["data"]["attributes"]["reference"].ToString();
                            string responseObject1amount = responseObject1["data"]["attributes"]["amount"].ToString();
                            string responseObject1currency = responseObject1["data"]["attributes"]["currency"].ToString();
                            string responseObject1status = responseObject1["data"]["attributes"]["status"].ToString();

                            if (responseObject1status == "COMPLETED" || responseObject1status == "PENDING")
                            {
                                string APIBranch_Details = entity.APIBranch_Details.ToString();

                                //New code
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
                                        var parameters = new
                                        {
                                            _BranchListAPI_ID = api_id,
                                            _APIBranch_Details = entity.APIBranch_Details,
                                            _TransactionRef = responseObject1["data"]["attributes"]["reference"].ToString(),
                                            _trn_referenceNo = responseObject1id,
                                            _APITransaction_Alert = 0,
                                            _Transaction_ID = entity.Transaction_ID,
                                            _Client_ID = entity.Client_ID,
                                            _payout_partner_rate = AgentRateapi,
                                        };

                                        var rowsAffected = await _dbConnection.ExecuteAsync("Update_TransactionDetails", parameters, commandType: CommandType.StoredProcedure);

                                    }
                                    catch (Exception ex)
                                    {
                                        Message1 = ex.Message;
                                        await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                    }
                                }
                                //End New code
                                apistatus = 0;

                            }
                            else if (responseObject1status == "FAILED")
                            {

                                return new ProceedResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = " Error Message: " + responseObject1status,
                                    ApiId = api_id,
                                    AgentRate = AgentRateapi,
                                    ApiStatus = apistatus,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }
                            else
                            {
                                return new ProceedResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = " Error Message: " + responseObject1status,
                                    ApiId = api_id,
                                    AgentRate = AgentRateapi,
                                    ApiStatus = apistatus,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }

                        }
                        else if (responseObject1["errors"] != null)
                        {
                            //{ "errors":[{ "title":"Bad Request","status":"400","detail":"Transfer with reference tp66443175 already exists"}]}
                            string errorTitle = responseObject1["errors"][0]["title"].ToString();
                            string errorStatus = responseObject1["errors"][0]["status"].ToString();
                            string errorDetail = responseObject1["errors"][0]["detail"].ToString();

                            return new ProceedResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = " Error Message: " + errorDetail,
                                ApiId = api_id,
                                AgentRate = AgentRateapi,
                                ApiStatus = apistatus,
                                ExtraFields = new List<string> { "", "" }
                            };
                        }

                    }
                }
                else
                {
                    return new ProceedResponseViewModel
                    {
                        Status = "Failed",
                        StatusCode = 2,
                        Message = " Error Message: " + responseObject["errors"][0]["detail"].ToString(),
                        ApiId = api_id,
                        AgentRate = AgentRateapi,
                        ApiStatus = apistatus,
                        ExtraFields = new List<string> { "", "" }
                    };
                }

                #endregion Anchor
            }
            else if (api_id == 27)
            {
                #region Muleshill
                try
                {
                    var body = "";
                    string beneficiary_name = result.Beneficiary_Name;
                    string beneficiary_account_number = result.Account_Number;
                    string beneficiary_bank_id = "";
                    string curr_id = "";
                    string beneficiary_country = result.Beneficiary_Country;
                    string beneficiary_phone = result.Beneficiary_Mobile;
                    string beneficiary_bank_code = result.bank_code;
                    string Reference_Id = result.ReferenceNo;
                    string naration = result.Comment;
                    string beneficiary_country_id = "";
                    string sender_name = result.Customer_Name;
                    string appID = result.appId.ToString();
                    string beneficiary_Id = "";
                    string transaction_ammount = result.AmountInPKR.ToString();
                    string sourceId = "";
                    if (naration == "")
                    {
                        naration = Reference_Id;
                    }

                    if (api_fields != "" && api_fields != null)
                    {
                        Newtonsoft.Json.Linq.JObject obj = Newtonsoft.Json.Linq.JObject.Parse(api_fields);
                        sourceId = Convert.ToString(obj["defaultgateway"]);
                    }
                    await SaveActivityLogTracker(" Source ID: " + sourceId, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "Proceed", entity.Branch_ID, Client_ID);

                    string bname = result.Beneficiary_Name.Trim();
                    string bfname = bname; string blname = " ";
                    if (bname.Contains(" "))
                    {
                        string[] spli = bname.Split(' ');
                        if (spli.Length > 1) { bfname = bname.Substring(0, (bname.Length - spli[spli.Length - 1].Length)); blname = spli[spli.Length - 1]; }
                    }

                    // Get bank list

                    var options = new RestClientOptions(apiurl + "getbanks")
                    {
                        MaxTimeout = -1
                    };
                    var client = new RestClient(options);
                    var request = new RestRequest()
                    {
                        Method = Method.Get
                    };

              
                    RestResponse response = client.Execute(request);
                    await SaveActivityLogTracker("getbanks Transfer rocket response parameters: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Muleshill Proceed", entity.Branch_ID, Client_ID);

                    dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
                    dynamic resultjson = null;
                    if (json.content != "")
                        resultjson = json["data"];
                    foreach (var item in resultjson)
                    {
                        if (beneficiary_bank_code == item.bankCode.Value.ToString())
                        {
                            beneficiary_bank_id = item.bankId.Value.ToString();
                        }
                    }
                    // Get countries List
                    options = new RestClientOptions(apiurl + "getcountries")
                    {
                        MaxTimeout = -1
                    };
                    client = new RestClient(options);
                    request = new RestRequest()
                    {
                        Method = Method.Get
                    };


    
                    await SaveActivityLogTracker("getcountries Transfer rocket request with parameters: <br/>" + apiurl + "getcountries" + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Muleshill Proceed", entity.Branch_ID, Client_ID);

                    response = client.Execute(request);
                    await SaveActivityLogTracker("getcountries Transfer rocket response parameters: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Muleshill Proceed", entity.Branch_ID, Client_ID);

                    json = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
                    resultjson = json["data"];
                    foreach (var item in resultjson)
                    {
                        if (beneficiary_country == item.name.Value)
                        {
                            // create Beneficiary
                            beneficiary_country_id = item.id.Value.ToString();
                        }
                    }
                    options = new RestClientOptions(apiurl + "getcurrency")
                    {
                        MaxTimeout = -1
                    };
                    client = new RestClient(options);
                    request = new RestRequest()
                    {
                        Method = Method.Get
                    };

            
                    await SaveActivityLogTracker("getcurrency Transfer rocket request parameters: <br/>" + apiurl + "getcurrency" + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Muleshill Proceed", entity.Branch_ID, Client_ID);
                    response = client.Execute(request);
                    await SaveActivityLogTracker("getcurrency Transfer rocket response parameters: <br/>" + response.Content + "getcurrency" + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Muleshill Proceed", entity.Branch_ID, Client_ID);

                    json = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
                    resultjson = json["data"];
                    foreach (var item in resultjson)
                    {

                        if ("Naira" == item.name.Value)
                        {
                            // create Beneficiary
                            curr_id = Convert.ToString(item.id.Value);
                        }
                    }
                    string credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{apiuser}:{apipass}"));
                    string Beneficary_API_ID = result.Beneficary_API_ID.Trim();
                    if (Beneficary_API_ID == "" || Beneficary_API_ID == null || Beneficary_API_ID == "0")
                    {
                        options = new RestClientOptions(apiurl + "creatpayoutbeneficiary.io")
                        {
                            MaxTimeout = -1
                        };
                        client = new RestClient(options);
                        request = new RestRequest()
                        {
                            Method = Method.Post
                        };
                      
                        request.AddHeader("Content-Type", "application/json");
                        request.AddHeader("Authorization", "Basic " + credentials);
                        body = @"{
" + "\n" +
                        @"    ""userId"": """ + apiuser + @""",
" + "\n" +
                         @"    ""userBeneficiary"": {
" + "\n" +
                         @"        ""beneficiaryCountry"": {
" + "\n" +
                         @"            ""id"": """ + beneficiary_country_id + @"""
" + "\n" +
                         @"        },
" + "\n" +
                         @"        ""beneficiaryName"": """ + beneficiary_name + @""", 
" + "\n" +
                         @"        ""beneficiaryPhoneNumber"": """ + beneficiary_phone + @""",
" + "\n" +
                         @"        ""beneficiaryBank"": {
" + "\n" +
                         @"            ""accountNumber"": """ + beneficiary_account_number + @""",
" + "\n" +
                         @"            ""bankId"": """ + beneficiary_bank_id + @"""
" + "\n" +
                         @"        }
" + "\n" +
                         @"    }
" + "\n" +
                         @"}";
                        request.AddParameter("application/json", body, ParameterType.RequestBody);
                        await SaveActivityLogTracker("creatpayoutbeneficiary.io Transfer rocket request parameters:<br/>" + body + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Muleshill Proceed", entity.Branch_ID, Client_ID);
                        response = client.Execute(request);
                        await SaveActivityLogTracker("creatpayoutbeneficiary.io Transfer rocket response parameters: <br/>" + response.Content + "getcurrency" + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Muleshill Proceed", entity.Branch_ID, Client_ID);

                        json = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
                        resultjson = json["data"];
                        if (json["transactionRef"] == "SUCCESS")
                            beneficiary_Id = resultjson.ToString();

                        if (beneficiary_Id != "")
                        {
                            string Beneficiary_API_ID = "";
                            Beneficiary_API_ID = beneficiary_Id;
                   
                        }
                    }
                    else
                    {
                        beneficiary_Id = Beneficary_API_ID;
                    }

                    // Validate Benefeciary
                    options = new RestClientOptions(apiurl + "validatebeneficiarybankdetails")
                    {
                        MaxTimeout = -1
                    };
                    client = new RestClient(options);
                    request = new RestRequest()
                    {
                        Method = Method.Post
                    };


                    
                    request.AddHeader("Content-Type", "application/json");
                    body = @"{
" + "\n" +
                   @"    ""beneficiaryBank"": { 
" + "\n" +
                   @"        ""accountNumber"":""" + beneficiary_account_number + @""" ,
" + "\n" +
                   @"        ""bankId"": """ + beneficiary_bank_id + @"""
" + "\n" +
                   @"    }
" + "\n" +
                   @"}";
                    request.AddParameter("application/json", body, ParameterType.RequestBody);
                    await SaveActivityLogTracker("validatebeneficiarybankdetails Transfer rocket request parameters: <br/>" + body + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Muleshill Proceed", entity.Branch_ID, Client_ID);

                    response = client.Execute(request);
                    await SaveActivityLogTracker("validatebeneficiarybankdetails Transfer rocket response parameters: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Muleshill Proceed", entity.Branch_ID, Client_ID);

                    json = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
                    resultjson = json["data"];


                    if (sourceId == "")
                    {
                        options = new RestClientOptions(apiurl + "getpayoutclientwalletprovider.io")
                        {
                            MaxTimeout = -1
                        };
                        client = new RestClient(options);
                        request = new RestRequest()
                        {
                            Method = Method.Get
                        };

                        request.AddHeader("Content-Type", "application/json");
                        request.AddHeader("clientId", apiuser);
                        credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{apiuser}:{apipass}"));
                        request.AddHeader("Authorization", "Basic " + credentials);
                        await SaveActivityLogTracker("getpayoutclientwalletprovider.io Transfer rocket request parameters: <br/>" + apiurl + "getpayoutclientwalletprovider.io" + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Muleshill Proceed", entity.Branch_ID, Client_ID);
                        response = client.Execute(request);
                        await SaveActivityLogTracker("getpayoutclientwalletprovider.io Transfer rocket response parameters: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Muleshill Proceed", entity.Branch_ID, Client_ID);

                        json = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
                
                        if (json["data"] is JArray dataArray && dataArray.Count > 0)
                        {
                            // Access the "providerId" of the first object in the array
                            sourceId = dataArray[0]["providerId"].ToString();
                        }
                        else
                            sourceId = "2";
                  
                    }

                    options = new RestClientOptions(apiurl + "processpayout.io")
                    {
                        MaxTimeout = -1
                    };
                    client = new RestClient(options);
                    request = new RestRequest()
                    {
                        Method = Method.Post
                    };

                    
                    request.AddHeader("Content-Type", "application/json");
                    request.AddHeader("clientId", apiuser);
                    credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{apiuser}:{apipass}"));
                    request.AddHeader("Authorization", "Basic " + credentials);

                    //amount=transaction_ammount
                    body = @"{
" + "\n" +
                    @"    ""refrenceId"": """ + Reference_Id + @""",
" + "\n" +
                    @"    ""appId"": """ + appID + @""",
" + "\n" +
                    @"    ""sourceId"": """ + sourceId + @""",
" + "\n" +
                    @"    ""currencyId"": """ + curr_id + @""",
" + "\n" +
                    @"    ""amount"": """ + transaction_ammount + @""",
" + "\n" +
                    @"    ""naration"": """ + naration + @""",
" + "\n" +
                    @"    ""senderName"": """ + sender_name + @""",
" + "\n" +
                    @"    ""beneficiaryId"" : """ + beneficiary_Id + @"""
" + "\n" +
                    @"}";

                    request.AddParameter("application/json", body, ParameterType.RequestBody);
                    await SaveActivityLogTracker("processpayout.io Transfer rocket request parameters: <br/>" + body + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Muleshill Proceed", entity.Branch_ID, Client_ID);

                    response = client.Execute(request);//response.StatusCode
                    await SaveActivityLogTracker("processpayout.io Transfer rocket response parameters: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Muleshill Proceed", entity.Branch_ID, Client_ID);

                    json = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);

                    resultjson = json["data"];
                    if (resultjson == "00" || resultjson == "05")
                    {
                       
                        string APIBranch_Details = entity.APIBranch_Details;
                        apistatus = 0;


                        //New code
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
                                var parameters = new
                                {
                                    _BranchListAPI_ID = api_id,
                                    _APIBranch_Details = APIBranch_Details,
                                    _TransactionRef = result.APITransaction_ID.ToString(),
                                    _trn_referenceNo = Convert.ToString(Reference_Id),
                                    _APITransaction_Alert = 0,
                                    _Transaction_ID = entity.Transaction_ID,
                                    _Client_ID = entity.Client_ID,
                                    _payout_partner_rate = AgentRateapi,
                                };

                                var rowsAffected = await _dbConnection.ExecuteAsync("Update_TransactionDetails", parameters, commandType: CommandType.StoredProcedure);

                            }
                            catch (Exception ex)
                            {
                                Message1 = ex.Message;
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
                            Message = " Error Message: " + json.message,
                            ApiId = api_id,
                            AgentRate = AgentRateapi,
                            ApiStatus = apistatus,
                            ExtraFields = new List<string> { "", "" }
                        };

                    }


                    options = new RestClientOptions(apiurl + "getpayouttransaction.io?trxId=" + Reference_Id)
                    {
                        MaxTimeout = -1
                    };
                    client = new RestClient(options);
                    request = new RestRequest()
                    {
                        Method = Method.Get
                    };
                  
                    request.AddHeader("Content-Type", "application/json");
                    request.AddHeader("clientId", apiuser);
                    credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{apiuser}:{apipass}"));
                    request.AddHeader("Authorization", "Basic " + credentials);
                    await SaveActivityLogTracker("processpayout.io Transfer rocket request parameters: <br/>" + apiurl + "getpayouttransaction.io?trxId=" + Reference_Id + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Muleshill Proceed", entity.Branch_ID, Client_ID);

                    response = client.Execute(request);
                    await SaveActivityLogTracker("makePayment Transfer rocket request parameters: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Muleshill Proceed", entity.Branch_ID, Client_ID);

                    json = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
                    resultjson = json["data"];
                }
                catch (Exception ex)
                {
                    return new ProceedResponseViewModel
                    {
                        Status = "Failed",
                        StatusCode = 2,
                        Message = " Error Message: " + ex.Message.ToString(),
                        ApiId = api_id,
                        AgentRate = AgentRateapi,
                        ApiStatus = apistatus,
                        ExtraFields = new List<string> { "", "" }
                    };

                }
                #endregion Muleshill
            }
            else if (api_id == 28)
            {
                #region Flutterwave
                try
                {

                    var body = "";
                    string beneficiary_name = result.Beneficiary_Name;
                    string beneficiary_account_number = result.Account_Number;
                    string beneficiary_bank_id = "";
                    string beneficiary_country = result.Beneficiary_Country;
                    string beneficiary_phone = result.Beneficiary_Mobile;
                    string beneficiary_bank_code = result.bank_code;
                    string Reference_Id = result.ReferenceNo;
                    string naration = result.Comment;
                    string beneficiary_country_id = "";
                    string sender_name = result.SenderNameOnID;
                    string Currency_Code = result.Currency_Code;
                    string beneficiary_Id = "";
                    string transaction_ammount = result.AmountInPKR;
                    string sourceId = "";
                    string beneficieary_country_code = result.benf_ISO_Code;
                    string Reference_new = Reference_Id;// + "_PMCKDU_1";

                    if (api_fields != "" && api_fields != null)
                    {
                        Newtonsoft.Json.Linq.JObject obj = Newtonsoft.Json.Linq.JObject.Parse(api_fields);
                        sourceId = Convert.ToString(obj["defaultgateway"]);
                    }
                    string bname = result.Beneficiary_Name.ToString().Trim();
                    string bfname = bname; string blname = " ";
                    if (bname.Contains(" "))
                    {
                        string[] spli = bname.Split(' ');
                        if (spli.Length > 1) { bfname = bname.Substring(0, (bname.Length - spli[spli.Length - 1].Length)); blname = spli[spli.Length - 1]; }
                    }


                    var options = new RestClientOptions(apiurl + "banks/" + beneficieary_country_code)
                    {
                        MaxTimeout = -1
                    };
                    var client = new RestClient(options);
                    var request = new RestRequest()
                    {
                        Method = Method.Get
                    };
                    string bnkdata = "Bank details fetch successfully";
  
                    request.AddHeader("Authorization", accesscode);
                    await SaveActivityLogTracker("get bank Flutterwave request parameters: <br/>" + bnkdata + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Flutterwave Proceed", entity.Branch_ID, Client_ID);
                    RestResponse response = client.Execute(request);
                    await SaveActivityLogTracker("get bank Flutterwave response parameters: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Flutterwave Proceed", entity.Branch_ID, Client_ID);

                    dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
                    dynamic jsonresult = json["data"];
                    foreach (var item in jsonresult)
                    {
                        if (beneficiary_bank_code == item.code.Value.ToString())
                        {
                            beneficiary_bank_id = item.code.Value.ToString();
                        }
                    }

                    options = new RestClientOptions(apiurl + "accounts/resolve")
                    {
                        MaxTimeout = -1
                    };
                    client = new RestClient(options);
                    request = new RestRequest()
                    {
                        Method = Method.Post
                    };

          
                    request.AddHeader("Content-Type", "application/json");
                    request.AddHeader("Authorization", accesscode);
                    body = @"{
" + "\n" +
                    @"  ""account_number"":  """ + beneficiary_account_number + @""",
" + "\n" +
                    @"  ""account_bank"":  """ + beneficiary_bank_id + @"""
" + "\n" +
                    @"}";
                    request.AddParameter("application/json", body, ParameterType.RequestBody);
                    await SaveActivityLogTracker("account check Flutterwave request parameters: <br/>" + body + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Flutterwave Proceed", entity.Branch_ID, Client_ID);

                    response = client.Execute(request);
                    await SaveActivityLogTracker("account check Flutterwave response parameters: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Flutterwave Proceed", entity.Branch_ID, Client_ID);

                    json = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
                    result = json["data"];


                    options = new RestClientOptions(apiurl + "transfers")
                    {
                        MaxTimeout = -1
                    };
                    client = new RestClient(options);
                    request = new RestRequest()
                    {
                        Method = Method.Post
                    };

                    request.AddHeader("Content-Type", "application/json");
                    request.AddHeader("Authorization", accesscode);
                    body = @"{
                                " + "\n" +
@"   
" + "\n" +
@"    ""account_bank"": """ + beneficiary_bank_id + @""",
" + "\n" +
@"    ""account_number"": """ + beneficiary_account_number + @""",
" + "\n" +
@"    ""amount"": """ + transaction_ammount + @""" ,
" + "\n" +
@"    ""narration"": """ + naration + @""",
" + "\n" +
@"    ""currency"":  """ + Currency_Code + @""",
" + "\n" +
@"    ""reference"": """ + Reference_new + @""",
" + "\n" +
@"    ""callback_url"": """",
" + "\n" +
@"    ""debit_currency"": """ + Currency_Code + @"""
" + "\n" +
@"}";

                    request.AddParameter("application/json", body, ParameterType.RequestBody);
                    await SaveActivityLogTracker("account check Flutterwave request parameters: <br/>" + body + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Flutterwave Proceed", entity.Branch_ID, Client_ID);

                    response = client.Execute(request);
                    await SaveActivityLogTracker("makePayment Flutterwave response parameters: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Flutterwave Proceed", entity.Branch_ID, Client_ID);


                    string transferId = "";
                    json = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
                    result = json["data"];
                    if (json["status"] == "success")
                        transferId = result.id.ToString();



                    string status = json["status"];
                    if (transferId != "")
                    {
                        options = new RestClientOptions(apiurl + "transfers/" + transferId)
                        {
                            MaxTimeout = -1
                        };
                        client = new RestClient(options);
                        request = new RestRequest()
                        {
                            Method = Method.Get
                        };

                        request.AddHeader("Authorization", accesscode);
                        await SaveActivityLogTracker("transfer status Flutterwave request parameters: <br/>" + apiurl + "transfers/" + transferId + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Flutterwave Proceed", entity.Branch_ID, Client_ID);

                        response = client.Execute(request);

                        await SaveActivityLogTracker("transfer status Flutterwave response parameters: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Flutterwave Proceed", entity.Branch_ID, Client_ID);

                        json = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
                        result = json["data"];
                        status = json.status;

                    }
                    if ((status == "SUCCESSFUL" || status == "NEW" || status == "success") && transferId != "")
                    {
                        
                        string APIBranch_Details = entity.APIBranch_Details.ToString();

                

                        //New code
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
                                var parameters = new
                                {
                                    _BranchListAPI_ID = api_id,
                                    _APIBranch_Details = APIBranch_Details,
                                    _TransactionRef = result.APITransaction_ID.ToString(),
                                    _trn_referenceNo = transferId,
                                    _APITransaction_Alert = 0,
                                    _Transaction_ID = entity.Transaction_ID,
                                    _Client_ID = entity.Client_ID,
                                    _payout_partner_rate = AgentRateapi,
                                };

                                var rowsAffected = await _dbConnection.ExecuteAsync("Update_TransactionDetails", parameters, commandType: CommandType.StoredProcedure);
                                apistatus = 0;
                            }
                            catch (Exception ex)
                            {
                                Message1 = ex.Message;
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
                            Message = " Error Message: " + json.message,
                            ApiId = api_id,
                            AgentRate = AgentRateapi,
                            ApiStatus = apistatus,
                            ExtraFields = new List<string> { "", "" }
                        };

                    }

                }
                catch (Exception ex)
                {
                    return new ProceedResponseViewModel
                    {
                        Status = "Failed",
                        StatusCode = 2,
                        Message = "Inner exception" + ex.Message,
                        ApiId = api_id,
                        AgentRate = AgentRateapi,
                        ApiStatus = apistatus,
                        ExtraFields = new List<string> { "", "" }
                    };

                }
                #endregion Flutterwave
            }

            else if (api_id == 7)
            {
                #region HNB API
                await SaveActivityLogTracker("transfer start HNB API : <br/>", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "HNB API Proceed", entity.Branch_ID, Client_ID);

                string webAccountNumber = "";
                string debtAcct = "";

                // Parse API fields if provided
                if (!string.IsNullOrEmpty(api_fields))
                {
                    var obj = Newtonsoft.Json.Linq.JObject.Parse(api_fields);
                    webAccountNumber = Convert.ToString(obj["webAccountNumber"]);
                    debtAcct = Convert.ToString(obj["debtAcct"]);
                }
                string soap_req = "";

               
                try
                {
                    string RemittanceType = "";
                    string Bankdetails = "";

                    // Ignore SSL certificate errors
                    System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                    XmlDocument SOAPReqBody = new XmlDocument();
                    int PaymentDepositType_ID = Convert.ToInt32(result.PaymentDepositType_ID);
                    string createTransactionid1 = createSessionid();

                    string AgentTransactionid = Convert.ToString(result.ReferenceNo);

                    // Set remittance type and bank details based on payment deposit type
                    if (PaymentDepositType_ID == 1)
                    {
                        if (Convert.ToInt32(result.BankCode) == 7083)
                            RemittanceType = "A";
                        else
                            RemittanceType = "O";

                        Bankdetails = $@"
                <xsd:beneficiaryBank>{Convert.ToString(result.BankCode)}</xsd:beneficiaryBank>
                <xsd:beneficiaryBranch>{Convert.ToString(result.BranchCode)}</xsd:beneficiaryBranch>";
                    }
                    else if (PaymentDepositType_ID == 2)
                    {
                        RemittanceType = "C";
                        Bankdetails = @"<xsd:beneficiaryBank>7083</xsd:beneficiaryBank>
                            <xsd:beneficiaryBranch>001</xsd:beneficiaryBranch>";
                    }

                    // Construct SOAP request body
                    soap_req = $@"
        <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ser=""http://service.com"" xmlns:xsd=""http://service.com/xsd"">
            <soapenv:Header/>
            <soapenv:Body>
                <ser:sendTransaction>
                    <ser:trx>
                        <xsd:amount>{Convert.ToString(result.AmountInPKR)}</xsd:amount>
                        <xsd:beneInfo>
                            <xsd:beneficiaryAccountNumber>{Convert.ToString(result.Account_Number)}</xsd:beneficiaryAccountNumber>
                            <xsd:beneficiaryAddress>{Convert.ToString(result.Beneficiary_Address)}</xsd:beneficiaryAddress>
                            {Bankdetails}
                            <xsd:beneficiaryContactNumber/>
                            <xsd:beneficiaryEmailAddress></xsd:beneficiaryEmailAddress>
                            <xsd:beneficiaryIdNumber/>
                            <xsd:beneficiaryName>{Convert.ToString(result.Beneficiary_Name)}</xsd:beneficiaryName>
                        </xsd:beneInfo>
                        <xsd:chargesFrom>S</xsd:chargesFrom>
                        <xsd:curency>{Convert.ToString(result.Currency_Code)}</xsd:curency>
                        <xsd:debtAcct>{Convert.ToString(debtAcct)}</xsd:debtAcct>
                        <xsd:details/>
                        <xsd:remittanceType>{RemittanceType}</xsd:remittanceType>
                        <xsd:senderInfo>
                            <xsd:senderAddress>{Convert.ToString(result.sender_address)}</xsd:senderAddress>
                            <xsd:senderIdNumber/>
                            <xsd:senderName>{Convert.ToString(result.Customer_Name)}</xsd:senderName>
                        </xsd:senderInfo>
                        <xsd:sourceofFunds>Salary</xsd:sourceofFunds>
                        <xsd:transActionDate>{Convert.ToString(result.transaction_date).Replace("/", "")}</xsd:transActionDate>
                        <xsd:transActionTime>{Convert.ToString(result.transaction_time).Replace(":", "")}</xsd:transActionTime>
                        <xsd:transactionRefNumber>{AgentTransactionid}</xsd:transactionRefNumber>
                        <xsd:valueDate>{Convert.ToString(result.transaction_date).Replace("/", "")}</xsd:valueDate>
                    </ser:trx>
                    <ser:serviceInfo>
                        <xsd:agentCode>{Convert.ToString(apicompany_id)}</xsd:agentCode>
                        <xsd:passkey>{Convert.ToString(accesscode)}</xsd:passkey>
                        <xsd:webAccountNumber>{Convert.ToString(webAccountNumber)}</xsd:webAccountNumber>
                    </ser:serviceInfo>
                </ser:sendTransaction>
            </soapenv:Body>
        </soap:Envelope>";

                    await SaveActivityLogTracker("Add remittance request parameters: <br/>" + soap_req, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "HNB API Proceed", entity.Branch_ID, Client_ID);

                    // Configure REST client
                    var options2 = new RestClientOptions(apiurl.Replace("/axis2/services/HNBRemittance?wsdl", ""))
                    {
                        MaxTimeout = -1
                    };
                    var client2 = new RestClient(options2);
                    var request2 = new RestRequest("/axis2/services/HNBRemittance.HNBRemittanceHttpSoap11Endpoint/", Method.Post);

                    request2.AddHeader("Content-Type", "text/xml; charset=utf-8");
                    request2.AddHeader("SOAPAction", "urn:sendTransaction");

                    request2.AddParameter("text/xml", soap_req, ParameterType.RequestBody);

                    // Add Authorization Header
                    string encoded = Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes($"{apiuser}:{apipass}"));
                    request2.AddHeader("Authorization", "Basic " + encoded);

                    await SaveActivityLogTracker("Add remittance Headers: <br/>Pin No: Basic " + encoded, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "HNB API Proceed", entity.Branch_ID, Client_ID);

                    // Execute request
                    RestResponse response2 = client2.Execute(request2);
                    await SaveActivityLogTracker("Add remittance response parameters: <br/>" + response2.Content, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "HNB API Proceed", entity.Branch_ID, Client_ID);

                    // Parse response XML
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(response2.Content);
                    XmlNodeList responsestring = xmlDoc.GetElementsByTagName("ns2:return");

                    string message = "", responseCode = "";
                    foreach (XmlNode node in responsestring)
                    {
                        message = node["ns1:message"]?.InnerText ?? "";
                        responseCode = node["ns1:responseCode"]?.InnerText ?? "";
                    }
                    if (responseCode == "0")
                    {
                        apistatus = 0;
                        int mappingid = Convert.ToInt32(result.TransMap_ID);

                        if (mappingid > 0)
                        {
                            try { AgentRateapi = 0; } catch { }

                            try
                            {
                                var parameters = new
                                {
                                    _BranchListAPI_ID = api_id,
                                    _APIBranch_Details = entity.APIBranch_Details,
                                    _TransactionRef = result.APITransaction_ID.ToString(),
                                    _trn_referenceNo = AgentTransactionid,
                                    _APITransaction_Alert = 0,
                                    _Transaction_ID = entity.Transaction_ID,
                                    _Client_ID = entity.Client_ID,
                                    _payout_partner_rate = AgentRateapi,
                                };

                                await _dbConnection.ExecuteAsync("Update_TransactionDetails", parameters, commandType: CommandType.StoredProcedure);
                            }
                            catch (Exception ex)
                            {
                                return new ProceedResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = " Error Message: " + "Failed In Update Transaction Status",
                                    ApiId = api_id,
                                    AgentRate = AgentRateapi,
                                    ApiStatus = apistatus,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }
                        }
                        //End New code
                    }
                    else
                    {
                        return new ProceedResponseViewModel
                        {
                            Status = "Failed",
                            StatusCode = 2,
                            Message = " Error Message: " + "Failed In Api Code",
                            ApiId = api_id,
                            AgentRate = AgentRateapi,
                            ApiStatus = apistatus,
                            ExtraFields = new List<string> { "", "" }
                        };
                    }
                }
                catch (Exception ex)
                {
                    await SaveActivityLogTracker("Add remittance request parameters: <br/>Pin No: " + soap_req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "HNB API Proceed", entity.Branch_ID, Client_ID);
                    return new ProceedResponseViewModel
                    {
                        Status = "Failed",
                        StatusCode = 2,
                        Message = " Error Message: " + "Failed In Api Call",
                        ApiId = api_id,
                        AgentRate = AgentRateapi,
                        ApiStatus = apistatus,
                        ExtraFields = new List<string> { "", "" }
                    };

                }
                #endregion HNB API
            }

            else if (api_id == 9)//AZA
            {
                #region transfer result data in to dt 
                DataTable dt = new DataTable();
                try
                {
                    if (result != null)
                    {
                        var dict = (IDictionary<string, object>)result;
                        foreach (var key in dict.Keys)
                        {
                            string columnName = key;
                            int counter = 1;
                            while (dt.Columns.Contains(columnName))
                            {
                                columnName = key + counter;
                                counter++;
                            }

                            dt.Columns.Add(columnName, dict[key]?.GetType() ?? typeof(object));
                        }
                      
                        DataRow row = dt.NewRow();
                        int colIndex = 0;
                        foreach (var kvp in dict)
                        {
                            // Use the same logic as above to find matching column name
                            string columnName = kvp.Key;
                            int counter = 1;
                            while (!dt.Columns.Contains(columnName))
                            {
                                columnName = kvp.Key + counter;
                                counter++;
                            }

                            row[columnName] = kvp.Value ?? DBNull.Value;
                            colIndex++;
                        }
                        dt.Rows.Add(row);
                    }
                }
                catch (Exception exp)
                {
                    return new ProceedResponseViewModel
                    {
                        Status = "Failed",
                        StatusCode = 2,
                        Message = " Error Message: " + exp.Message,
                        ApiId = api_id,
                        AgentRate = AgentRateapi,
                        ApiStatus = apistatus,
                        ExtraFields = new List<string> { "", "" }
                    };
                }
                #endregion transfer result data in to dt 


                #region TransferZero
                try
                {
                    await SaveActivityLogTracker("AZA step 1.0", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "AZA Transaction Proceed", entity.Branch_ID, Client_ID);

                    //string cust_id = Convert.ToString(dt.Rows[0]["WireTransfer_ReferanceNo"]);
                    TransferZero.Sdk.Client.Configuration configuration = new TransferZero.Sdk.Client.Configuration();
                    await SaveActivityLogTracker("AZA step 1.1 ", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "AZA Transaction Proceed", entity.Branch_ID, Client_ID);


                    configuration.ApiKey = accesscode;
                    configuration.ApiSecret = apipass;
                    configuration.BasePath = apiurl;
                    await SaveActivityLogTracker("AZA step 1.2 ", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "AZA Transaction Proceed", entity.Branch_ID, Client_ID);

                    var apiInstance = new CurrencyInfoApi(configuration);
                    ////t.BranchListAPI_ID = api_id;
                    ////t.Is_Procedure = "QUERY";
                    ////t.Operation_Name = "Proceed_mail_details";
                    //DataTable dt = new DataTable();
                    Guid Customer_API_ID = new Guid();
                    await SaveActivityLogTracker("AZA step 1.3 Count ", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "AZA Transaction Proceed", entity.Branch_ID, Client_ID);

                    if (result.Customer_API_ID != "0" && result.Customer_API_ID != "")
                    {
                        Customer_API_ID = Guid.Parse(result.Customer_API_ID.ToString());
                        //Customer_API_ID = Guid.Parse(dt.Rows[0]["Customer_API_ID"].ToString());
                    }
                    else
                    {
                        //if ((dt.Rows[0]["Sender_DOB"]).ToString() != "" && (dt.Rows[0]["Email_ID"]).ToString() != "" && ((dt.Rows[0]["Mobile_Number"]).ToString() != "" || (dt.Rows[0]["Phone_Number"]).ToString() != ""))
                        if ((result.Sender_DOB.ToString() != "" && result.Email_ID != "" && (result.Mobile_Number.ToString() != "" || result.Phone_Number.ToString() != "")))
                        {
                            //SenderResponse response = TransferZeroCreateSender(configuration, dt, t);
                            //Customer_API_ID = (Guid)response.Object.Id;
                            //t.Cust_API_ID = Customer_API_ID.ToString();
                            //t.Is_Procedure = "QUERY";
                            //t.Operation_Name = "CustomerAPIid_Insert";
                            //t.InsertToDatabase();
                        }
                        else
                        {
                            string err_msg = "";
                            //if ((dt.Rows[0]["Sender_DOB"]).ToString() == "")
                            if (result.Sender_DOB == "")
                            {
                                if (err_msg != "")
                                    err_msg = err_msg + ", Birthdate ";
                                else
                                    err_msg = " Birthdate ";
                            }
                            //if ((dt.Rows[0]["Email_ID"]).ToString() == "")
                            if (result.Email_ID == "")
                            {
                                if (err_msg != "")
                                    err_msg = err_msg + ", Email ";
                                else
                                    err_msg = " Email ";
                            }
                            //if ((dt.Rows[0]["Mobile_Number"]).ToString() == "" && (dt.Rows[0]["Phone_Number"]).ToString() == "")
                            if (result.Mobile_Number == "" && result.Phone_Number == "")
                            {
                                if (err_msg != "")
                                    err_msg = err_msg + ", Mobile Number Or Phone Number ";
                                else
                                    err_msg = " Mobile Number Or Phone Number ";
                                if (err_msg != "")
                                    err_msg = err_msg + ", ";
                                else
                                    err_msg = " Mobile Number Or Phone Number ";
                            }
                            await SaveActivityLogTracker("Validation Error : Sender " + err_msg + "details are required", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "AZA Proceed", entity.Branch_ID, Client_ID);

                            //ds.Rows.Add(2, "Validation Error : Sender " + err_msg + "details are required"); return ds;
                        }
                    }
                    //Transaction Getresult = TransferZeroGetTransactionFromExternalId(configuration, "TRANSACTION-" + Convert.ToString(dt.Rows[0]["ReferenceNo"]).Substring(2));
                    Transaction Getresult = TransferZeroGetTransactionFromExternalId(configuration, "TRANSACTION-" + result.ReferenceNo.Substring(2));
                    await SaveActivityLogTracker("AZA step 2 ", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "AZA Transaction Proceed", entity.Branch_ID, Client_ID);

                    TransactionResponse Transresponse = null;
                    if (Getresult == null)
                        //Transresponse = TransferZeroCreateTransaction(configuration, dt, Customer_API_ID.ToString(), PaymentDepositType_ID, t);
                        Transresponse = TransferZeroCreateTransaction(configuration, dt, Customer_API_ID.ToString(), result.PaymentDepositType_ID);
                    await SaveActivityLogTracker("AZA step 3 ", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "AZA Transaction Proceed", entity.Branch_ID, Client_ID);

                    if (Transresponse != null || Getresult != null)
                    {
                        Guid Trans_ID = new Guid();
                        if (Transresponse != null)
                        {
                            if (Transresponse.Object.Id != null)
                            {
                                string json = Newtonsoft.Json.JsonConvert.SerializeObject(Transresponse);
                                await SaveActivityLogTracker("Create Transaction response parameters: <br/>" + json + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "AZA Transaction Proceed", entity.Branch_ID, Client_ID);

                                Trans_ID = (Guid)Transresponse.Object.Id;
                            }
                            else
                            {
                                string json1 = Newtonsoft.Json.JsonConvert.SerializeObject(Transresponse.Object.Errors);
                                await SaveActivityLogTracker("Exception API parameters: <br/>" + json1 + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "AZA Transaction Proceed", entity.Branch_ID, Client_ID);

                                // ds.Rows.Add(2, " Error Message: Validation Error :" + json1); return ds;
                            }
                        }
                        else if (Getresult != null)
                        {
                            if (Getresult.Id != null)
                            {
                                Trans_ID = (Guid)Getresult.Id;
                            }
                            else
                            {
                                string json1 = Newtonsoft.Json.JsonConvert.SerializeObject(Getresult.Errors);
                                await SaveActivityLogTracker("Exception API parameters: <br/>" + json1 + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "AZA Transaction Proceed", entity.Branch_ID, Client_ID);

                                //ds.Rows.Add(2, " Error Message: Validation Error :" + json1); return ds;
                            }
                        }
                        Guid? transactionId = Trans_ID;
                        Debit debit = new Debit(
                                //currency: Convert.ToString(dt.Rows[0]["Currency_Code"]),
                                //amount: Convert.ToDouble(dt.Rows[0]["AmountInPKR"]),
                                toId: transactionId,
                                toType: "Transaction"
                            );

                        DebitRequestWrapper debitRequest = new DebitRequestWrapper(debit: new List<Debit>() { debit });
                        ServicePointManager.Expect100Continue = true;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                               | SecurityProtocolType.Tls11
                               | SecurityProtocolType.Tls12
                               | SecurityProtocolType.Ssl3;
                        AccountValidationApi accountValidationApi = new AccountValidationApi(configuration);
                        AccountDebitsApi debitsApi = new AccountDebitsApi(configuration);
                        try
                        {
                            DebitListResponse debitListResponse = debitsApi.PostAccountsDebits(debitRequest);
                            string json2 = Newtonsoft.Json.JsonConvert.SerializeObject(debitListResponse);
                            await SaveActivityLogTracker("Debit Transaction request parameters: <br/>Pin No: " + json2, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "AZA Transaction Proceed", entity.Branch_ID, Client_ID);

                            string json1 = Newtonsoft.Json.JsonConvert.SerializeObject(debitListResponse);
                            await SaveActivityLogTracker("Debit Transaction request parameters: <br/> " + json1, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "AZA Transaction Proceed", entity.Branch_ID, Client_ID);

                            apistatus = 0;

                            string APIBranch_Details = entity.APIBranch_Details;
                            apistatus = 0;

                            int mappingid = Convert.ToInt32(dt.Rows[0]["TransMap_ID"]);

                        }

                        catch (Exception e)
                        {

                            if (e.ToString() == "")
                            {
                                Message1 = e.Message;
                                await SaveErrorLogAsync(e.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);

                            }
                            else
                            {
                                await SaveErrorLogAsync(e.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                await SaveActivityLogTracker("Debit Transaction Error response parameters: <br/> " + e.ToString(), 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "AZA Transaction Proceed", entity.Branch_ID, Client_ID);
                                throw e;
                            }
                        }
                    }
                    else
                    {
                        if (Transresponse.Object.Id != null)
                        {
                            string json1 = Newtonsoft.Json.JsonConvert.SerializeObject(Transresponse.Object.Errors);
                            await SaveActivityLogTracker("Exception API parameters: <br/>" + json1 + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "AZA Transaction Proceed", entity.Branch_ID, Client_ID);

                        }
                        if (Getresult.Id != null)
                        {
                            string json1 = Newtonsoft.Json.JsonConvert.SerializeObject(Getresult.Errors);
                            await SaveActivityLogTracker("Exception API parameters: <br/>" + json1 + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "AZA Transaction Proceed", entity.Branch_ID, Client_ID);

                        }
                    }
                }
                catch (Exception ex)
                {
                    Message1 = ex.Message;
                    await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                    await SaveActivityLogTracker("Exception API parameters: <br/>" + ex.ToString(), 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(entity.Transaction_ID), "AZA Transaction Proceed", entity.Branch_ID, Client_ID);

                }
                #endregion
            }

            else if (api_id == 29)
            {
                #region IPay
                try
                {
                    int PaymentDepositType_ID = Convert.ToInt32(result.PaymentDepositType_ID);
                    var body = "";
                    DateTime dateTime = DateTime.Now;

                    string Sender_Type = "Individual";
                    string scity = Convert.ToString(result.City_Name);
                    string country = Convert.ToString(result.Beneficiary_Country);
                    string apibank_code = Convert.ToString(result.bank_code);

                    await SaveActivityLogTracker(" Bank code in databse: <br/>" + apibank_code + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "Get IPay Transaction Status", entity.Branch_ID, Client_ID);

                    //string PaymentDepositType_ID = Convert.ToString(dt.Rows[0]["Beneficiary_Country"]);
                    /* if (api_fields != "" && api_fields != null)
                     {
                         Newtonsoft.Json.Linq.JObject obj = Newtonsoft.Json.Linq.JObject.Parse(api_fields);
                         string sourceId = Convert.ToString(obj["defaultgateway"]);
                     }*/
                    string bname = Convert.ToString(result.Beneficiary_Name1).Trim();
                    string bfname = bname;
                    string blname = " ";

                    if (bname.Contains(" "))
                    {
                        string[] spli = bname.Split(' ');
                        if (spli.Length > 1)
                        {
                            bfname = bname.Substring(0, (bname.Length - spli[spli.Length - 1].Length));
                            blname = spli[spli.Length - 1];
                        }
                    }

                    string Gender = result.Gender.Trim();
                    string genders = Gender;
                    if (Gender == "Male")
                    {
                        Gender = "Male";
                    }
                    else if (Gender == "Female")
                    {
                        Gender = "Female";
                    }
                    else
                    {
                        Gender = "Other";
                    }
                    string revtype = "Individual";


                    string idName = Convert.ToString(result.ID_Name);

                    string sendTransferPurpose = "";
                    string purposeCode = Convert.ToString(result.Purpose_Code);
                    switch (purposeCode)
                    {
                        case "Accounting services":
                            sendTransferPurpose = "Accounting services";
                            break;
                        case "Administrative Expenses":
                            sendTransferPurpose = "Administrative Expenses";
                            break;
                        case "Business profits":
                            sendTransferPurpose = "Business profits";
                            break;
                        case "Business travel":
                            sendTransferPurpose = "Business travel";
                            break;
                        case "Educational expenses":
                            sendTransferPurpose = "Educational expenses";
                            break;
                        case "Family maintenance / saving":
                            sendTransferPurpose = "Family maintenance / saving";
                            break;
                        case "Financial lease":
                            sendTransferPurpose = "Financial lease";
                            break;
                        case "Fines and Penalties":
                            sendTransferPurpose = "Fines and Penalties";
                            break;
                        case "Hotel expenses":
                            sendTransferPurpose = "Hotel expenses";
                            break;
                        case "Insurance premium":
                            sendTransferPurpose = "Insurance premium";
                            break;
                        case "Interest on loans":
                            sendTransferPurpose = "Interest on loans";
                            break;
                        case "Investment in real estate":
                            sendTransferPurpose = "Investment in real estate";
                            break;
                        case "Investment in securities":
                            sendTransferPurpose = "Investment in securities";
                            break;
                        case "Investment in shares":
                            sendTransferPurpose = "Investment in shares";
                            break;
                        case "Legal services":
                            sendTransferPurpose = "Legal services";
                            break;
                        case "Medical expenses":
                            sendTransferPurpose = "Medical expenses";
                            break;
                        case "Other Personal Services":
                            sendTransferPurpose = "Other Personal Services";
                            break;
                        case "Payment for Goods and services":
                            sendTransferPurpose = "Payment for Goods and services";
                            break;
                        case "Personal travel and tour":
                            sendTransferPurpose = "Personal travel and tour";
                            break;
                        case "Pilgrimage / Religious - Related":
                            sendTransferPurpose = "Pilgrimage / Religious - Related";
                            break;
                        case "Research and development services":
                            sendTransferPurpose = "Research and development services";
                            break;
                        case "Tax payment":
                            sendTransferPurpose = "Tax payment";
                            break;
                        default:
                            sendTransferPurpose = "Other Personal Services";
                            break;
                    }

                    string relationwithBen = "OTHERS";
                    if (!string.IsNullOrWhiteSpace(Convert.ToString(result.Relation)))
                    {
                        relationwithBen = Convert.ToString(result.Relation).Trim();
                    }

                    string payMethod_type = "";
                    string bankid = "";
                    string accountnumber = "";
                    string bankidSelected = "";
                    string locationidSelected = "";

                    //string payerIdValue = Convert.ToString(dictObjMain["APIBranch_Details"]);
                    string payerIdValue = entity.APIBranch_Details;
                    string[] elements = payerIdValue.Split('-');
                    string Location_id = (elements.Length > 0) ? elements[0].Trim() : "";
                    locationidSelected = Location_id;
                    try
                    {
                        bankidSelected = (elements.Length == 3) ? elements[2].Trim() : "";
                    }
                    catch (Exception ex)
                    {
                        bankidSelected = "";
                    }
                    string LOCATIONID = "";
                    try
                    {
                        if (PaymentDepositType_ID == 1) // BANK
                        {

                            payMethod_type = "D";
                        }
                        else
                        if (PaymentDepositType_ID == 2) // CASH
                        {
                            payMethod_type = "C";
                        }
                        else if (PaymentDepositType_ID == 3) // Wallet
                        {
                            payMethod_type = "W";
                        }
                        accountnumber = Convert.ToString(result.Account_Number);
                        if (country != "India")
                        {
                            string sign = accesscode + apiuser + dateTime + payMethod_type + country + apipass;
                            sign = ComputeSha256Hash(sign);


                            var options = new RestClientOptions(apiurl)
                            {
                                MaxTimeout = -1
                            };
                            var client = new RestClient(options);
                            var request = new RestRequest("/sendwsv4/webService.asmx")
                            {
                                Method = Method.Post
                            };

                            //var client = new RestClient(apiurl);
                            //var request = new RestRequest("/sendwsv4/webService.asmx");
                            //request.Method = Method.POST;
                            request.AddHeader("Content-Type", "text/xml; charset=utf-8");
                            request.AddHeader("SOAPAction", "WebServices/GetAgentList");
                            body = @"<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">" + "\n" +
                            @"  <soap:Body>" + "\n" +
                            @"    <GetAgentList xmlns=""WebServices"">" + "\n" +
                            @"      <AGENT_CODE>" + accesscode + "</AGENT_CODE>" + "\n" +
                            @"      <USER_ID>" + apiuser + "</USER_ID>" + "\n" +
                            @"      <AGENT_SESSION_ID>" + dateTime + "</AGENT_SESSION_ID> " + "\n" +
                            @"      <PAYMENTMODE>" + payMethod_type + "</PAYMENTMODE>" + "\n" +
                            @"      <PAYOUT_COUNTRY>" + country + "</PAYOUT_COUNTRY>" + "\n" +
                            @"      <SIGNATURE>" + sign + "</SIGNATURE>" + "\n" +
                            @"    </GetAgentList>" + "\n" +
                            @"  </soap:Body>" + "\n" +
                            @"</soap:Envelope>" + "\n" +
                            @"";
                            request.AddParameter("text/xml", body, ParameterType.RequestBody);
                            await SaveActivityLogTracker("IPay request parameters GetAgentlist: <br/>" + body + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, " Get IPay Transaction Status", entity.Branch_ID, Client_ID);

                            //mtsmethods.InsertActivityLogDetails(" IPay request parameters GetAgentlist: <br/>" + body + "", t.User_ID, t.Transaction_ID, t.User_ID, t.Customer_ID, " Get IPay Transaction Status", t.CB_ID, t.Client_ID);
                            RestResponse response = client.Execute(request); //RestResponse response = await client.ExecuteAsync(request);
                            Console.WriteLine(response.Content);
                            string soapResponse = response.Content;
                            XmlDocument doc = new XmlDocument();
                            doc.LoadXml(response.Content);
                            await SaveActivityLogTracker("IPay response parameters GetAgentlist: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, " Get IPay Transaction Status", entity.Branch_ID, Client_ID);

                            //mtsmethods.InsertActivityLogDetails(" IPay response parameters GetAgentlist: <br/>" + response.Content + "", t.User_ID, t.Transaction_ID, t.User_ID, t.Customer_ID, " Get IPay Transaction Status", t.CB_ID, t.Client_ID);
                            XmlNodeList nodeList = doc.GetElementsByTagName("GetAgentListResult");
                            foreach (XmlNode node in nodeList)
                            {
                                foreach (XmlNode child1 in node.ChildNodes)
                                {
                                    if (child1.Name == "Return_AGENTLIST")
                                    {
                                        string LOCATIONID1 = ""; string BANKID = "";
                                        string PAYOUT_AGENT_ID = "";

                                        foreach (XmlNode child in child1.ChildNodes)
                                        {
                                            if (child.Name == "PAYOUT_AGENT_ID") { PAYOUT_AGENT_ID = child.InnerText; }
                                            if (child.Name == "LOCATIONID") { LOCATIONID1 = child.InnerText; }
                                            if (child.Name == "BANKID") { BANKID = child.InnerText; }
                                        }
                                        if (apibank_code == BANKID)
                                        {
                                            LOCATIONID = LOCATIONID1;
                                            bankid = BANKID;
                                        }
                                        //if (PAYOUT_AGENT_ID == Location_id)
                                        //{
                                        //    bankid = BANKID;//mtsmethods.InsertActivityLogDetails(" Agentlist Selected value: <br/> PAYOUT_AGENT_ID:" + PAYOUT_AGENT_ID + " BANKID: "+BANKID + " LOCATIONID: " + LOCATIONID, t.User_ID, t.Transaction_ID, t.User_ID, t.Customer_ID, " Get IPay Transaction Status", t.CB_ID, t.Client_ID);
                                        //}
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Message1 = ex.Message;
                        await SaveActivityLogTracker("IPay Error Agentlist: <br/>" + ex.ToString() + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, " Get IPay Transaction Status", entity.Branch_ID, Client_ID);
                    }
                    string loc_id_india = "", bnk_id_india = "";
                    if (api_fields != "" && api_fields != null)
                    {
                        Newtonsoft.Json.Linq.JObject obj = Newtonsoft.Json.Linq.JObject.Parse(api_fields);
                        locationidSelected = Convert.ToString(obj["cash_location"]);
                        bankidSelected = Convert.ToString(obj["cash_bankid"]);
                        loc_id_india = Convert.ToString(obj["bank_location_india"]);
                        bnk_id_india = Convert.ToString(obj["bank_bankid_india"]);
                    }
                    if (payMethod_type == "C" && country == "Nepal")
                    {
                        LOCATIONID = bankidSelected;
                        bankid = locationidSelected;
                    }
                    if (payMethod_type == "D" && country == "India")
                    {
                        LOCATIONID = loc_id_india;
                        bankid = bnk_id_india;
                    }
                    //if (bankidSelected != "")
                    //{
                    //    bankid = bankidSelected;
                    //}
                    string ph_no = "";
                    if (!string.IsNullOrEmpty(result.Mobile_Number))
                    {
                        ph_no = result.Mobile_Number;
                    }
                    else if (!string.IsNullOrEmpty(result.Phone_Number))
                    {
                        ph_no = result.Phone_Number;
                    }
                    string sfname = Convert.ToString(result.First_Name);
                    string agenttid = Convert.ToString(result.ReferenceNo);
                    string smname = Convert.ToString(result.Middle_Name);
                    string slname = Convert.ToString(result.Last_Name);
                    string saddress = Convert.ToString(result.sender_address);
                    string postcode = Convert.ToString(result.Post_Code);
                    string scountry = Convert.ToString(result.Country_Name);
                    string senderID = Convert.ToString(result.SenderID_Number);
                    string nationality = Convert.ToString(result.Nationality_Country);

                    // Dates
                    string formattedDate = Convert.ToString(result.Issue_Date);
                    string formattedDate1 = Convert.ToString(result.SenderID_ExpiryDate);
                    string formattedDate2 = Convert.ToString(result.Sender_DateOfBirth);

                    await SaveActivityLogTracker("IPay test: issue: <br/>" + formattedDate + "  & sexpiry " + formattedDate1 + "   & sdob:" + formattedDate2 + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, " Get IPay Transaction Status", entity.Branch_ID, Client_ID);

                    string sOccupation = Convert.ToString(result.Profession);
                    switch (sOccupation)
                    {
                        case "Accountant":
                            sOccupation = "Accountant";
                            break;
                        case "Actor /Actress":
                            sOccupation = "Actor /Actress";
                            break;
                        case "Architect":
                            sOccupation = "Architect";
                            break;
                        case "Artist":
                            sOccupation = "Artist";
                            break;
                        case "Broker":
                            sOccupation = "Broker";
                            break;
                        case "Butcher":
                            sOccupation = "Butcher";
                            break;
                        case "Carpenter":
                            sOccupation = "Carpenter";
                            break;
                        case "Chef":
                            sOccupation = "Chef";
                            break;
                        case "Consultant":
                            sOccupation = "Consultant";
                            break;
                        case "Dentist":
                            sOccupation = "Dentist";
                            break;
                        case "Designer":
                            sOccupation = "Designer";
                            break;
                        case "Doctor":
                            sOccupation = "Doctor";
                            break;
                        case "Driver":
                            sOccupation = "Driver";
                            break;
                        case "Electrician":
                            sOccupation = "Electrician";
                            break;
                        case "Engineer":
                            sOccupation = "Engineer";
                            break;
                        case "Factory worker":
                            sOccupation = "Factory worker";
                            break;
                        case "Fisherman":
                            sOccupation = "Fisherman";
                            break;
                        case "Gardener":
                            sOccupation = "Gardener";
                            break;
                        case "Government worker":
                            sOccupation = "Government worker";
                            break;
                        case "Journalist":
                            sOccupation = "Journalist";
                            break;
                        case "Judge":
                            sOccupation = "Judge";
                            break;
                        case "Labourer":
                            sOccupation = "Labourer";
                            break;
                        case "Lawyer":
                            sOccupation = "Lawyer";
                            break;
                        case "Mechanic":
                            sOccupation = "Mechanic";
                            break;
                        case "Musician":
                            sOccupation = "Musician";
                            break;
                        case "Nurses":
                            sOccupation = "Nurses";
                            break;
                        case "Physician":
                            sOccupation = "Physician";
                            break;
                        case "Pilot":
                            sOccupation = "Pilot";
                            break;
                        case "Plumber":
                            sOccupation = "Plumber";
                            break;
                        case "Police Officer":
                            sOccupation = "Police Officer";
                            break;
                        case "Real estate agent":
                            sOccupation = "Real estate agent";
                            break;
                        case "Receptionist":
                            sOccupation = "Receptionist";
                            break;
                        case "Salesman":
                            sOccupation = "Salesman";
                            break;
                        case "Scientist":
                            sOccupation = "Scientist";
                            break;
                        case "Secretary":
                            sOccupation = "Secretary";
                            break;
                        case "Software developer":
                            sOccupation = "Software developer";
                            break;
                        case "Tailor":
                            sOccupation = "Tailor";
                            break;
                        case "Teacher":
                            sOccupation = "Teacher";
                            break;
                        case "Technician":
                            sOccupation = "Technician";
                            break;
                        case "Travel agent":
                            sOccupation = "Travel agent";
                            break;
                        case "Waiter/Waitress":
                            sOccupation = "Waiter/Waitress";
                            break;

                    }


                    string sof = "Salary";
                    string benaddress = Convert.ToString(result.Beneficiary_Address);
                    string bdob = Convert.ToString(result.benef_DOB_ymd);
                    string calc_by = "P";
                    string tamount = Convert.ToString(result.AmountInPKR);
                    string currcode = Convert.ToString(result.Currency_Code);
                    string acctype = "Saving";
                    string sstate = "UK";

                    string PAYOUT_BANKCODE = "";
                    if (country == "India" && PaymentDepositType_ID == 1)
                    {
                        PAYOUT_BANKCODE = Convert.ToString(result.Ifsc_Code);
                    }


                    try
                    {
                        string sign = accesscode + apiuser + dateTime.ToString("ddMMyyyyHHmmss") + agenttid + LOCATIONID + Sender_Type + sfname + smname + slname + Gender + saddress + scity + sstate + postcode + scountry + ph_no + nationality + scountry +
                            idName + senderID + formattedDate + formattedDate1 + formattedDate2 + sOccupation + sof + relationwithBen + sendTransferPurpose + revtype + bfname + blname + benaddress +
                            country + country + bdob + calc_by + tamount + currcode + payMethod_type + acctype + bankid + accountnumber + "N" + PAYOUT_BANKCODE + apipass;

                        await SaveActivityLogTracker(" IPay sign parameter: <br/>" + sign + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, " Get IPay Transaction Status", entity.Branch_ID, Client_ID);

                        sign = ComputeSha256Hash(sign);
                        var options = new RestClientOptions(apiurl)
                        {
                            MaxTimeout = -1
                        };
                        var client = new RestClient(options);
                        var request = new RestRequest("/sendwsv4/webService.asmx")
                        {
                            Method = Method.Post
                        };


                        //var client = new RestClient(apiurl);
                        //client.Timeout = -1;
                        //var request = new RestRequest("/sendwsv4/webService.asmx");
                        //request.Method = Method.POST;
                        request.AddHeader("Content-Type", "text/xml; charset=utf-8");
                        request.AddHeader("SOAPAction", "WebServices/SendTransaction");
                        body = @"<?xml version=""1.0"" encoding=""utf-8""?>" + "\n" +
                       @"<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">" + "\n" +
                       @"  <soap:Body>" + "\n" +
                       @"    <SendTransaction xmlns=""WebServices"">" + "\n" +
                       @"      <AGENT_CODE>" + accesscode + "</AGENT_CODE>" + "\n" +
                       @"      <USER_ID>" + apiuser + "</USER_ID>" + "\n" +
                       @"      <AGENT_SESSION_ID>" + dateTime.ToString("ddMMyyyyHHmmss") + "</AGENT_SESSION_ID>" + "\n" +
                       @"      <AGENT_TXNID>" + agenttid + "</AGENT_TXNID>" + "\n" +
                       @"      <LOCATION_ID>" + LOCATIONID + "</LOCATION_ID>" + "\n" +
                       @"      <SENDER_TYPE>" + Sender_Type + "</SENDER_TYPE>" + "\n" +
                       @"      <SENDER_FIRST_NAME>" + sfname + "</SENDER_FIRST_NAME>" + "\n" +
                       @"      <SENDER_MIDDLE_NAME>" + smname + "</SENDER_MIDDLE_NAME>" + "\n" +
                       @"      <SENDER_LAST_NAME>" + slname + "</SENDER_LAST_NAME>" + "\n" +
                       @"      <SENDER_GENDER>" + Gender + "</SENDER_GENDER>" + "\n" +
                       @"      <SENDER_ADDRESS>" + saddress + "</SENDER_ADDRESS>" + "\n" +
                       @"      <SENDER_CITY>" + scity + "</SENDER_CITY>" + "\n" +
                       @"      <SENDER_STATES>" + sstate + "</SENDER_STATES>" + "\n" +
                       @"      <SENDER_ZIPCODE>" + postcode + "</SENDER_ZIPCODE>" + "\n" +
                       @"      <SENDER_COUNTRY>" + scountry + "</SENDER_COUNTRY>" + "\n" +
                       @"      <SENDER_MOBILE>" + ph_no + "</SENDER_MOBILE>" + "\n" +
                       @"      <SENDER_NATIONALITY>" + nationality + "</SENDER_NATIONALITY>" + "\n" +
                       @"      <SENDER_ADDRESS_COUNTRY>" + scountry + "</SENDER_ADDRESS_COUNTRY>" + "\n" +
                       @"      <SENDER_ID_TYPE>" + idName + "</SENDER_ID_TYPE>" + "\n" +
                       @"      <SENDER_ID_NUMBER>" + senderID + "</SENDER_ID_NUMBER>" + "\n" +
                       @"      <SENDER_ID_ISSUE_DATE>" + formattedDate + "</SENDER_ID_ISSUE_DATE>" + "\n" +
                       @"      <SENDER_ID_EXPIRE_DATE>" + formattedDate1 + "</SENDER_ID_EXPIRE_DATE>" + "\n" +
                       @"      <SENDER_DATE_OF_BIRTH>" + formattedDate2 + "</SENDER_DATE_OF_BIRTH>" + "\n" +
                       @"      <SENDER_OCCUPATION>" + sOccupation + "</SENDER_OCCUPATION>" + "\n" +
                       @"      <SENDER_SOURCE_OF_FUND>" + sof + "</SENDER_SOURCE_OF_FUND>" + "\n" +
                       @"      <SENDER_BENEFICIARY_RELATIONSHIP>" + relationwithBen + "</SENDER_BENEFICIARY_RELATIONSHIP>" + "\n" +
                       @"      <PURPOSE_OF_REMITTANCE>" + sendTransferPurpose + "</PURPOSE_OF_REMITTANCE>" + "\n" +
                       @"      <RECEIVER_TYPE>" + revtype + "</RECEIVER_TYPE>" + "\n" +
                       @"      <RECEIVER_FIRST_NAME>" + bfname + "</RECEIVER_FIRST_NAME>" + "\n" +
                       @"      <RECEIVER_MIDDLE_NAME></RECEIVER_MIDDLE_NAME>" + "\n" +
                       @"      <RECEIVER_LAST_NAME>" + blname + "</RECEIVER_LAST_NAME>" + "\n" +
                       @"      <RECEIVER_ADDRESS>" + benaddress + "</RECEIVER_ADDRESS>" + "\n" +
                       @"      <RECEIVER_COUNTRY>" + country + "</RECEIVER_COUNTRY>" + "\n" +
                       @"      <RECEIVER_NATIONALITY>" + country + "</RECEIVER_NATIONALITY>" + "\n" +
                       @"      <RECEIVER_ID_TYPE></RECEIVER_ID_TYPE>" + "\n" +
                       @"      <RECEIVER_ID_NUMBER></RECEIVER_ID_NUMBER>" + "\n" +
                       @"      <RECEIVER_ID_EXPIRY_DATE></RECEIVER_ID_EXPIRY_DATE>" + "\n" +
                       @"      <RECEIVER_ZIPCODE></RECEIVER_ZIPCODE>" + "\n" +
                       @"      <RECEIVER_STATES></RECEIVER_STATES>" + "\n" +
                       @"      <RECEIVER_DATE_OF_BIRTH>" + bdob + "</RECEIVER_DATE_OF_BIRTH>" + "\n" +
                       @"      <CALC_BY>" + calc_by + "</CALC_BY>" + "\n" +
                       @"      <TRANSFER_AMOUNT>" + tamount + "</TRANSFER_AMOUNT>" + "\n" +
                       @"      <TRANSFER_CURRENCY>" + currcode + "</TRANSFER_CURRENCY>" + "\n" +
                       @"       <PAYMENTMODE>" + payMethod_type + "</PAYMENTMODE>" + "\n" +
                       @"      <ACCOUNT_TYPE>" + acctype + "</ACCOUNT_TYPE>" + "\n" +
                       @"      <BANKID>" + bankid + "</BANKID>" + "\n" +
                       @"      <BANK_NAME></BANK_NAME>" + "\n" +
                       @"      <BANK_BRANCHID></BANK_BRANCHID>" + "\n" +
                       @"      <BANK_BRANCH_NAME></BANK_BRANCH_NAME>" + "\n" +
                       @"      <BANK_ACCOUNT_NUMBER>" + accountnumber + "</BANK_ACCOUNT_NUMBER>" + "\n" +
                       @"      <AUTHORIZED_REQUIRED>N</AUTHORIZED_REQUIRED>" + "\n" +
                       @"      <PAYOUT_BANKCODE>" + PAYOUT_BANKCODE + "</PAYOUT_BANKCODE>" + "\n" +
                       @"      <SIGNATURE>" + sign + "</SIGNATURE>" + "\n" +
                       @"    </SendTransaction>" + "\n" +
                       @"  </soap:Body>" + "\n" +
                       @"</soap:Envelope>" + "\n" +
                       @"";
                        request.AddParameter("text/xml", body, ParameterType.RequestBody);
                        await SaveActivityLogTracker("IPay request parameters: <br/>" + body + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, " Get IPay Transaction Status", entity.Branch_ID, Client_ID);

                        RestResponse response = client.Execute(request);
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(response.Content);
                        await SaveActivityLogTracker("IPay response parameters: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, " Get IPay Transaction Status", entity.Branch_ID, Client_ID);

                        XmlNodeList nodeList = doc.GetElementsByTagName("SendTransactionResult");
                        string CODE = "", PINNO = "", EXCHANGE_RATE = "", SERVICE_CHARGE = "", messageDisplay = "";
                        foreach (XmlNode node in nodeList)
                        {
                            foreach (XmlNode child in node.ChildNodes)
                            {
                                try { if (child.Name == "CODE") { CODE = child.InnerText; } } catch (Exception ex) { }
                                ;
                                try { if (child.Name == "MESSAGE") { messageDisplay = child.InnerText; } } catch (Exception ex) { }
                                ;
                                try { if (child.Name == "PINNO") { PINNO = child.InnerText; } } catch (Exception ex) { }
                                ;
                                try { if (child.Name == "EXCHANGE_RATE") { EXCHANGE_RATE = child.InnerText; } } catch (Exception ex) { }
                                ;
                                try { if (child.Name == "SERVICE_CHARGE") { SERVICE_CHARGE = child.InnerText; } } catch (Exception ex) { }
                                ;
                            }
                        }
                        if (CODE == "0")
                        {
                            //string dataref = Convert.ToString(json["Data"]["ReferenceNum"]);
                            //string APIBranch_Details = Convert.ToString(dictObjMain["APIBranch_Details"]);
                            string APIBranch_Details = entity.APIBranch_Details;


                            //New code
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
                                    var parameters = new
                                    {
                                        _BranchListAPI_ID = api_id,
                                        _APIBranch_Details = APIBranch_Details,
                                        _TransactionRef = result.APITransaction_ID.ToString(),
                                        _trn_referenceNo = PINNO,
                                        _APITransaction_Alert = 0,
                                        _Transaction_ID = entity.Transaction_ID,
                                        _Client_ID = entity.Client_ID,
                                        _payout_partner_rate = Convert.ToDouble(EXCHANGE_RATE),
                                    };

                                    var rowsAffected = await _dbConnection.ExecuteAsync("Update_TransactionDetails", parameters, commandType: CommandType.StoredProcedure);
                                    apistatus = 0;
                                }
                                catch (Exception ex)
                                {
                                    Message1 = ex.Message;
                                    await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Proceed", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                }
                            }
                            //End New code
                        }
                        else
                        {
                            return new ProceedResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = " Error Message: " + messageDisplay + " - " + CODE,
                                ApiId = api_id,
                                AgentRate = AgentRateapi,
                                ApiStatus = apistatus,
                                ExtraFields = new List<string> { "", "" }
                            };
                            //ds.Rows.Add(2, " Error Message: " + messageDisplay + " - " + CODE); return ds;
                        }

                    }

                    catch (Exception ex)
                    {
                        await SaveActivityLogTracker("IPay Proceed Error: <br/>" + ex.ToString() + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "IPay Transaction", entity.Branch_ID, Client_ID);
                        return new ProceedResponseViewModel
                        {
                            Status = "Failed",
                            StatusCode = 2,
                            Message = " Error Message: " + ex.Message,
                            ApiId = api_id,
                            AgentRate = AgentRateapi,
                            ApiStatus = apistatus,
                            ExtraFields = new List<string> { "", "" }
                        };

                    }
                }
                catch (Exception ex)
                {
                    await SaveActivityLogTracker("IPay Proceed Error: <br/>" + ex.ToString() + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, 0, "IPayProceed Transaction", entity.Branch_ID, Client_ID);
                    return new ProceedResponseViewModel
                    {
                        Status = "Failed",
                        StatusCode = 2,
                        Message = " Error Message: " + ex.Message,
                        ApiId = api_id,
                        AgentRate = AgentRateapi,
                        ApiStatus = apistatus,
                        ExtraFields = new List<string> { "", "" }
                    };

                }
                #endregion IPay
            }
            else if(api_id == 49)
            {
                #region Hello Paisa
                string HelloPaisa_IntegrationProject_logs="";
                try
                {
                    try
                    {
                        string jsonResponse = "";
                        JObject responseObject = null;
                        string BankRoute = Convert.ToString(result.bank_code);
                        string bankBranch = Convert.ToString(result.BranchCode);

                        int Customer_ID= Convert.ToInt32(result.Customer_ID);
                        string bankAcc = Convert.ToString(result.Account_Number);
                        string SubAgent = Convert.ToString(result.Bank_Name);
                        string AmountInGBP = Convert.ToString(result.AmountInpkr);
                        int AmountInGBPAsInt = Convert.ToInt32(Convert.ToDecimal(AmountInGBP));
                        string ReferenceNo = Convert.ToString(result.ReferenceNo);
                        HelloPaisa_IntegrationProject_logs=" Hello Paisa API Request Data Preparation Started for Transaction ID: "+ Transaction_ID.ToString() + " , ReferenceNo: " + ReferenceNo + " , Customer_ID: " + Customer_ID.ToString() + " ";
                        string senderFirstName = Convert.ToString(result.First_Name);
                        string landedCurrency = Convert.ToString(result.Currency_Code);
                        string senderMiddleName = Convert.ToString(result.Middle_Name);
                        string senderLastName = Convert.ToString(result.Last_Name);
                        string senderAddress = Convert.ToString(result.Country_Name);
                        //string PayoutCountry = Convert.ToString(dtt.Rows[0]["sendercountrycode"]);
                        string PayoutCountry = Convert.ToString(result.ISO_Code);
                        string benf_ISO_Code = Convert.ToString(result.benf_ISO_Code);
                        string BeneficiaryEmail = Convert.ToString(result.Email_ID);
                        HelloPaisa_IntegrationProject_logs= HelloPaisa_IntegrationProject_logs + " Sender and Beneficiary Basic Details Fetched. ";
                        string SenderAddressCountry = Convert.ToString(result.ISO_Code_Three);
                        string senderMobile = Convert.ToString(result.Mobile_Number);
                        string senderIdNumber = Convert.ToString(result.SenderID_Number);
                        string CustomerDateOfBirth = Convert.ToString(result.Sender_DOB);
                        string CustomerIdExpiryDate = Convert.ToString(result.SenderID_ExpiryDate);
                        string senderIdIssueDate = Convert.ToString(result.Issue_Datemdy);
                        string senderIdIssueCountry = Convert.ToString(result.NISO_Code_Three);
                        string senderZipCode = Convert.ToString(result.Post_Code);
                        HelloPaisa_IntegrationProject_logs= HelloPaisa_IntegrationProject_logs + " Sender ID Details Fetched. ";
                        string RemitPurpose = "";
                        if (!string.IsNullOrWhiteSpace(Convert.ToString(result.Purpose)))
                        {
                            RemitPurpose = Convert.ToString(result.Purpose);
                        }
                        else { RemitPurpose = "other"; }
                        HelloPaisa_IntegrationProject_logs= HelloPaisa_IntegrationProject_logs + " Remittance Purpose Fetched. ";
                        string receiverAddress = Convert.ToString(result.Beneficiary_Address);
                        string receiverContactNumber = Convert.ToString(result.Beneficiary_Mobile);
                        string receiverCity = Convert.ToString(result.Beneficiary_City);
                        string BeneficiaryNationalityCountryISOCode = Convert.ToString(result.BISO_Code_Three);
                        string transactionDate = Convert.ToString(result.transaction_date);
                        HelloPaisa_IntegrationProject_logs= HelloPaisa_IntegrationProject_logs + " Beneficiary Details Fetched. ";
                        string Beneficiary_Name = "";
                        if (!string.IsNullOrWhiteSpace(Convert.ToString(result.Beneficiary_Name)))
                        {
                            Beneficiary_Name = Convert.ToString(result.Beneficiary_Name);
                        }
                        else
                        {
                            Beneficiary_Name = Convert.ToString(result.Beneficiary_Name1);
                        }

                        HelloPaisa_IntegrationProject_logs= HelloPaisa_IntegrationProject_logs + " Beneficiary Name Fetched. ";
                        string SenderFirstName = Convert.ToString(result.First_Name);
                        string SenderLastName = Convert.ToString(result.Last_Name);
                        string SenderMSISDN = Convert.ToString(result.Mobile_Number);
                        string BeneficiaryMsisdn = Convert.ToString(result.Beneficiary_Mobile);
                        HelloPaisa_IntegrationProject_logs= HelloPaisa_IntegrationProject_logs + " Sender and Beneficiary MSISDN Fetched. ";
                        //string ForeignCurrency = Convert.ToString(dtt.Rows[0]["FromCurrency_Code"]);
                        string BeneficiaryAddress = Convert.ToString(result.Beneficiary_Address);
                        string BeneficiaryBranchCode = Convert.ToString(result.BranchCode);
                        if (Convert.ToString(result.Ifsc_Code) != null)
                        {
                            BeneficiaryBranchCode = Convert.ToString(result.Ifsc_Code);
                        }
                        string BeneficiaryAccountNo = Convert.ToString(result.Account_Number);
                        string SenderAddress = Convert.ToString(result.sender_address);
                        int PaymentDepositType_ID = Convert.ToInt32(result.PaymentDepositType_ID);
                        string BeneficiaryFirstName = "";
                        string BeneficiaryMiddleName = "";
                        string BeneficiaryLastName = "";
                        HelloPaisa_IntegrationProject_logs= HelloPaisa_IntegrationProject_logs + " Payment Deposit Type ID Fetched: " + PaymentDepositType_ID.ToString() + " ";
                        if (!string.IsNullOrWhiteSpace(Beneficiary_Name))
                        {

                            //string[] parts = Beneficiary_Name.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                            //BeneficiaryFirstName = parts[0];
                            //BeneficiaryLastName = parts.Length > 1 ? parts[1] : "";
                            string firstName = "", middleName = "", lastName = "";

                            string[] nameParts = Beneficiary_Name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            if (nameParts.Length == 2)
                            {
                                BeneficiaryFirstName = nameParts[0];
                                BeneficiaryLastName = nameParts[1];
                            }
                            else if (nameParts.Length >= 3)
                            {
                                BeneficiaryFirstName = nameParts[0];
                                BeneficiaryMiddleName = nameParts[1];
                                BeneficiaryLastName = string.Join(" ", nameParts.Skip(2)); // support 3+ names like "pradip manohar deshmukh rao"
                            }
                            else if (nameParts.Length == 1)
                            {
                                BeneficiaryFirstName = nameParts[0];
                            }
                            if (!string.IsNullOrWhiteSpace(BeneficiaryMiddleName))
                            {
                                BeneficiaryFirstName = BeneficiaryFirstName + " " + BeneficiaryMiddleName;
                            }
                        }
                        HelloPaisa_IntegrationProject_logs= HelloPaisa_IntegrationProject_logs + " Beneficiary Name Split into First and Last Names. ";
                        string ForeignAmount = Convert.ToString(result.AmountInPKR);
                        string PayoutType = "";
                        //string PayoutType = "sdsfsfd";
                        string BranchName = "";

                        string Doc_ID_Name = Convert.ToString(result.ID_Name);
                        string IdType = "";
                        string CustomerIDType = "";
                        if (Doc_ID_Name == "Passport") { CustomerIDType = "53"; IdType = "3"; }
                        else { CustomerIDType = "0"; IdType = "53"; }
                        // CustomerIDType: 0 = Id Document 53 = Passport
                        //IDType: 53 for customers with ID document 3 for customers with Passports or Asylums
                        HelloPaisa_IntegrationProject_logs= HelloPaisa_IntegrationProject_logs + " Customer ID Type and ID Type Set. ";
                        string BuildingNo = Convert.ToString(result.House_Number);
                        string StreetNo = Convert.ToString(result.Street);
                        string SenderGender = Convert.ToString(result.Gender);//M=F
                        if (!string.IsNullOrWhiteSpace(SenderGender))
                        {
                            if (SenderGender == "Male") SenderGender = "M";
                            else { SenderGender = "F"; }
                        }
                        else { SenderGender = ""; }
                        HelloPaisa_IntegrationProject_logs = HelloPaisa_IntegrationProject_logs + " SenderGender=" + SenderGender;
                        string SenderAddressZIP = Convert.ToString(result.Post_Code);
                        string SenderCity = Convert.ToString(result.City_Name);
                        string SenderCountry = Convert.ToString(result.ISO_Code_Three);
                        string SenderCurrency = Convert.ToString(result.FromCurrency_Code);
                        HelloPaisa_IntegrationProject_logs= HelloPaisa_IntegrationProject_logs + " Sender Address and Location Details Fetched. ";
                        string SenderAddressStreet = StreetNo;
                        string BeneficiaryBranchName = Convert.ToString(result.Branch);
                        if (string.IsNullOrWhiteSpace(BeneficiaryBranchName))
                        {
                            BeneficiaryBranchName = Convert.ToString(result.Branch1);
                        }
                        HelloPaisa_IntegrationProject_logs= HelloPaisa_IntegrationProject_logs + " Beneficiary Branch Name Fetched. ";
                        string RoutingCode = "";
                        if (api_fields != "" && api_fields != null)
                        {
                            Newtonsoft.Json.Linq.JObject obj = Newtonsoft.Json.Linq.JObject.Parse(api_fields);
                            RoutingCode = Convert.ToString(obj["Routingcode"]);
                        }

                        if (PaymentDepositType_ID == 1) { PayoutType = "BankAccount"; }
                        else if (PaymentDepositType_ID == 2 || PaymentDepositType_ID == 3)
                        {
                            HelloPaisa_IntegrationProject_logs= HelloPaisa_IntegrationProject_logs + " Payment Deposit Type is COTC or Mobile Wallet. Preparing to fetch additional details from API Response. ";
                            BeneficiaryBranchName = "Anywhere";
                            BeneficiaryBranchCode = "";
                            if (PaymentDepositType_ID == 2)
                            {
                                PayoutType = "COTC";
                             
                                   
                                    string payerId_datafull = entity.payerId_datafull;

                                    if (!string.IsNullOrEmpty(payerId_datafull))
                                    {
                                        // Split the string by '|'
                                        string[] parts = payerId_datafull.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                                        SubAgent = parts.Length > 0 ? parts[0].Trim() : "";
                                        BankRoute = parts.Length > 2 ? parts[2].Trim() : "";
                                    }
                                
                            }
                            HelloPaisa_IntegrationProject_logs= HelloPaisa_IntegrationProject_logs + " SubAgent and BankRoute fetched for COTC. ";
                            if (PaymentDepositType_ID == 3)
                            {
                                string Mobile_providerID = Convert.ToString(result.Mobile_provider);
                                string Provider_name = Convert.ToString(result.Provider_name);
                                //Get_Provider_Details_By_Id
                                try
                                {
                                    string clouse = "pt.Provider_Id = " + Mobile_providerID;

                                    HelloPaisa_IntegrationProject_logs= HelloPaisa_IntegrationProject_logs + " Fetching Provider Details for Mobile Wallet with Provider ID: " + Mobile_providerID + " ";


                                    var storedProcedureName125 = "Get_Provider_Details_By_Id";
                                    var values125 = new
                                    {
                                        in_Client_ID= Convert.ToInt32(Client_ID),
                                        WhereClause = clouse,
                                    };

                                    var Get_Provider_Details_By_Id = await _dbConnection.QueryAsync(storedProcedureName125, values125, commandType: CommandType.StoredProcedure);
                                    dynamic dtp = Get_Provider_Details_By_Id.FirstOrDefault();
                                    HelloPaisa_IntegrationProject_logs= HelloPaisa_IntegrationProject_logs + " Provider Details fetched for Mobile Wallet. ";


                                    if (dtp != "")
                                    {
                                        BankRoute = Convert.ToString(dtp.ProviderPayerID);
                                        SubAgent = Provider_name;
                                    }
                                }
                                catch (Exception ex) { }

                                if (landedCurrency == "PKR" || landedCurrency == "PHP")
                                {
                                    PayoutType = "BankAccount";
                                    BeneficiaryAccountNo = BeneficiaryMsisdn;
                                }
                                else { PayoutType = "Mobile Wallet"; }
                            }
                        }


                        ServicePointManager.Expect100Continue = true;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                               | SecurityProtocolType.Tls11
                               | SecurityProtocolType.Tls12;

                        string credentials = $"{apiuser}:{apipass}";
                        HelloPaisa_IntegrationProject_logs= HelloPaisa_IntegrationProject_logs+" Hello Paisa API Credentials Encoded. ";
                        string encodedCredentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));
                        HelloPaisa_IntegrationProject_logs= HelloPaisa_IntegrationProject_logs + " Hello Paisa API Credentials Prepared. ";
                        var options = new RestClientOptions(apiurl + "/postTransaction")
                        {
                            MaxTimeout = -1
                        };
                        var clientpostTransaction = new RestClient(options);
                        var requestpostTransaction = new RestRequest()
                        {
                            Method = Method.Post
                        };


                        //requestpostTransaction.AddHeader("Authorization", "Basic Q0FMWVhTTE5VSzp0ZXN0MTIz");
                        requestpostTransaction.AddHeader("Authorization", "Basic " + encodedCredentials);
                        requestpostTransaction.AddHeader("Content-Type", "application/json");
                        //ReferenceNo = "11422002300156";
                        try
                        {
                            ReferenceNo = GenerateRandomNumber(14);
                        }catch(Exception ex)
                        {
                            HelloPaisa_IntegrationProject_logs= HelloPaisa_IntegrationProject_logs + " Error Generating Reference No: " + ex.ToString() + " ";
                        }
                        HelloPaisa_IntegrationProject_logs= HelloPaisa_IntegrationProject_logs + " Hello Paisa API Request Body Preparation Started. ";
                        //if (PayoutCountry == "US") { PayoutCountry = "GB"; }
                        //if (BeneficiaryNationalityCountryISOCode == "USA") { BeneficiaryNationalityCountryISOCode = "GBR"; }
                        //if (SenderCountry == "USA") { SenderCountry = "GBR"; }
                        //if (SenderAddressCountry == "USA") { SenderAddressCountry = "GBR"; }
                        //if (SenderCurrency == "USD") { SenderCurrency = "GBP"; }
                        //if (SenderCity == "Albany") { SenderCity = "Rochester"; }
                        string normalizedMsisdn = "";
                        HelloPaisa_IntegrationProject_logs= HelloPaisa_IntegrationProject_logs + " Normalizing Beneficiary MSISDN and Account Number if Landed Currency is PKR. "+ landedCurrency;
                        if (landedCurrency == "PKR")
                        {
                            try
                            {
                                normalizedMsisdn = NormalizeMsisdn(BeneficiaryMsisdn);
                                BeneficiaryAccountNo = NormalizeMsisdn(BeneficiaryAccountNo);
                            }
                            catch(Exception ex) 
                            {
                                HelloPaisa_IntegrationProject_logs= HelloPaisa_IntegrationProject_logs + " Error Normalizing MSISDN: " + ex.ToString() + " ";
                            }
                        }
                        else
                        {
                            normalizedMsisdn = BeneficiaryMsisdn;
                            BeneficiaryAccountNo = BeneficiaryMsisdn;
                        }
                        HelloPaisa_IntegrationProject_logs= HelloPaisa_IntegrationProject_logs + " Beneficiary MSISDN Normalized. ";
                        var body1 = @"{
" + "\n" +
@"  ""DcmRequest"": [
" + "\n" +
@"    {
" + "\n" +
@"      ""PayoutType"": """ + PayoutType + @""",
" + "\n" +
@"      ""ReferenceNo"": """ + ReferenceNo + @""",
" + "\n" +
@"      ""ForeignCurrency"": """ + landedCurrency + @""",
" + "\n" +
@"      ""ForeignAmount"": """ + ForeignAmount + @""",
" + "\n" +
@"      ""RemitPurpose"": """ + RemitPurpose + @""",
" + "\n" +
@"      ""BeneficiaryFirstName"": """ + BeneficiaryFirstName + @""",
" + "\n" +
@"      ""BeneficiaryLastName"": """ + BeneficiaryLastName + @""",
" + "\n" +
@"      ""BeneficiaryMSISDN"": """ + normalizedMsisdn + @""",
" + "\n" +
@"      ""BeneficiaryNationalityCountryISOCode"": """ + BeneficiaryNationalityCountryISOCode + @""",
" + "\n" +
@"      ""BeneficiaryAccountNo"": """ + BeneficiaryAccountNo + @""",
" + "\n" +
@"      ""BeneficiaryBranchCode"": """ + BeneficiaryBranchCode + @""",
" + "\n" +
@"      ""BranchCode"": """ + BeneficiaryBranchCode + @""",
" + "\n" +
@"      ""BeneficiaryBranchName"": """ + BeneficiaryBranchName + @""",
" + "\n" +
@"      ""BeneficiaryAddress"": """ + BeneficiaryAddress + @""",
" + "\n" +
@"      ""SenderFirstName"": """ + SenderFirstName + @""",
" + "\n" +
@"      ""SenderLastName"": """ + SenderLastName + @""",
" + "\n" +
@"      ""SenderMSISDN"": """ + SenderMSISDN + @""",
" + "\n" +
@"      ""SenderAddress"": """ + SenderAddress + @""",
" + "\n" +
@"      ""SenderAddressStreet"": """ + SenderAddressStreet + @""",
" + "\n" +
@"      ""SenderCountryCode"": """ + PayoutCountry + @""",
" + "\n" +
@"      ""SenderAddressCity"": """ + SenderCity + @""",
" + "\n" +
@"      ""SenderNationalityCountryISOCode"": """ + SenderCountry + @""",
" + "\n" +
@"      ""SenderAddressCountry"": """ + SenderAddressCountry + @""",
" + "\n" +
@"      ""SenderCurrency"": """ + SenderCurrency + @""",
" + "\n" +
@"      ""SenderCountry"": """ + SenderCountry + @""",
" + "\n" +
@"      ""SenderCity"": """ + SenderCity + @""",
" + "\n" +
@"      ""SenderAddressZIP"": """ + SenderAddressZIP + @""",
" + "\n" +
@"      ""SenderGender"": """ + SenderGender + @""",
" + "\n" +
@"      ""SenderProvinceState"": """ + SenderCity + @""",
" + "\n" +
@"      ""Suburb"": """ + SenderCity + @" road"",
" + "\n" +
@"      ""StreetNo"": """ + StreetNo + @""",
" + "\n" +
@"      ""BuildingNo"": """ + BuildingNo + @""",
" + "\n" +
@"      ""SenderIdCountryISOCode"": """ + SenderCountry + @""",
" + "\n" +
@"      ""SenderId"": """ + senderIdNumber + @""",
" + "\n" +
@"      ""CustomerDateOfBirth"": """ + CustomerDateOfBirth + @""",
" + "\n" +
@"      ""CustomerIdExpiryDate"": """ + CustomerIdExpiryDate + @""",
" + "\n" +
@"      ""CustomerNationalityCode"": """ + PayoutCountry + @""",
" + "\n" +
@"      ""CustomerIdIssueDate"": """ + senderIdIssueDate + @""",
" + "\n" +
@"      ""CustomerIDType"": """ + CustomerIDType + @""",
" + "\n" +
@"      ""CustomerAddressCity"": """ + SenderCity + @""",
" + "\n" +
@"      ""CustomerIdIssuedAt"": """ + PayoutCountry + @""",
" + "\n" +
@"      ""CustomerAddressCountryCode"": """ + PayoutCountry + @""",
" + "\n" +
@"      ""IdType"": """ + IdType + @""",
" + "\n" +
@"      ""PayoutCountry"": """ + benf_ISO_Code + @""",
" + "\n" +
@"      ""BranchName"": """ + BeneficiaryBranchName + @""",
" + "\n" +
@"      ""AccountTtile"": """ + SubAgent + @""",
" + "\n" +
@"      ""RoutingCode"": """ + RoutingCode + @""",
" + "\n" +
@"      ""BankRoute"": """ + BankRoute + @""",
" + "\n" +
@"      ""SubAgent"": """",
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
" + "\n" +
@"}";

                        await SaveActivityLogTracker("Hello Paisa transaction requestpost : <br/>" + body1, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Budpay Proceed", entity.Branch_ID, Client_ID);

                        requestpostTransaction.AddParameter("application/json", body1, ParameterType.RequestBody);
                        var responsepostTransaction = clientpostTransaction.Execute(requestpostTransaction);
                        await SaveActivityLogTracker("Hello Paisa transaction responsepost : <br/>" + responsepostTransaction.Content, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Budpay Proceed", entity.Branch_ID, Client_ID);



                        dynamic dJson = Newtonsoft.Json.JsonConvert.DeserializeObject(responsepostTransaction.Content);
                        var responseCode = dJson.DcmResponse.responseCode;
                        HelloPaisa_IntegrationProject_logs= HelloPaisa_IntegrationProject_logs + " Hello Paisa API Response Received with Response Code: " + responseCode.ToString() + " ";
                        if (responseCode == 200 || responseCode == 206)
                        {
                            apistatus = 0;

                            var responseMessage = dJson.DcmResponse.responseMessage;
                            var payoutPartnerRef = dJson.DcmResponse.payoutPartnerRef;
                            var DCode = dJson.DcmResponse.DCode;
                            var payOutPartnerRefName = dJson.DcmResponse.payOutPartnerRefName;
                            var DcmTransactionFee = dJson.DcmResponse.DcmTransactionFee;
                            var Rate = dJson.DcmResponse.Rate;
                            var payInPartnerRef = dJson.DcmResponse.payInPartnerRef;
                            HelloPaisa_IntegrationProject_logs = HelloPaisa_IntegrationProject_logs + "payInPartnerRef" + payInPartnerRef;

                            if (responseCode == 200 || responseCode == 206)
                            {

                                string refer = Convert.ToString(result.ReferenceNo);
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

                                        var parameters = new
                                        {
                                            _BranchListAPI_ID = api_id,
                                            _APIBranch_Details = entity.APIBranch_Details,
                                            _TransactionRef = refer,
                                            _trn_referenceNo = DCode + "-" + BankRoute,
                                            _APITransaction_Alert = 0,
                                            _Transaction_ID = entity.Transaction_ID,
                                            _Client_ID = entity.Client_ID,
                                            _payout_partner_rate = AgentRateapi,
                                        };
                                        HelloPaisa_IntegrationProject_logs= HelloPaisa_IntegrationProject_logs + " Updating Transaction Details in Database for Transaction ID: " + entity.Transaction_ID.ToString() + " ";
                                        var rowsAffected = await _dbConnection.ExecuteAsync("Update_TransactionDetails", parameters, commandType: CommandType.StoredProcedure);
                                        HelloPaisa_IntegrationProject_logs= HelloPaisa_IntegrationProject_logs + " Transaction Details Updated in Database for Transaction ID: " + entity.Transaction_ID.ToString() + " ";

                                    }
                                    catch (Exception ex)
                                    {
                                        Message1 = ex.Message;
                                        await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "HelloPaisa Update transaction mapping table sp error", entity.user_id, entity.Branch_ID, Client_ID, 0);
                                    }

                                }
                      
                            }
                            else 
                            { 

                                if (Message == "")
                                {
                                    Message = "Unable To Process The Transaction.";
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
                                Message = "Unable To Process The Transaction.";
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
                    catch (Exception exp)
                    {
                        await SaveErrorLogAsync("Error In Hello Paisa Procceed Transaction: <br/>" + exp.ToString(), DateTime.Now, "Budpay Transaction Procced Error", entity.user_id, entity.Branch_ID, Client_ID, 0);

                        if (Message == "")
                        {
                            Message = "Unable To Process The Transaction.";
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
                    await SaveErrorLogAsync("Error In Hello Paisa Procceed Transaction: <br/>" + ex.ToString(), DateTime.Now, "Budpay Transaction Procced Error", entity.user_id, entity.Branch_ID, Client_ID, 0);

                    if (Message == "")
                    {
                        Message = "Unable To Process The Transaction.";
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
                finally
                {
                    await SaveActivityLogTracker("Hello Paisa transaction Integration Logs : <br/>" + HelloPaisa_IntegrationProject_logs, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(0), "Budpay Proceed", entity.Branch_ID, Client_ID);
                }

                #endregion Hello Paisa

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
                Message = "Description : " + Message1;
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