using Microsoft.AspNetCore.Mvc;

namespace RateGate.Api.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                status =  "ok",
                service = "RateGate.Api"
            });
        }
    }
}