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
        public async Task<IActionResult> Open(int numberOfIllegelOpenning)
        {
            VariableService.numberOfOpenning += 1;
            VariableService.numberOfIlliegelOpenning += numberOfIllegelOpenning;
            return Ok();
        }
        [HttpGet]
        public async Task<IActionResult> getOpenningsNumber()
        {
            string html = $@"
            <html>
                <body>
                    <p>Number Of paid door open : {VariableService.numberOfOpenning}</p>
                    <p>Number Of unpaid door openning: {VariableService.numberOfIlliegelOpenning}</p>
                </body>
            </html>";

            return Content(html, "text/html");
        }
    }
}
