using Microsoft.AspNetCore.Mvc;
using Project.Core.Entities.Business;
using Project.Core.Interfaces.IServices;
using Project.Core.Services;

namespace Project.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CancelTransactionController : ControllerBase
    {
        private readonly ILogger<CancelTransactionController> _logger;
        private readonly ICancelTransactionService _CancelTransactionService;

        public CancelTransactionController(ILogger<CancelTransactionController> logger, ICancelTransactionService CancelTransactionService)
        {
            _logger = logger;
            _CancelTransactionService = CancelTransactionService;
        }



        [HttpPost]
        public async Task<IActionResult> CancelTransaction(CancelTransactionViewModel model)
        {
            if (ModelState.IsValid)
            {
                string message = "";
                if (await _CancelTransactionService.IsExists("Transaction_ID", model.Transaction_ID))
                {
                    try
                    {
                        var data = await _CancelTransactionService.CancelTransaction(model);
                        return Ok(data);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"An error occurred while CancelTransaction");
                        message = $"An error occurred while CancelTransaction - {ex.Message}";

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
