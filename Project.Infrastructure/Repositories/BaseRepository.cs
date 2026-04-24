using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Project.API.Configuration;
using Project.Core.Entities;
using Project.Core.Interfaces.IRepositories;
using RestSharp;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using TransferZero.Sdk.Api;
using TransferZero.Sdk.Client;
using TransferZero.Sdk.Model;
using static Dapper.SqlMapper;

public class BaseRepository<T> : IBaseRepository<T> where T : class
{
    protected readonly IDbConnection _dbConnection;

    public BaseRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<IEnumerable<T>> GetAll()
    {
        var query = $"SELECT * FROM {typeof(T).Name}";
        return await _dbConnection.QueryAsync<T>(query);
    }

    public async Task<T> GetById<Tid>(Tid id)
    {
        var query = $"SELECT * FROM {typeof(T).Name} WHERE Id = @id";
        return await _dbConnection.QuerySingleOrDefaultAsync<T>(query, new { id });
    }

    public async Task<bool> IsExists<Tvalue>(string key, Tvalue value)
    {
        var query = $"SELECT COUNT(1) FROM {typeof(T).Name} WHERE {key} = @value";
        var result = await _dbConnection.ExecuteScalarAsync<int>(query, new { value });
        return result == 1;
    }

    public async Task<bool> IsExistsForUpdate<Tid>(Tid id, string key, string value)
    {
        var query = $"SELECT COUNT(1) FROM {typeof(T).Name} WHERE Id != @id AND {key} = @value";
        var result = await _dbConnection.ExecuteScalarAsync<int>(query, new { id, value });
        return result == 1;
    }

    public async Task<T> Create(T model)
    {
        var query = $"INSERT INTO {typeof(T).Name} VALUES (@model); SELECT CAST(SCOPE_IDENTITY() as int)";
        var id = await _dbConnection.ExecuteScalarAsync<int>(query, model);
        return await GetById(id);
    }

    public async Task CreateRange(List<T> models)
    {
        var query = $"INSERT INTO {typeof(T).Name} VALUES (@model)";
        await _dbConnection.ExecuteAsync(query, models);
    }

    public async Task Update(T model)
    {
        var query = $"UPDATE {typeof(T).Name} SET @model WHERE Id = @Id";
        await _dbConnection.ExecuteAsync(query, model);
    }

    public async Task Delete(T model)
    {
        var query = $"DELETE FROM {typeof(T).Name} WHERE Id = @Id";
        await _dbConnection.ExecuteAsync(query, model);
    }

    public async Task SaveChangeAsync()
    {
        // Dapper does not have a SaveChanges method like EF, so this can be left empty or removed.
    }
    public async Task<int> SaveActivityLogTracker(
       string activity,
       int whoAccessed,
       DateTime recordInsertDate,
       int deleteStatus,
       string transactionId,
       int? userId,
       int? custId,
       string functionName,
       int? branchId,
       int? clientId)
    {
        var parameters = new
        {
            _activity = "MTS_Integration - " + activity,
            _whoAccessed = whoAccessed,
            _recordInsertDate = recordInsertDate,
            _deleteStatus = deleteStatus,
            _transactionId = transactionId,
            _userId = userId,
            _custId = custId,
            _functionName = functionName,
            _branchId = branchId,
            _clientId = clientId
        };

        return await _dbConnection.ExecuteAsync("sp_save_activity_log_tracker", parameters, commandType: CommandType.StoredProcedure);
    }

    public async Task SaveErrorLogAsync(string error, DateTime recordInsertDateTime, string functionName, int? userId, int? branchId, int? clientId, int deleteStatus)
    {
        var parameters = new
        {
            _Error = "MTS_Integration - " + error,
            _Record_insert_Date_time = recordInsertDateTime,
            _Function_Name = functionName,
            _User_ID = userId,
            _Branch_ID = branchId,
            _Client_ID = clientId,
            _Delete_Status = deleteStatus
        };

        await _dbConnection.ExecuteAsync("sp_save_error_log", parameters, commandType: CommandType.StoredProcedure);
    }


    public Transaction TransferZeroGetTransactionFromExternalId(TransferZero.Sdk.Client.Configuration configuration, string externalId)
    {
        //mtsmethods.InsertActivityLogDetails("AZA step 2 " + externalId, 0, 0, 0, 0, "Get Remittance Status", 0, 1);
        try
        {
            // Please see https://docs.transferzero.com/docs/transaction-flow/#external-id
            // for more details on external IDs

            TransactionsApi transactionsApi = new TransactionsApi(configuration);
            //String externalId = "TRANSACTION-00001";
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                   | SecurityProtocolType.Tls11
                   | SecurityProtocolType.Tls12
                   | SecurityProtocolType.Ssl3;
            TransactionListResponse transactionListResponse = transactionsApi.GetTransactions(externalId: externalId);
            if (transactionListResponse.Object.Count > 0)
            {
                System.Console.WriteLine("Transaction found");
                Transaction result = transactionListResponse.Object[0];
                System.Console.WriteLine(result);
                return result;
            }
            else
            {
                System.Console.WriteLine("Transaction not found");
                return null;
            }
        }
        catch (ApiException e)
        {
            if (e.IsValidationError)
            {
                // Process validation error
                RecipientResponse transactionResponse = e.ParseObject<RecipientResponse>();
                System.Console.WriteLine("Validation Error" + transactionResponse.Object.Errors);
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(transactionResponse.Object.Errors);
                //mtsmethods.InsertActivityLogDetails("GetTrans remittance - Error response parameters: " + json + "", 0, 0, 0, 0, "Get Remittance Status", 0, 1);

                return null;

            }
            else
            {
                // mtsmethods.InsertActivityLogDetails("GetTrans remittance - Error response parameters: " + e.ToString() + "", 0, 0, 0, 0, "Get Remittance Status", 0, 1);

                return null;
            }
        }
    }


    //TransactionResponse TransferZeroCreateTransaction(TransferZero.Sdk.Client.Configuration configuration, DataTable dt, string cust_id, int PaymentDepositType_ID, ITransfer t)
    public TransactionResponse TransferZeroCreateTransaction(TransferZero.Sdk.Client.Configuration configuration, DataTable dt, string cust_id, int PaymentDepositType_ID)
    {
        //mtsmethods.InsertActivityLogDetails("AZA Create transaction step 1 Trans ID:" + t.Transaction_ID, t.User_ID, 0, t.User_ID, t.Customer_ID, "Proceed Transaction", t.CB_ID, t.Client_ID);
        Transaction transaction = new Transaction();
        TransactionsApi api = new TransactionsApi(configuration);
        try
        {
            string payMethod_type = "";
            if (PaymentDepositType_ID == 1)
            {
                payMethod_type = "Bank";
            }
            else if (PaymentDepositType_ID == 2)
            {
                payMethod_type = "Cash";
            }
            else if (PaymentDepositType_ID == 3)
            {
                payMethod_type = "Mobile";
            }
            string bank_ac_no = "";
            string iban_no = "";
            // Please check our documentation at https://docs.transferzero.com/docs/transaction-flow/
            // for details on how transactions work

            // When adding a sender to transaction, please use either an id or external_id. Providing both will result in a validation error.
            // Please see our documentation at https://docs.transferzero.com/docs/transaction-flow/#sender

            Sender sender = new Sender(id: Guid.Parse(cust_id)); //This  is sender id  we got from CreateSender()

            // You can find the various payout options at https://docs.transferzero.com/docs/transaction-flow/#payout-details
            //benfname
            string bname = Convert.ToString(dt.Rows[0]["Beneficiary_Name"]); string bfname = bname; string blname = ".";
            if (bname.Contains(" "))
            {
                string[] spli = bname.Split(' ');
                if (spli.Length > 1) { bfname = bname.Substring(0, (bname.Length - spli[spli.Length - 1].Length)); blname = spli[spli.Length - 1]; } //if (spli.Length > 1) { bfname = spli[0]; blname = spli[1]; }
            }
            PayoutMethodDetails ngnBankDetails = new PayoutMethodDetails();
            if (Convert.ToString(dt.Rows[0]["Iban_ID"]) != "" && Convert.ToString(dt.Rows[0]["Iban_ID"]).Length > 2)
            {
                // mtsmethods.InsertActivityLogDetails("AZA Create transaction step 2 Trans ID:" + t.Transaction_ID, t.User_ID, 0, t.User_ID, t.Customer_ID, "Proceed Transaction", t.CB_ID, t.Client_ID);
                iban_no = (dt.Rows[0]["Iban_ID"]).ToString();
                ngnBankDetails = new PayoutMethodDetails(
                       iban: iban_no,
                       firstName: Convert.ToString(bfname),
                       lastName: Convert.ToString(blname)
                   );
            }
            else if (Convert.ToString(dt.Rows[0]["Account_Number"]) != "")
            {
                // mtsmethods.InsertActivityLogDetails("AZA Create transaction step 3 Trans ID:" + t.Transaction_ID, t.User_ID, 0, t.User_ID, t.Customer_ID, "Proceed Transaction", t.CB_ID, t.Client_ID);
                bank_ac_no = (dt.Rows[0]["Account_Number"]).ToString();
                PayoutMethodMobileProviderEnum mobile_prov = new PayoutMethodMobileProviderEnum();
                mobile_prov = PayoutMethodMobileProviderEnum.Airtel;
                //    ngnBankDetails = new PayoutMethodDetails(
                //           bankAccount: bank_ac_no,
                //           bankAccountType: PayoutMethodBankAccountTypeEnum._20,
                //           bankCode: Convert.ToString(dt.Rows[0]["bank_code"]),
                //           firstName: Convert.ToString(bfname),
                //           lastName: Convert.ToString(blname),
                //street: "Main Street",
                //bankName: Convert.ToString(dt.Rows[0]["Bank_Name"]),
                //mobileProvider: mobile_prov,
                //branchCode: Convert.ToString(dt.Rows[0]["branchcode"]),
                //swiftCode: Convert.ToString(dt.Rows[0]["Ifsc_Code"]),
                //identityCardType: TransferZero.Sdk.Model.PayoutMethodIdentityCardTypeEnum.ID,
                //identityCardId: Convert.ToString(dt.Rows[0]["BID_Number"]), // refers to the recipient's ID details
                //transferReason: PayoutMethodTransferReasonEnum.Thirdpartypersonaccount// "third_party_person_account"    
                //       );
                if ((dt.Rows[0]["Beneficiary_Country"]).ToString() == "Nigeria")
                {
                    ngnBankDetails = new PayoutMethodDetails(
                              firstName: Convert.ToString(bfname),
                              lastName: Convert.ToString(blname),
                              bankCode: Convert.ToString(dt.Rows[0]["bank_code"]),
                              bankAccount: bank_ac_no
                    );
                }
                else
                {
                    ngnBankDetails = new PayoutMethodDetails(
                               firstName: Convert.ToString(bfname),
                               lastName: Convert.ToString(blname),
                               street: Convert.ToString(dt.Rows[0]["Beneficiary_Address"]),
                               bankCode: Convert.ToString(dt.Rows[0]["bank_code"]),
                               bankAccount: bank_ac_no,
                    bankName: Convert.ToString(dt.Rows[0]["Bank_Name"]),
                    branchCode: Convert.ToString(dt.Rows[0]["branchcode"]),
                    swiftCode: Convert.ToString(dt.Rows[0]["Ifsc_Code"]),
                    transferReason: PayoutMethodTransferReasonEnum.Thirdpartypersonaccount,// "third_party_person_account"    
                               bankAccountType: PayoutMethodBankAccountTypeEnum._20,
                    //mobileProvider: mobile_prov,
                    identityCardType: TransferZero.Sdk.Model.PayoutMethodIdentityCardTypeEnum.ID,
                    identityCardId: Convert.ToString(dt.Rows[0]["BID_Number"]) // refers to the recipient's ID details
                           );
                }
            }


            if (PaymentDepositType_ID == 2)
            {
                // mtsmethods.InsertActivityLogDetails("AZA Create transaction step 4 Trans ID:" + t.Transaction_ID, t.User_ID, 0, t.User_ID, t.Customer_ID, "Proceed Transaction", t.CB_ID, t.Client_ID);
                ngnBankDetails = new PayoutMethodDetails(
                    firstName: Convert.ToString(bfname),
                    lastName: Convert.ToString(blname),
                    phoneNumber: "+" + Convert.ToString(dt.Rows[0]["Beneficiary_Mobile"]), // E.164 international format
                    cashProvider: PayoutMethodCashProviderEnum.Wizall // "wizall" // Mandatory
                    );
            }
            if (PaymentDepositType_ID == 3)
            {
                PayoutMethodMobileProviderEnum mobile_prov = new PayoutMethodMobileProviderEnum();
                mobile_prov = PayoutMethodMobileProviderEnum.Airtel;
                ngnBankDetails = new PayoutMethodDetails(
                    firstName: Convert.ToString(bfname),
                    lastName: Convert.ToString(blname),
                    phoneNumber: "+" + Convert.ToString(dt.Rows[0]["Beneficiary_Mobile"]), // E.164 international format
                    mobileProvider: mobile_prov,
                    transferReason: PayoutMethodTransferReasonEnum.Thirdpartypersonaccount// "third_party_person_account"    
                );

            }
            PayoutMethod payoutMethod = new PayoutMethod(
                type: Convert.ToString(dt.Rows[0]["Currency_Code"]) + "::" + payMethod_type,//"NGN::Bank", 
                details: ngnBankDetails
            );

            // mtsmethods.InsertActivityLogDetails("AZA Create transaction step 5 Trans ID:" + t.Transaction_ID, t.User_ID, 0, t.User_ID, t.Customer_ID, "Proceed Transaction", t.CB_ID, t.Client_ID);

            Recipient recipient = new Recipient(
                    requestedAmount: Convert.ToDecimal(dt.Rows[0]["AmountInPKR"]),
                    requestedCurrency: Convert.ToString(dt.Rows[0]["Currency_Code"]),
                    payoutMethod: payoutMethod
                );

            // Similarly you can check https://docs.transferzero.com/docs/transaction-flow/#requested-amount-and-currency
            // on details about the input currency parameter

            // Find more details on external IDs at https://docs.transferzero.com/docs/transaction-flow/#external-id
            //mtsmethods.InsertActivityLogDetails("AZA Create transaction step 6 Trans ID:" + t.Transaction_ID, t.User_ID, 0, t.User_ID, t.Customer_ID, "Proceed Transaction", t.CB_ID, t.Client_ID);
            string base_currency_code = Convert.ToString(dt.Rows[0]["FromCurrency_Code"]);
            if (Convert.ToString(dt.Rows[0]["FromCurrency_Code"]) != "GBP" && Convert.ToString(dt.Rows[0]["FromCurrency_Code"]) != "CAD") //if (Convert.ToString(dt.Rows[0]["FromCurrency_Code"]) != "GBP")
                base_currency_code = "EUR";
            transaction = new Transaction(
               inputCurrency: base_currency_code,
               sender: sender,
               recipients: new List<Recipient>() { recipient },
               externalId: "TRANSACTION-" + Convert.ToString(dt.Rows[0]["ReferenceNo"]).Substring(2)
           );
            //mtsmethods.InsertActivityLogDetails("AZA Create transaction step 7 Trans ID:" + t.Transaction_ID, t.User_ID, 0, t.User_ID, t.Customer_ID, "Proceed Transaction", t.CB_ID, t.Client_ID);

        }
        catch (Exception ex)
        {

            //mtsmethods.InsertActivityLogDetails("Create Transaction Error response parameters: <br/>" + ex.ToString() + "", t.User_ID, t.Transaction_ID, t.User_ID, t.Customer_ID, "Proceed Transaction", t.CB_ID, t.Client_ID);

            throw ex;
        }
        try
        {


            //mtsmethods.InsertActivityLogDetails("AZA Create transaction step 8 Trans ID:" + t.Transaction_ID, t.User_ID, 0, t.User_ID, t.Customer_ID, "Proceed Transaction", t.CB_ID, t.Client_ID);
            TransactionRequest transactionRequest = new TransactionRequest(
                transaction: transaction
            );
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                   | SecurityProtocolType.Tls11
                   | SecurityProtocolType.Tls12
                   | SecurityProtocolType.Ssl3;

            //mtsmethods.InsertActivityLogDetails("AZA Create transaction step 9 Trans ID:" + t.Transaction_ID, t.User_ID, 0, t.User_ID, t.Customer_ID, "Proceed Transaction", t.CB_ID, t.Client_ID);
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(transactionRequest);
            //mtsmethods.InsertActivityLogDetails("Create Transaction request parameters: <br/> " + json + "", t.User_ID, t.Transaction_ID, t.User_ID, t.Customer_ID, "Proceed Transaction", t.CB_ID, t.Client_ID);
            TransactionResponse transactionResponse = api.PostTransactions(transactionRequest);

            System.Console.WriteLine("Transaction created! ID" + transactionResponse.Object.Id);
            System.Console.WriteLine(transactionResponse.Object);
            return transactionResponse;
        }
        catch (ApiException e)
        {
            if (e.IsValidationError)
            {
                TransactionResponse transactionResponse = e.ParseObject<TransactionResponse>();
                System.Console.WriteLine("Validation Error" + transactionResponse.Object.Errors);
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(transactionResponse.Object.Errors);
                //mtsmethods.InsertActivityLogDetails("Create Transaction Error Response parameters: <br/> " + json + "", t.User_ID, t.Transaction_ID, t.User_ID, t.Customer_ID, "Proceed Transaction", t.CB_ID, t.Client_ID);

                return transactionResponse;
            }
            else
            {
                //mtsmethods.InsertActivityLogDetails("Create Transaction Error Response parameters: <br/> " + e.ToString() + "", t.User_ID, t.Transaction_ID, t.User_ID, t.Customer_ID, "Proceed Transaction", t.CB_ID, t.Client_ID);
                throw e;
                //return null;
            }

        }
    }



    public static string GenerateHashedData(string referenceNumber, string accountReference, string callbackUrl, string hmac)//pradip
    {
        try
        {
            string[] hashParams = { referenceNumber, accountReference, callbackUrl };
            StringBuilder hashData = new StringBuilder();

            foreach (string param in hashParams)
            {
                hashData.Append(param ?? string.Empty);
            }
            hashData.Append(hmac);

            using (SHA512 sha512 = SHA512.Create())
            {
                byte[] hashBytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(hashData.ToString()));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
        catch
        {
            return string.Empty;
        }
    }
    public static string GenerateJsonData(
      string accountReference, decimal amountInGBP, string phoneNumber1, string bankId1,
      string fullName, string emailId, string beneficiaryName, string accountNumber,
      string currentDateTimeUTC, string callBackUrl1)
    {
        var requestBody = new
        {
            referenceNumber = accountReference,
            amount = Convert.ToInt32(amountInGBP),
            currency = "NGN",
            payer = new
            {
                name = fullName,
                email = emailId,
                bankId = bankId1
            },
            payee = new
            {
                name = beneficiaryName,
                financialIdentificationNumber = accountNumber
            },
            expiryDateTimeUTC = currentDateTimeUTC,
            isSuppressMessages = false,
            payerCollectionFeeShare = 1.0,
            payeeCollectionFeeShare = 0.0,
            isAllowPartialPayments = false,
            isAllowOverPayments = false,
            callBackUrl = callBackUrl1,
            paymentMethods = new string[] { "BANK_TRANSFER", "FUNDING_USSD", "REQUEST_MONEY" },
            displayBankDetailToPayer = false
        };

        return JsonConvert.SerializeObject(requestBody);
    }

    public static string GenerateHashedData2(string jsonData, string hmac)
    {
        try
        {
            // Parse JSON Data
            JObject data = JObject.Parse(jsonData);

            // Extract values based on the expected keys
            string referenceNumber = data["referenceNumber"]?.ToString() ?? string.Empty;
            string amount = data["amount"]?.ToString() ?? string.Empty;
            string currency = data["currency"]?.ToString() ?? string.Empty;
            string payerPhoneNumber = data.SelectToken("payer.phoneNumber")?.ToString() ?? string.Empty;
            string payerEmail = data.SelectToken("payer.email")?.ToString() ?? string.Empty;
            string payeePhoneNumber = data.SelectToken("payee.phoneNumber")?.ToString() ?? string.Empty;

            // Concatenate values in the required order
            StringBuilder hashData = new StringBuilder();
            hashData.Append(referenceNumber);
            hashData.Append(amount);
            hashData.Append(currency);
            hashData.Append(payerPhoneNumber);
            hashData.Append(payerEmail);
            hashData.Append(payeePhoneNumber);
            hashData.Append(hmac);

            // Generate SHA-512 Hash
            using (SHA512 sha512 = SHA512.Create())
            {
                byte[] hashBytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(hashData.ToString()));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error generating hash: " + ex.Message);
            return string.Empty;
        }
    }
    public static string GenerateHashedData3(string jsonData, string hmacKey)
    {
        try
        {
            // Define the parameters to hash
            string[] hashParams = { "referenceNumber" };

            // Parse the JSON string
            JObject data = JObject.Parse(jsonData);
            StringBuilder hashData = new StringBuilder();

            // Extract values based on hashParams
            foreach (string param in hashParams)
            {
                string[] keys = param.Split('.');
                JToken node = data;

                foreach (string key in keys)
                {
                    node = node?[key];
                    if (node == null) break;
                }

                hashData.Append(node?.ToString() ?? "");
            }

            // Append HMAC key
            hashData.Append(hmacKey);

            // Compute SHA512 hash
            using (SHA512 sha512 = SHA512.Create())
            {
                byte[] hashBytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(hashData.ToString()));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
        catch
        {
            return string.Empty;
        }
    }

    public string GetsecurityQuestionAnswer(string RefNo)//pradip
    {
        string securityQuestionAnswer = "";
        try
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            char[] randomCode = new char[8];
            Random random = new Random();
            for (int i = 0; i < 8; i++)
            {
                randomCode[i] = chars[random.Next(chars.Length)];
            }
            securityQuestionAnswer = new string(randomCode);
        }
        catch (Exception ex)
        {
            securityQuestionAnswer = "";
        }
        return securityQuestionAnswer;
    }


    public string Encrypt(string plainText)
    {

        byte[] Key = System.Text.Encoding.ASCII.GetBytes("XMlkfg2845acGTbvdr270FGHBfghjkdc");

        byte[] IV = System.Text.Encoding.ASCII.GetBytes("HQreTFgdtm1485rt");




        if (plainText.Trim() == null || plainText.Trim().Length <= 0)
            throw new ArgumentNullException("plainText");
        if (Key == null || Key.Length <= 0)
            throw new ArgumentNullException("Key");
        byte[] encrypted;
        // Create an RijndaelManaged object
        // with the specified key and IV.  
        using (RijndaelManaged rijAlg = new RijndaelManaged())
        {
            //rijAlg.BlockSize = 256;
            //rijAlg.KeySize = 32;
            rijAlg.Mode = CipherMode.CBC;
            rijAlg.Padding = PaddingMode.Zeros;
            rijAlg.Key = Key;
            rijAlg.IV = IV; // Key ' Convert.FromBase64String("9532654BD781547023AB4FA7723F2FCD")

            // Create a decrytor to perform the stream transform.
            ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

            // Create the streams used for encryption.
            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {

                        // Write all data to the stream.
                        swEncrypt.Write(plainText.TrimStart().TrimEnd());
                    }
                    encrypted = msEncrypt.ToArray();
                }
            }
        }
        // Return the encrypted bytes from the memory stream.
        return Convert.ToBase64String(encrypted).TrimStart().TrimEnd();
    }
    public static string ComputeSha256Hash(string rawData)
    {
        // Create a SHA256   
        using (System.Security.Cryptography.SHA256 sha256Hash = System.Security.Cryptography.SHA256.Create())
        {
            // ComputeHash - returns byte array
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

            // Convert byte array to a string
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
    public static string Gcc_getAvailableBalance(string apiurl, string apipass, string accesscode, string apiuser)
    {
        var options = new RestClientOptions(apiurl)
        {
            MaxTimeout = -1
        };
        var clientAvailableBalance = new RestClient(options);
        var requestAvailableBalance = new RestRequest()
        {
            Method = Method.Post
        };



        //var clientAvailableBalance = new RestClient(apiurl);
        //clientAvailableBalance.Timeout = -1;
        //var requestAvailableBalance = new RestRequest(Method.POST);
        //var requestAvailableBalance = new RestRequest();
        requestAvailableBalance.AddHeader("Content-Type", "text/xml; charset=utf-8");
        requestAvailableBalance.AddHeader("SOAPAction", "http://tempuri.org/ISendAPI/GetAvailableBalance");
        var bodyAvailableBalance = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:tem=""http://tempuri.org/"" xmlns:grem=""http://schemas.datacontract.org/2004/07/GRemitWCFService.Send"">
   " + "\n" +
@"   <soapenv:Header/>
   " + "\n" +
@"   <soapenv:Body>
   " + "\n" +
@"      <tem:GetAvailableBalance>
   " + "\n" +
@"         <tem:req>
   " + "\n" +
@"            <grem:Password>" + apipass + "</grem:Password>" + "\n" +
@"            <grem:SecurityKey>" + accesscode + "</grem:SecurityKey>" + "\n" +
@"            <grem:UniqueID>" + apiuser + "</grem:UniqueID>" + "\n" +
@"         </tem:req>
   " + "\n" +
@"      </tem:GetAvailableBalance>
   " + "\n" +
@"   </soapenv:Body>
   " + "\n" +
@"</soapenv:Envelope>";
        requestAvailableBalance.AddParameter("text/xml", bodyAvailableBalance, ParameterType.RequestBody);
        //mtsmethods.InsertActivityLogDetails("Gcc_getAvailableBalance request parameter : <br/>" + bodyAvailableBalance + "", 0, 0, 0, 0, "Gcc_getAvailableBalance", 0, 0);
        RestResponse responseAvailableBalance = clientAvailableBalance.Execute(requestAvailableBalance);
        //mtsmethods.InsertActivityLogDetails("Gcc_getAvailableBalance responce parameter : <br/>" + responseAvailableBalance.Content + "", 0, 0, 0, 0, "Gcc_getAvailableBalance", 0, 0);

        XmlDocument xmlDocAvailableBalance = new XmlDocument();
        xmlDocAvailableBalance.LoadXml(responseAvailableBalance.Content);

        string availableBalanceAmount = "", availableBalanceCurrencyCode = "", availableBalanceDate = "";
        string availableBalanceTime = "", responseCode = "", responseMessage = "", successful = "";

        XmlNodeList nodeListAvailableBalance = xmlDocAvailableBalance.GetElementsByTagName("GetAvailableBalanceResult");
        foreach (XmlNode node in nodeListAvailableBalance)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeXmlNode(node);
            var obj = Newtonsoft.Json.Linq.JObject.Parse(json);

            availableBalanceAmount = Convert.ToString(obj["GetAvailableBalanceResult"]["a:AvailableBalanceAmount"]).Trim();
            availableBalanceCurrencyCode = Convert.ToString(obj["GetAvailableBalanceResult"]["a:AvailableBalanceCurrencyCode"]).Trim();
            availableBalanceDate = Convert.ToString(obj["GetAvailableBalanceResult"]["a:AvailableBalanceDate"]).Trim();
            availableBalanceTime = Convert.ToString(obj["GetAvailableBalanceResult"]["a:AvailableBalanceTime"]).Trim();
            responseCode = Convert.ToString(obj["GetAvailableBalanceResult"]["a:ResponseCode"]).Trim();
            responseMessage = Convert.ToString(obj["GetAvailableBalanceResult"]["a:ResponseMessage"]).Trim();
            successful = Convert.ToString(obj["GetAvailableBalanceResult"]["a:Successful"]).Trim();
        }
        string message = "";
        try
        {
            if (Convert.ToDouble(availableBalanceAmount) >= 0)
            {
                message = availableBalanceAmount;
            }
            else { message = "0"; }
        }
        catch (Exception ex)
        {
            message = "0";
            //mtsmethods.InsertActivityLogDetails("Gcc_getAvailableBalance validate error : <br/>" + ex.ToString().Replace("'", "\"") + "", 0, 0, 0, 0, "Gcc_getAvailableBalance", 0, 0);
        }
        return message;
    }
    public dynamic providusBalance(string username, string password, string API_URL, string debitAccountNo, string requestLink)
    {
        dynamic Json = null;
        RestResponse response = null;
        try
        {
            string responseCode = "";
            //username = Convert.ToString(dtt.Rows[0]["APIUser_ID"]);
            //password = Convert.ToString(dtt.Rows[0]["Password"]);

            var options = new RestClientOptions(API_URL)
            {
                MaxTimeout = -1
            };
            var client = new RestClient(options);
            var request = new RestRequest(requestLink)
            {
                Method = Method.Post
            };


            //var client = new RestClient(API_URL);
            //var request = new RestRequest(requestLink);
            //request.Method = Method.POST;
            request.AddHeader("Content-Type", "application/xml");
            var body = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:prov=""http://providus.com/"">
                    " + "\n" +
                @"   <soapenv:Header/>
                    " + "\n" +
                @"   <soapenv:Body>
                    " + "\n" +
                @"      <prov:GetProvidusAccount>
                    " + "\n" +
                @"         <!--Optional:-->
                    " + "\n" +
                @"         <account_number>" + debitAccountNo + "</account_number>" + "\n" +
                @"         <!--Optional:-->
                    " + "\n" +
                @"         <username>" + username + "</username>" + "\n" +
                @"         <!--Optional:-->
                    " + "\n" +
                @"         <password>" + password + "</password>" + "\n" +
                @"      </prov:GetProvidusAccount>
                    " + "\n" +
                @"   </soapenv:Body>
                    " + "\n" +
                @"</soapenv:Envelope>";
            request.AddParameter("text/xml", body, ParameterType.RequestBody);
            //mtsmethods.InsertActivityLogDetails("Backofc providusBalance GetProvidusAccount Balance request: <br/>" + body + "", t.User_ID, t.Transaction_ID, t.User_ID, t.Customer_ID, "providusBalance", t.CB_ID, t.Client_ID);
            RestResponse response1 = client.Execute(request);
            //mtsmethods.InsertActivityLogDetails("Backofc Providus response parameters GetProvidusAccount: <br/>" + response1.Content + "", t.User_ID, t.Transaction_ID, t.User_ID, t.Customer_ID, "providusBalance", t.CB_ID, t.Client_ID);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(response1.Content);
            XmlNodeList nodeList = doc.GetElementsByTagName("return");
            string availableBalance = "";
            foreach (XmlNode node in nodeList)
            {
                string jsonContent = node.InnerText;

                try
                {
                    JObject jsonObject = JObject.Parse(jsonContent);

                    responseCode = (string)jsonObject["responseCode"];
                    availableBalance = (string)jsonObject["availableBalance"];

                    if (responseCode == "00")
                    {
                        return jsonObject;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error parsing JSON: " + ex.Message);
                }
            }

        }
        catch (Exception ex)
        {
            // mtsmethods.InsertActivityLogDetails("providusBalance Balance error : <br/>" + ex.ToString().Replace("'", "\"") + "", t.User_ID, t.Transaction_ID, t.User_ID, t.Customer_ID, "providusBalance", t.CB_ID, t.Client_ID);
        }
        return Json;
    }

    public static string createSessionid()
    {
        string chars = "0123456789";
        StringBuilder pass = new StringBuilder();
        Random ran = new Random();
        for (int i = 0; i < 9; i++)
        {
            pass.Append(chars[ran.Next(0, chars.Length)]);

        }
        return pass.ToString();
    }

    public DataTable ConvertDynamicResultToDataTable(dynamic result)
    {
        DataTable dt = new DataTable();
        try
        {
            if (result != null)
            {
                var dict = (IDictionary<string, object>)result;
                Dictionary<string, string> finalColumnNames = new Dictionary<string, string>();

                // Create unique column names
                foreach (var key in dict.Keys)
                {
                    string columnName = key;
                    int counter = 1;

                    // Ensure column name uniqueness
                    while (dt.Columns.Contains(columnName))
                    {
                        columnName = key + counter;
                        counter++;
                    }
                    dt.Columns.Add(columnName, dict[key]?.GetType() ?? typeof(object));
                    finalColumnNames[key] = columnName;
                }
                // Add row with matching columns
                DataRow row = dt.NewRow();
                foreach (var kvp in dict)
                {
                    string columnName = finalColumnNames[kvp.Key];
                    row[columnName] = kvp.Value ?? DBNull.Value;
                }
                dt.Rows.Add(row);
            }
        }
        catch (Exception)
        {
            dt = null;
        }
        return dt;
    }


    public async Task<string> GenerateWalletReferenceAsync(int clientId)
    {
        string refNo = string.Empty;

        try
        {
            int size = 8;
            bool isUnique = false;
            Random rng = new Random(Environment.TickCount);

            while (!isUnique)
            {
                // Step 1: Get Initial Characters
                var initialParams = new { _Client_ID = clientId };
                string initialChars = await _dbConnection.QueryFirstOrDefaultAsync<string>("getWalRef_InitialChar",initialParams,commandType: CommandType.StoredProcedure);

                // Step 2: Generate reference number
                refNo = initialChars + string.Concat(Enumerable.Range(0, size).Select(i => rng.Next(10).ToString()));

                // Step 3: Check for duplicates
                var checkParams = new{_Client_ID = clientId,_Discount_Code = refNo};
                var ds = await _dbConnection.QueryAsync<string>("CheckDuplicateWalletReference",checkParams,commandType: CommandType.StoredProcedure);
                if (!ds.Any()) {isUnique = true; }
            }
        }
        catch (Exception)
        {
            refNo = string.Empty;
        }

        return refNo;
    }

    public async Task<string> CreateWalletInOurSideAsync(dynamic obj, string referenceNumber, double availableBalance, int customerId, int walletId,int bankAccountId)
    {
        string walletStatus = string.Empty;

        try
        {
            // Generate wallet reference
            string wrefNo = await GenerateWalletReferenceAsync(obj.Client_ID);

            if (string.IsNullOrEmpty(wrefNo))
            {
                //throw new Exception("Failed to generate wallet reference.");
                walletStatus = "Error";
                await SaveErrorLogAsync(
                    "Error In Create Wallet insertinwallet_table - ",
                    DateTime.Now,
                    "CreateWalletInOurSideAsync",
                    obj.User_ID,
                    obj.Branch_ID,
                    obj.Client_ID,
                    0
                );
            }

            // Prepare parameters for SP
            var walletParams = new
            {
                _WireTransfer_ReferanceNo = referenceNumber,
                _Currency_ID = obj.Currency_Id,
                _Delete_Status = 0,
                _Record_Insert_DateTime = DateTime.Now,
                _wallet_reference = wrefNo,
                _Wallet_balance = availableBalance,
                _Client_ID = obj.Client_ID,
                _Branch_ID = obj.Branch_ID,
                _Customer_ID = customerId,
                _AgentFlag = 1,
                _Wallet_API_ID = walletId
            };

            // Execute insert
            var rowsAffected = await _dbConnection.ExecuteAsync(
                "insertinwallet_table",
                walletParams,
                commandType: CommandType.StoredProcedure
            );

            walletStatus = rowsAffected > 0 ? "Created" : "Not Created";
           
            if (walletStatus == "Created")
            {
                var getParams = new
                {
                    _WireTransfer_ReferanceNo = referenceNumber,
                    _Currency_ID = Convert.ToInt32(obj.Currency_Id),
                    _Client_ID = Convert.ToInt32(obj.Client_ID)
                };

                var result = await _dbConnection.QueryFirstOrDefaultAsync<dynamic>(
                    "getwalletid",
                    getParams,
                    commandType: CommandType.StoredProcedure
                );

                int System_Wallet_Id = (result != null && result.Wallet_ID != null)? Convert.ToInt32(result.Wallet_ID) : 0;


                #region  update_System_Wallet_Id
                try
                {
                    if (bankAccountId > 0)
                    {
                        var updateParams = new
                        {
                            _Bank_Account_ID = bankAccountId,
                            _System_Wallet_Id = System_Wallet_Id
                        };

                        var rowsAffectedupdate = await _dbConnection.ExecuteAsync(
                            "update_System_Wallet_Id",
                            updateParams,
                            commandType: CommandType.StoredProcedure
                        );
                    }
                }
                catch (Exception ex)
                {
                    await SaveErrorLogAsync(" update_System_Wallet_Id Error:" + ex.ToString(), DateTime.Now, "CreateWalletInOurSideAsync", obj.User_ID, obj.Branch_ID, obj.Client_ID, 0);
                }
                #endregion  update_System_Wallet_Id

                // START this block use to set defaoult wallet transaction and convertion limit

                #region transaction limit set
                try
                {

                    var storedProcedureName = "Add_default_limit_App";
                    var parameters1lmt = new DynamicParameters();
                    parameters1lmt.Add("_Customer_id", Convert.ToInt32(customerId));
                    parameters1lmt.Add("_basecurrency_id", Convert.ToInt32(obj.Currency_Id));
                    parameters1lmt.Add("_client_id", obj.Client_ID);
                    parameters1lmt.Add("_RecordDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    parameters1lmt.Add("_branch_id", obj.Branch_ID);
                    int msg1limit1 = await _dbConnection.ExecuteAsync(
                        storedProcedureName,
                        parameters1lmt,
                        commandType: CommandType.StoredProcedure
                    );

                    await SaveErrorLogAsync(" Add_default_limit_App  Successfully add limit :" + msg1limit1, DateTime.Now, "CreateWalletInOurSideAsync", obj.User_ID, obj.Branch_ID, obj.Client_ID, 0);

                }
                catch (Exception ex)
                {
                    await SaveErrorLogAsync(" Add_default_limit_App Error:" + ex.ToString(), DateTime.Now, "CreateWalletInOurSideAsync", obj.User_ID, obj.Branch_ID, obj.Client_ID, 0);
                }
                #endregion transaction limit set
               
                #region convertion limit set
                 
                try
                {
                    if (!string.IsNullOrEmpty(wrefNo))
                    {
                        var limitParams = new
                        {
                            _Customer_ID = obj.Customer_ID,
                            _Wallet_ID = System_Wallet_Id,
                            _WalletReference = wrefNo,
                            _Basecurrency_id = Convert.ToInt32(obj.Currency_Id),
                            _Client_ID = obj.Client_ID,
                            _Branch_ID = obj.Branch_ID,
                            _RecordDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        };

                        int res_limits = await _dbConnection.ExecuteAsync(
                            "Add_Limits_For_Conversion_Wallet",
                            limitParams,
                            commandType: CommandType.StoredProcedure
                        );

                        if (res_limits > 0)
                        {
                            await SaveActivityLogTracker("BETA-Paga Conversion limit added successfully.: <br/>", 0, DateTime.Now, 0, "0", 0, 0, "BETA-Paga CreateWalletInOurSideAsync", obj.Branch_ID, obj.Client_ID);
                        }
                    }
                }
                catch (Exception ex)
                {
                 await SaveErrorLogAsync(
                 "Error in Adding limits for conversion wallet during customer creation: " + ex.ToString(),
                 DateTime.Now,
                 "CreateWalletInOurSideAsync",
                 obj.User_ID,
                 obj.Branch_ID,
                 obj.Client_ID,
                 0
                  );
                }

                #endregion convertion limit set
                // END this block use to set defaoult wallet transaction and convertin limit
            }
            else
            {
                await SaveErrorLogAsync(
                    "Error In Create Wallet insertinwallet_table - No rows affected",
                    DateTime.Now,
                    "CreateWalletInOurSideAsync",
                    obj.User_ID,
                    obj.Branch_ID,
                    obj.Client_ID,
                    0
                );
            }
        }
        catch (Exception ex)
        {
            walletStatus = "Error";
            await SaveErrorLogAsync(
                "Error In Create Wallet insertinwallet_table - " + ex.ToString(),
                DateTime.Now,
                "CreateWalletInOurSideAsync",
                obj.User_ID,
                obj.Branch_ID,
                obj.Client_ID,
                0
            );
        }

        return walletStatus;
    }

    //public static string AuthToken(string apiuser, string accesscode, string authbaseurl)
    //{
    //    string token = "";
    //    string awsClientId = apiuser;
    //    string awsSecret = accesscode;

    //    ServicePointManager.Expect100Continue = true;
    //    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
    //           | SecurityProtocolType.Tls11
    //       | SecurityProtocolType.Tls12;

    //    var plainTextBytes = Encoding.UTF8.GetBytes($"{awsClientId}:{awsSecret}");
    //    string authorizationheader = Convert.ToBase64String(plainTextBytes);

    //    var options = new RestClientOptions(authbaseurl)
    //    {
    //        MaxTimeout = -1
    //    };
    //    var client = new RestClient(options);
    //    var request = new RestRequest()
    //    {
    //        Method = Method.Post
    //    };
    //    request.AddHeader("Content-type", "application/x-www-form-urlencoded");
    //    request.AddHeader("Authorization", $"Basic {authorizationheader}");
    //    string paycellerClientId = getclientidPayceller();
    //    if (paycellerClientId != "")
    //    {
    //        request.AddHeader("paycelerclientid", paycellerClientId);
    //    }
    //    request.Resource = "/oauth2/token";
    //    request.AddParameter("client_id", awsClientId, ParameterType.GetOrPost);
    //    request.AddParameter("grant_type", "client_credentials", ParameterType.GetOrPost);

    //    var response = client.Execute(request);
    //    if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.BadRequest || response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
    //    {
    //        kmoney json = Newtonsoft.Json.JsonConvert.DeserializeObject<kmoney>(response.Content);
    //        token = json.access_token;
    //    }
    //    return token;
    //}

    //private readonly AppSettings _appSettings;

    //public ProceedRepository(IDbConnection dbConnection, IOptions<AppSettings> appSettings) : base(dbConnection)
    //{
    //    _appSettings = appSettings.Value;
    //}

    //public static string getclientidPayceller()
    //{
    //    string paycellerClientId = "";
    //    try
    //    {
    //        //string Query = " select * from api_master where ID = 14 ";
    //        //MySqlCommand cmd5 = new MySqlCommand(Query);
    //        // DataTable dtt = dbconnection.ExecuteQueryDataTableProcedure(cmd5);
    //        string whereclause = "and a.ID = 14";
    //        string? SecurityKey = _appSettings.SecurityKey;
    //        var storedProcedureName = "Get_APIDetails";
    //        var values = new
    //        {
    //            _whereclause = whereclause,
    //            _security_key = SecurityKey
    //        };

    //        //var apidetails = await _dbConnection.QueryAsync(storedProcedureName, values, commandType: CommandType.StoredProcedure);
    //        dynamic dtt = apidetails.FirstOrDefault();

    //        if (dtt.Rows.Count > 0)
    //        {
    //            string api_fields = Convert.ToString(dtt.Rows[0]["api_Fields"]);

    //            if (api_fields != "" && api_fields != null)
    //            {
    //                Newtonsoft.Json.Linq.JObject obj = Newtonsoft.Json.Linq.JObject.Parse(api_fields);
    //                paycellerClientId = Convert.ToString(obj["clientid"]);
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        // mtsmethods.InsertActivityLogDetails("Backoffice Payceler Client Id error: <br/>" + ex.ToString().Replace("'", "\"") + "", 0, 0, 0, 0, "getclientidPayceller", 0, 0);
    //    }
    //    return paycellerClientId;
    //}

    public string GenerateRandomNumber(int size)
    {
        if (size <= 0)
            throw new ArgumentException("Size must be greater than 0.");

        Random rnd = new Random();

        // Generates a number with leading zeros if necessary
        string result = "";
        for (int i = 0; i < size; i++)
        {
            result += rnd.Next(0, 10);
        }
        return result;
    }
    public string NormalizeMsisdn(string msisdn)
    {
        if (string.IsNullOrEmpty(msisdn))
            return msisdn;

        if (msisdn.StartsWith("0"))
        {
            return msisdn;
        }
        else if (msisdn.StartsWith("92"))
        {
            return "0" + msisdn.Substring(2);
        }
        else if (msisdn.Length >= 10)
        {
            return "0" + msisdn;
        }

        return msisdn;
    }

    public bool GetApiWalletBankAccountDetails(string securityKey, string whereClause)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("_security_key", securityKey);
            parameters.Add("_where_clause", string.IsNullOrEmpty(whereClause) ? null : whereClause);

            var result = _dbConnection.Query<dynamic>(
                "Get_ApiWalletBankAccountDetails",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result != null && result.Any();
        }
        catch (Exception ex)
        {
            SaveErrorLogAsync(whereClause + "whereClause Error in GetApiWalletBankAccountDetails: " + ex.ToString(),DateTime.Now,"GetApiWalletBankAccountDetails",0, 0, 0, 0);
            return false;            
        }
    }

    public static string GenerateHmacAuthorizationHeader(string httpMethod, string url, string appId, string apiKey, string requestBody)
    {
        // Step 1: Normalize inputs
        httpMethod = httpMethod.ToUpperInvariant();
        string requestUri = HttpUtility.UrlEncode(url.ToLowerInvariant());

        // Step 2: Generate Nonce and Timestamp
        string nonce = Guid.NewGuid().ToString("N");
        string timestamp = ((int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds).ToString();

        // Step 3: Hash request body (if present) with MD5 and convert to Base64
        string contentHashBase64 = string.Empty;
        if (!string.IsNullOrEmpty(requestBody))
        {
            byte[] contentBytes = Encoding.UTF8.GetBytes(requestBody);
            using (MD5 md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(contentBytes);
                contentHashBase64 = Convert.ToBase64String(hashBytes);
            }
        }

        // Step 4: Create the raw signature string (no delimiters)
        string signatureRawData = string.Concat(appId, httpMethod, requestUri, timestamp, nonce, contentHashBase64);

        // Step 5: Compute HMACSHA256 with API Key (convert to Base64 first)
        byte[] secretKeyBytes = Convert.FromBase64String(apiKey);
        byte[] signatureBytes;
        using (HMACSHA256 hmac = new HMACSHA256(secretKeyBytes))
        {
            byte[] rawSignatureBytes = Encoding.UTF8.GetBytes(signatureRawData);
            signatureBytes = hmac.ComputeHash(rawSignatureBytes);
        }

        string signature = Convert.ToBase64String(signatureBytes);

        // Step 6: Construct authorization header
        string authorizationHeader = $"hmacauth {appId}:{signature}:{nonce}:{timestamp}";


        return authorizationHeader;
    }
}