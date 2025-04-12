using Microsoft.AspNetCore.Mvc;
using Project.Core.Entities.Business;
using Project.Core.Interfaces.IServices;
using Project.Core.Services;

namespace Project.API.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ProceedWithWalletController : ControllerBase
    {

        private readonly ILogger<ProceedWithWalletController> _logger;
        private readonly IProceedWithWalletService _ProceedWithWalletService;
        private readonly IProceedWithWalletService _CreateNGNWalletService;
        public ProceedWithWalletController(ILogger<ProceedWithWalletController> logger, IProceedWithWalletService ProceedWithWalletService, IProceedWithWalletService CreateNGNWalletService)
        {
            _logger = logger;
            _ProceedWithWalletService = ProceedWithWalletService;
            _CreateNGNWalletService= CreateNGNWalletService;
    }



        [HttpPost("ProceedWithWallet")]
        public async Task<IActionResult> ProceedWithWallet(ProceedWithWalletViewModel model)
        { 
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
