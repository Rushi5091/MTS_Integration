using Microsoft.AspNetCore.Mvc;
using Project.Core.Entities.Business;
using Project.Core.Entities.General;
using Project.Core.Interfaces.IServices;

namespace Project.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionStatusController : ControllerBase
    {
        private readonly ILogger<TransactionStatusController> _logger;
        private readonly ITransactionStatusService _transactionStatusService;

        public TransactionStatusController(ILogger<TransactionStatusController> logger, ITransactionStatusService transactionStatusService)
        {
            _logger = logger;
            _transactionStatusService = transactionStatusService;
        }



        [HttpPost]
        public async Task<IActionResult> TransactionStatus(TransactionStatusViewModel model)
        {
            if (ModelState.IsValid)
            {
                string message = "";
                if (await _transactionStatusService.IsExists("Transaction_ID", model.Transaction_ID))
                {
                    try
                    {
                        var data = await _transactionStatusService.TransactionStatus(model);
                        return Ok(data);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"An error occurred while checking TransactionStatus");
                        message = $"An error occurred while checking TransactionStatus- {ex.Message}";

                        return StatusCode(StatusCodes.Status500InternalServerError, message);
                    }

                }
                else
                {  
                    message = $"The customer Transaction_ID- '{model.Transaction_ID}' not exists in system";
                    return StatusCode(StatusCodes.Status400BadRequest, message);
                }

            }
            return StatusCode(StatusCodes.Status400BadRequest, "Please input all required data");
        }


    }
}
