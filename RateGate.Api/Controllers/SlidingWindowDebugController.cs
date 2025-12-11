using Microsoft.AspNetCore.Mvc;
using RateGate.Api.Models;
using RateGate.Domain.RateLimiting;
using RateGate.Infrastructure.RateLimiting;

namespace RateGate.Api.Controllers
{
    [ApiController]
    [Route("debug/sliding")]
    public class SlidingWindowDebugController : ControllerBase
    {
        private readonly SlidingWindowLogRateLimiter _rateLimiter;

        public SlidingWindowDebugController(SlidingWindowLogRateLimiter rateLimiter)
        {
            _rateLimiter = rateLimiter;
        }
        
        [HttpPost("check")]
        public async Task<ActionResult<RateLimitCheckResponseDto>> Check(
            [FromBody] RateLimitCheckRequestDto requestDto,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(requestDto.ApiKey) ||
                string.IsNullOrWhiteSpace(requestDto.Endpoint))
            {
                return BadRequest("ApiKey and Endpoint are required.");
            }

            var cost = requestDto.Cost ?? 1;

            var rlRequest = new RateLimitRequest(
                apiKey: requestDto.ApiKey,
                endpoint: requestDto.Endpoint,
                cost: cost);

            var result = await _rateLimiter.CheckAsync(rlRequest, cancellationToken);

            var responseDto = RateLimitCheckResponseDto.FromDomain(result);

            return Ok(responseDto);
        }
    }
}