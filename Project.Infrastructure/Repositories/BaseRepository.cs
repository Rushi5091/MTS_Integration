using Dapper;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using Project.Core.Interfaces.IRepositories;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

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
            _activity = activity,
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
            _Error = error,
            _Record_insert_Date_time = recordInsertDateTime,
            _Function_Name = functionName,
            _User_ID = userId,
            _Branch_ID = branchId,
            _Client_ID = clientId,
            _Delete_Status = deleteStatus
        };

        await _dbConnection.ExecuteAsync("sp_save_error_log", parameters, commandType: CommandType.StoredProcedure);
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
      string accountReference, decimal amountInGBP, string phoneNumber, string bankId,
      string fullName, string emailId, string beneficiaryName, string accountNumber,
      string currentDateTimeUTC, string callBackUrl)
    {
        var requestBody = new
        {
            referenceNumber = accountReference,
            amount = Convert.ToInt32(amountInGBP),
            currency = "NGN",
            payer = new
            {
                name = fullName,
                phoneNumber = "0" + phoneNumber,
                email = emailId,
                bankId = bankId
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
            callBackUrl = callBackUrl,
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

}