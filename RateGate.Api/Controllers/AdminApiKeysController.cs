using Microsoft.AspNetCore.Mvc;
using RateGate.Api.Models.Admin;
using RateGate.Api.Services.Admin;

namespace RateGate.Api.Controllers
{
    [ApiController]
    [Route("admin/apikeys")]
    public class AdminApiKeysController : ControllerBase
    {
        private readonly AdminApiKeysService _adminApiKeysService;

        public AdminApiKeysController(AdminApiKeysService adminApiKeysService)
        {
            _adminApiKeysService = adminApiKeysService;
        }

        [HttpPost]
        public async Task<ActionResult<AdminApiKeyDto>> Create(
            [FromBody] AdminApiKeyCreateDto dto,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var result = await _adminApiKeysService.CreateAsync(dto, cancellationToken);

            if (result.apiKey == null)
            {
                return BadRequest(new
                {
                    message = $"{result.errorMessage}"
                });
            }

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.apiKey.Id },
                result.apiKey);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<AdminApiKeyDto>> GetById(int id, CancellationToken cancellationToken)
        {

            var apiKey = await _adminApiKeysService.GetByIdAsync(id, cancellationToken);

            if (apiKey == null)
            {
                return NotFound();
            }

            return Ok(apiKey);
        }

        [HttpPost("{id:int}/activate")]
        public async Task<ActionResult<AdminApiKeyDto>> Activate(int id, CancellationToken cancellationToken)
        {
            var result = await _adminApiKeysService.ActivateAsync(id, cancellationToken);

            if (result is null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpPost("{id:int}/deactivate")]
        public async Task<ActionResult<AdminApiKeyDto>> Deactivate(int id, CancellationToken cancellationToken)
        {
           var result = await _adminApiKeysService.DeactivateAsync(id, cancellationToken);

           if (result is null)
            {
                return NotFound();
            }
            return Ok(result);
        }
    }
}
