using Microsoft.Extensions.Options;
using Project.API.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Project.Core.Interfaces.IRepositories;
using Newtonsoft.Json.Linq;
using Project.Core.Entities.General;
using RestSharp;
using System.Data.Common;
using System.Net;
using Project.API.Configuration;
using Project.Core.Entities.Business;
using RestSharp;
using MySql.Data.MySqlClient;
using System.Globalization;
using Org.BouncyCastle.Ocsp;
using Dapper;
using Azure;
using Microsoft.IdentityModel.Tokens;

namespace Project.Infrastructure.Repositories
{
    public class ProceedWithWalletRepository : BaseRepository<ProceedWithWallet>, IProceedWithWalletRepository
    {
        private readonly AppSettings _appSettings;

        public ProceedWithWalletRepository(IDbConnection dbConnection, IOptions<AppSettings> appSettings) : base(dbConnection)
        {
            _appSettings = appSettings.Value;
        }

        //public async Task<bool> IsExists(string key, int? value)
        //{
        //    var query = $"SELECT COUNT(1) FROM transaction_table WHERE {key} = @value;";
        //    var result = "";// await _dbConnection.ExecuteScalarAsync<int>(query, new { value });
        //    return result == "1";
        //}

        public async Task<ProceedResponseViewModel> ProceedWithWallet(ProceedWithWallet entity)
        {
            ProceedWithWallet obj = new ProceedWithWallet { };
            obj = entity;

            string FromCurrency_Code = "";
            string Currency_Code = "";
            string apiStatus = "";


            int? api_id = 0;// entity.BranchListAPI_ID;
            int? Customer_ID = 0;
            int? Client_ID = entity.Client_ID;
            int? Transaction_ID = 0;// entity.Transaction_ID;
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

            try
            {
                if (entity.FromCurrency_Code == "NGN" && entity.ToCurrency_Code == "NGN")// New Beta PAGA
                {
                    try
                    {

                       storedProcedureName = "active_walletTransactionapi_api";
                        var valuesApi = new
                        {
                            _whereclause = "and ID=3",
                            _securitykey = SecurityKey
                        };
                        var dtt = await _dbConnection.QueryAsync(storedProcedureName, valuesApi, commandType: CommandType.StoredProcedure);
                        dynamic transactiondetail = dtt.FirstOrDefault();

                        string bankId = "";
                      
                        apiurl = transactiondetail.API_URL.ToString();//   Convert.ToString(dtt.Rows[0]["API_URL"]);
                        int APIID = transactiondetail.ID;
                        accesscode = transactiondetail.APIAccess_Code.ToString();
                        string Password = transactiondetail.Password.ToString();
                        string api_Fields = transactiondetail.api_Fields.ToString();


                        string Hmac = "", callBackUrl = "";
                        if (api_Fields != "" && api_Fields != null)
                        {
                            Newtonsoft.Json.Linq.JObject objAPI = Newtonsoft.Json.Linq.JObject.Parse(api_Fields);
                           
                            Hmac = objAPI["HMAC"]?.ToString();
                            callBackUrl = objAPI["callBackUrl"]?.ToString();
                            bankId = objAPI["bankId"]?.ToString();
                        }

                        storedProcedureName = "GetCustDetailsByID";
                        var CustDetailsvalues = new
                        {
                            cust_ID =entity.Customer_ID
                        };
                        var dttCustDetails = await _dbConnection.QueryAsync(storedProcedureName, CustDetailsvalues, commandType: CommandType.StoredProcedure);
                        dynamic CustDetails = dttCustDetails.FirstOrDefault();

                        int CustApiWalletAccountExist = 0;
                        string where = "and Customer_ID='" + entity.Customer_ID + "'";
                        storedProcedureName = "SP_GetWalletBankDetails";
                        var CustWalletBankDetails = new
                        {
                            _where = where
                        };
                        var WalletBankDetails = await _dbConnection.QueryAsync(storedProcedureName, CustWalletBankDetails, commandType: CommandType.StoredProcedure);
                        dynamic CustWalletBank_Details = WalletBankDetails.FirstOrDefault();

                       if (CustWalletBank_Details != null)
                        { CustApiWalletAccountExist = 1; }
                        else
                        { CustApiWalletAccountExist = 0;

                            return new ProceedResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = "Failed from Search Api Wallet Response",
                                ApiId = api_id,
                                AgentRate = AgentRateapi,
                                ApiStatus = apistatus,
                                ExtraFields = new List<string> { "", "" }
                            };
                        }

                        string credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{accesscode}:{Password}"));

                        string referenceNumber = CustDetails.WireTransfer_ReferanceNo.ToString();
                        string phoneNumber = CustDetails.Phone_Number.ToString();
                        string firstName = CustDetails.First_Name.ToString();
                        string lastName = CustDetails.Last_Name.ToString();
                        string accountName = CustDetails.Full_name.ToString();
                        string accountReference = CustDetails.WireTransfer_ReferanceNo.ToString();
                        //string callbackUrl = "https://webhook.site/a49689c2-e241-46dc-a9b0-57b9e44eed85";
                        //accountReference = "kobot-" + accountReference;
                        //accountReference = obj.ReferenceNo + "-" + accountReference;
                        accountReference =entity.TransactionRef.ToString();
                        string hashedData = GenerateHashedData(accountReference, accountReference, callBackUrl, Hmac);
                        string jsonResponse = "";
                        JObject responseObject = null;
                        if (CustApiWalletAccountExist == 1)
                        {
                            #region get api walletdata    

                            string where1 = "and Customer_ID=" + entity.Customer_ID;
                            storedProcedureName = "SP_GetWalletBankDetails";
                            var Cust_getWaldt = new
                            {
                                _where = where1
                            };
                            var _getWaldt = await _dbConnection.QueryAsync(storedProcedureName, Cust_getWaldt, commandType: CommandType.StoredProcedure);
                            dynamic dt_getWaldt = _getWaldt.FirstOrDefault();


                            //if (dt_getWaldt.Rows.Count > 0)
                            if (dt_getWaldt != null)
                            {
                                #region get paga api details

                                string currentDateTimeUTC = DateTime.UtcNow.AddMinutes(5).ToString("yyyy-MM-ddTHH:mm:ss");
                                string counterPartyid = "";
                                int Currency_ID =Convert.ToInt32(dt_getWaldt.Currency_ID);
                                //int APIID = Convert.ToInt32(dtt.Rows[0]["ID"]);
                                //string apiurl = Convert.ToString(dtt.Rows[0]["Decrypted_API_URL"]);
                                //string accesscode = Convert.ToString(dtt.Rows[0]["Decrypted_APIAccess_Code"]);

                                //string hashedData2 = GenerateHashedData2(referenceNumber, Convert.ToString(obj.AmountInGBP), Currency_Code, phoneNumber, Convert.ToString(dtCD.Rows[0]["Email_ID"]), phoneNumber, Hmac);
                                // decimal amountInGBP1 = Convert.ToDecimal(obj.AmountInGBP);
                                decimal amountInGBP1 = 10;
                                string fullName = CustDetails.Full_name.ToString();
                             //   string fullName = Convert.ToString(dtCD.Rows[0]["Full_name"]);
                               // string emailId = Convert.ToString(dtCD.Rows[0]["Email_ID"]);
                                string emailId = CustDetails.Email_ID.ToString();
                               // string accountNumber = Convert.ToString(dt_getWaldt.Rows[0]["Account_Number"]);
                                string accountNumber = dt_getWaldt.Account_Number.ToString();


                                string jsonData = GenerateJsonData(accountReference, amountInGBP1, phoneNumber, bankId, fullName, emailId, entity.BeneficiaryName.ToString(), accountNumber, currentDateTimeUTC, callBackUrl);

                                // Generate Hash 
                                string hashedData2 = GenerateHashedData2(jsonData, Hmac);


                                if (accountNumber != null && accountNumber != "")
                                {
                                    var options1 = new RestClientOptions(apiurl + "/paymentRequest")
                                    {
                                        MaxTimeout = -1
                                    };
                                    var client1 = new RestClient(options1);
                                    var request1 = new RestRequest()
                                    {
                                        Method = Method.Post
                                    };
                                    request1.AddHeader("hash", hashedData2);
                                    request1.AddHeader("Content-Type", "application/json");
                                    //request.AddHeader("Authorization", "Basic QTdCNjc3N0YtQzk2OS00RjJGLUE1QzktNzY0ODI2QkREOTE3OmRWNSt4Yk1nZnJoYU1DRw==\r\n");
                                    request1.AddHeader("Authorization", "Basic " + credentials);

                                    string tbody = @"{
" + "\n" +
@"    ""referenceNumber"": """ + accountReference + @""",
" + "\n" +
@"    ""amount"": " + amountInGBP1 + @",
" + "\n" +
@"    ""currency"": ""NGN"",
" + "\n" +
@"    ""payer"": {
" + "\n" +
@"        ""name"": """ + fullName + @""",
" + "\n" +
@"        ""phoneNumber"": """ + "0" + phoneNumber + @""",
" + "\n" +
@"        ""email"": """ + emailId + @""",
" + "\n" +
@"        ""bankId"": """ + bankId + @"""
" + "\n" +
@"    },
" + "\n" +
@"    ""payee"": {
" + "\n" +
@"        ""name"": """ + entity.BeneficiaryName + @""",
" + "\n" +
@"        ""financialIdentificationNumber"": """ + accountNumber + @"""
" + "\n" +
@"    },
" + "\n" +
@"    ""expiryDateTimeUTC"": """ + currentDateTimeUTC + @""",
" + "\n" +
@"    ""isSuppressMessages"": false,
" + "\n" +
@"    ""payerCollectionFeeShare"": 1.0,
" + "\n" +
@"    ""payeeCollectionFeeShare"": 0.0,
" + "\n" +
@"    ""isAllowPartialPayments"": false,
" + "\n" +
@"    ""isAllowOverPayments"": false,
" + "\n" +
@"    ""callBackUrl"": """ + callBackUrl + @""",
" + "\n" +
@"    ""paymentMethods"": [
" + "\n" +
@"        ""BANK_TRANSFER"",
" + "\n" +
@"        ""FUNDING_USSD"",
" + "\n" +
@"        ""REQUEST_MONEY""
" + "\n" +
@"    ],
" + "\n" +
@"    ""displayBankDetailToPayer"": false
"
                                    + "\n" +
                                    @"}";

                                    request1.AddParameter("application/json", jsonData, ParameterType.RequestBody);
                                    await SaveActivityLogTracker("BETA-Paga paymentRequest request: <br/>" + jsonData + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.Customer_ID, Convert.ToInt32(Customer_ID), "BETA-Paga Proceed", entity.Branch_ID, Client_ID);
                                    
                                    RestResponse response1 = client1.Execute(request1);
                                    await SaveActivityLogTracker("BETA-Paga paymentRequest response: <br/>" + response1.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.Customer_ID, Convert.ToInt32(Customer_ID), "BETA-Paga Proceed", entity.Branch_ID, Client_ID);

                                    jsonResponse = response1.Content;
                                    responseObject = JObject.Parse(jsonResponse);

                                    // Extract individual fields
                                    referenceNumber = responseObject["referenceNumber"]?.ToString();
                                    string statusCode = responseObject["statusCode"]?.ToString();
                                    string statusMessage = responseObject["statusMessage"]?.ToString();
                                    string requestAmount = responseObject["requestAmount"]?.ToString();
                                    string totalPaymentAmount = responseObject["totalPaymentAmount"]?.ToString();
                                    string expiryDateTimeUTC = responseObject["expiryDateTimeUTC"]?.ToString();
                                    string isPayerPagaAccountHolder = responseObject["isPayerPagaAccountHolder"]?.ToString();
                                    string bankName = responseObject["bankName"]?.ToString();

                                    string referenceNumberstatus = "";
                                    // Check if `status` exists
                                    if (!string.IsNullOrEmpty(statusCode))
                                    {
                                        if (statusCode == "0")
                                        {
                                            string jsonDatastatus = $"{{ \"referenceNumber\": \"{referenceNumber}\" }}";
                                            string hashedDatastatus = GenerateHashedData3(jsonDatastatus, Hmac);

                                            var options2 = new RestClientOptions(apiurl + "/status")
                                            {
                                                MaxTimeout = -1
                                            };
                                            var clientstatusCode = new RestClient(options2);
                                            var requeststatusCode = new RestRequest()
                                            {
                                                Method = Method.Post
                                            };

                                            requeststatusCode.AddHeader("hash", hashedDatastatus);
                                            requeststatusCode.AddHeader("Content-Type", "application/json");                                        
                                            requeststatusCode.AddHeader("Authorization", "Basic " + credentials);


                                            requeststatusCode.AddParameter("application/json", jsonDatastatus, ParameterType.RequestBody);

                                            await SaveActivityLogTracker("BETA-Paga statusCode Request: <br/>" + jsonDatastatus + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.Customer_ID, Convert.ToInt32(Customer_ID), "BETA-Paga Proceed", entity.Branch_ID, Client_ID);
                                            RestResponse responsestatusCode = clientstatusCode.Execute(requeststatusCode);
                                            await SaveActivityLogTracker("BETA-Paga statusCode response statusCode: <br/>" + responsestatusCode.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.Customer_ID, Convert.ToInt32(Customer_ID), "BETA-Paga Proceed", entity.Branch_ID, Client_ID);

                                            jsonResponse = "";
                                            jsonResponse = responsestatusCode.Content;
                                            responseObject = JObject.Parse(jsonResponse);
                                            try
                                            {
                                                referenceNumberstatus = responseObject["referenceNumber"].ToString();
                                                string statusCodestatus = responseObject["statusCode"].ToString();
                                                string statusMessagestatus = responseObject["statusMessage"].ToString();
                                                
                                                JObject dataObject = (JObject)responseObject["data"];
                                                string dataStatusCode = dataObject["statusCode"].ToString();
                                                if (statusMessagestatus == "success")
                                                {
                                                    apiStatus = "COMPLETED";
                                                    string dataReferenceNumber = dataObject["referenceNumber"].ToString();
                                                    string dataStatusMessage = dataObject["statusMessage"].ToString();
                                                }
                                                else
                                                {
                                                    return new ProceedResponseViewModel
                                                    {
                                                        Status = "Failed",
                                                        StatusCode = 2,
                                                        Message = responseObject["statusMessage"].ToString(),
                                                        //Message = "Bank Code Not Found.",
                                                        ApiId = api_id,
                                                        AgentRate = AgentRateapi,
                                                        ApiStatus = apistatus,
                                                        ExtraFields = new List<string> { "", "" }
                                                    };
                                                    // transaction.Rollback();
                                                    // dt.Rows.Add(5, responseObject["statusMessage"].ToString(), referenceNumberstatus, obj.Transaction_ID);
                                                    //  return dt;
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                return new ProceedResponseViewModel
                                                {
                                                    Status = "Failed",
                                                    StatusCode = 2,
                                                    Message = "Failed to Update Wallet at api",
                                                    ApiId = api_id,
                                                    AgentRate = AgentRateapi,
                                                    ApiStatus = apistatus,
                                                    ExtraFields = new List<string> { "", "" }
                                                };
                                                //  transaction.Rollback();
                                                // dt.Rows.Add(5, "Failed to Update Wallet at api", referenceNumberstatus, obj.Transaction_ID);
                                                //  return dt;
                                            }
                                        }
                                        else
                                        {
                                            return new ProceedResponseViewModel
                                            {
                                                Status = "Failed",
                                                StatusCode = 2,
                                                Message = "Failed to Update Wallet api",
                                                ApiId = api_id,
                                                AgentRate = AgentRateapi,
                                                ApiStatus = apistatus,
                                                ExtraFields = new List<string> { "response", response1.Content.ToString() }
                                            };
                                            // transaction.Rollback();
                                            //dt.Rows.Add(5, "Failed to Update Wallet api", Cust_ReferanceNo, obj.Transaction_ID);
                                            //return dt;
                                        }
                                    }
                                    else
                                    {
                                        return new ProceedResponseViewModel
                                        {
                                            Status = "Failed",
                                            StatusCode = 2,
                                            Message = "Failed to Update Wallet api",
                                            ApiId = api_id,
                                            AgentRate = AgentRateapi,
                                            ApiStatus = apistatus,
                                            ExtraFields = new List<string> { "", "" }
                                        };
                                        // transaction.Rollback();
                                        //dt.Rows.Add(5, "Failed to Update Wallet api", Cust_ReferanceNo, obj.Transaction_ID);
                                        //return dt;
                                    }
                                }
                                #endregion get paga api details

                            }
                            else { }

                            #endregion get api walletdata
                        }
                    }
                    catch (Exception ex) { }
                }
                else if((entity.FromCurrency_Code == "CAD" && entity.FromCurrency_Code == "CAD") || (entity.FromCurrency_Code == "NGN" && entity.FromCurrency_Code == "CAD"))
                {
                    int phoneCountryCode = 0; string phoneNumber = "", AccountNumber = "", customerName = "", email = "";
                    int countryCode = 0;

                    storedProcedureName = "GetCustDetailsByID";
                    var CustDetailsvalues = new
                    {
                        cust_ID = entity.Customer_ID
                    };
                    var dttCustDetails = await _dbConnection.QueryAsync(storedProcedureName, CustDetailsvalues, commandType: CommandType.StoredProcedure);
                    dynamic CustDetails = dttCustDetails.FirstOrDefault();
                    if (CustDetails!=null)
                    {
                        customerName = CustDetails.Full_name.ToString();
                        email = CustDetails.Email_ID.ToString();
                    }

                    //cmdBD.Parameters.AddWithValue("_whereclause", " and bm.Beneficiary_ID =" + obj.Beneficiary_ID);

                    storedProcedureName = "GetBeneficiaryDetails";
                    var BenefDetailsvalues = new
                    {
                        _whereclause = " and bm.Beneficiary_ID ="+entity.Beneficiary_ID
                    };
                    var dttBenefDetails = await _dbConnection.QueryAsync(storedProcedureName, BenefDetailsvalues, commandType: CommandType.StoredProcedure);
                    dynamic BenefDetails = dttBenefDetails.FirstOrDefault();
                    if (BenefDetails != null)
                    {
                        phoneCountryCode =Convert.ToInt32(BenefDetails.countryCode);
                        phoneNumber = BenefDetails.ReceiverMobileNo.ToString();
                        AccountNumber = BenefDetails.ReceiverAccountNumber.ToString();
                        phoneCountryCode = Convert.ToInt32(BenefDetails.countryCode);
                    }

                    whereclause = "and ID =1";
                    storedProcedureName = "active_walletTransactionapi_api";
                    var walletTransactionapi = new
                    {
                        _whereclause = whereclause,
                        _securitykey = SecurityKey
                    };
                    var dtwalletTransactionapi = await _dbConnection.QueryAsync(storedProcedureName, walletTransactionapi, commandType: CommandType.StoredProcedure);
                    dynamic dtwalletTransactionapiDetails = dtwalletTransactionapi.FirstOrDefault();

                    //MySqlCommand cmdAD = new MySqlCommand("active_walletTransactionapi_api");
                    //cmdAD.CommandType = CommandType.StoredProcedure;
                    ////string whereclause = "and ID =1";
                    //cmdAD.Parameters.AddWithValue("_whereclause", whereclause);
                    //cmdAD.Parameters.AddWithValue("_securitykey", CompanyInfo.SecurityKey());

                    // DataTable dtAD = null;
                   
                    string country = "";
                    //if (Convert.ToString(dtAD.Rows[0]["api_Fields"]) != "" && Convert.ToString(dtAD.Rows[0]["api_Fields"]) != null)
                    if (dtwalletTransactionapiDetails.api_Fields.ToString() != "" && dtwalletTransactionapiDetails.api_Fields.ToString() != null)
                    {
                        //Newtonsoft.Json.Linq.JObject objAPI = Newtonsoft.Json.Linq.JObject.Parse(api_Fields);

                        //Hmac = objAPI["HMAC"]?.ToString();
                        //callBackUrl = objAPI["callBackUrl"]?.ToString();
                        //bankId = objAPI["bankId"]?.ToString();

                        Newtonsoft.Json.Linq.JObject objAPI = Newtonsoft.Json.Linq.JObject.Parse(dtwalletTransactionapiDetails.api_Fields.ToString());
                        country =objAPI["country"]?.ToString();                        
                    }

                    if (dtwalletTransactionapiDetails!=null)
                    {
                        apiurl = dtwalletTransactionapiDetails.API_URL.ToString();
                        //apiurl = Convert.ToString(dtAD.Rows[0]["API_URL"]);
                        int apiid =Convert.ToInt32(dtwalletTransactionapiDetails.ID);
                       // int apiid = Convert.ToInt32(dtAD.Rows[0]["ID"]);
                        string APIAccess_Code = dtwalletTransactionapiDetails.APIAccess_Code.ToString();
                      //  string APIAccess_Code = Convert.ToString(dtAD.Rows[0]["APIAccess_Code"]);

                        double amount = Convert.ToDouble(entity.AmountInPKR);
                        //  double amount = Convert.ToDouble(obj.AmountInPKR);

                     //   string fullDateTime = "DateTime.Now"; // Assuming this is a string
                     ////   string fullDateTime = obj.Record_Insert_DateTime; // Assuming this is a string
                     //   string dateOnly = DateTime.Parse(fullDateTime).ToString("yyyy-MM-dd");

                        DateTime fullDateTime = DateTime.Now; // Get current date and time
                        string dateOnly = fullDateTime.ToString("yyyy-MM-dd");


                        string accountNumber = AccountNumber;
                        //string formattedAccountNumber = $"{accountNumber.Substring(0, 3)}-{accountNumber.Substring(3, 5)}-{accountNumber.Substring(8)}";
                        //Console.WriteLine(formattedAccountNumber);
                        string ClientReferenceNumber = entity.TransactionRef;

                        if (phoneNumber.StartsWith(Convert.ToString(phoneCountryCode)))
                        {
                            phoneNumber = phoneNumber.Substring(Convert.ToString(phoneCountryCode).Length);
                        }
                        string securityQuestionAnswer = GetsecurityQuestionAnswer(entity.TransactionRef);


                        var options1 = new RestClientOptions(apiurl + "/integrationapi/v1.0/ETransfer/CreateEtransferTransactionWithCustomer")
                        {
                            MaxTimeout = -1
                        };
                        var client = new RestClient(options1);
                        var request = new RestRequest()
                        {
                            Method = Method.Post
                        };

                        request.AddHeader("Authorization", "Bearer " + APIAccess_Code);
                        request.AddHeader("Accept", "application/json");
                        request.AddHeader("Content-Type", "application/json");
                        var bodyJson = "";
                        try
                        {


                            bodyJson = @"{
" + "\n" +
@"    ""customerName"": """ + customerName + @""",
" + "\n" +
@"    ""email"": """ + email + @""",
" + "\n" +
@"    ""phoneCountryCode"": """ + phoneCountryCode + @""",
" + "\n" +
//@"    ""phoneNumber"": """ + phoneNumber.Remove(phoneNumber.IndexOf('1'), 1) + @""",
@"    ""phoneNumber"": """ + phoneNumber + @""",
" + "\n" +
@"    ""priorityTypeCode"": ""0"", " + "\n" +
@"    ""notificationType"": ""0"", " + "\n" +
@"    ""transactionTypeCode"": ""C"", " + "\n" +
@"    ""amount"": " + amount + @",
" + "\n" +
@"    ""dateOfFunds"": """ + dateOnly + @""",
" + "\n" +
//@"    ""securityQuestion"": ""What is my favorite color?"",
@"    ""securityQuestion"": ""What is my security code?"",
" + "\n" +
@"    ""securityQuestionAnswer"": """ + securityQuestionAnswer + @""",
" + "\n" +
@"    ""description"": ""This is my memo!"",
" + "\n" +
@"    ""ClientReferenceNumber"": """ + ClientReferenceNumber + @"""
" + "\n" +
//@"    ""TransferType"": ""AccountDeposit"",
//" + "\n" +
//@"    ""AccountNumber"": """ + formattedAccountNumber + @"""
//" + "\n" +
@"}";
                        }
                        catch (Exception ex) { }

                        request.AddParameter("application/json", bodyJson, ParameterType.RequestBody);
                        //await SaveActivityLogTracker(" DC bank CreateEtransferTransactionWithCustomer request  " + bodyJson, entity.user_id, obj.Transaction_ID, entity.user_id, Customer_ID, "Send-InsertTransfer", entity.Branch_ID, obj.Client_ID, "");
                        await SaveActivityLogTracker("DC bank CreateEtransferTransactionWithCustomer request" + bodyJson + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.Customer_ID, Convert.ToInt32(Customer_ID), "DC bank Proceed", entity.Branch_ID, Client_ID);

                        RestResponse response1 = client.Execute(request);
                        await SaveActivityLogTracker("DC bank CreateEtransferTransactionWithCustomer response" + response1.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.Customer_ID, Convert.ToInt32(Customer_ID), "DC bank Proceed", entity.Branch_ID, Client_ID);

                        string jsonResponse = "";
                        Newtonsoft.Json.Linq.JObject responseObject1 = null;
                        string jsonResponse1 = response1.Content;

                        #region Security QueAns
                        try
                        {
                            storedProcedureName = "sp_InsertSecurityQuestionAnswer";

                            var parameters = new DynamicParameters();
                            parameters.Add("_TransactionRef", entity.TransactionRef); // Remove quotes to use actual value
                            parameters.Add("_Question", "What is my security code?");
                            parameters.Add("_Answer", securityQuestionAnswer);

                            await _dbConnection.ExecuteAsync(
                                storedProcedureName,
                                parameters,
                                commandType: CommandType.StoredProcedure
                            );

                            //var dtInsertSecurityQuestionAnswer = await _dbConnection.QueryAsync(storedProcedureName, values, commandType: CommandType.StoredProcedure);
                            //dynamic InsertSecurityQuestionAnswer = dtInsertSecurityQuestionAnswer.FirstOrDefault();
                        }
                        catch (Exception ex)
                        {
                            // Log or handle exception
                        }
                        #endregion

                        //storedProcedureName = "active_walletTransactionapi_api";
                        //var walletTransactionapi = new
                        //{
                        //    cust_ID = Customer_ID,
                        //    _securitykey = SecurityKey
                        //};
                        //var dtwalletTransactionapi = await _dbConnection.QueryAsync(storedProcedureName, values, commandType: CommandType.StoredProcedure);
                        //dynamic dtwalletTransactionapiDetails = dtwalletTransactionapi.FirstOrDefault();

                        //#region Security QueAns
                        //try
                        //{
                        //    MySqlCommand cmdQA = new MySqlCommand("sp_InsertSecurityQuestionAnswer");//InsertSecurityQuestionAnswer
                        //    cmdQA.CommandType = CommandType.StoredProcedure;
                        //    cmdQA.Parameters.AddWithValue("_TransactionRef", "obj.ReferenceNo");
                        //    cmdQA.Parameters.AddWithValue("_Question", "What is my security code?");
                        //    cmdQA.Parameters.AddWithValue("_Answer", securityQuestionAnswer);
                        //    db_connection.ExecuteNonQueryProcedure(cmdQA);
                        //}
                        //catch (Exception ex) { }

                        //if (!dt.Columns.Contains("SecurityCode"))
                        //{
                        //    dt.Columns.Add("SecurityCode", typeof(string));
                        //    //dt.Rows.Add(7, "Failed from DCBank Api Response", obj.ReferenceNo, obj.Transaction_ID, "", securityQuestionAnswer);
                        //}                       


                        responseObject1 = Newtonsoft.Json.Linq.JObject.Parse(jsonResponse1);
                        if (responseObject1["Item"] != null)
                        {
                            string transactionId = ""; string clientReferenceNumber = "";
                            string errorDescription = ""; string payeeId = "";
                            string ErrorCode = responseObject1["ErrorCode"]?.ToString();
                            string ErrorDescription = responseObject1["ErrorDescription"]?.ToString();
                            try
                            {
                                string customerNumber = responseObject1["Item"]["CustomerNumber"]?.ToString();
                                string isSucceeded = responseObject1["Item"]["IsSucceeded"]?.ToString();
                                transactionId = responseObject1["Item"]["TransactionId"]?.ToString();
                                string transactionEtransferId = responseObject1["Item"]["TransactionEtransferId"]?.ToString();
                                string transactionReferenceNumber = responseObject1["Item"]["TransactionReferenceNumber"]?.ToString();
                                payeeId = responseObject1["Item"]["PayeeId"]?.ToString();
                                string transactionDetailTableId = responseObject1["Item"]["TransactionDetailTableId"]?.ToString();
                                errorDescription = responseObject1["Item"]["ErrorDescription"]?.ToString();
                                string validationCode = responseObject1["Item"]["ValidationCode"]?.ToString();
                                string interacReferenceNumber = responseObject1["Item"]["InteracReferenceNumber"]?.ToString();
                                clientReferenceNumber = responseObject1["Item"]["ClientReferenceNumber"]?.ToString();
                                string gatewayUrl = responseObject1["Item"]["GatewayUrl"]?.ToString();
                            }
                            catch (Exception ex)
                            {
                                await SaveErrorLogAsync(ex.ToString(), DateTime.Now, "Proceed", entity.Customer_ID, entity.Branch_ID, Client_ID, 0);
                                return new ProceedResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = "Failed from DCBank Api Response",
                                    ApiId = api_id,
                                    AgentRate = AgentRateapi,
                                    ApiStatus = apistatus,
                                    ExtraFields = new List<string> { "", "" }
                                };
                                // transaction.Rollback();
                                //if (!dt.Columns.Contains("SecurityCode"))
                                //{
                                //    dt.Rows.Add(7, "Failed from DCBank Api Response", "obj.ReferenceNo", obj.Transaction_ID);
                                //    //dt.Columns.Add("SecurityCode", typeof(string));
                                //}
                                //else
                                //{
                                //    //dt.Columns.Add("SecurityCode", typeof(string));
                                //    dt.Rows.Add(7, "Failed from DCBank Api Response", "obj.ReferenceNo", obj.Transaction_ID, "", "securityQuestionAnswer");
                                //}

                                //dt.Rows.Add(7, "Failed from DCBank Api Response", obj.ReferenceNo, obj.Transaction_ID);
                                //await SaveActivityLogTracker(" Failed from DCBank Api Response " + obj.ReferenceNo, entity.user_id, obj.Transaction_ID, entity.user_id, Customer_ID, "Send-InsertTransfer", entity.Branch_ID, obj.Client_ID, "");

                                //return dt;
                            }
                            //CompanyInfo.InsertApiWalletTransactionDetails(obj.Transaction_ID, Customer_ID, refer, TransactionId, Reference, CreatedAt, Amount, Currency, Status, CounterPartyId, AccountId, CustomerId);
                            if (ErrorCode == "0" && ErrorDescription == "Success")
                            {
                                var options = new RestClientOptions(apiurl + "/integrationapi/v1.0/ETransfer/SearchEtransferTransaction")
                                {
                                    MaxTimeout = -1
                                };
                                var clientSearchEtransferTransaction = new RestClient(options);
                                var requestSearchEtransferTransaction = new RestRequest()
                                {
                                    Method = Method.Post
                                };




                                //string temp_url = apiurl + "/integrationapi/v1.0/ETransfer/SearchEtransferTransaction";
                                //var clientSearchEtransferTransaction = new RestClient(temp_url);
                               // clientSearchEtransferTransaction.Timeout = -1;
                               // var requestSearchEtransferTransaction = new RestRequest(Method.POST);
                                //var requestSearchEtransferTransaction = new RestRequest();
                                requestSearchEtransferTransaction.AddHeader("Authorization", "Bearer " + APIAccess_Code);
                                requestSearchEtransferTransaction.AddHeader("Accept", "application/json");
                                requestSearchEtransferTransaction.AddHeader("Content-Type", "application/json");
                                bodyJson = @"{
" + "\n" +
@"     ""TransactionId"": " + transactionId + @"  " + "\n" +
@"}";

                                requestSearchEtransferTransaction.AddParameter("application/json", bodyJson, ParameterType.RequestBody);
                              //  await SaveActivityLogTracker(" DC bank SearchEtransferTransaction request  " + bodyJson, entity.user_id, obj.Transaction_ID, entity.user_id, Customer_ID, "Send-InsertTransfer", entity.Branch_ID, obj.Client_ID, "");
                                RestResponse responseSearchEtransferTransaction = clientSearchEtransferTransaction.Execute(requestSearchEtransferTransaction);
                               // await SaveActivityLogTracker(" DC bank CreateEtransferTransactionWithCustomer response  " + responseSearchEtransferTransaction.Content, entity.user_id, obj.Transaction_ID, entity.user_id, Customer_ID, "Send-InsertTransfer", entity.Branch_ID, obj.Client_ID, "");

                                jsonResponse = responseSearchEtransferTransaction.Content;
                                responseObject1 = Newtonsoft.Json.Linq.JObject.Parse(jsonResponse);
                                apiStatus = "COMPLETED";
                               // await SaveActivityLogTracker("Dynathopia Create Transaction Request: <br/>" + req + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.user_id, Convert.ToInt32(Customer_ID), "Dynathopia Proceed", entity.Branch_ID, Client_ID);

                              //  await SaveActivityLogTracker(obj.Transaction_ID, apiid, Customer_ID, clientReferenceNumber, transactionId, obj.ReferenceNo, Convert.ToDateTime(dateOnly), Convert.ToDecimal(amount), FromCurrency_Code, errorDescription, "CounterPartyId", payeeId, Convert.ToString(dtAD.Rows[0]["ID"]));
                            }
                            else
                            {
                                return new ProceedResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = "Failed from DCBank Api Response",
                                    ApiId = api_id,
                                    AgentRate = AgentRateapi,
                                    ApiStatus = apistatus,
                                    ExtraFields = new List<string> { "", "" }
                                };
                                //transaction.Rollback();
                                //// dt.Rows.Add(5, "Failed to Update ApiWallet DC Balance", "Cust_ReferanceNo", obj.Transaction_ID);
                                //dt.Rows.Add(7, "Failed from DCBank Api Response", obj.ReferenceNo, obj.Transaction_ID, "", securityQuestionAnswer);

                                // return dt;
                            }

                        }
                        else
                        {
                            return new ProceedResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = "Failed to Update ApiWallet",
                                ApiId = api_id,
                                AgentRate = AgentRateapi,
                                ApiStatus = apistatus,
                                ExtraFields = new List<string> { "", "" }
                            };
                        }
                    }

                }
            }
            catch (Exception exr)
            {
                return new ProceedResponseViewModel
                {
                    Status = "Failed",
                    StatusCode = 2,
                    Message = "Failed to Update ApiWallet",
                    ApiId = api_id,
                    AgentRate = AgentRateapi,
                    ApiStatus = apistatus,
                    ExtraFields = new List<string> { "", "" }
                };
                //transaction.Rollback();
                //// dt.Rows.Add(5, "Failed to Update ApiWallet Balance", "Cust_ReferanceNo", obj.Transaction_ID);
                // return dt;
            }

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


        public async Task<CreateNGNWalletResponseViewModel> CreateNGNWallet(CreateNGNWallet entity)
        {            
            int? api_id = 0;// entity.BranchListAPI_ID;
            int? Customer_ID = 0;
            //int? Client_ID = entity.Client_ID;
            int? Transaction_ID = 0;// entity.Transaction_ID;
            
            string apiurl = "",  accesscode = "";
            string? SecurityKey = _appSettings.SecurityKey;

            try
            {
                try
                {
                    int CustApiWalletAccountExist = 0;
                    string where = "and Customer_ID='" + entity.Customer_ID + "'";
                    var storedProcedureName = "SP_GetWalletBankDetails";
                    var CustWalletBankDetails = new
                    {
                        _where = where
                    };
                    var WalletBankDetails = await _dbConnection.QueryAsync(storedProcedureName, CustWalletBankDetails, commandType: CommandType.StoredProcedure);
                    dynamic CustWalletBank_Details = WalletBankDetails.FirstOrDefault();

                    if (CustWalletBank_Details != null)
                    { CustApiWalletAccountExist = 1; }
                    else
                    { CustApiWalletAccountExist = 0; }
                    if (CustApiWalletAccountExist == 0)
                    {
                        storedProcedureName = "active_walletTransactionapi_api";
                        var valuesApi = new
                        {
                            _whereclause = "and ID=3",
                            _securitykey = SecurityKey
                        };
                        var dtt = await _dbConnection.QueryAsync(storedProcedureName, valuesApi, commandType: CommandType.StoredProcedure);
                        dynamic transactiondetail = dtt.FirstOrDefault();

                        string bankId = "";

                        apiurl = transactiondetail.API_URL.ToString();//   Convert.ToString(dtt.Rows[0]["API_URL"]);
                        int APIID = transactiondetail.ID;
                        accesscode = transactiondetail.APIAccess_Code.ToString();
                        string Password = transactiondetail.Password.ToString();
                        string api_Fields = transactiondetail.api_Fields.ToString();


                        string Hmac = "", callBackUrl = "";
                        if (api_Fields != "" && api_Fields != null)
                        {
                            Newtonsoft.Json.Linq.JObject objAPI = Newtonsoft.Json.Linq.JObject.Parse(api_Fields);

                            Hmac = objAPI["HMAC"]?.ToString();
                            callBackUrl = objAPI["callBackUrl"]?.ToString();
                            bankId = objAPI["bankId"]?.ToString();
                        }

                        storedProcedureName = "GetCustDetailsByID";
                        var CustDetailsvalues = new
                        {
                            cust_ID = entity.Customer_ID
                        };
                        var dttCustDetails = await _dbConnection.QueryAsync(storedProcedureName, CustDetailsvalues, commandType: CommandType.StoredProcedure);
                        dynamic CustDetails = dttCustDetails.FirstOrDefault();



                        string credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{accesscode}:{Password}"));

                        string referenceNumber = CustDetails.WireTransfer_ReferanceNo.ToString();
                        string phoneNumber = CustDetails.Phone_Number.ToString();
                        string firstName = CustDetails.First_Name.ToString();
                        string lastName = CustDetails.Last_Name.ToString();
                        string accountName = CustDetails.Full_name.ToString();
                        string accountReference = CustDetails.WireTransfer_ReferanceNo.ToString();
                        
                        accountReference = accountReference+"-" + accountReference;
                        string hashedData = GenerateHashedData(accountReference, accountReference, callBackUrl, Hmac);

                        string jsonResponse = "";
                        JObject responseObject = null;

                        var options = new RestClientOptions(apiurl + "/registerPersistentPaymentAccount")
                        {
                            MaxTimeout = -1
                        };
                        var client = new RestClient(options);
                        var request = new RestRequest()
                        {
                            Method = Method.Post
                        };
                        request.AddHeader("hash", hashedData);
                        request.AddHeader("Content-Type", "application/json");
                        request.AddHeader("Authorization", "Basic " + credentials);
                        string body = @"
        {
            ""referenceNumber"": """ + accountReference + @""",
            ""phoneNumber"": """ + "0" + phoneNumber + @""",
            ""firstName"": """ + firstName + @""",
            ""lastName"": """ + lastName + @""",
            ""accountName"": """ + accountName + @""",
            ""accountReference"": """ + accountReference + @""",
            ""callbackUrl"": """ + callBackUrl + @"""
        }";
                        request.AddParameter("application/json", body, ParameterType.RequestBody);
                        await SaveActivityLogTracker("BETA-Paga registerPersistentPaymentAccount request for account Create: <br/>" + body + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.Customer_ID, Convert.ToInt32(Customer_ID), "Budpay Proceed", 2, 1);
                        RestResponse response = client.Execute(request);
                        await SaveActivityLogTracker("BETA-Paga registerPersistentPaymentAccount response for account Create: <br/>" + response.Content + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.Customer_ID, Convert.ToInt32(Customer_ID), "Budpay Proceed", 2, 1);

                        jsonResponse = response.Content;
                        responseObject = JObject.Parse(jsonResponse);
                        string statusCode = responseObject["statusCode"]?.ToString();
                        if (statusCode == "0")
                        {
                            string statusMessage = responseObject["statusMessage"]?.ToString();
                            string accountNumber = responseObject["accountNumber"]?.ToString();
                            string bankName = responseObject["bankName"]?.ToString();

                            storedProcedureName = "SaveWalletBankAccountDetails";
                            var parameters = new DynamicParameters();
                            parameters.Add("_Currency_ID", 43);
                            parameters.Add("_Base_Currency_ID", 0);
                            parameters.Add("_Customer_ID", entity.Customer_ID);
                            parameters.Add("_ApiCustomer_ID", accountReference);
                            parameters.Add("_Opening_Date", DateTime.Now);
                            parameters.Add("_Description", "");
                            parameters.Add("_Bank_Name", bankName);
                            parameters.Add("_Bank_Address", "");
                            parameters.Add("_IBAN", 0);
                            parameters.Add("_Swift_Code", "");
                            parameters.Add("_BIC_Code", "");
                            parameters.Add("_Account_Number", accountNumber);
                            parameters.Add("_Sort_Code", 0);
                            parameters.Add("_Account_Holder_Name", accountName);
                            parameters.Add("_API_ID", APIID);
                            parameters.Add("_Client_ID", entity.Client_ID);
                            parameters.Add("_Delete_Status", 0);
                            parameters.Add("_Record_Insert_DateTime", DateTime.Now);
                            parameters.Add("_Last_Updated_DateTime", DateTime.Now);
                            parameters.Add("_Parent_Bank_Account_ID", 0);
                            parameters.Add("_Account_Nick_Name", "");
                            parameters.Add("_ApiAccountID", referenceNumber);
                            parameters.Add("_ApiAccountIDACC", "");

                            try
                            {
                                int result = await _dbConnection.ExecuteAsync(
                                    storedProcedureName,
                                    parameters,
                                    commandType: CommandType.StoredProcedure
                                );                               
                            }
                            catch (Exception ex){}
                            try
                            {
                                #region set limit
                                storedProcedureName = "Add_default_limit_ApiWalletbank";
                                var parameters1 = new DynamicParameters();
                                parameters1.Add("_Customer_id", Convert.ToInt32(entity.Customer_ID));
                                parameters1.Add("_basecurrency_id", 43);
                                parameters1.Add("_client_id", entity.Client_ID);
                                parameters1.Add("_RecordDate", DateTime.Now);
                                parameters1.Add("_branch_id", entity.Branch_ID);
                                int msg1limit = await _dbConnection.ExecuteAsync(
                                    storedProcedureName,
                                    parameters1,
                                    commandType: CommandType.StoredProcedure
                                );
                                #endregion
                            }
                            catch (Exception ex){}
                        }
                        else
                        {
                            return new CreateNGNWalletResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = "Failed to Create ApiWallet",
                                ApiId = api_id,
                                ExtraFields = new List<string> { "", "" }
                            };
                        }
                    }
                    else
                    {
                        return new CreateNGNWalletResponseViewModel
                        {
                            Status = "Failed",
                            StatusCode = 2,
                            Message = "Failed to Create ApiWallet",
                            ApiId = api_id,
                            ExtraFields = new List<string> { "", "" }
                        };

                    }
                }
                catch (Exception ex) { }
            }
            catch (Exception exr)
            {
                return new CreateNGNWalletResponseViewModel
                {
                    Status = "Failed",
                    StatusCode = 2,
                    Message = "Failed to Create ApiWallet",
                    ApiId = api_id,
                    ExtraFields = new List<string> { "", "" }
                };
              
            }

            return new CreateNGNWalletResponseViewModel
            {
                Status = "Success",
                StatusCode = 0,
                Message = "Wallet Created",
                ApiId = api_id,
                ExtraFields = new List<string> { "", "" }
            };

        }


    }
}
