using API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GateController : ControllerBase
    {
        [HttpGet("Open")]
        public async Task<IActionResult> Open()
        {
            VariableService.numberOfOpenning += 1;
            return Ok();
        }
        [HttpGet]
        public async Task<IActionResult> getOpenningsNumber()
        {
            return Ok(VariableService.numberOfOpenning);
        }
    }
}
