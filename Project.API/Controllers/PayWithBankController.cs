using Microsoft.AspNetCore.Mvc;
using Project.Core.Entities.Business;
using Project.Core.Interfaces.IServices;

namespace Project.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayWithBankController : ControllerBase
    {
        private readonly ILogger<PayWithBankController> _logger;
        private readonly IPayWithBankService _paywithbankService;

        public PayWithBankController(ILogger<PayWithBankController> logger, IPayWithBankService paywithbankService)
        {
            _logger = logger;
            _paywithbankService = paywithbankService;
        }



        [HttpPost]
        public async Task<IActionResult> PayWithBank(PayWithBankViewModel model)
        {
            if (ModelState.IsValid)
            {
                string message = "";
              
                    try
                    {
                        var data = await _paywithbankService.PayWithBank(model);
                        return Ok(data);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"An error occurred while Pay With Bank transaction");
                        message = $"An error occurred while Pay With Bank transaction- {ex.Message}";

                        return StatusCode(StatusCodes.Status500InternalServerError, message);
                    }

               

            }
            return StatusCode(StatusCodes.Status400BadRequest, "Please input all required data");
        }
    }
}
