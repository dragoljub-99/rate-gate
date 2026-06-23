using Microsoft.AspNetCore.Mvc;
using RateGate.Api.Models;
using RateGate.Domain.RateLimiting;
using RateGate.Domain.Abstractions;


namespace RateGate.Api.Controllers
{
    [ApiController]
    [Route("check")]
    public class RateLimitController : ControllerBase
    {
       private readonly IRateLimitDecisionService _rateLimitDecisionService;

        public RateLimitController(IRateLimitDecisionService rateLimitDecisionService)
        {
              _rateLimitDecisionService = rateLimitDecisionService;
        }

        [HttpPost]
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

            try
            {
      
               var result = await _rateLimitDecisionService.EvaluateAsync(requestDto.ApiKey,
                                                                           requestDto.Endpoint,
                                                                           requestDto.Cost, cancellationToken);

              var dto = RateLimitCheckResponseDto.FromDomain(result);

              return Ok(dto);
            }
            catch (Exception ex)
            {
                var result = RateLimitResult.Deny(
                    RateLimitDecisionReason.InternalError,
                    message: $"An internal error occurred while evaluating the rate limit: {ex.Message}");

                return StatusCode(500, RateLimitCheckResponseDto.FromDomain(result));
            }
        }

        
    }
}