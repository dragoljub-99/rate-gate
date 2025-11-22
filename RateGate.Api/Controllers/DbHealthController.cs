using Microsoft.AspNetCore.Mvc;
using RateGate.Infrastructure.Data;

namespace RateGate.Api.Controllers
{
    [ApiController]
    [Route("health/db")]
    public class DbHealthController : ControllerBase
    {
        private readonly RateGateDbContext _dbContext;

        public DbHealthController(RateGateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var canConnect = await _dbContext.Database.CanConnectAsync();

                return Ok(new
                {
                    status = canConnect ? "ok" : "error",
                    canConnect
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "error",
                    message = ex.Message
                });
            }
        }
    }
}