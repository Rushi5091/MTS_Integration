using Microsoft.AspNetCore.Mvc;
using Project.Core.Entities.Business;
using Project.Core.Entities.General;
using Project.Core.Interfaces.IServices;

namespace Project.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BranchListController : ControllerBase
    {
        private readonly ILogger<BranchListController> _logger;
        private readonly IBranchListService _branchListService;

        public BranchListController(ILogger<BranchListController> logger, IBranchListService branchListService)
        {
            _logger = logger;
            _branchListService = branchListService;
        }



        [HttpPost]
        public async Task<IActionResult> BranchList(BranchListViewModel model)
        {
            if (ModelState.IsValid)
            {
                string message = "";
                if (await _branchListService.IsExists("Transaction_ID", model.Transaction_ID))
                {
                    try
                    {
                        var data = await _branchListService.BranchList(model);
                        return Ok(data);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"An error occurred while getting BranchList");
                        message = $"An error occurred while getting BranchList- {ex.Message}";

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
