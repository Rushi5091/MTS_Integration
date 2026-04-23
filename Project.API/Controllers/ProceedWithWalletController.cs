using Microsoft.AspNetCore.Mvc;
using Project.Core.Entities.Business;
using Project.Core.Interfaces.IServices;
using Project.Core.Services;
using System.Security.Cryptography;
using System.Text;

namespace Project.API.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ProceedWithWalletController : ControllerBase
    {

        private readonly ILogger<ProceedWithWalletController> _logger;
        private readonly IProceedWithWalletService _ProceedWithWalletService;
        private readonly IProceedWithWalletService _CreateNGNWalletService;
        private readonly IConfiguration _configuration;
        public ProceedWithWalletController(ILogger<ProceedWithWalletController> logger, IProceedWithWalletService ProceedWithWalletService, IProceedWithWalletService CreateNGNWalletService, IConfiguration configuration)
        {
            _logger = logger;
            _ProceedWithWalletService = ProceedWithWalletService;
            _CreateNGNWalletService = CreateNGNWalletService;
            _configuration = configuration;
        }



        [HttpPost("ProceedWithWallet")]
        public async Task<IActionResult> ProceedWithWallet(ProceedWithWalletViewModel model)
        {
            var providedKey = Request.Headers["X-Internal-Api-Key"].ToString();
            var timestamp = Request.Headers["X-Internal-Timestamp"].ToString();

            string keylog = $"Step 1 : providedKey: " + providedKey.ToString() + "and time stamp:" + timestamp.ToString();
            await _ProceedWithWalletService.LogMessage(keylog);

            if (!DateTime.TryParseExact(timestamp, "yyyyMMddHHmm",
                null, System.Globalization.DateTimeStyles.None, out DateTime requestTime))
            {
                return Unauthorized(new { message = "Invalid or missing timestamp." });
            }
            if (Math.Abs((DateTime.UtcNow - requestTime).TotalMinutes) > 5)
            {
                return Unauthorized(new { message = "Request expired. Possible replay attack." });
            }

            string sharedSecret = _configuration["AppSettings:SecurityKey"];// sTORE AS ENV VARIABLE

            string stringToHash = model.TransactionRef.ToString() + model.AmountInGBP.ToString() + model.AmountInPKR.ToString() + model.FromCurrency_Code.ToString() + model.ToCurrency_Code.ToString()
                                  + model.Customer_ID.ToString() + model.Beneficiary_ID.ToString() + model.BeneficiaryName.ToString() + model.PaymentType_ID.ToString() + model.Client_ID.ToString() +
                                   model.Branch_ID.ToString() + model.User_ID.ToString() + model.Transaction_ID.ToString()
                                    + timestamp         // same timestamp from header
                                 + sharedSecret;

            await _ProceedWithWalletService.LogMessage("Step 2" + stringToHash);

            string expectedKey;
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(stringToHash));
                expectedKey = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }

            await _ProceedWithWalletService.LogMessage("Step 3 Provided Key " + providedKey);
            await _ProceedWithWalletService.LogMessage("Step 4 expectedKey " + expectedKey);

            if (string.IsNullOrEmpty(providedKey) || (providedKey != expectedKey))
            {
                _logger.LogWarning("Invalid API key attempt at {Time} for Customer {ID}", DateTime.UtcNow, model.Customer_ID);
                return Unauthorized(new { message = "Access denied." });
            }

            if (ModelState.IsValid)
            {
                string message = "";
                //if (await _ProceedWithWallateService.IsExists("Customer_ID", model.Customer_ID))
                //{
                    try
                    {
                        var data = await _ProceedWithWalletService.ProceedWithWallet(model);
                        return Ok(data);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"An error occurred while ProceedWithWallet");
                        message = $"An error occurred while ProceedWithWallet- {ex.Message}";

                        return StatusCode(StatusCodes.Status500InternalServerError, message);
                    }
                //}
                //else
                //{
                //    message = $"The customer Transaction_ID- '{model.Customer_ID}' already exists";
                //    return StatusCode(StatusCodes.Status400BadRequest, message);
                //}

            }
            return StatusCode(StatusCodes.Status400BadRequest, "Please input all required data");
        }



        [HttpPost("CreateNGNWallet")]
        public async Task<IActionResult> CreateNGNWallet(CreateNGNWalletViewModel model)
        {
            var providedKey = Request.Headers["X-Internal-Api-Key"].ToString();
            var timestamp = Request.Headers["X-Internal-Timestamp"].ToString();

            string keylog = $"Step 1 : providedKey: " + providedKey.ToString()  + "and time stamp:" + timestamp.ToString();
             await _ProceedWithWalletService.LogMessage(keylog);

            if (!DateTime.TryParseExact(timestamp, "yyyyMMddHHmm",
                null, System.Globalization.DateTimeStyles.None, out DateTime requestTime))
            {
                return Unauthorized(new { message = "Invalid or missing timestamp." });
            }
            if (Math.Abs((DateTime.UtcNow - requestTime).TotalMinutes) > 5)
            {
                return Unauthorized(new { message = "Request expired. Possible replay attack." });
            }


            string sharedSecret = _configuration["AppSettings:SecurityKey"];// sTORE AS ENV VARIABLE
            string stringToHash = model.Customer_ID.ToString() + model.Branch_ID.ToString() + model.Client_ID.ToString() + model.Currency_ID.ToString()
                                + model.User_ID.ToString() + model.BankVerificationNumber.ToString()
                                + model.Wallet_Transaction_Reference.ToString()
                                 + timestamp         // same timestamp from header
                                 + sharedSecret;     // same secret from appsettings;

            await _ProceedWithWalletService.LogMessage("Step 2" + stringToHash);


            string expectedKey;
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(stringToHash));
                expectedKey = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }

            await _ProceedWithWalletService.LogMessage("Step 3 Provided Key " + providedKey);
            await _ProceedWithWalletService.LogMessage("Step 4 expectedKey " + expectedKey);

            if (string.IsNullOrEmpty(providedKey) || (providedKey != expectedKey))
            {
                _logger.LogWarning("Invalid API key attempt at {Time} for Customer {ID}", DateTime.UtcNow, model.Customer_ID);
                return Unauthorized(new { message = "Access denied." });
            }
            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _CreateNGNWalletService.CreateNGNWallet(model);
                    return Ok(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"An error occurred while CreateNGNWallet");
                    return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
                }
            }
            return BadRequest("Invalid input data");
        }
    } 

}
