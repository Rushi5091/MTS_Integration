using Microsoft.AspNetCore.Mvc;
using Project.Core.Entities.Business;
using Project.Core.Interfaces.IServices;

namespace Project.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProceedController : ControllerBase
    {
        private readonly ILogger<ProceedController> _logger;
        private readonly IProceedService _proceedService;

        public ProceedController(ILogger<ProceedController> logger, IProceedService proceedService)
        {
            _logger = logger;
            _proceedService = proceedService;
        }



        [HttpPost]
        public async Task<IActionResult> Proceed(ProceedViewModel model)
        {
            if (ModelState.IsValid)
            {
                string message = "";
                if (await _proceedService.IsExists("Transaction_ID", model.Transaction_ID))
                {
                    try
                    {
                        var data = await _proceedService.Proceed(model);
                        return Ok(data);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"An error occurred while Proceed the transaction");
                        message = $"An error occurred while Proceed the transaction- {ex.Message}";

                        return StatusCode(StatusCodes.Status500InternalServerError, message);
                    }

                }
                else
                {
                    message = $"The customer Transaction_ID- '{model.Transaction_ID}' is not exists in system";
                    return StatusCode(StatusCodes.Status400BadRequest, message);
                }

            }
            return StatusCode(StatusCodes.Status400BadRequest, "Please input all required data");
        }
    }
}
