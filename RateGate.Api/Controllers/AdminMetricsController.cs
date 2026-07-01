using Microsoft.AspNetCore.Mvc;
using RateGate.Api.Models.Admin;
using RateGate.Api.Services.Admin;

namespace RateGate.Api.Controllers
{
    [ApiController]
    [Route("admin/metrics")]
    public class AdminMetricsController : ControllerBase
    {
        private readonly AdminMetricsService _adminMetricsService;

        public AdminMetricsController(
            AdminMetricsService adminMetricsService)
        {
            _adminMetricsService = adminMetricsService;
        }

        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<AdminUserMetricsDto>>>
            GetUsersMetrics(
                [FromQuery] int windowSeconds = 3600,
                CancellationToken cancellationToken = default)
        {
            if (windowSeconds <= 0)
            {
                return BadRequest(
                    "windowSeconds must be a positive integer.");
            }

            var metrics =
                await _adminMetricsService.GetUsersMetricsAsync(
                    windowSeconds,
                    cancellationToken);

            return Ok(metrics);
        }

        [HttpGet("users/{userId:int}")]
        public async Task<ActionResult<IEnumerable<AdminUserEndpointMetricsDto>>>
            GetUserEndpointMetrics(
                int userId,
                [FromQuery] int windowSeconds = 3600,
                CancellationToken cancellationToken = default)
        {
            if (windowSeconds <= 0)
            {
                return BadRequest(
                    "windowSeconds must be a positive integer.");
            }

            var metrics =
                await _adminMetricsService.GetUserEndpointMetricsAsync(
                    userId,
                    windowSeconds,
                    cancellationToken);

            if (metrics is null)
            {
                return NotFound();
            }

            return Ok(metrics);
        }
    }
}