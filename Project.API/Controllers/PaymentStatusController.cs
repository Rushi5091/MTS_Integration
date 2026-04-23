using Microsoft.AspNetCore.Mvc;
using Project.Core.Entities.Business;
using Project.Core.Interfaces.IServices;

namespace Project.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentStatusController : ControllerBase
    {
        private readonly ILogger<PaymentStatusController> _logger;
        private readonly IPaymentStatusService _PaymentStatusService;

        public PaymentStatusController(ILogger<PaymentStatusController> logger, IPaymentStatusService PaymentStatusService)
        {
            _logger = logger;
            _PaymentStatusService = PaymentStatusService;
        }



        [HttpPost]
        public async Task<IActionResult> PaymentStatus(PaymentStatusViewModel model)
        {
            if (ModelState.IsValid)
            {
                string message = "";

                try
                {
                    var data = await _PaymentStatusService.PaymentStatus(model);
                    return Ok(data);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"An error occurred while Getting the Payment Status of the transaction");
                    message = $"An error occurred while Getting the Payment Status of the transaction- {ex.Message}";

                    return StatusCode(StatusCodes.Status500InternalServerError, message);
                }



            }
            return StatusCode(StatusCodes.Status400BadRequest, "Please input all required data");
        }
    }
}
