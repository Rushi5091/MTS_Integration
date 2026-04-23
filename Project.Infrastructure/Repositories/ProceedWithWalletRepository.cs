using Azure;
using Azure.Core;
using Bogus.DataSets;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Project.API.Configuration;
using Project.API.Configuration;
using Project.Core.Entities.Business;
using Project.Core.Entities.General;
using Project.Core.Interfaces.IRepositories;
using RestSharp;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TransferZero.Sdk.Model;

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
            string Message1="";
            string FromCurrency_Code = "";
            string Currency_Code = "";
            string apiStatus = "";


            int? api_id = 0;// entity.BranchListAPI_ID;
            int? Customer_ID = entity.Customer_ID;
            int? Client_ID = entity.Client_ID;
            int? Transaction_ID = 0;// entity.Transaction_ID;
            int? apistatus = 1;
            double? AgentRateapi = 0;
            string Message = "";
            string apibankname = "", apiurl = "", apiuser = "", apipass = "", accesscode = "", apicompany_id = "", api_fields = "",bankId = ""; ;
            string? SecurityKey = _appSettings.SecurityKey;
            string whereclause = " and a.ID=" + api_id;
            dynamic CustWalletBank_Details = null;

            #region we check the account here and that account created api id
            try
            {
                await SaveActivityLogTracker("Get_CustomerBankAccountAndApiDetails -- Start : <br/>"+ Customer_ID, 0, DateTime.Now, 0, "0", entity.Customer_ID, 0, "CreateNGNWallet", 2, 1);
                string _whereclause = "cm.Currency_Code = '" + entity.FromCurrency_Code + "' and m.Customer_ID = " + Customer_ID;
                var storedProcedureName = "Get_CustomerBankAccountAndApiDetails";
                var custparameters = new
                {
                    _security_key = SecurityKey,
                    _where_clause = _whereclause
                };

                var bankAccounts = await _dbConnection.QueryAsync(storedProcedureName, custparameters, commandType: CommandType.StoredProcedure);

                 CustWalletBank_Details = bankAccounts.FirstOrDefault();
                await SaveActivityLogTracker("Get_CustomerBankAccountAndApiDetails -- Successfull : <br/>", 0, DateTime.Now, 0, "0", entity.Customer_ID, 0, "CreateNGNWallet", 2, 1);
                if (CustWalletBank_Details != null)
                {
                    api_id = Convert.ToInt32(CustWalletBank_Details.API_ID);
                    apiurl = Convert.ToString(CustWalletBank_Details.API_URL);
                    accesscode = Convert.ToString(CustWalletBank_Details.UserName);
                    apipass =Convert.ToString(CustWalletBank_Details.Password);
                }
            }
            catch (Exception ex)
            {
                await SaveActivityLogTracker("Error in Get_CustomerBankAccountAndApiDetails -- Start : <br/>"+ex.ToString(), 0, DateTime.Now, 0, "0", entity.Customer_ID, 0, "CreateNGNWallet", 2, 1);
                return new ProceedResponseViewModel
                {
                    Status = "Failed",
                    StatusCode = 2,
                    Message = "Accounts : Failed to Update transaction on Api Wallet",
                    ApiId = api_id,
                    AgentRate = AgentRateapi,
                    ApiStatus = apistatus,
                    ExtraFields = new List<string> { "", "" }
                };
            }
            #endregion  we check the account here and that account created api id



            //var storedProcedureName = "Get_APIDetails";
            // var values = new
            //{
            //    _whereclause = whereclause,
            //    _security_key = SecurityKey
            //};
            //var apidetails = await _dbConnection.QueryAsync(storedProcedureName, values, commandType: CommandType.StoredProcedure);
            //dynamic apidetail = apidetails.FirstOrDefault();

            //whereclause = "and Client_ID=1";
            //storedProcedureName = "active_walletTransactionapi_api";
            //var walletTransactionapi = new
            //{
            //    _whereclause = whereclause,
            //    _securitykey = SecurityKey
            //};
            //var dtwalletTransactionapi = await _dbConnection.QueryAsync(storedProcedureName, walletTransactionapi, commandType: CommandType.StoredProcedure);
            ////dynamic dtwalletTransactionapiDetails = dtwalletTransactionapi.FirstOrDefault();
            ////int apiid = Convert.ToInt32(dtwalletTransactionapiDetails.ID);

            try
            {
                if (api_id == 333)
                {
                    if (entity.FromCurrency_Code == "NGN" && entity.ToCurrency_Code == "NGN")// New Beta PAGA
                    {
                        await SaveActivityLogTracker("BETA-Paga paymentRequest request: <br/>" + entity.FromCurrency_Code + " To " + entity.ToCurrency_Code, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.Customer_ID, Convert.ToInt32(Customer_ID), "BETA-Paga Proceed", entity.Branch_ID, Client_ID);

                        try
                        {

                            string api_Fields = Convert.ToString(CustWalletBank_Details.API_Fields);

                            string Hmac = "", callBackUrl = "";
                            if (api_Fields != "" && api_Fields != null)
                            {
                                Newtonsoft.Json.Linq.JObject objAPI = Newtonsoft.Json.Linq.JObject.Parse(api_Fields);

                                Hmac = objAPI["HMAC"]?.ToString();
                                callBackUrl = objAPI["callBackUrl"]?.ToString();
                                bankId = objAPI["bankId"]?.ToString();
                            }
                            string accountNumber = "";
                            try
                            {
                                var storedProcedureNameB = "GetBeneficiaryDetails";
                                var BenefDetailsvalues = new
                                {
                                    _whereclause = " and bm.Beneficiary_ID =" + entity.Beneficiary_ID
                                };
                                var dttBenefDetails = await _dbConnection.QueryAsync(storedProcedureNameB, BenefDetailsvalues, commandType: CommandType.StoredProcedure);
                                dynamic BenefDetails = dttBenefDetails.FirstOrDefault();
                                if (BenefDetails != null)
                                {
                                    accountNumber = Convert.ToString(BenefDetails.ReceiverAccountNumber);// ReceiverAccountNumber

                                    //phoneNumber = BenefDetails.ReceiverMobileNo.ToString();
                                    //AccountNumber = BenefDetails.ReceiverAccountNumber.ToString();
                                    //phoneCountryCode = Convert.ToInt32(BenefDetails.countryCode);
                                }
                            }
                            catch (Exception ex)
                            {
                                await SaveErrorLogAsync("Error AT the time of  GetBeneficiaryDetails  ProceedWithWallet  Transaction -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                                return new ProceedResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = "Failed to get details of ProceedWithWallet",
                                    ApiId = api_id,
                                    AgentRate = AgentRateapi,
                                    ApiStatus = apistatus,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }



                            var storedProcedureName = "GetCustDetailsByID";
                            var CustDetailsvalues = new
                            {
                                cust_ID = entity.Customer_ID
                            };
                            var dttCustDetails = await _dbConnection.QueryAsync(storedProcedureName, CustDetailsvalues, commandType: CommandType.StoredProcedure);
                            dynamic CustDetails = dttCustDetails.FirstOrDefault();


                            string credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{accesscode}:{apipass}"));

                            string referenceNumber = Convert.ToString(CustDetails.WireTransfer_ReferanceNo);
                            string phoneNumber = Convert.ToString(CustDetails.Phone_Number);
                            string firstName = Convert.ToString(CustDetails.First_Name);
                            string lastName = Convert.ToString(CustDetails.Last_Name);
                            string accountName = Convert.ToString(CustDetails.Full_name);
                            string accountReference = Convert.ToString(CustDetails.WireTransfer_ReferanceNo);
                            //string callbackUrl = "https://webhook.site/a49689c2-e241-46dc-a9b0-57b9e44eed85";
                            //accountReference = "kobot-" + accountReference;
                            //accountReference = obj.ReferenceNo + "-" + accountReference;
                            accountReference = Convert.ToString(entity.TransactionRef);
                            string hashedData = GenerateHashedData(accountReference, accountReference, callBackUrl, Hmac);
                            string jsonResponse = "";
                            JObject responseObject = null;
                            dynamic dt_getWaldt = null;
                            if (accountNumber != "")
                            {
                                #region get api walletdata
                                if (!string.IsNullOrEmpty(phoneNumber))
                                {
                                    if (!phoneNumber.StartsWith("0"))
                                    {
                                        phoneNumber = "0" + phoneNumber;
                                    }
                                }
                                dt_getWaldt = CustWalletBank_Details;
                                //if (dt_getWaldt.Rows.Count > 0)
                                if (CustWalletBank_Details != null)
                                {
                                    #region get paga api details

                                    string currentDateTimeUTC = DateTime.UtcNow.AddMinutes(5).ToString("yyyy-MM-ddTHH:mm:ss");
                                    string counterPartyid = "";
                                    int Currency_ID = int.TryParse(Convert.ToString(dt_getWaldt.Currency_ID), out int temp) ? temp : 0;
                                    //int APIID = Convert.ToInt32(dtt.Rows[0]["ID"]);
                                    //string apiurl = Convert.ToString(dtt.Rows[0]["Decrypted_API_URL"]);
                                    //string accesscode = Convert.ToString(dtt.Rows[0]["Decrypted_APIAccess_Code"]);

                                    //string hashedData2 = GenerateHashedData2(referenceNumber, Convert.ToString(obj.AmountInGBP), Currency_Code, phoneNumber, Convert.ToString(dtCD.Rows[0]["Email_ID"]), phoneNumber, Hmac);
                                    decimal amountInGBP1 = Convert.ToDecimal(obj.AmountInGBP);
                                    //decimal amountInGBP1 = 10;
                                    string fullName = CustDetails.Full_name.ToString();

                                    string emailId = CustDetails.Email_ID.ToString();
                                    // string accountNumber = Convert.ToString(dt_getWaldt.Rows[0]["Account_Number"]);
                                    //string accountNumber = dt_getWaldt.Account_Number.ToString();


                                    string jsonData = GenerateJsonData(accountReference, amountInGBP1, phoneNumber, bankId, fullName, emailId, entity.BeneficiaryName.ToString(), accountNumber, currentDateTimeUTC, callBackUrl);

                                    // Generate Hash 
                                    string hashedData2 = GenerateHashedData2(jsonData, Hmac);
                                    await SaveActivityLogTracker("BETA-Paga Generate Hash : <br/>" + hashedData2 + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.Customer_ID, Convert.ToInt32(Customer_ID), "BETA-Paga Proceed", entity.Branch_ID, Client_ID);

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
    @"        ""phoneNumber"": """ + phoneNumber + @""",
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
                                                        await SaveErrorLogAsync("Error in statusCode response Transaction -" + responseObject["statusMessage"].ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
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
                                                    await SaveErrorLogAsync("Error in response Transaction -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                                                    return new ProceedResponseViewModel
                                                    {
                                                        Status = "Failed",
                                                        StatusCode = 2,
                                                        Message = "Failed to Update Transaction Wallet at api",
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

                                                await SaveErrorLogAsync("Error in paymentRequest response - " + statusMessage, DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
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
                                            await SaveErrorLogAsync("Error in response statusCode - " + statusMessage, DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);

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
                            else
                            {

                            }
                        }
                        catch (Exception ex) { }
                    }
                    else if ((entity.FromCurrency_Code == "CAD" && entity.ToCurrency_Code == "CAD") || (entity.FromCurrency_Code == "NGN" && entity.ToCurrency_Code == "CAD"))
                    {
                        await SaveActivityLogTracker("DC bank paymentRequest Start : <br/>" + entity.FromCurrency_Code + " To " + entity.ToCurrency_Code, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.Customer_ID, Convert.ToInt32(Customer_ID), "DC bank Proceed", entity.Branch_ID, Client_ID);

                        int phoneCountryCode = 0; string phoneNumber = "", AccountNumber = "", customerName = "", email = "";
                        int countryCode = 0;
                        dynamic CustDetails = null;

                        try
                        {
                            await SaveActivityLogTracker("DC bank Start  GetCustDetailsByID  ProceedWithWallet  Transaction : <br/>" + entity.FromCurrency_Code + " To " + entity.ToCurrency_Code, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.Customer_ID, Convert.ToInt32(Customer_ID), "DC bank Proceed", entity.Branch_ID, Client_ID);

                            var storedProcedureName = "GetCustDetailsByID";
                            var CustDetailsvalues = new
                            {
                                cust_ID = entity.Customer_ID
                            };
                            var dttCustDetails = await _dbConnection.QueryAsync(storedProcedureName, CustDetailsvalues, commandType: CommandType.StoredProcedure);
                            CustDetails = dttCustDetails.FirstOrDefault();
                            if (CustDetails != null)
                            {
                                customerName = CustDetails.Full_name.ToString();
                                email = CustDetails.Email_ID.ToString();
                            }
                        }
                        catch (Exception ex)
                        {
                            await SaveErrorLogAsync("Error AT the time of  GetCustDetailsByID  ProceedWithWallet  Transaction -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                            return new ProceedResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = "Failed to get details of ProceedWithWallet",
                                ApiId = api_id,
                                AgentRate = AgentRateapi,
                                ApiStatus = apistatus,
                                ExtraFields = new List<string> { "", "" }
                            };
                        }

                        //cmdBD.Parameters.AddWithValue("_whereclause", " and bm.Beneficiary_ID =" + obj.Beneficiary_ID);
                        dynamic BenefDetails = null;
                        try
                        {
                            await SaveActivityLogTracker("DC bank Start sp GetBeneficiaryDetails  ProceedWithWallet  Transaction : <br/>" + entity.FromCurrency_Code + " To " + entity.ToCurrency_Code, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.Customer_ID, Convert.ToInt32(Customer_ID), "DC bank Proceed", entity.Branch_ID, Client_ID);

                            var storedProcedureName = "GetBeneficiaryDetails";
                            var BenefDetailsvalues = new
                            {
                                _whereclause = " and bm.Beneficiary_ID =" + entity.Beneficiary_ID
                            };
                            var dttBenefDetails = await _dbConnection.QueryAsync(storedProcedureName, BenefDetailsvalues, commandType: CommandType.StoredProcedure);
                            BenefDetails = dttBenefDetails.FirstOrDefault();
                            if (BenefDetails != null)
                            {
                                phoneCountryCode = Convert.ToInt32(BenefDetails.countryCode);
                                phoneNumber = BenefDetails.ReceiverMobileNo.ToString();
                                AccountNumber = BenefDetails.ReceiverAccountNumber.ToString();
                                phoneCountryCode = Convert.ToInt32(BenefDetails.countryCode);
                            }
                        }
                        catch (Exception ex)
                        {
                            await SaveErrorLogAsync("Error AT the time of sp GetBeneficiaryDetails  ProceedWithWallet  Transaction -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                            return new ProceedResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = "Failed to get details of ProceedWithWallet",
                                ApiId = api_id,
                                AgentRate = AgentRateapi,
                                ApiStatus = apistatus,
                                ExtraFields = new List<string> { "", "" }
                            };
                        }


                        string country = "";
                        dynamic dtwalletTransactionapiDetails = CustWalletBank_Details;
                        await SaveActivityLogTracker("DC bank Start sp active_walletTransactionapi  ProceedWithWallet  Transaction : <br/>" + entity.FromCurrency_Code + " To " + entity.ToCurrency_Code, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.Customer_ID, Convert.ToInt32(Customer_ID), "DC bank Proceed", entity.Branch_ID, Client_ID);

                        string APIAccess_Code = "";
                        var storedProcedurewalletTransactionapi = "active_walletTransactionapi";
                        var AnchorDetailsvalues = new
                        {
                            _whereclause = " and API_ID =6",
                            _securitykey = SecurityKey
                        };
                        var dttAnchorNew = await _dbConnection.QueryAsync(storedProcedurewalletTransactionapi, AnchorDetailsvalues, commandType: CommandType.StoredProcedure);
                        dynamic dttAnchorNewDetails = dttAnchorNew.FirstOrDefault();
                        if (dttAnchorNewDetails != null)
                        {
                            apiurl = Convert.ToString(dttAnchorNewDetails.DecryptedAPI_URL);
                            string api_Fields = Convert.ToString(dttAnchorNewDetails.API_Fields);

                            string Hmac = "", callBackUrl = "";
                            if (api_Fields != "" && api_Fields != null)
                            {
                                Newtonsoft.Json.Linq.JObject objAPI = Newtonsoft.Json.Linq.JObject.Parse(api_Fields);
                                APIAccess_Code = objAPI["token"]?.ToString();
                            }
                            //}
                            //if (dtwalletTransactionapiDetails != null)
                            //{

                            ////api_id = Convert.ToInt32(CustWalletBank_Details.API_ID);
                            ////apiurl = Convert.ToString(CustWalletBank_Details.API_URL);
                            ////accesscode = Convert.ToString(CustWalletBank_Details.UserName);
                            ////apipass = Convert.ToString(CustWalletBank_Details.Password);

                            ////apiurl = dtwalletTransactionapiDetails.API_URL.ToString();
                            //apiurl = Convert.ToString(CustWalletBank_Details.API_URL);

                            // APIAccess_Code = Convert.ToString(CustWalletBank_Details.UserName);
                            ////string APIAccess_Code = dtwalletTransactionapiDetails.APIAccess_Code.ToString();

                            double amount = Convert.ToDouble(entity.AmountInPKR);

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
                            await SaveActivityLogTracker("DC bank Start /integrationapi/v1.0/ETransfer/CreateEtransferTransactionWithCustomer  apiurl  ProceedWithWallet  Transaction : <br/>" + apiurl + " " + entity.FromCurrency_Code + " To " + entity.ToCurrency_Code, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.Customer_ID, Convert.ToInt32(Customer_ID), "DC bank Proceed", entity.Branch_ID, Client_ID);

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
    @"    ""transactionTypeCode"": ""D"", " + "\n" +
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
                            catch (Exception ex)
                            {
                                await SaveErrorLogAsync("Error AT the time of  bodyJson  ProceedWithWallet  Transaction -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                                return new ProceedResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = "Failed to get details of ProceedWithWallet",
                                    ApiId = api_id,
                                    AgentRate = AgentRateapi,
                                    ApiStatus = apistatus,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }

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
                                await SaveActivityLogTracker("DC bank Start sp sp_InsertSecurityQuestionAnswer  ProceedWithWallet  Transaction : <br/>" + entity.FromCurrency_Code + " To " + entity.ToCurrency_Code, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.Customer_ID, Convert.ToInt32(Customer_ID), "DC bank Proceed", entity.Branch_ID, Client_ID);

                                var storedProcedureName = "sp_InsertSecurityQuestionAnswer";
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
                                await SaveErrorLogAsync("Error AT the time of Security QueAns ProceedWithWallet Transaction -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                                return new ProceedResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = "Failed In Security QueAns of ProceedWithWallet",
                                    ApiId = api_id,
                                    AgentRate = AgentRateapi,
                                    ApiStatus = apistatus,
                                    ExtraFields = new List<string> { "", "" }
                                };
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
                                    await SaveErrorLogAsync("DCBank Api Response " + ex.ToString(), DateTime.Now, "ProceedWithWallet", entity.Customer_ID, entity.Branch_ID, Client_ID, 0);
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

                                }
                                //CompanyInfo.InsertApiWalletTransactionDetails(obj.Transaction_ID, Customer_ID, refer, TransactionId, Reference, CreatedAt, Amount, Currency, Status, CounterPartyId, AccountId, CustomerId);
                                if (ErrorCode == "0" && ErrorDescription == "Success" || ErrorDescription == "Successfull")
                                {
                                    await SaveActivityLogTracker("DC bank Start /integrationapi/v1.0/ETransfer/SearchEtransferTransaction ProceedWithWallet  Transaction apiurl : <br/>" + apiurl + " : " + entity.FromCurrency_Code + " To " + entity.ToCurrency_Code, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.Customer_ID, Convert.ToInt32(Customer_ID), "DC bank Proceed", entity.Branch_ID, Client_ID);

                                    var options = new RestClientOptions(apiurl + "/integrationapi/v1.0/ETransfer/SearchEtransferTransaction")
                                    {
                                        MaxTimeout = -1
                                    };
                                    var clientSearchEtransferTransaction = new RestClient(options);
                                    var requestSearchEtransferTransaction = new RestRequest()
                                    {
                                        Method = Method.Post
                                    };

                                    requestSearchEtransferTransaction.AddHeader("Authorization", "Bearer " + APIAccess_Code);
                                    requestSearchEtransferTransaction.AddHeader("Accept", "application/json");
                                    requestSearchEtransferTransaction.AddHeader("Content-Type", "application/json");
                                    bodyJson = @"{
" + "\n" +
    @"     ""TransactionId"": " + transactionId + @"  " + "\n" +
    @"}";

                                    requestSearchEtransferTransaction.AddParameter("application/json", bodyJson, ParameterType.RequestBody);
                                    await SaveActivityLogTracker("DC bank SearchEtransferTransaction request : <br/>" + bodyJson + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.Customer_ID, Convert.ToInt32(Customer_ID), "ProceedWithWalletd", entity.Branch_ID, Client_ID);

                                    RestResponse responseSearchEtransferTransaction = clientSearchEtransferTransaction.Execute(requestSearchEtransferTransaction);
                                    await SaveActivityLogTracker("DC bank SearchEtransferTransaction response : <br/>" + responseSearchEtransferTransaction + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.Customer_ID, Convert.ToInt32(Customer_ID), "ProceedWithWalletd", entity.Branch_ID, Client_ID);

                                    jsonResponse = responseSearchEtransferTransaction.Content;
                                    responseObject1 = Newtonsoft.Json.Linq.JObject.Parse(jsonResponse);
                                    apiStatus = "COMPLETED";
                                }
                                else
                                {
                                    await SaveErrorLogAsync("Error In DCBank Api Response Transaction -", DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
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
                                }
                            }
                            else
                            {
                                await SaveErrorLogAsync("Error In DCBank Api Response Transaction -", DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
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
                else if (api_id == 4)
                {
                    string providus_walletLogs = "";
                    try
                    {
                        string refreshToken = "", accessToken = "", wallet_bank_name = "", wallet_bank_code = "";
                        string secretKey = "", Benf_accountNumber = ""; string accountName = "", AccountHolderName = "";
                        string api_wallet_customerID = "", narration = "", Transaction_Reference = "";
                        string api_customer_walletID = ""; string urll = ""; string Bank_code = ""; string AvailableBalance = "";


                        api_wallet_customerID = Convert.ToString(CustWalletBank_Details.bankholderID);//"[API_Wallet_ID, 76ab7321-c5f7-4675-957e-1e420e851eae]"
                        api_customer_walletID = Convert.ToString(CustWalletBank_Details.referenceID);//"[ApiCustomer_ID, 538fc273-b862-4526-ba1e-953b2a5991bc]"
                        providus_walletLogs = providus_walletLogs + " api_wallet_customerID=" + api_wallet_customerID + " api_customer_walletID=" + api_customer_walletID;                                                               //                   
                        if (CustWalletBank_Details != "")
                        {
                            try
                            {
                                var storedProcedureName = "get_api_wallet_transaction_details";
                                var walletTransactionetails = new
                                {
                                    iClient_ID = obj.Client_ID,
                                    iTransaction_ID = obj.Transaction_ID,
                                    iBranchListAPI_ID = api_id
                                };
                                providus_walletLogs= providus_walletLogs+ " iClient_ID="+ obj.Client_ID+ " iTransaction_ID="+ obj.Transaction_ID+ " iBranchListAPI_ID="+ api_id;
                                var dtwalletTransactiondetails = await _dbConnection.QueryAsync(storedProcedureName, walletTransactionetails, commandType: CommandType.StoredProcedure);
                                providus_walletLogs= providus_walletLogs+ " dtwalletTransactiondetails="+ dtwalletTransactiondetails;
                                var dtr = dtwalletTransactiondetails.FirstOrDefault();
                                providus_walletLogs= providus_walletLogs+ " dtr="+ dtr;
                                if (dtr != "")
                                {
                                    wallet_bank_code = Convert.ToString(dtr.wallet_bank_code);
                                    wallet_bank_name = Convert.ToString(dtr.wallet_bank_name);
                                    Benf_accountNumber = Convert.ToString(dtr.Account_Number);
                                    AccountHolderName = Convert.ToString(dtr.AccountHolderName);
                                    narration = Convert.ToString(dtr.Purpose);
                                    //Transaction_Reference = Convert.ToString(dtr.ReferenceNo);
                                    providus_walletLogs = providus_walletLogs + " wallet_bank_code=" + wallet_bank_code + " wallet_bank_name=" + wallet_bank_name + " Benf_accountNumber=" + Benf_accountNumber + " AccountHolderName=" + AccountHolderName + " narration=" + narration;
                                }
                            }
                            catch (Exception ex)
                            {
                                await SaveErrorLogAsync("Error AT the time of  get_api_wallet_transaction_details Insert Transaction -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                            }

                            string bankholderID = Convert.ToString(CustWalletBank_Details.bankholderID);
                            #region check wallet status 
                            
                            int walletstatus = Convert.ToInt32(CustWalletBank_Details.Delete_Status);
                            if (walletstatus == 1)
                            {
                                await SaveActivityLogTracker("Wallet is inactive ", 0, DateTime.Now, 0, "0", entity.Customer_ID, 0, "CreateNGNWallet", 2, 1);
                                return new ProceedResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = "Customer wallet is inactive .",
                                    ApiId = api_id,
                                    AgentRate = AgentRateapi,
                                    ApiStatus = apistatus,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }
                            #endregion check wallet status 
                            //wallet_bank_name = Convert.ToString(CustWalletBank_Details.Bank_Name);
                            //Benf_accountNumber = Convert.ToString(CustWalletBank_Details.Account_Number);
                            //AccountHolderName = Convert.ToString(CustWalletBank_Details.Account_Holder_Name);
                            //narration = "Wallet Transaction Purpose";
                            //narration = Convert.ToString(CustWalletBank_Details.Purpose);
                            Transaction_Reference = Convert.ToString(entity.TransactionRef);

                            providus_walletLogs = providus_walletLogs + " bankholderID=" + bankholderID + " Transaction_Reference=" + Transaction_Reference;
                            string email = accesscode;//"aW5mb0BzdXBlcnRyYW5zZmVyLmNvLnVr";
                            string password = apipass;//"RklLS3k0Y2FyZDEjQA==";
                                                      //string password = Password;//"RklLS3k0Y2FyZDEjQA==";
                            providus_walletLogs = providus_walletLogs + " email=" + email + " password=" + password;
                            // Login
                            try
                            {
                                urll = apiurl;
                                ServicePointManager.Expect100Continue = true;
                                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                       | SecurityProtocolType.Tls11
                                       | SecurityProtocolType.Tls12;
                                var options = new RestClientOptions(urll + "/api/v1/auth/login")
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
                                var body1 = @"{" + "\n" +
                     @"    ""email"": """ + email + @""",
        " + "\n" +
                     @"    ""password"": """ + password + @""""
                     + "\n" +
                     @"}";
                                request.AddParameter("application/json", body1, ParameterType.RequestBody);
                                await SaveActivityLogTracker("Providus Wallet login api Request Insert Transaction -" + urll + "/api/v1/auth/login"+body1, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);

                                RestResponse response = client.Execute(request);

                                await SaveActivityLogTracker("Providus Wallet login api Response Insert Transaction - " + response.Content, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);


                                accessToken = response.Headers
                    .FirstOrDefault(h => h.Name == "X-Access-Token")?.Value?.ToString();

                                refreshToken = response.Headers
                                   .FirstOrDefault(h => h.Name == "X-Refresh-Token")?.Value?.ToString();
                                await SaveActivityLogTracker("Providus Wallet login api accessToken and refreshToken Insert Transaction - " + accessToken + " |refreshToken -" + refreshToken, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);

                            }
                            catch (Exception ex)
                            {
                                await SaveErrorLogAsync(" Error In Providus Wallet login api Insert Transaction  -" + ex.ToString(), DateTime.Now, "Proceed", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                                return new ProceedResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = "Failed to Create Wallet Transaction.",
                                    ApiId = api_id,
                                    AgentRate = AgentRateapi,
                                    ApiStatus = apistatus,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }
                            providus_walletLogs = providus_walletLogs + " refreshToken=" + refreshToken + " accessToken=" + accessToken;
                            if (refreshToken != "" && accessToken != "")
                            {
                                // Generate Secret Key
                                try
                                {


                                    var options = new RestClientOptions(urll + "/api/v1/merchant/generate-access-keys")
                                    {
                                        MaxTimeout = -1
                                    };
                                    var client = new RestClient(options);
                                    var request = new RestRequest()
                                    {
                                        Method = Method.Post
                                    };

                                    request.AddHeader("Content-Type", "application/json");
                                    request.AddHeader("X-Access-Token", accessToken);
                                    request.AddHeader("X-Refresh-Token", refreshToken);

                                    await SaveActivityLogTracker("Providus Wallet Generate Secret Key Request Insert Transaction - " + urll + "/api/v1/merchant/generate-access-keys", 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);
                                    RestResponse response = client.Execute(request);


                                    await SaveActivityLogTracker("Providus Wallet Generate Secret Key Response Insert Transaction - " + response.Content, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);

                                    dynamic dynJsonn = JsonConvert.DeserializeObject(response.Content);

                                    bool status = (bool)dynJsonn.status;
                                    if (status)
                                    {
                                        string publicKey = (string)dynJsonn.data.publicKey;
                                        secretKey = (string)dynJsonn.data.privateKey;
                                        providus_walletLogs = providus_walletLogs + " publicKey=" + publicKey + " secretKey=" + secretKey;
                                    }
                                    else
                                    {
                                        Console.WriteLine("Status is false.");
                                        return new ProceedResponseViewModel
                                        {
                                            Status = "Failed",
                                            StatusCode = 2,
                                            Message = "Failed to Create Wallet Transaction.",
                                            ApiId = api_id,
                                            AgentRate = AgentRateapi,
                                            ApiStatus = apistatus,
                                            ExtraFields = new List<string> { "", "" }
                                        };
                                    }

                                }
                                catch (Exception ex)
                                {
                                    await SaveErrorLogAsync(" Error In Providus Wallet Generate Secret Key Insert Transaction -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                                    return new ProceedResponseViewModel
                                    {
                                        Status = "Failed",
                                        StatusCode = 2,
                                        Message = "Failed to Create Wallet",
                                        ApiId = api_id,
                                        AgentRate = AgentRateapi,
                                        ApiStatus = apistatus,
                                        ExtraFields = new List<string> { "", "" }
                                    };
                                }

                                if (secretKey != "")
                                {


                                    //Get Wallet by Customer ID
                                    try
                                    {

                                        var options = new RestClientOptions(urll + "/api/v1/wallet/customer?customerId=" + bankholderID)
                                        {
                                            MaxTimeout = -1
                                        };
                                        var client = new RestClient(options);
                                        var request = new RestRequest()
                                        {
                                            Method = Method.Get
                                        };


                                        request.AddHeader("Content-Type", "application/json");
                                        request.AddHeader("Authorization", "Bearer " + secretKey);
                                        await SaveActivityLogTracker("Providus Wallet Get Customer Wallet Request Insert Transaction - " + urll + "/api/v1/wallet/customer?customerId=" + api_wallet_customerID, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);

                                        var response = client.Execute(request);
                                        await SaveActivityLogTracker("Providus Wallet Get Customer Wallet Response Insert Transaction - " + response.Content, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);

                                        try
                                        {
                                            JObject jsonResponse = JObject.Parse(response.Content);


                                            bool apiStatus2 = jsonResponse["status"]?.Value<bool>() ?? false;
                                            if (!apiStatus2)
                                            {
                                                Message1 = jsonResponse["message"]?.ToString();

                                                if(Message1 == "")
                                                {
                                                    Message1 = "Unable To Create Wallet Transaction.";
                                                }
                                                return new ProceedResponseViewModel
                                                {
                                                    Status = "Failed",
                                                    StatusCode = 2,
                                                    Message = Message1,
                                                    ApiId = api_id,
                                                    AgentRate = AgentRateapi,
                                                    ApiStatus = apistatus,
                                                    ExtraFields = new List<string> { "", "" }
                                                };
                                            }


                                            var wallet = jsonResponse["wallet"];
                                            if (wallet != null)
                                            {
                                                AvailableBalance = wallet["availableBalance"]?.ToString();
                                                providus_walletLogs = providus_walletLogs + " AvailableBalance=" + AvailableBalance;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"Error parsing JSON: {ex.Message}");
                                            await SaveErrorLogAsync("Error In Providus parsing JSON Insert Transaction -" + ex.ToString(), DateTime.Now, "Proceed", obj.User_ID, obj.Branch_ID, Client_ID, 0);

                                            if (Message1 == "")
                                            {
                                                Message1 = "Unable To Create Wallet Transaction.";
                                            }
                                            return new ProceedResponseViewModel
                                            {
                                                Status = "Failed",
                                                StatusCode = 2,
                                                Message = Message1,
                                                ApiId = api_id,
                                                AgentRate = AgentRateapi,
                                                ApiStatus = apistatus,
                                                ExtraFields = new List<string> { "", "" }
                                            };
                                        }

                                    }
                                    catch (Exception ex)
                                    {
                                        await SaveErrorLogAsync("Error In Providus Wallet Get Customer wallet Api Insert Transaction -" + ex.ToString(), DateTime.Now, "Proceed", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                                        return new ProceedResponseViewModel
                                        {
                                            Status = "Failed",
                                            StatusCode = 2,
                                            Message = "Failed Create Customer Wallet Transaction.",
                                            ApiId = api_id,
                                            AgentRate = AgentRateapi,
                                            ApiStatus = apistatus,
                                            ExtraFields = new List<string> { "", "" }
                                        };
                                    }
                                    providus_walletLogs = providus_walletLogs + " obj.Total_Amount=" + obj.Total_Amount;
                                    if (Convert.ToDouble(AvailableBalance) >= Convert.ToDouble(obj.Total_Amount))
                                    {

                                        string amount = Convert.ToString(obj.Total_Amount);
                                        providus_walletLogs= providus_walletLogs + " amount=" + amount;
                                        bool localtransaction = false;
                                        providus_walletLogs= providus_walletLogs + " FromCurrency_Code=" + obj.FromCurrency_Code + " ToCurrency_Code=" + obj.ToCurrency_Code;
                                        if (obj.FromCurrency_Code == obj.ToCurrency_Code)
                                        {
                                            localtransaction = false;
                                        }
                                        else
                                        {
                                            localtransaction = true;
                                        }
                                        providus_walletLogs= providus_walletLogs + " localtransaction=" + localtransaction;
                                        if (localtransaction == true)
                                        {
                                            string someData = "Transfer to the client wallet";
                                            string moreData = "Transfer to the client wallet";
                                            var options = new RestClientOptions(urll + "/api/v1/wallet/debit")
                                            {
                                                MaxTimeout = -1
                                            };
                                            var client = new RestClient(options);
                                            var request = new RestRequest()
                                            {
                                                Method = Method.Post
                                            };

                                            request.AddHeader("Content-Type", "application/json");
                                            request.AddHeader("Authorization", "Bearer " + secretKey);

                                            string body_transferwallet = @"{" + "\n" +
                                        @"    ""amount"": " + amount + @"," + "\n" +
                                        @"    ""reference"": """ + obj.TransactionRef + @"""," + "\n" +
                                        @"    ""customerId"": """ + api_wallet_customerID + @"""," + "\n" +
                                        @"    ""metadata"": {" + "\n" +
                                        @"        ""some-data"": """ + someData + @"""," + "\n" +
                                        @"        ""more-data"": """ + moreData + @"""" + "\n" +
                                        @"    }" + "\n" +
                                        @"}";

                                            request.AddParameter("application/json", body_transferwallet, ParameterType.RequestBody);
                                            await SaveActivityLogTracker("Providus Wallet Debit(International) Request Insert Transaction - " + urll + "/api/v1/wallet/debit" + body_transferwallet, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);

                                            RestResponse response = client.Execute(request);

                                            await SaveActivityLogTracker("Providus Wallet Debit(International) Response Insert Transaction - " + response.Content, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);


                                            try
                                            {
                                                JObject jsonResponse = JObject.Parse(response.Content);


                                                bool transactionapiStatus = jsonResponse["status"]?.Value<bool>() ?? false;
                                                providus_walletLogs= providus_walletLogs + " transactionapiStatus=" + transactionapiStatus;
                                                if (!transactionapiStatus)
                                                {
                                                    Message1 = jsonResponse["message"]?.ToString();
                                                    providus_walletLogs= providus_walletLogs + " Message1=" + Message1;
                                                    await SaveActivityLogTracker("App - Providus Wallet Debit(International) Transfer Status Failed Insert Transaction - " + apiStatus + ".", 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);
                                                    return new ProceedResponseViewModel
                                                    {
                                                        Status = "Failed",
                                                        StatusCode = 2,
                                                        Message = Message1,
                                                        ApiId = api_id,
                                                        AgentRate = AgentRateapi,
                                                        ApiStatus = apistatus,
                                                        ExtraFields = new List<string> { "", "" }
                                                    };

                                                }

                                                Message = jsonResponse["message"]?.ToString();
                                                providus_walletLogs= providus_walletLogs + " Message=" + Message;
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
                                            catch (Exception ex)
                                            {
                                                await SaveErrorLogAsync("Error In Providus Wallet Debit(International) Transfer Insert Transaction -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                                                return new ProceedResponseViewModel
                                                {
                                                    Status = "Failed",
                                                    StatusCode = 2,
                                                    Message = "Failed to Create Wallet Transaction.",
                                                    ApiId = api_id,
                                                    AgentRate = AgentRateapi,
                                                    ApiStatus = apistatus,
                                                    ExtraFields = new List<string> { "", "" }
                                                };
                                            }



                                        }
                                        else if (localtransaction == false)
                                        {
                                            //Get Bank List
                                            try
                                            {

                                                var options = new RestClientOptions(urll + "/api/v1/transfer/banks")
                                                {
                                                    MaxTimeout = -1
                                                };
                                                var client = new RestClient(options);
                                                var request = new RestRequest()
                                                {
                                                    Method = Method.Get
                                                };

                                                request.AddHeader("Content-Type", "application/json");
                                                request.AddHeader("Authorization", "Bearer " + secretKey);

                                                await SaveActivityLogTracker("Providus Wallet Get Bank List Api Request Insert Transaction - " + urll + "/api/v1/transfer/banks", 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);

                                                RestResponse response = client.Execute(request);
                                                await SaveActivityLogTracker("Providus Wallet Get Bank List Api Response Insert Transaction - " + response.Content, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);

                                                JObject jsonResponse = JObject.Parse(response.Content);
                                                JArray banks = (JArray)jsonResponse["banks"];
                                                var bank = banks.FirstOrDefault(b => b["code"]?.ToString() == wallet_bank_code);

                                                if (bank != null)
                                                {
                                                    Bank_code = bank["code"]?.ToString();
                                                    string name = bank["name"]?.ToString();
                                                    await SaveActivityLogTracker("Providus Wallet Bank Code Found in Insert Transaction - " + name + " |code" + Bank_code, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);
                                                }
                                                else
                                                {
                                                    await SaveActivityLogTracker("Providus Wallet Bank Code Not Found in Insert Transaction - " + wallet_bank_code + ".", 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);
                                                    return new ProceedResponseViewModel
                                                    {
                                                        Status = "Failed",
                                                        StatusCode = 2,
                                                        Message = "Failed to Create Wallet Transaction.",
                                                        ApiId = api_id,
                                                        AgentRate = AgentRateapi,
                                                        ApiStatus = apistatus,
                                                        ExtraFields = new List<string> { "", "" }
                                                    };
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                await SaveErrorLogAsync(" Error In Providus Wallet Get Bank List Api Insert Transaction -" + ex.ToString(), DateTime.Now, "Proceed", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                                                return new ProceedResponseViewModel
                                                {
                                                    Status = "Failed",
                                                    StatusCode = 2,
                                                    Message = "Failed to Create Wallet Transaction.",
                                                    ApiId = api_id,
                                                    AgentRate = AgentRateapi,
                                                    ApiStatus = apistatus,
                                                    ExtraFields = new List<string> { "", "" }
                                                };
                                            }

                                            if (Bank_code != "")
                                            {
                                                //Fetch Account Details
                                                try
                                                {



                                                    var options = new RestClientOptions(urll + "/api/v1/transfer/account/details?sortCode=" + wallet_bank_code + "&accountNumber=" + Benf_accountNumber)
                                                    {
                                                        MaxTimeout = -1
                                                    };
                                                    var client = new RestClient(options);
                                                    var request = new RestRequest()
                                                    {
                                                        Method = Method.Get
                                                    };

                                                    request.AddHeader("Content-Type", "application/json");
                                                    request.AddHeader("Authorization", "Bearer " + secretKey);
                                                    await SaveActivityLogTracker("Providus Wallet Fetch Account Details Api Request Insert Transaction - " + urll + "/api/v1/transfer/account/details?sortCode=" + wallet_bank_code + "&accountNumber=" + Benf_accountNumber, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);
                                                    RestResponse response = client.Execute(request);

                                                    await SaveActivityLogTracker("Providus Wallet Fetch Account Details Api Response Insert Transaction - " + response.Content, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);

                                                    JObject jsonResponse = JObject.Parse(response.Content);


                                                    JObject account = (JObject)jsonResponse["account"];

                                                    if (account != null)
                                                    {
                                                        string bankCode = account["bankCode"]?.ToString();
                                                        accountName = account["accountName"]?.ToString();
                                                        string AccountNumber = account["accountNumber"]?.ToString();
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    await SaveErrorLogAsync(" Error In Providus Wallet Fetch Account Details Api Request Insert Transaction -" + ex.ToString(), DateTime.Now, "Proceed", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                                                    return new ProceedResponseViewModel
                                                    {
                                                        Status = "Failed",
                                                        StatusCode = 2,
                                                        Message = "Failed to Create Wallet Transaction.",
                                                        ApiId = api_id,
                                                        AgentRate = AgentRateapi,
                                                        ApiStatus = apistatus,
                                                        ExtraFields = new List<string> { "", "" }
                                                    };
                                                }

                                                if (accountName != "")
                                                {

                                                    try
                                                    {
                                                       

                                                        if (accountName == "")
                                                        {
                                                            accountName = AccountHolderName;
                                                        }

                                                        var options = new RestClientOptions(urll + "/api/v1/transfer/bank/customer")
                                                        {
                                                            MaxTimeout = -1
                                                        };
                                                        var client = new RestClient(options);
                                                        var request = new RestRequest()
                                                        {
                                                            Method = Method.Post
                                                        };

                                                        request.AddHeader("Content-Type", "application/json");
                                                        request.AddHeader("Authorization", "Bearer " + secretKey);

                                                        var body2 = @"{" + "\n" +
                                                         @"    ""amount"": """ + amount + @"""," + "\n" +
                                                         @"    ""sortCode"": """ + wallet_bank_code + @"""," + "\n" +
                                                         @"    ""narration"": """ + narration + @"""," + "\n" +
                                                         @"    ""accountNumber"": """ + Benf_accountNumber + @"""," + "\n" +
                                                         @"    ""accountName"": """ + accountName + @"""," + "\n" +
                                                         @"    ""customerId"": """ + api_wallet_customerID + @"""," + "\n" +
                                                         @"    ""metadata"": {" + "\n" +
                                                         @"        ""customer-data"": """ + Transaction_Reference + @"""" + "\n" +
                                                         @"    }" + "\n" +
                                                         @"}";

                                                        request.AddParameter("application/json", body2, ParameterType.RequestBody);
                                                        await SaveActivityLogTracker("Providus Wallet Create Wallet to Bank Transfer Request Insert Transaction - " + urll + "/api/v1/transfer/bank/customer" + body2, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);

                                                        RestResponse response = client.Execute(request);

                                                        await SaveActivityLogTracker("Providus Wallet Create Wallet to Bank Transfer Response Insert Transaction - " + response.Content + ".", 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);

                                                        try
                                                        {
                                                            JObject jsonResponse = JObject.Parse(response.Content);


                                                            bool transactionapiStatus = jsonResponse["status"]?.Value<bool>() ?? false;
                                                            if (!transactionapiStatus)
                                                            {
                                                                Message1 = jsonResponse["message"]?.ToString();
                                                                await SaveActivityLogTracker("App - Providus Wallet Create Wallet to Bank Transfer Status Failed Insert Transaction - " + apiStatus + ".", 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);
                                                                return new ProceedResponseViewModel
                                                                {
                                                                    Status = "Failed",
                                                                    StatusCode = 2,
                                                                    Message = Message1,
                                                                    ApiId = api_id,
                                                                    AgentRate = AgentRateapi,
                                                                    ApiStatus = apistatus,
                                                                    ExtraFields = new List<string> { "", "" }
                                                                };

                                                            }

                                                            Message = jsonResponse["message"]?.ToString();


                                                            var transfer = jsonResponse["transfer"];
                                                            if (transfer != null)
                                                            {
                                                                string Amount = transfer["amount"]?.ToString();
                                                                string Charges = transfer["charges"]?.ToString();
                                                                string Vat = transfer["vat"]?.ToString();
                                                                Transaction_Reference = transfer["reference"]?.ToString();
                                                                string Total = transfer["total"]?.ToString();
                                                                string SessionId = transfer["sessionId"]?.ToString();
                                                                string Destination = transfer["destination"]?.ToString();
                                                                string TxRef = transfer["transactionReference"]?.ToString();
                                                                string Description = transfer["description"]?.ToString();

                                                                string Metadata = transfer["metadata"]?["customer-data"]?.ToString();
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
                                                        catch (Exception ex)
                                                        {
                                                            await SaveErrorLogAsync("Error In Providus Wallet Create Wallet to Bank Transfer Insert Transaction -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                                                            return new ProceedResponseViewModel
                                                            {
                                                                Status = "Failed",
                                                                StatusCode = 2,
                                                                Message = "Failed to Create Wallet Transaction.",
                                                                ApiId = api_id,
                                                                AgentRate = AgentRateapi,
                                                                ApiStatus = apistatus,
                                                                ExtraFields = new List<string> { "", "" }
                                                            };
                                                        }

                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        await SaveErrorLogAsync("Error In Providus Wallet Create Wallet to Bank Transfer Insert Transaction -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                                                        return new ProceedResponseViewModel
                                                        {
                                                            Status = "Failed",
                                                            StatusCode = 2,
                                                            Message = "Failed to Create Wallet Transaction.",
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
                                                        Message = "Unable To Fetch Beneficiery Account Details.",
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
                                                    Message = "Beneficiery Bank Not Found.",
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
                                            Message = "Insufficient Funds, Please Fund Your Wallet Your Balance is: " + AvailableBalance,
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
                                        Message = "Failed to Create Wallet Transaction.",
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
                                    Message = "Failed to Create Wallet Transaction.",
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
                        await SaveErrorLogAsync(" Error In Providus Wallet  Api Insert Transaction -" + ex.ToString(), DateTime.Now, "Proceed", obj.User_ID, obj.Branch_ID, Client_ID, 0);

                    }
                    finally
                    {
                        await SaveActivityLogTracker("Providus Wallet all trasaction logs - " + providus_walletLogs + ".", 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);

                    }
                }
                else 
                {
                   // Message = "Success";
                    return new ProceedResponseViewModel
                    {
                        Status = "Failed",
                        StatusCode = 2,
                        Message = "Failed to Create Wallet Transaction.",
                        ApiId = api_id,
                        AgentRate = AgentRateapi,
                        ApiStatus = apistatus,
                        ExtraFields = new List<string> { "", "" }
                    };
                }
            }
            catch (Exception exr)
            {
              await SaveErrorLogAsync("Error In DCBank Api Transaction -"+ exr.ToString(),DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                return new ProceedResponseViewModel
                {
                    Status = "Failed",
                    StatusCode = 2,
                    Message = "Failed to Create Wallet Transaction.",
                    ApiId = api_id,
                    AgentRate = AgentRateapi,
                    ApiStatus = apistatus,
                    ExtraFields = new List<string> { "", "" }
                };
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
            await SaveActivityLogTracker("CreateNGNWallet -- Start : <br/>", 0, DateTime.Now, 0, "0", entity.Customer_ID, 0, "CreateNGNWallet", 2, 1);
            CreateNGNWallet obj = new CreateNGNWallet { };
            obj = entity;
            int? api_id = 0;// entity.BranchListAPI_ID;
            int? Customer_ID = 0; int bankAccountId = 0;
            //int? Client_ID = entity.Client_ID;
            int ? Transaction_ID = 0;// entity.Transaction_ID;
            string referenceNumber = "", phoneNumber="", firstName="", lastName="", accountName="", accountReference="", email="";
            Customer_ID = obj.Customer_ID;
            string apiurl = "", accesscode = "";
            string? SecurityKey = _appSettings.SecurityKey;
            string api_Fields = "";
            try
            {
                try
                {
                    await SaveActivityLogTracker("GetCustomerBankAccountsByCurrency -- Start : <br/>", 0, DateTime.Now, 0, "0", entity.Customer_ID, 0, "CreateNGNWallet", 2, 1);
                    int CustApiWalletAccountExist = 0;

                    var storedProcedureName = "GetCustomerBankAccountsByCurrency";
                    var custparameters = new
                    {
                        _Customer_ID =Convert.ToString(entity.Customer_ID),
                        _Currency_ID = Convert.ToString(entity.Currency_Id)
                    };

                    var bankAccounts = await _dbConnection.QueryAsync(storedProcedureName, custparameters, commandType: CommandType.StoredProcedure);

                    dynamic CustWalletBank_Details = bankAccounts.FirstOrDefault();
                    await SaveActivityLogTracker("GetCustomerBankAccountsByCurrency -- Successfull : <br/>", 0, DateTime.Now, 0, "0", entity.Customer_ID, 0, "CreateNGNWallet", 2, 1);
                    string SysEmail_ID = "";
                    string SysWireTransfer_ReferanceNo = "";
                    string SysMobile_Number = "";
                    if (CustWalletBank_Details != null)
                    {
                         SysEmail_ID = CustWalletBank_Details.Email_ID?.ToString();
                         SysWireTransfer_ReferanceNo = CustWalletBank_Details.WireTransfer_ReferanceNo?.ToString();
                         SysMobile_Number = CustWalletBank_Details.Mobile_Number?.ToString();

                        string clouse = "";

                        // ✅ Email Validation
                        if (string.IsNullOrEmpty(SysEmail_ID))
                        {
                            return new CreateNGNWalletResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = "Customer Email Id is required",
                                ApiId = 0,
                                ExtraFields = new List<string> { "", "" }
                            };
                        }

                        clouse = "Email_ID='" + SysEmail_ID + "'";
                        if (GetApiWalletBankAccountDetails(SecurityKey, clouse))
                        {
                            return new CreateNGNWalletResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = "Customer Email Id is already exist",
                                ApiId = 0,
                                ExtraFields = new List<string> { "", "" }
                            };
                        }


                        // ✅ Reference No Validation
                        if (string.IsNullOrEmpty(SysWireTransfer_ReferanceNo))
                        {
                            return new CreateNGNWalletResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = "Customer Reference No is required",
                                ApiId = 0,
                                ExtraFields = new List<string> { "", "" }
                            };
                        }

                        clouse = "WireTransfer_ReferanceNo='" + SysWireTransfer_ReferanceNo + "'";
                        if (GetApiWalletBankAccountDetails(SecurityKey, clouse))
                        {
                            return new CreateNGNWalletResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = "Customer Reference No is already exist",
                                ApiId = 0,
                                ExtraFields = new List<string> { "", "" }
                            };
                        }


                        // ✅ Mobile Validation
                        if (string.IsNullOrEmpty(SysMobile_Number))
                        {
                            return new CreateNGNWalletResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = "Customer Mobile Number is required",
                                ApiId = 0,
                                ExtraFields = new List<string> { "", "" }
                            };
                        }

                        clouse = "Mobile_Number='" + SysMobile_Number + "'";
                        if (GetApiWalletBankAccountDetails(SecurityKey, clouse))
                        {
                            return new CreateNGNWalletResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = "Customer Mobile Number is already exist",
                                ApiId = 0,
                                ExtraFields = new List<string> { "", "" }
                            };
                        }

                        // ✅ BVN Validation
                        if (string.IsNullOrEmpty(obj.BankVerificationNumber))
                        {
                            return new CreateNGNWalletResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = "Customer BVN Number is required",
                                ApiId = 0,
                                ExtraFields = new List<string> { "", "" }
                            };
                        }

                        clouse = "BVN='" + obj.BankVerificationNumber + "'";
                        if (GetApiWalletBankAccountDetails(SecurityKey, clouse))
                        {
                            return new CreateNGNWalletResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = "Customer BVN Number is already exist",
                                ApiId = 0,
                                ExtraFields = new List<string> { "", "" }
                            };
                        }
                        CustApiWalletAccountExist = 1; 
                    }
                    else
                    {
                        dynamic CustWalletBank_Details1 = null;
                        try
                        {
                            storedProcedureName = "GetCustDetailsByID";
                            var CustDetailsvalues = new
                            {
                                cust_ID = entity.Customer_ID
                            };
                            var dtwalletCustomerdetails = await _dbConnection.QueryAsync(storedProcedureName, CustDetailsvalues, commandType: CommandType.StoredProcedure);
                             CustWalletBank_Details1 = dtwalletCustomerdetails.FirstOrDefault();
                        }
                        catch (Exception ex)
                        {
                            await SaveErrorLogAsync(" Error In GetCustDetailsByID SP Create  Wallet -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, obj.Client_ID, 0);
                            return new CreateNGNWalletResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = "Failed to Create ApiWallet",
                                ApiId = 0,
                                ExtraFields = new List<string> { "", "" }
                            };
                        }
                        if (CustWalletBank_Details1 != null)
                        {
                            SysEmail_ID = CustWalletBank_Details1.Email_ID?.ToString();
                            SysWireTransfer_ReferanceNo = CustWalletBank_Details1.WireTransfer_ReferanceNo?.ToString();
                            SysMobile_Number = CustWalletBank_Details1.Mobile_Number?.ToString();

                            string clouse = "";

                            // ✅ Email Validation
                            if (string.IsNullOrEmpty(SysEmail_ID))
                            {
                                return new CreateNGNWalletResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = "Customer Email Id is required",
                                    ApiId = 0,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }

                            clouse = "Email_ID='" + SysEmail_ID + "'";
                            if (GetApiWalletBankAccountDetails(SecurityKey, clouse))
                            {
                                return new CreateNGNWalletResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = "Customer Email Id is already exist",
                                    ApiId = 0,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }


                            // ✅ Reference No Validation
                            if (string.IsNullOrEmpty(SysWireTransfer_ReferanceNo))
                            {
                                return new CreateNGNWalletResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = "Customer Reference No is required",
                                    ApiId = 0,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }

                            clouse = "WireTransfer_ReferanceNo='" + SysWireTransfer_ReferanceNo + "'";
                            if (GetApiWalletBankAccountDetails(SecurityKey, clouse))
                            {
                                return new CreateNGNWalletResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = "Customer Reference No is already exist",
                                    ApiId = 0,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }


                            // ✅ Mobile Validation
                            if (string.IsNullOrEmpty(SysMobile_Number))
                            {
                                return new CreateNGNWalletResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = "Customer Mobile Number is required",
                                    ApiId = 0,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }

                            clouse = "Mobile_Number='" + SysMobile_Number + "'";
                            if (GetApiWalletBankAccountDetails(SecurityKey, clouse))
                            {
                                return new CreateNGNWalletResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = "Customer Mobile Number is already exist",
                                    ApiId = 0,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }

                            // ✅ BVN Validation
                            if (string.IsNullOrEmpty(obj.BankVerificationNumber))
                            {
                                return new CreateNGNWalletResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = "Customer BVN Number is required",
                                    ApiId = 0,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }

                            clouse = "BVN='" + obj.BankVerificationNumber + "'";
                            if (GetApiWalletBankAccountDetails(SecurityKey, clouse))
                            {
                                return new CreateNGNWalletResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = "Customer BVN Number is already exist",
                                    ApiId = 0,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }
                            CustApiWalletAccountExist = 0;
                        }
                        else 
                        {
                            return new CreateNGNWalletResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = "Customer details missing",
                                ApiId = 0,
                                ExtraFields = new List<string> { "", "" }
                            };
                        }
                    }
                    if (CustApiWalletAccountExist == 0)
                    {

                        await SaveActivityLogTracker("Get_bank_account_provider_api_details -- Start : <br/>", 0, DateTime.Now, 0, "0", entity.Customer_ID, 0, "CreateNGNWallet", 2, 1);

                        dynamic bankAccountProviderDetails = null;
                        try
                        {
                            storedProcedureName = "Get_bank_account_provider_api_details";

                            var apiparameters = new
                            {
                                _Client_ID = entity.Client_ID,
                                _status = 0,
                                _security_key = SecurityKey
                            };

                            var apiDetails = await _dbConnection.QueryAsync(
                                storedProcedureName,
                                apiparameters,
                                commandType: CommandType.StoredProcedure
                            );

                             bankAccountProviderDetails = apiDetails.FirstOrDefault();
                        }
                        catch (Exception ex)
                        {
                            await SaveErrorLogAsync(" Error In Get_bank_account_provider_api_details SP Create  Wallet -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, obj.Client_ID, 0);
                            return new CreateNGNWalletResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = "Failed to Create ApiWallet",
                                ApiId = 0,
                                ExtraFields = new List<string> { "", "" }
                            };
                        }
                        int apiid = Convert.ToInt32(bankAccountProviderDetails.API_ID);

                        await SaveActivityLogTracker("Get_bank_account_provider_api_details -- Successfull with api id : <br/>" + apiid, 0, DateTime.Now, 0, "0", entity.Customer_ID, 0, "CreateNGNWallet", 2, 1);

                        string bankId = "";
                        apiurl = bankAccountProviderDetails.API_URL.ToString();
                        int APIID = bankAccountProviderDetails.API_ID;
                        accesscode = bankAccountProviderDetails.UserName.ToString();
                        string Password = bankAccountProviderDetails.Password.ToString();

                        await SaveActivityLogTracker("GetCustDetailsByID -- Start : <br/>", 0, DateTime.Now, 0, "0", entity.Customer_ID, 0, "CreateNGNWallet", 2, 1);
                        dynamic dtcd = null;
                        try
                        {
                            storedProcedureName = "GetCustDetailsByID";
                            var CustDetailsvalues = new
                            {
                                cust_ID = entity.Customer_ID
                            };
                            var dtwalletCustomerdetails = await _dbConnection.QueryAsync(storedProcedureName, CustDetailsvalues, commandType: CommandType.StoredProcedure);
                            dtcd = dtwalletCustomerdetails.FirstOrDefault();
                        }
                        catch (Exception ex)
                        {
                            await SaveErrorLogAsync(" Error In GetCustDetailsByID SP Create  Wallet -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, obj.Client_ID, 0);
                            return new CreateNGNWalletResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = "Failed to Create ApiWallet",
                                ApiId = APIID,
                                ExtraFields = new List<string> { "", "" }
                            };
                        }
                        await SaveActivityLogTracker("GetCustDetailsByID -- Successfull ", 0, DateTime.Now, 0, "0", entity.Customer_ID, 0, "CreateNGNWallet", 2, 1);


                        referenceNumber = Convert.ToString(dtcd.WireTransfer_ReferanceNo);
                        phoneNumber = Convert.ToString(dtcd.Mobile_Number);
                        obj.User_ID = Convert.ToInt32(dtcd.User_ID);
                        firstName = Convert.ToString(dtcd.First_Name);
                        lastName = Convert.ToString(dtcd.Last_Name);
                        accountName = Convert.ToString(dtcd.Full_name);
                        accountReference = Convert.ToString(dtcd.WireTransfer_ReferanceNo);
                        if (!string.IsNullOrEmpty(phoneNumber))
                        {
                            if (!phoneNumber.StartsWith("0"))
                            {
                                phoneNumber = "0" + phoneNumber;
                            }
                        }
                        else { phoneNumber = "0"; }

                        if (APIID == 3)
                        {
                            await SaveActivityLogTracker("BETA-Paga registerPersistentPaymentAccount -- Start account Create: <br/>" , 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.Customer_ID, Convert.ToInt32(Customer_ID), "CreateNGNWallet", 2, 1);

                            string urll = apiurl; //""; paga
                            try
                            {
                                 api_Fields = bankAccountProviderDetails.API_Fields.ToString();
                            }
                            catch (Exception ex)
                            {
                                await SaveActivityLogTracker("Error in BETA-Paga api_Fields details: <br/>" + ex.ToString(), 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.Customer_ID, Convert.ToInt32(Customer_ID), "CreateNGNWallet", 2, 1);
                            }

                            string Hmac = "", callBackUrl = "";
                            //api_Fields = transactiondetail.api_Fields.ToString();
                            if (api_Fields != "" && api_Fields != null)
                            {
                                Newtonsoft.Json.Linq.JObject objAPI = Newtonsoft.Json.Linq.JObject.Parse(api_Fields);

                                Hmac = objAPI["HMAC"]?.ToString();
                                callBackUrl = objAPI["callBackUrl"]?.ToString();
                                bankId = objAPI["bankId"]?.ToString();
                            }

                            dynamic CustDetails = null;
                            try
                            {
                                storedProcedureName = "GetCustDetailsByID";
                                var CustDetailsvalues = new
                                {
                                    cust_ID = entity.Customer_ID
                                };
                                var dttCustDetails = await _dbConnection.QueryAsync(storedProcedureName, CustDetailsvalues, commandType: CommandType.StoredProcedure);
                                CustDetails = dttCustDetails.FirstOrDefault();
                            }
                            catch (Exception ex)
                            {
                                await SaveErrorLogAsync(" Error In GetCustDetailsByID SP Create  Wallet -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, obj.Client_ID, 0);
                                return new CreateNGNWalletResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = "Failed to Create ApiWallet",
                                    ApiId = APIID,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }

                            string credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{accesscode}:{Password}"));

                            referenceNumber = CustDetails.WireTransfer_ReferanceNo.ToString();
                            firstName = CustDetails.First_Name.ToString();
                             email = CustDetails.Email_ID.ToString();
                            lastName = CustDetails.Last_Name.ToString();
                            accountName = CustDetails.Full_name.ToString();
                            accountReference = CustDetails.WireTransfer_ReferanceNo.ToString();

                            accountReference = accountReference + "-" + accountReference;
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
            ""email"": """+ email + @""",
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
                            if (statusCode =="121")
                            {
                                string statusMessage = responseObject["statusMessage"]?.ToString();
                                return new CreateNGNWalletResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = statusMessage,
                                    ApiId = api_id,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }
                            else if(statusCode == "102")
                            {
                                string statusMessage = responseObject["statusMessage"]?.ToString();
                                return new CreateNGNWalletResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = statusMessage,
                                    ApiId = api_id,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }
                            else if (statusCode == "0")
                            {
                                string statusMessage = responseObject["statusMessage"]?.ToString();
                                string accountNumber = responseObject["accountNumber"]?.ToString();
                                string bankName = responseObject["bankName"]?.ToString();

                                storedProcedureName = "insert_WalletBankAccountDetails"; // insert_WalletBankAccountDetails
                                var parameters = new DynamicParameters();
                                parameters.Add("_Customer_ID", entity.Customer_ID);
                                parameters.Add("_Company_ID", APIID);
                                parameters.Add("_Currency_ID", 43);
                                parameters.Add("_Base_Currency_ID", 0);
                                parameters.Add("_Opening_Date", DateTime.Now);
                                parameters.Add("_Bank_Name", bankName);
                                parameters.Add("_Account_Number", accountNumber);
                                parameters.Add("_Account_Holder_Name", accountName);
                                parameters.Add("_API_ID", APIID);
                                parameters.Add("_Client_ID", entity.Client_ID);
                                parameters.Add("_Delete_Status", 0);
                                parameters.Add("_referenceID", referenceNumber);
                                parameters.Add("_bankholderID", accountReference);
                                parameters.Add("_Record_Insert_DateTime", DateTime.Now);
                                parameters.Add("_isWallet", 0);

                                // new parameters save in wallet details page.
                                parameters.Add("_Email_ID", email);
                                parameters.Add("_WireTransfer_ReferanceNo", accountReference);
                                parameters.Add("_Mobile_Number", phoneNumber);
                                parameters.Add("_BVN", obj.BankVerificationNumber);

                                parameters.Add("_Bank_Account_ID", dbType: DbType.Int32, direction: ParameterDirection.Output);

                                #region save bvn against of this customer
                                try
                                {
                                    var parametersbvn = new
                                    {
                                        p_Customer_BVN = obj.BankVerificationNumber,
                                        p_Customer_Id = Customer_ID
                                    };

                                        var rowsAffected = await _dbConnection.ExecuteAsync("Update_Customer_BVN", parametersbvn, commandType: CommandType.StoredProcedure);
                                    }
                                    catch (Exception ex)
                                    {
                                        await SaveErrorLogAsync("Error In Update_Customer_BVN  - " + ex.ToString(), DateTime.Now, "CreateNGNWallet", obj.User_ID, obj.Branch_ID, obj.Client_ID, 0);
                                    }
                                    #endregion save bvn against of this customer

                                try
                                {
                                    await _dbConnection.ExecuteAsync(
                                        storedProcedureName,
                                        parameters,
                                        commandType: CommandType.StoredProcedure
                                    );

                                    bankAccountId = parameters.Get<int>("_Bank_Account_ID");
                                   

                                    await SaveActivityLogTracker("insert_WalletBankAccountDetails -- Successful. BankAccountID: " + bankAccountId, 0, DateTime.Now, 0, "0", entity.Customer_ID, 0, "CreateNGNWallet", 2, 1);
                                }
                                catch (Exception ex)
                                {
                                    await SaveErrorLogAsync("Error In SaveWalletBankAccountDetails SP Create Wallet - " + ex.ToString(), DateTime.Now, "CreateNGNWallet", obj.User_ID, obj.Branch_ID, obj.Client_ID, 0);
                                    return new CreateNGNWalletResponseViewModel
                                    {
                                        Status = "Failed",
                                        StatusCode = 2,
                                        Message = "Failed to Create ApiWallet",
                                        ApiId = APIID,
                                        ExtraFields = new List<string> { "", "" }
                                    };
                                }

                                try
                                {
                                    // CreateWalletInOurSideAsync -- here in this method we create wallet and also set the default limits and conversion limits
                                    string status = await CreateWalletInOurSideAsync(obj, referenceNumber, 0, Convert.ToInt32(entity.Customer_ID), APIID, bankAccountId);
                                    await SaveActivityLogTracker("BETA-Paga registerPersistentPaymentAccount request for account Create status: <br/>" + status + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.Customer_ID, Convert.ToInt32(Customer_ID), "CreateNGNWallet", 2, 1);

                                    #region set limits
                                    //storedProcedureName = "Add_default_limit_ApiWalletbank";
                                    //var parameters1 = new DynamicParameters();
                                    //parameters1.Add("_Customer_id", Convert.ToInt32(entity.Customer_ID));
                                    //parameters1.Add("_basecurrency_id", 43);
                                    //parameters1.Add("_client_id", entity.Client_ID);
                                    //parameters1.Add("_RecordDate", DateTime.Now);
                                    //parameters1.Add("_branch_id", entity.Branch_ID);
                                    //int msg1limit = await _dbConnection.ExecuteAsync(
                                    //    storedProcedureName,
                                    //    parameters1,
                                    //    commandType: CommandType.StoredProcedure
                                    //);

                                    //try
                                    //{

                                    //    storedProcedureName = "Add_default_limit_App";
                                    //    var parameters1lmt = new DynamicParameters();
                                    //    parameters1lmt.Add("_Customer_id", Convert.ToInt32(entity.Customer_ID));
                                    //    parameters1lmt.Add("_basecurrency_id", 43);
                                    //    parameters1lmt.Add("_client_id", entity.Client_ID);
                                    //    parameters1lmt.Add("_RecordDate", DateTime.Now);
                                    //    parameters1lmt.Add("_branch_id", entity.Branch_ID);
                                    //    int msg1limit1 = await _dbConnection.ExecuteAsync(
                                    //        storedProcedureName,
                                    //        parameters1lmt,
                                    //        commandType: CommandType.StoredProcedure
                                    //    );

                                    //    await SaveErrorLogAsync(" Add_default_limit_App  Successfully add limit :" + msg1limit1, DateTime.Now, "CreateNGNWallet", obj.User_ID, obj.Branch_ID, obj.Client_ID, 0);

                                    //}
                                    //catch (Exception ex)
                                    //{
                                    //    await SaveErrorLogAsync(" Add_default_limit_App Error:" + ex.ToString(), DateTime.Now, "CreateNGNWallet", obj.User_ID, obj.Branch_ID, obj.Client_ID, 0);
                                    //}


                                    #region set conversion rates

                                    //#region Add limits for conversion wallet
                                    //try
                                    //{
                                    //    if (wrefNo != "")
                                    //    {
                                    //        MySqlCommand cmd_limits = new MySqlCommand("Add_Limits_For_Conversion_Wallet");
                                    //        cmd_limits.CommandType = CommandType.StoredProcedure;
                                    //        cmd_limits.Parameters.AddWithValue("_Customer_ID", obj.Id);
                                    //        cmd_limits.Parameters.AddWithValue("_Wallet_ID", Wallet_Id);
                                    //        cmd_limits.Parameters.AddWithValue("_Basecurrency_id", cid);
                                    //        cmd_limits.Parameters.AddWithValue("_Client_ID", obj.Client_ID);
                                    //        cmd_limits.Parameters.AddWithValue("_Branch_ID", obj.Branch_ID);
                                    //        cmd_limits.Parameters.AddWithValue("_RecordDate", obj.RecordDate.ToString("yyyy-MM-dd HH:mm:ss"));
                                    //        int res_limits = db_connection.ExecuteNonQueryProcedure(cmd_limits);
                                    //        if (res_limits > 0)
                                    //        {
                                    //            CompanyInfo.InsertActivityLogDetails("Conversion limit added successfully.", Convert.ToInt32(obj.User_ID), 0, Convert.ToInt32(obj.User_ID), 0, "Service.srvCustomer-Create", Convert.ToInt32(obj.Branch_ID), Convert.ToInt32(obj.Client_ID), "CreateCustomer", context);
                                    //        }
                                    //    }
                                    //}
                                    //catch (Exception ex)
                                    //{
                                    //    CompanyInfo.InsertErrorLogTracker("Error in Adding limits for conversion wallet during customer creation: " + ex.ToString(), 0, 0, 0, 0, "Service.srvCustomer-Create", Convert.ToInt32(obj.Branch_ID), Convert.ToInt32(obj.Client_ID), "", context);

                                    //}
                                    //#endregion


                                    #endregion  set conversion rates

                                    #endregion set limits

                                    return new CreateNGNWalletResponseViewModel
                                    {
                                        Status = "Success",
                                        StatusCode = 0,
                                        Message = "Wallet created successfully.",
                                        ApiId = api_id,
                                        ExtraFields = new List<string> { "", "" }
                                    };
                                    
                                }
                                catch (Exception ex)
                                {
                                    await SaveErrorLogAsync(" Error In CreateWalletInOurSideAsync & Add_default_limit_ApiWalletbank  SP Create  Wallet -" + ex.ToString(), DateTime.Now, "CreateNGNWallet", obj.User_ID, obj.Branch_ID, obj.Client_ID, 0);
                                    return new CreateNGNWalletResponseViewModel
                                    {
                                        Status = "Failed",
                                        StatusCode = 2,
                                        Message = "Failed to Create ApiWallet",
                                        ApiId = APIID,
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
                        else if (APIID == 4)
                        {

                            //accesscode = Convert.ToString(dtCDapi.UserName);
                            //Password = Convert.ToString(dtCDapi.Password);
                            string urll = apiurl; //"https://payment.xpress-wallet.com";
                            //try
                            //{

                            //    storedProcedureName = "Get_wallet_API_Details";
                            //    var walletapidetails = new
                            //    {
                            //        p_api_ID = APIID
                            //    };
                            //    var dtwalletapidetails = await _dbConnection.QueryAsync(storedProcedureName, walletapidetails, commandType: CommandType.StoredProcedure);
                            //    dynamic dtCDapi = dtwalletapidetails.FirstOrDefault();
                            //    if (dtCDapi != null)
                            //    {
                            //        accesscode = Convert.ToString(dtCDapi.UserName);
                            //        Password = Convert.ToString(dtCDapi.Password);
                            //    }
                            //}
                            //catch (Exception ex)
                            //{

                            //}


                            email = accesscode;//"aW5mb0BzdXBlcnRyYW5zZmVyLmNvLnVr";
                            string password = Password;//"RklLS3k0Y2FyZDEjQA==";
                            string Providus_Customer_ID = "";
                            string Transaction_Reference = "";

                            

                            string Wallet_ID = "";
                            var secretKey = ""; // Replace with your secret key
                            var accessToken = "";
                            var refreshToken = "";

                            if (obj.BankVerificationNumber != "")
                            {

                                // Login
                                try
                                {
                                    ServicePointManager.Expect100Continue = true;
                                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                           | SecurityProtocolType.Tls11
                                           | SecurityProtocolType.Tls12;
                                    var options = new RestClientOptions(urll + "/api/v1/auth/login")
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
                                    var body1 = @"{" + "\n" +
                         @"    ""email"": """ + email + @""",
" + "\n" +
                         @"    ""password"": """ + password + @""""
                         + "\n" +
                         @"}";
                                    request.AddParameter("application/json", body1, ParameterType.RequestBody);
                                    await SaveActivityLogTracker("Providus Wallet login api Request Insert Transaction -" + urll + "/api/v1/auth/login", 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", obj.Branch_ID, obj.Client_ID);

                                    RestResponse response = client.Execute(request);

                                    await SaveActivityLogTracker("Providus Wallet login api Response Insert Transaction - " + response.Content, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", obj.Branch_ID, obj.Client_ID);


                                    accessToken = response.Headers.FirstOrDefault(h => h.Name == "X-Access-Token")?.Value?.ToString();

                                    refreshToken = response.Headers.FirstOrDefault(h => h.Name == "X-Refresh-Token")?.Value?.ToString();
                                    await SaveActivityLogTracker("Providus Wallet login api accessToken and refreshToken Insert Transaction - " + accessToken + " |refreshToken -" + refreshToken, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", obj.Branch_ID, obj.Client_ID);

                                }
                                catch (Exception ex)
                                {
                                    await SaveErrorLogAsync(" Error In Providus Wallet login api Insert Transaction  -" + ex.ToString(), DateTime.Now, "Proceed", obj.User_ID, obj.Branch_ID, obj.Client_ID, 0);
                                    return new CreateNGNWalletResponseViewModel
                                    {
                                        Status = "Failed",
                                        StatusCode = 2,
                                        Message = "Failed to Create ApiWallet",
                                        ApiId = api_id,
                                        ExtraFields = new List<string> { "", "" }
                                    };
                                }

                                if (accessToken != "" && refreshToken != "")
                                {
                                    // Generate Secret Key
                                    try
                                    {
                                        var options = new RestClientOptions(urll + "/api/v1/merchant/generate-access-keys")
                                        {
                                            MaxTimeout = -1
                                        };
                                        var client = new RestClient(options);
                                        var request = new RestRequest()
                                        {
                                            Method = Method.Post
                                        };

                                        request.AddHeader("Content-Type", "application/json");
                                        request.AddHeader("X-Access-Token", accessToken);
                                        request.AddHeader("X-Refresh-Token", refreshToken);

                                        await SaveActivityLogTracker("Providus Wallet Generate Secret Key Request Insert Transaction - " + urll + "/api/v1/merchant/generate-access-keys", 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", obj.Branch_ID, obj.Client_ID);

                                        RestResponse response = client.Execute(request);

                                        await SaveActivityLogTracker("Providus Wallet Generate Secret Key Response Insert Transaction - " + response.Content, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", obj.Branch_ID, obj.Client_ID);

                                        dynamic dynJsonn = JsonConvert.DeserializeObject(response.Content);

                                        bool status = (bool)dynJsonn.status;
                                        if (status)
                                        {
                                            string publicKey = (string)dynJsonn.data.publicKey;
                                            secretKey = (string)dynJsonn.data.privateKey;
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
                                    catch (Exception ex)
                                    {
                                        await SaveErrorLogAsync(" Error In Providus Wallet Generate Secret Key Insert Transaction -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, obj.Client_ID, 0);
                                        return new CreateNGNWalletResponseViewModel
                                        {
                                            Status = "Failed",
                                            StatusCode = 2,
                                            Message = "Failed to Create ApiWallet",
                                            ApiId = api_id,
                                            ExtraFields = new List<string> { "", "" }
                                        };
                                    }

                                    if (secretKey != "")
                                    {
                                        //Create Wallet and customer
                                        try
                                        {
                                            string bankverificationnum = obj.BankVerificationNumber; //"22181111633";
                                            DateTime dob = Convert.ToDateTime(dtcd.DateOf_Birth);
                                            string dateOfBirth = dob.ToString("yyyy-MM-dd");
                                            //phoneNumber = "07712711168";
                                            string email1 = Convert.ToString(dtcd.Email_ID);//"test@gmail.com";

                                            string custref = referenceNumber;
                                            string City_Name = "";
                                            string House_Number = "";
                                            string Street = "";
                                            string Country_Name = "";
                                            string Post_Code = "";
                                            string BankName = "";
                                            string AccountName = "";
                                            string AccountNumber = "";
                                            string AvailableBalance = "";
                                            string BookedBalance = "";
                                            try { City_Name = Convert.ToString(dtcd.City_Name); } catch { }
                                            try { House_Number = Convert.ToString(dtcd.House_Number); } catch { }
                                            try { Street = Convert.ToString(dtcd.Street); } catch { }
                                            try { Country_Name = Convert.ToString(dtcd.Country_Name); } catch { }
                                            try { Post_Code = Convert.ToString(dtcd.Post_Code); } catch { }

                                            string address = House_Number + ", " + Street + ", " + City_Name + ", " + Country_Name + ", " + Post_Code;//"No 10, Adewale Ajasin University";

                                            var options = new RestClientOptions(urll + "/api/v1/wallet")
                                            {
                                                MaxTimeout = -1
                                            };
                                            var client = new RestClient(options);
                                            var request = new RestRequest()
                                            {
                                                Method = Method.Post
                                            };

                                            request.AddHeader("Content-Type", "application/json");
                                            request.AddHeader("Authorization", "Bearer " + secretKey);
                                            var body = @"{" + "\n" +
                            @"    ""bvn"": """ + bankverificationnum + @"""," + "\n" +
                            @"    ""firstName"": """ + firstName + @"""," + "\n" +
                            @"    ""lastName"": """ + lastName + @"""," + "\n" +
                            @"    ""dateOfBirth"": """ + dateOfBirth + @"""," + "\n" +
                            @"    ""phoneNumber"": """ + phoneNumber + @"""," + "\n" +
                            @"    ""email"": """ + email1 + @"""," + "\n" +
                            @"    ""address"": """ + address + @"""," + "\n" +
                            @"    ""metadata"": {" + "\n" +
                            @"        ""even-more"": ""Other data""," + "\n" +
                            @"        ""additional-data"": """ + custref + @"""" + "\n" +
                            @"    }" + "\n" +
                            @"}";
                                            request.AddParameter("application/json", body, ParameterType.RequestBody);
                                            await SaveActivityLogTracker("Providus Wallet Create Customer Wallet Request Insert Transaction - " + urll + "/api/v1/wallet" + body, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", obj.Branch_ID, obj.Client_ID);

                                            var response = client.Execute(request);
                                            await SaveActivityLogTracker("Providus Wallet Create Customer Wallet Response Insert Transaction - " + response.Content, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", obj.Branch_ID, obj.Client_ID);

                                            try
                                            {
                                                JObject jsonResponse = JObject.Parse(response.Content);

                                                // Step 2: Check "status" field
                                                bool apiStatus = jsonResponse["status"]?.Value<bool>() ?? false;

                                                if (!apiStatus)
                                                {
                                                    string message = jsonResponse.Value<string>("message") ?? "";

                                                    // Get first error object (if exists)
                                                    JArray errors = (JArray)jsonResponse["errors"];
                                                    if (errors != null && errors.Count > 0)
                                                    {
                                                        string path = errors[0]["path"]?.ToString() ?? "";
                                                        string errorMessage = errors[0]["message"]?.ToString() ?? "";
                                                        message = errorMessage;
                                                        await SaveActivityLogTracker("Response of Providus Wallet Create Customer Wallet: " + path + " , " + errorMessage, 0, DateTime.Now, 0, "0", entity.Customer_ID, 0, "CreateNGNWallet", 2, 1);
                                                    }
                                                    else { message = "Failed to Create ApiWallet"; }

                                                    if (message.IndexOf("phone", StringComparison.OrdinalIgnoreCase) >= 0)
                                                    {
                                                        message = System.Text.RegularExpressions.Regex.Replace(message, "phone", "mobile", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                                    }
                                                    return new CreateNGNWalletResponseViewModel
                                                    {
                                                        Status = "Failed",
                                                        StatusCode = 2,
                                                        Message = message,
                                                        ApiId = APIID,
                                                        ExtraFields = new List<string> { "", "" }
                                                    };
                                                }

                                                // Step 3: Extract Customer fields
                                                var customer = jsonResponse["customer"];
                                                if (customer != null)
                                                {
                                                    Providus_Customer_ID = customer["id"]?.ToString();
                                                    string FirstName = customer["firstName"]?.ToString();
                                                    string LastName = customer["lastName"]?.ToString();
                                                    string Email = customer["email"]?.ToString();
                                                    string PhoneNumber = customer["phoneNumber"]?.ToString();
                                                    string DOB = customer["dateOfBirth"]?.ToString();
                                                    string Currency = customer["currency"]?.ToString();
                                                    string Address = customer["address"]?.ToString();
                                                    string BVN = customer["bvn"]?.ToString();
                                                    string Tier = customer["tier"]?.ToString();
                                                }

                                                // Step 4: Extract Wallet fields
                                                var wallet = jsonResponse["wallet"];
                                                if (wallet != null)
                                                {
                                                    Wallet_ID = wallet["id"]?.ToString();
                                                    string Wallet_Status = wallet["status"]?.ToString();
                                                    BankName = wallet["bankName"]?.ToString();
                                                    AccountName = wallet["accountName"]?.ToString();
                                                    AccountNumber = wallet["accountNumber"]?.ToString();
                                                    AvailableBalance = wallet["availableBalance"]?.ToString();
                                                    BookedBalance = wallet["bookedBalance"]?.ToString();
                                                }

                                                storedProcedureName = "insert_WalletBankAccountDetails"; // insert_WalletBankAccountDetails
                                                var parameters = new DynamicParameters();
                                                parameters.Add("_Customer_ID", entity.Customer_ID);
                                                parameters.Add("_Company_ID", APIID);
                                                parameters.Add("_Currency_ID", obj.Currency_Id);
                                                parameters.Add("_Base_Currency_ID", 0);
                                                parameters.Add("_Opening_Date", DateTime.Now);
                                                parameters.Add("_Bank_Name", BankName);
                                                parameters.Add("_Account_Number", AccountNumber);
                                                parameters.Add("_Account_Holder_Name", accountName);
                                                parameters.Add("_API_ID", APIID);
                                                parameters.Add("_Client_ID", entity.Client_ID);
                                                parameters.Add("_Delete_Status", 0);
                                                parameters.Add("_referenceID", Wallet_ID);
                                                parameters.Add("_bankholderID", Providus_Customer_ID);
                                                parameters.Add("_Record_Insert_DateTime", DateTime.Now);
                                                parameters.Add("_isWallet", 0);

                                                // new parameters save in wallet details page.
                                                parameters.Add("_Email_ID", email1);
                                                parameters.Add("_WireTransfer_ReferanceNo", custref);
                                                parameters.Add("_Mobile_Number", phoneNumber);
                                                parameters.Add("_BVN", obj.BankVerificationNumber);

                                                parameters.Add("_Bank_Account_ID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                                               
                                                #region save bvn against of this customer
                                                try
                                                {
                                                    var parametersbvn = new
                                                    {
                                                        p_Customer_BVN = obj.BankVerificationNumber,
                                                        p_Customer_Id = Customer_ID
                                                    };

                                                    var rowsAffected = await _dbConnection.ExecuteAsync("Update_Customer_BVN", parametersbvn, commandType: CommandType.StoredProcedure);
                                                }
                                                catch (Exception ex)
                                                {
                                                    await SaveErrorLogAsync("Error In Update_Customer_BVN  - " + ex.ToString(), DateTime.Now, "CreateNGNWallet", obj.User_ID, obj.Branch_ID, obj.Client_ID, 0);
                                                }
                                                #endregion save bvn against of this customer
                                                try
                                                {
                                                    await _dbConnection.ExecuteAsync(
                                                        storedProcedureName,
                                                        parameters,
                                                        commandType: CommandType.StoredProcedure
                                                    );

                                                     bankAccountId = parameters.Get<int>("_Bank_Account_ID");
                                                    await SaveActivityLogTracker("insert_WalletBankAccountDetails -- Successful. BankAccountID: " + bankAccountId, 0, DateTime.Now, 0, "0", entity.Customer_ID, 0, "CreateNGNWallet", 2, 1);
                                                }
                                                catch (Exception ex)
                                                {
                                                    await SaveErrorLogAsync("Error In SaveWalletBankAccountDetails SP Create Wallet - " + ex.ToString(), DateTime.Now, "CreateNGNWallet", obj.User_ID, obj.Branch_ID, obj.Client_ID, 0);
                                                    return new CreateNGNWalletResponseViewModel
                                                    {
                                                        Status = "Failed",
                                                        StatusCode = 2,
                                                        Message = "Failed to Create ApiWallet",
                                                        ApiId = APIID,
                                                        ExtraFields = new List<string> { "", "" }
                                                    };
                                                }

                                                try
                                                {
                                                    //Walllet = CreateWalletOnWallateTbl(43, Customer_ID);
                                                    string walletStatus = "";

                                                    int walletExist = 0;
                                                    try
                                                    {
                                                        #region check Wallet Existance
                                                        //Currency_ID


                                                        //storedProcedureName = "getwalletid";
                                                        //var walletapidetails = new
                                                        //{
                                                        //    _WireTransfer_ReferanceNo = referenceNumber,
                                                        //    _Currency_ID = obj.Currency_Id,
                                                        //    _Client_ID = obj.Client_ID
                                                        //};
                                                        //var dtwalletapidetails = await _dbConnection.QueryAsync(storedProcedureName, walletapidetails, commandType: CommandType.StoredProcedure);
                                                        //dynamic dt_custWal = dtwalletapidetails.FirstOrDefault();
                                                        //if (dt_custWal != null)
                                                        //{
                                                        //    Wallet_ID = Convert.ToString(dt_custWal.Rows[0]["Wallet_ID"]);
                                                        //    walletExist = 1;
                                                        //}
                                                        //else { walletExist = 0; }
                                                        #endregion
                                                    }
                                                    catch (Exception ex2)
                                                    {
                                                        //CompanyInfo.InsertActivityLogDetails("In check Wallet Existance" + ex2.ToString(), 0, 0, 0, 0, "", 1, 1, "CustomerAllDetails", context);
                                                    }
                                                    if (walletExist == 0)
                                                    {
                                                        try
                                                        {
                                                            ////dynamic msg1limit = dtwalletlimit_ApiWalletbank.FirstOrDefault();
                                                            //var rowsAffected = await _dbConnection.ExecuteAsync(storedProcedureName, walletapidetails, commandType: CommandType.StoredProcedure);
                                                            string status = await CreateWalletInOurSideAsync(obj, referenceNumber, 0, Convert.ToInt32(entity.Customer_ID), APIID, bankAccountId);
                                                            walletStatus = status;
                                                            //walletStatus = "Created";
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            await SaveErrorLogAsync(" Error In Create Providus Wallet insertinwallet_table-" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, obj.Client_ID, 0);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        walletStatus = "AlreadyCreated";
                                                    }

                                                    if (walletStatus == "Created")
                                                    {
                                                        #region set limit
                                                        //await SaveActivityLogTracker("Providus Wallet Add_default_limit_ApiWalletbank  for account Created and set limit  - ", 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, obj.Client_ID);

                                                        //storedProcedureName = "Add_default_limit_ApiWalletbank";
                                                        //var walletapidetails = new
                                                        //{
                                                        //    _Customer_id = Customer_ID,
                                                        //    _Basecurrency_id = obj.Currency_Id,
                                                        //    _Client_id = obj.Client_ID,
                                                        //    _RecordDate = DateTime.Now,
                                                        //    _Opening_Date = DateTime.Now,
                                                        //    _branch_id = obj.Branch_ID
                                                        //};
                                                        //var rowsAffected = await _dbConnection.ExecuteAsync(storedProcedureName, walletapidetails, commandType: CommandType.StoredProcedure);

                                                        #endregion

                                                        return new CreateNGNWalletResponseViewModel
                                                        {
                                                            Status = "Success",
                                                            StatusCode = 0,
                                                            Message = "Wallet created successfully.",
                                                            ApiId = api_id,
                                                            ExtraFields = new List<string> { "", "" }
                                                        };
                                                    }
                                                    else
                                                    {
                                                        await SaveActivityLogTracker("Providus Wallet Failed to Create ApiWallet - " + walletStatus, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, obj.Client_ID);

                                                        return new CreateNGNWalletResponseViewModel
                                                        {
                                                            Status = "Failed",
                                                            StatusCode = 2,
                                                            Message = "Failed to Create Wallet",
                                                            ApiId = api_id,
                                                            ExtraFields = new List<string> { "", "" }
                                                        };
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    await SaveErrorLogAsync(" Error In Create Providus Wallet -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, obj.Client_ID, 0);

                                                    return new CreateNGNWalletResponseViewModel
                                                    {
                                                        Status = "Failed",
                                                        StatusCode = 2,
                                                        Message = "Failed to Create Wallet",
                                                        ApiId = api_id,
                                                        ExtraFields = new List<string> { "", "" }
                                                    };
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                await SaveErrorLogAsync(" Error In Create Providus Wallet -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, obj.Client_ID, 0);
                                                return new CreateNGNWalletResponseViewModel
                                                {
                                                    Status = "Failed",
                                                    StatusCode = 2,
                                                    Message = "Failed to Create Wallet",
                                                    ApiId = api_id,
                                                    ExtraFields = new List<string> { "", "" }
                                                };
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            await SaveErrorLogAsync(" Error In Create Providus Wallet -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, obj.Client_ID, 0);
                                            return new CreateNGNWalletResponseViewModel
                                            {
                                                Status = "Failed",
                                                StatusCode = 2,
                                                Message = "Failed to Create Wallet",
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
                                            Message = "Failed to Create Wallet",
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
                                        Message = "Failed to Create Wallet",
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
                                    Message = "BVN is required to create wallet",
                                    ApiId = api_id,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }

                        }
                    }
                    else
                    {
                        return new CreateNGNWalletResponseViewModel
                        {
                            Status = "Failed",
                            StatusCode = 2,
                            Message = "Wallet Already Created",
                            ApiId = api_id,
                            ExtraFields = new List<string> { "", "" }
                        };

                    }
                }
                catch (Exception ex)
                {
                    await SaveErrorLogAsync(" Error In Create  Wallet -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, obj.Client_ID, 0);
                    return new CreateNGNWalletResponseViewModel
                    {
                        Status = "Failed",
                        StatusCode = 2,
                        Message = "Failed to Create Wallet",
                        ApiId = api_id,
                        ExtraFields = new List<string> { "", "" }
                    };
                }
            }
            catch (Exception exr)
            {
                return new CreateNGNWalletResponseViewModel
                {
                    Status = "Failed",
                    StatusCode = 2,
                    Message = "Failed to Create Wallet",
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

        public async Task<ProceedResponseViewModel> ProceedWithWalletold(ProceedWithWallet entity)
        {
            ProceedWithWallet obj = new ProceedWithWallet { };
            obj = entity;
            string Message1 = "";
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

            whereclause = "and Client_ID=1";
            storedProcedureName = "active_walletTransactionapi_api";
            var walletTransactionapi = new
            {
                _whereclause = whereclause,
                _securitykey = SecurityKey
            };
            var dtwalletTransactionapi = await _dbConnection.QueryAsync(storedProcedureName, walletTransactionapi, commandType: CommandType.StoredProcedure);
            dynamic dtwalletTransactionapiDetails = dtwalletTransactionapi.FirstOrDefault();
            int apiid = Convert.ToInt32(dtwalletTransactionapiDetails.ID);

            try
            {

                if (apiid == 3)
                {
                    if (entity.FromCurrency_Code == "NGN" && entity.ToCurrency_Code == "NGN")// New Beta PAGA
                    {
                        await SaveActivityLogTracker("BETA-Paga paymentRequest request: <br/>" + entity.FromCurrency_Code + " To " + entity.ToCurrency_Code, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.Customer_ID, Convert.ToInt32(Customer_ID), "BETA-Paga Proceed", entity.Branch_ID, Client_ID);

                        try
                        {
                            storedProcedureName = "active_walletTransactionapi_api";
                            var valuesApi = new
                            {
                                _whereclause = "and ID=" + apiid,
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

                            int CustApiWalletAccountExist = 0;

                            dynamic CustWalletBank_Details = null;
                            try
                            {
                                string where = "and Customer_ID='" + entity.Customer_ID + "'";
                                storedProcedureName = "SP_GetWalletBankDetails";
                                var CustWalletBankDetails = new
                                {
                                    _where = where
                                };
                                var WalletBankDetails = await _dbConnection.QueryAsync(storedProcedureName, CustWalletBankDetails, commandType: CommandType.StoredProcedure);
                                CustWalletBank_Details = WalletBankDetails.FirstOrDefault();
                            }
                            catch (Exception ex)
                            {
                                await SaveErrorLogAsync("Error AT the time of  SP_GetWalletBankDetails ProceedWithWallet Transaction -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                                return new ProceedResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = "Failed to ProceedWithWallet Transaction",
                                    ApiId = api_id,
                                    AgentRate = AgentRateapi,
                                    ApiStatus = apistatus,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }

                            if (CustWalletBank_Details != null)
                            { CustApiWalletAccountExist = 1; }
                            else
                            {
                                CustApiWalletAccountExist = 0;
                                await SaveErrorLogAsync("Error AT the time of  CustApiWalletAccountExist Not Found-" + CustApiWalletAccountExist, DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);

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
                            accountReference = entity.TransactionRef.ToString();
                            string hashedData = GenerateHashedData(accountReference, accountReference, callBackUrl, Hmac);
                            string jsonResponse = "";
                            JObject responseObject = null;
                            dynamic dt_getWaldt = null;
                            if (CustApiWalletAccountExist == 1)
                            {
                                #region get api walletdata

                                try
                                {
                                    string where1 = "and Customer_ID=" + entity.Customer_ID;
                                    storedProcedureName = "SP_GetWalletBankDetails";
                                    var Cust_getWaldt = new
                                    {
                                        _where = where1
                                    };
                                    var _getWaldt = await _dbConnection.QueryAsync(storedProcedureName, Cust_getWaldt, commandType: CommandType.StoredProcedure);
                                    dt_getWaldt = _getWaldt.FirstOrDefault();
                                }
                                catch (Exception ex)
                                {
                                    await SaveErrorLogAsync("Error AT the time of  SP_GetWalletBankDetails ProceedWithWallet Transaction -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                                    return new ProceedResponseViewModel
                                    {
                                        Status = "Failed",
                                        StatusCode = 2,
                                        Message = "Failed to Create Wallet",
                                        ApiId = api_id,
                                        AgentRate = AgentRateapi,
                                        ApiStatus = apistatus,
                                        ExtraFields = new List<string> { "", "" }
                                    };
                                }

                                //if (dt_getWaldt.Rows.Count > 0)
                                if (dt_getWaldt != null)
                                {
                                    #region get paga api details

                                    string currentDateTimeUTC = DateTime.UtcNow.AddMinutes(5).ToString("yyyy-MM-ddTHH:mm:ss");
                                    string counterPartyid = "";
                                    int Currency_ID = int.TryParse(Convert.ToString(dt_getWaldt.Currency_ID), out int temp) ? temp : 0;
                                    //int APIID = Convert.ToInt32(dtt.Rows[0]["ID"]);
                                    //string apiurl = Convert.ToString(dtt.Rows[0]["Decrypted_API_URL"]);
                                    //string accesscode = Convert.ToString(dtt.Rows[0]["Decrypted_APIAccess_Code"]);

                                    //string hashedData2 = GenerateHashedData2(referenceNumber, Convert.ToString(obj.AmountInGBP), Currency_Code, phoneNumber, Convert.ToString(dtCD.Rows[0]["Email_ID"]), phoneNumber, Hmac);
                                    decimal amountInGBP1 = Convert.ToDecimal(obj.AmountInGBP);
                                    //decimal amountInGBP1 = 10;
                                    string fullName = CustDetails.Full_name.ToString();

                                    string emailId = CustDetails.Email_ID.ToString();
                                    // string accountNumber = Convert.ToString(dt_getWaldt.Rows[0]["Account_Number"]);
                                    string accountNumber = dt_getWaldt.Account_Number.ToString();


                                    string jsonData = GenerateJsonData(accountReference, amountInGBP1, phoneNumber, bankId, fullName, emailId, entity.BeneficiaryName.ToString(), accountNumber, currentDateTimeUTC, callBackUrl);

                                    // Generate Hash 
                                    string hashedData2 = GenerateHashedData2(jsonData, Hmac);
                                    await SaveActivityLogTracker("BETA-Paga Generate Hash : <br/>" + hashedData2 + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.Customer_ID, Convert.ToInt32(Customer_ID), "BETA-Paga Proceed", entity.Branch_ID, Client_ID);

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
                                                        await SaveErrorLogAsync("Error in statusCode response Transaction -" + responseObject["statusMessage"].ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
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
                                                    await SaveErrorLogAsync("Error in response Transaction -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                                                    return new ProceedResponseViewModel
                                                    {
                                                        Status = "Failed",
                                                        StatusCode = 2,
                                                        Message = "Failed to Update Transaction Wallet at api",
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

                                                await SaveErrorLogAsync("Error in paymentRequest response - " + statusMessage, DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
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
                                            await SaveErrorLogAsync("Error in response statusCode - " + statusMessage, DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);

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
                            else
                            {

                            }
                        }
                        catch (Exception ex) { }
                    }
                    else if ((entity.FromCurrency_Code == "CAD" && entity.ToCurrency_Code == "CAD") || (entity.FromCurrency_Code == "NGN" && entity.ToCurrency_Code == "CAD"))
                    {
                        await SaveActivityLogTracker("DC bank paymentRequest Start : <br/>" + entity.FromCurrency_Code + " To " + entity.ToCurrency_Code, 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.Customer_ID, Convert.ToInt32(Customer_ID), "DC bank Proceed", entity.Branch_ID, Client_ID);

                        int phoneCountryCode = 0; string phoneNumber = "", AccountNumber = "", customerName = "", email = "";
                        int countryCode = 0;
                        dynamic CustDetails = null;

                        try
                        {
                            storedProcedureName = "GetCustDetailsByID";
                            var CustDetailsvalues = new
                            {
                                cust_ID = entity.Customer_ID
                            };
                            var dttCustDetails = await _dbConnection.QueryAsync(storedProcedureName, CustDetailsvalues, commandType: CommandType.StoredProcedure);
                            CustDetails = dttCustDetails.FirstOrDefault();
                            if (CustDetails != null)
                            {
                                customerName = CustDetails.Full_name.ToString();
                                email = CustDetails.Email_ID.ToString();
                            }
                        }
                        catch (Exception ex)
                        {
                            await SaveErrorLogAsync("Error AT the time of  GetCustDetailsByID  ProceedWithWallet  Transaction -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                            return new ProceedResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = "Failed to get details of ProceedWithWallet",
                                ApiId = api_id,
                                AgentRate = AgentRateapi,
                                ApiStatus = apistatus,
                                ExtraFields = new List<string> { "", "" }
                            };
                        }

                        //cmdBD.Parameters.AddWithValue("_whereclause", " and bm.Beneficiary_ID =" + obj.Beneficiary_ID);
                        dynamic BenefDetails = null;
                        try
                        {

                            storedProcedureName = "GetBeneficiaryDetails";
                            var BenefDetailsvalues = new
                            {
                                _whereclause = " and bm.Beneficiary_ID =" + entity.Beneficiary_ID
                            };
                            var dttBenefDetails = await _dbConnection.QueryAsync(storedProcedureName, BenefDetailsvalues, commandType: CommandType.StoredProcedure);
                            BenefDetails = dttBenefDetails.FirstOrDefault();
                            if (BenefDetails != null)
                            {
                                phoneCountryCode = Convert.ToInt32(BenefDetails.countryCode);
                                phoneNumber = BenefDetails.ReceiverMobileNo.ToString();
                                AccountNumber = BenefDetails.ReceiverAccountNumber.ToString();
                                phoneCountryCode = Convert.ToInt32(BenefDetails.countryCode);
                            }
                        }
                        catch (Exception ex)
                        {
                            await SaveErrorLogAsync("Error AT the time of  GetBeneficiaryDetails  ProceedWithWallet  Transaction -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                            return new ProceedResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = "Failed to get details of ProceedWithWallet",
                                ApiId = api_id,
                                AgentRate = AgentRateapi,
                                ApiStatus = apistatus,
                                ExtraFields = new List<string> { "", "" }
                            };
                        }


                        string country = "";

                        if (dtwalletTransactionapiDetails.api_Fields.ToString() != "" && dtwalletTransactionapiDetails.api_Fields.ToString() != null)
                        {

                            Newtonsoft.Json.Linq.JObject objAPI = Newtonsoft.Json.Linq.JObject.Parse(dtwalletTransactionapiDetails.api_Fields.ToString());
                            country = objAPI["country"]?.ToString();
                        }

                        if (dtwalletTransactionapiDetails != null)
                        {
                            apiurl = dtwalletTransactionapiDetails.API_URL.ToString();

                            string APIAccess_Code = dtwalletTransactionapiDetails.APIAccess_Code.ToString();

                            double amount = Convert.ToDouble(entity.AmountInPKR);

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
                            catch (Exception ex)
                            {
                                await SaveErrorLogAsync("Error AT the time of  bodyJson  ProceedWithWallet  Transaction -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                                return new ProceedResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = "Failed to get details of ProceedWithWallet",
                                    ApiId = api_id,
                                    AgentRate = AgentRateapi,
                                    ApiStatus = apistatus,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }

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
                                await SaveErrorLogAsync("Error AT the time of Security QueAns ProceedWithWallet Transaction -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                                return new ProceedResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = "Failed In Security QueAns of ProceedWithWallet",
                                    ApiId = api_id,
                                    AgentRate = AgentRateapi,
                                    ApiStatus = apistatus,
                                    ExtraFields = new List<string> { "", "" }
                                };
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
                                    await SaveErrorLogAsync("DCBank Api Response " + ex.ToString(), DateTime.Now, "ProceedWithWallet", entity.Customer_ID, entity.Branch_ID, Client_ID, 0);
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

                                    requestSearchEtransferTransaction.AddHeader("Authorization", "Bearer " + APIAccess_Code);
                                    requestSearchEtransferTransaction.AddHeader("Accept", "application/json");
                                    requestSearchEtransferTransaction.AddHeader("Content-Type", "application/json");
                                    bodyJson = @"{
" + "\n" +
    @"     ""TransactionId"": " + transactionId + @"  " + "\n" +
    @"}";

                                    requestSearchEtransferTransaction.AddParameter("application/json", bodyJson, ParameterType.RequestBody);
                                    await SaveActivityLogTracker("DC bank SearchEtransferTransaction request : <br/>" + bodyJson + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.Customer_ID, Convert.ToInt32(Customer_ID), "ProceedWithWalletd", entity.Branch_ID, Client_ID);

                                    RestResponse responseSearchEtransferTransaction = clientSearchEtransferTransaction.Execute(requestSearchEtransferTransaction);
                                    await SaveActivityLogTracker("DC bank SearchEtransferTransaction response : <br/>" + responseSearchEtransferTransaction + "", 0, DateTime.Now, 0, Transaction_ID.ToString(), entity.Customer_ID, Convert.ToInt32(Customer_ID), "ProceedWithWalletd", entity.Branch_ID, Client_ID);

                                    jsonResponse = responseSearchEtransferTransaction.Content;
                                    responseObject1 = Newtonsoft.Json.Linq.JObject.Parse(jsonResponse);
                                    apiStatus = "COMPLETED";
                                }
                                else
                                {
                                    await SaveErrorLogAsync("Error In DCBank Api Response Transaction -", DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
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

                                }

                            }
                            else
                            {
                                await SaveErrorLogAsync("Error In DCBank Api Response Transaction -", DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
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
                else if (apiid == 4)
                {
                    string refreshToken = "", accessToken = "", wallet_bank_name = "", wallet_bank_code = "";
                    string secretKey = "", Benf_accountNumber = ""; string accountName = "", AccountHolderName = "";
                    string api_wallet_customerID = "", narration = "", Transaction_Reference = "";
                    string api_customer_walletID = ""; string Password = ""; string urll = ""; string Bank_code = ""; string AvailableBalance = "";
                    try
                    {
                        storedProcedureName = "Get_wallet_API_Details";
                        var parameters = new
                        {
                            p_api_ID = apiid
                        };

                        var walletTransactionApiDetails = await _dbConnection.QueryAsync(storedProcedureName, parameters, commandType: CommandType.StoredProcedure);
                        var apiDetail = walletTransactionApiDetails.FirstOrDefault();
                        if (apiDetail != null)
                        {
                            accesscode = Convert.ToString(apiDetail.UserName);
                            Password = Convert.ToString(apiDetail.Password);
                            urll = Convert.ToString(apiDetail.API_URL);
                        }
                    }
                    catch (Exception ex)
                    {
                        await SaveErrorLogAsync("Error while executing Get_wallet_API_Details during wallet transaction - " + ex, DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                        return new ProceedResponseViewModel
                        {
                            Status = "Failed",
                            StatusCode = 2,
                            Message = "Failed to create wallet.",
                            ApiId = api_id,
                            AgentRate = AgentRateapi,
                            ApiStatus = apistatus,
                            ExtraFields = new List<string> { "", "" }
                        };
                    }

                    try
                    {
                        string where = "and Customer_ID=" + Customer_ID + " and API_ID=" + apiid + " order by Opening_Date desc limit 1";
                        storedProcedureName = "SP_GetWalletBankDetails";
                        var walletTransactionbankdetails = new
                        {
                            _where = where
                        };
                        var dtwalletTransactionbankdetails = await _dbConnection.QueryAsync(storedProcedureName, walletTransactionbankdetails, commandType: CommandType.StoredProcedure);
                        var dtEXcidtl = dtwalletTransactionbankdetails.FirstOrDefault();

                        if (dtEXcidtl != "")
                        {
                            api_customer_walletID = Convert.ToString(dtEXcidtl.API_Wallet_ID);
                            api_wallet_customerID = Convert.ToString(dtEXcidtl.ApiCustomer_ID);
                        }
                    }
                    catch (Exception ex)
                    {
                        await SaveErrorLogAsync("Error AT the time of  SP_GetWalletBankDetails Insert Transaction -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                        return new ProceedResponseViewModel
                        {
                            Status = "Failed",
                            StatusCode = 2,
                            Message = "Failed to Create Wallet",
                            ApiId = api_id,
                            AgentRate = AgentRateapi,
                            ApiStatus = apistatus,
                            ExtraFields = new List<string> { "", "" }
                        };
                    }

                    if (api_customer_walletID != "")
                    {
                        try
                        {
                            storedProcedureName = "get_api_wallet_transaction_details";
                            var walletTransactionetails = new
                            {
                                iClient_ID = obj.Client_ID,
                                iTransaction_ID = obj.Transaction_ID,
                                iBranchListAPI_ID = apiid
                            };
                            var dtwalletTransactiondetails = await _dbConnection.QueryAsync(storedProcedureName, walletTransactionetails, commandType: CommandType.StoredProcedure);
                            var dtr = dtwalletTransactiondetails.FirstOrDefault();

                            if (dtr != "")
                            {
                                wallet_bank_code = Convert.ToString(dtr.wallet_bank_code);
                                wallet_bank_name = Convert.ToString(dtr.wallet_bank_name);
                                Benf_accountNumber = Convert.ToString(dtr.Account_Number);
                                AccountHolderName = Convert.ToString(dtr.AccountHolderName);
                                narration = Convert.ToString(dtr.Purpose);
                                Transaction_Reference = Convert.ToString(dtr.ReferenceNo);
                            }
                        }
                        catch (Exception ex)
                        {
                            await SaveErrorLogAsync("Error AT the time of  get_api_wallet_transaction_details Insert Transaction -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                        }

                        string email = accesscode;//"aW5mb0BzdXBlcnRyYW5zZmVyLmNvLnVr";
                        string password = Password;//"RklLS3k0Y2FyZDEjQA==";

                        // Login
                        try
                        {
                            ServicePointManager.Expect100Continue = true;
                            /*ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                   | SecurityProtocolType.Tls11
                                   | SecurityProtocolType.Tls12
                                   | SecurityProtocolType.Ssl3;*/
                            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                   | SecurityProtocolType.Tls11
                                   | SecurityProtocolType.Tls12;
                            var options = new RestClientOptions(urll + "/api/v1/auth/login")
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
                            var body1 = @"{" + "\n" +
                 @"    ""email"": """ + email + @""",
        " + "\n" +
                 @"    ""password"": """ + password + @""""
                 + "\n" +
                 @"}";
                            request.AddParameter("application/json", body1, ParameterType.RequestBody);
                            await SaveActivityLogTracker("Providus Wallet login api Request Insert Transaction -" + urll + "/api/v1/auth/login", 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);

                            RestResponse response = client.Execute(request);

                            await SaveActivityLogTracker("Providus Wallet login api Response Insert Transaction - " + response.Content, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);


                            accessToken = response.Headers
                .FirstOrDefault(h => h.Name == "X-Access-Token")?.Value?.ToString();

                            refreshToken = response.Headers
                               .FirstOrDefault(h => h.Name == "X-Refresh-Token")?.Value?.ToString();
                            await SaveActivityLogTracker("Providus Wallet login api accessToken and refreshToken Insert Transaction - " + accessToken + " |refreshToken -" + refreshToken, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);

                        }
                        catch (Exception ex)
                        {
                            await SaveErrorLogAsync(" Error In Providus Wallet login api Insert Transaction  -" + ex.ToString(), DateTime.Now, "Proceed", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                            return new ProceedResponseViewModel
                            {
                                Status = "Failed",
                                StatusCode = 2,
                                Message = "Failed to Create Wallet",
                                ApiId = api_id,
                                AgentRate = AgentRateapi,
                                ApiStatus = apistatus,
                                ExtraFields = new List<string> { "", "" }
                            };
                        }

                        if (refreshToken != "" && accessToken != "")
                        {
                            // Generate Secret Key
                            try
                            {


                                var options = new RestClientOptions(urll + "/api/v1/merchant/generate-access-keys")
                                {
                                    MaxTimeout = -1
                                };
                                var client = new RestClient(options);
                                var request = new RestRequest()
                                {
                                    Method = Method.Post
                                };

                                request.AddHeader("Content-Type", "application/json");
                                request.AddHeader("X-Access-Token", accessToken);
                                request.AddHeader("X-Refresh-Token", refreshToken);

                                await SaveActivityLogTracker("Providus Wallet Generate Secret Key Request Insert Transaction - " + urll + "/api/v1/merchant/generate-access-keys", 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);

                                RestResponse response = client.Execute(request);

                                await SaveActivityLogTracker("Providus Wallet Generate Secret Key Response Insert Transaction - " + response.Content, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);

                                dynamic dynJsonn = JsonConvert.DeserializeObject(response.Content);

                                bool status = (bool)dynJsonn.status;
                                if (status)
                                {
                                    string publicKey = (string)dynJsonn.data.publicKey;
                                    secretKey = (string)dynJsonn.data.privateKey;

                                }
                                else
                                {
                                    Console.WriteLine("Status is false.");
                                    return new ProceedResponseViewModel
                                    {
                                        Status = "Failed",
                                        StatusCode = 2,
                                        Message = "Failed to Create Wallet",
                                        ApiId = api_id,
                                        AgentRate = AgentRateapi,
                                        ApiStatus = apistatus,
                                        ExtraFields = new List<string> { "", "" }
                                    };
                                }

                            }
                            catch (Exception ex)
                            {
                                await SaveErrorLogAsync(" Error In Providus Wallet Generate Secret Key Insert Transaction -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                                return new ProceedResponseViewModel
                                {
                                    Status = "Failed",
                                    StatusCode = 2,
                                    Message = "Failed to Create Wallet",
                                    ApiId = api_id,
                                    AgentRate = AgentRateapi,
                                    ApiStatus = apistatus,
                                    ExtraFields = new List<string> { "", "" }
                                };
                            }

                            if (secretKey != "")
                            {


                                //Get Wallet by Customer ID
                                try
                                {

                                    var options = new RestClientOptions(urll + "/api/v1/wallet/customer?customerId=" + api_wallet_customerID)
                                    {
                                        MaxTimeout = -1
                                    };
                                    var client = new RestClient(options);
                                    var request = new RestRequest()
                                    {
                                        Method = Method.Get
                                    };


                                    request.AddHeader("Content-Type", "application/json");
                                    request.AddHeader("Authorization", "Bearer " + secretKey);
                                    await SaveActivityLogTracker("Providus Wallet Get Customer Wallet Request Insert Transaction - " + urll + "/api/v1/wallet/customer?customerId=" + api_wallet_customerID, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);

                                    var response = client.Execute(request);
                                    await SaveActivityLogTracker("Providus Wallet Get Customer Wallet Response Insert Transaction - " + response.Content, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);

                                    try
                                    {
                                        JObject jsonResponse = JObject.Parse(response.Content);


                                        bool apiStatus2 = jsonResponse["status"]?.Value<bool>() ?? false;
                                        if (!apiStatus2)
                                        {
                                            return new ProceedResponseViewModel
                                            {
                                                Status = "Failed",
                                                StatusCode = 2,
                                                Message = "Unable To Check Wallet Balance.",
                                                ApiId = api_id,
                                                AgentRate = AgentRateapi,
                                                ApiStatus = apistatus,
                                                ExtraFields = new List<string> { "", "" }
                                            };
                                        }


                                        var wallet = jsonResponse["wallet"];
                                        if (wallet != null)
                                        {
                                            AvailableBalance = wallet["availableBalance"]?.ToString();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error parsing JSON: {ex.Message}");
                                        await SaveErrorLogAsync("Error In Providus parsing JSON Insert Transaction -" + ex.ToString(), DateTime.Now, "Proceed", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                                        return new ProceedResponseViewModel
                                        {
                                            Status = "Failed",
                                            StatusCode = 2,
                                            Message = "Unable To Check response.",
                                            ApiId = api_id,
                                            AgentRate = AgentRateapi,
                                            ApiStatus = apistatus,
                                            ExtraFields = new List<string> { "", "" }
                                        };
                                    }

                                }
                                catch (Exception ex)
                                {
                                    await SaveErrorLogAsync("Error In Providus Wallet Get Customer wallet Api Insert Transaction -" + ex.ToString(), DateTime.Now, "Proceed", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                                    return new ProceedResponseViewModel
                                    {
                                        Status = "Failed",
                                        StatusCode = 2,
                                        Message = "Failed Get Customer Wallet.",
                                        ApiId = api_id,
                                        AgentRate = AgentRateapi,
                                        ApiStatus = apistatus,
                                        ExtraFields = new List<string> { "", "" }
                                    };
                                }

                                if (Convert.ToDouble(AvailableBalance) <= Convert.ToDouble(obj.Total_Amount))
                                {
                                    //Get Bank List
                                    try
                                    {

                                        var options = new RestClientOptions(urll + "/api/v1/transfer/banks")
                                        {
                                            MaxTimeout = -1
                                        };
                                        var client = new RestClient(options);
                                        var request = new RestRequest()
                                        {
                                            Method = Method.Get
                                        };

                                        request.AddHeader("Content-Type", "application/json");
                                        request.AddHeader("Authorization", "Bearer " + secretKey);

                                        await SaveActivityLogTracker("Providus Wallet Get Bank List Api Request Insert Transaction - " + urll + "/api/v1/transfer/banks", 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);

                                        RestResponse response = client.Execute(request);
                                        await SaveActivityLogTracker("Providus Wallet Get Bank List Api Response Insert Transaction - " + response.Content, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);

                                        JObject jsonResponse = JObject.Parse(response.Content);
                                        JArray banks = (JArray)jsonResponse["banks"];
                                        var bank = banks.FirstOrDefault(b => b["code"]?.ToString() == wallet_bank_code);

                                        if (bank != null)
                                        {
                                            Bank_code = bank["code"]?.ToString();
                                            string name = bank["name"]?.ToString();
                                            await SaveActivityLogTracker("Providus Wallet Bank Code Found in Insert Transaction - " + name + " |code" + Bank_code, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);
                                        }
                                        else
                                        {
                                            await SaveActivityLogTracker("Providus Wallet Bank Code Not Found in Insert Transaction - " + wallet_bank_code + ".", 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);
                                            return new ProceedResponseViewModel
                                            {
                                                Status = "Failed",
                                                StatusCode = 2,
                                                Message = "Failed to Create Wallet",
                                                ApiId = api_id,
                                                AgentRate = AgentRateapi,
                                                ApiStatus = apistatus,
                                                ExtraFields = new List<string> { "", "" }
                                            };
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        await SaveErrorLogAsync(" Error In Providus Wallet Get Bank List Api Insert Transaction -" + ex.ToString(), DateTime.Now, "Proceed", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                                        return new ProceedResponseViewModel
                                        {
                                            Status = "Failed",
                                            StatusCode = 2,
                                            Message = "Failed to Create Wallet",
                                            ApiId = api_id,
                                            AgentRate = AgentRateapi,
                                            ApiStatus = apistatus,
                                            ExtraFields = new List<string> { "", "" }
                                        };
                                    }

                                    if (Bank_code != "")
                                    {
                                        //Fetch Account Details
                                        try
                                        {



                                            var options = new RestClientOptions(urll + "/api/v1/transfer/account/details?sortCode=" + wallet_bank_code + "&accountNumber=" + Benf_accountNumber)
                                            {
                                                MaxTimeout = -1
                                            };
                                            var client = new RestClient(options);
                                            var request = new RestRequest()
                                            {
                                                Method = Method.Get
                                            };

                                            request.AddHeader("Content-Type", "application/json");
                                            request.AddHeader("Authorization", "Bearer " + secretKey);
                                            await SaveActivityLogTracker("Providus Wallet Fetch Account Details Api Request Insert Transaction - " + urll + "/api/v1/transfer/account/details?sortCode=" + wallet_bank_code + "&accountNumber=" + Benf_accountNumber, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);
                                            RestResponse response = client.Execute(request);

                                            await SaveActivityLogTracker("Providus Wallet Fetch Account Details Api Response Insert Transaction - " + response.Content, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);

                                            JObject jsonResponse = JObject.Parse(response.Content);


                                            JObject account = (JObject)jsonResponse["account"];

                                            if (account != null)
                                            {
                                                string bankCode = account["bankCode"]?.ToString();
                                                accountName = account["accountName"]?.ToString();
                                                string AccountNumber = account["accountNumber"]?.ToString();
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            await SaveErrorLogAsync(" Error In Providus Wallet Fetch Account Details Api Request Insert Transaction -" + ex.ToString(), DateTime.Now, "Proceed", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                                            return new ProceedResponseViewModel
                                            {
                                                Status = "Failed",
                                                StatusCode = 2,
                                                Message = "Failed to Create Wallet",
                                                ApiId = api_id,
                                                AgentRate = AgentRateapi,
                                                ApiStatus = apistatus,
                                                ExtraFields = new List<string> { "", "" }
                                            };
                                        }

                                        if (accountName != "")
                                        {

                                            //Customer Wallet To Bank Transfer

                                            try
                                            {
                                                string amount = Convert.ToString(obj.Total_Amount);


                                                if (accountName == "")
                                                {
                                                    accountName = AccountHolderName;
                                                }

                                                var options = new RestClientOptions(urll + "/api/v1/transfer/bank/customer")
                                                {
                                                    MaxTimeout = -1
                                                };
                                                var client = new RestClient(options);
                                                var request = new RestRequest()
                                                {
                                                    Method = Method.Post
                                                };

                                                request.AddHeader("Content-Type", "application/json");
                                                request.AddHeader("Authorization", "Bearer " + secretKey);

                                                var body2 = @"{" + "\n" +
                                                 @"    ""amount"": """ + amount + @"""," + "\n" +
                                                 @"    ""sortCode"": """ + wallet_bank_code + @"""," + "\n" +
                                                 @"    ""narration"": """ + narration + @"""," + "\n" +
                                                 @"    ""accountNumber"": """ + Benf_accountNumber + @"""," + "\n" +
                                                 @"    ""accountName"": """ + accountName + @"""," + "\n" +
                                                 @"    ""customerId"": """ + api_wallet_customerID + @"""," + "\n" +
                                                 @"    ""metadata"": {" + "\n" +
                                                 @"        ""customer-data"": """ + Transaction_Reference + @"""" + "\n" +
                                                 @"    }" + "\n" +
                                                 @"}";

                                                request.AddParameter("application/json", body2, ParameterType.RequestBody);
                                                await SaveActivityLogTracker("Providus Wallet Create Wallet to Bank Transfer Request Insert Transaction - " + urll + "/api/v1/transfer/bank/customer" + body2, 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);

                                                RestResponse response = client.Execute(request);

                                                await SaveActivityLogTracker("Providus Wallet Create Wallet to Bank Transfer Response Insert Transaction - " + response.Content + ".", 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);

                                                try
                                                {
                                                    JObject jsonResponse = JObject.Parse(response.Content);


                                                    bool transactionapiStatus = jsonResponse["status"]?.Value<bool>() ?? false;
                                                    if (!transactionapiStatus)
                                                    {
                                                        Message1 = jsonResponse["message"]?.ToString();
                                                        await SaveActivityLogTracker("App - Providus Wallet Create Wallet to Bank Transfer Status Failed Insert Transaction - " + apiStatus + ".", 0, DateTime.Now, 0, Transaction_ID.ToString(), obj.User_ID, Convert.ToInt32(Customer_ID), "ProceedWithWallet", entity.Branch_ID, Client_ID);
                                                        return new ProceedResponseViewModel
                                                        {
                                                            Status = "Failed",
                                                            StatusCode = 2,
                                                            Message = Message1,
                                                            ApiId = api_id,
                                                            AgentRate = AgentRateapi,
                                                            ApiStatus = apistatus,
                                                            ExtraFields = new List<string> { "", "" }
                                                        };

                                                    }

                                                    Message = jsonResponse["message"]?.ToString();


                                                    var transfer = jsonResponse["transfer"];
                                                    if (transfer != null)
                                                    {
                                                        string Amount = transfer["amount"]?.ToString();
                                                        string Charges = transfer["charges"]?.ToString();
                                                        string Vat = transfer["vat"]?.ToString();
                                                        Transaction_Reference = transfer["reference"]?.ToString();
                                                        string Total = transfer["total"]?.ToString();
                                                        string SessionId = transfer["sessionId"]?.ToString();
                                                        string Destination = transfer["destination"]?.ToString();
                                                        string TxRef = transfer["transactionReference"]?.ToString();
                                                        string Description = transfer["description"]?.ToString();


                                                        string Metadata = transfer["metadata"]?["customer-data"]?.ToString();


                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    await SaveErrorLogAsync("Error In Providus Wallet Create Wallet to Bank Transfer Insert Transaction -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                                                    return new ProceedResponseViewModel
                                                    {
                                                        Status = "Failed",
                                                        StatusCode = 2,
                                                        Message = "Failed to Create Wallet",
                                                        ApiId = api_id,
                                                        AgentRate = AgentRateapi,
                                                        ApiStatus = apistatus,
                                                        ExtraFields = new List<string> { "", "" }
                                                    };
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                await SaveErrorLogAsync("Error In Providus Wallet Create Wallet to Bank Transfer Insert Transaction -" + ex.ToString(), DateTime.Now, "ProceedWithWallet", obj.User_ID, obj.Branch_ID, Client_ID, 0);
                                                return new ProceedResponseViewModel
                                                {
                                                    Status = "Failed",
                                                    StatusCode = 2,
                                                    Message = "Failed to Create Wallet",
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
                                                Message = "Unable To Fetch Beneficiery Account Details.",
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
                                            Message = "Beneficiery Bank Not Found.",
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
                                        Message = "Insufficient Funds, Please Fund Your Wallet Your Balance is: " + AvailableBalance,
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
                                    Message = "Failed to Create Wallet",
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
                                Message = "Customer Wallet Not Created, Please Create the wallet First.",
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
    }
}
