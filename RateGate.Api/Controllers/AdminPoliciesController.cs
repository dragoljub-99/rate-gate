using Microsoft.AspNetCore.Mvc;
using RateGate.Api.Models.Admin;
using RateGate.Api.Services.Admin;

namespace RateGate.Api.Controllers
{
    [ApiController]
    [Route("admin/policies")]
    public class AdminPoliciesController : ControllerBase
    {
        private readonly AdminPoliciesService _adminPoliciesService;

        public AdminPoliciesController(AdminPoliciesService adminPoliciesService)
        {
            _adminPoliciesService = adminPoliciesService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AdminPolicyDto>>> GetAll(CancellationToken cancellationToken)
        {            
            var policies = await _adminPoliciesService.GetAllAsync(cancellationToken);
             
            return Ok(policies);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<AdminPolicyDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var policy = await _adminPoliciesService.GetByIdAsync(id, cancellationToken);

            if (policy is null)
            {
                return NotFound();
            }
            return Ok(policy);
        }

        [HttpPost]
        public async Task<ActionResult<AdminPolicyDto>> Create(
            [FromBody] AdminPolicyCreateDto dto,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var policy = await _adminPoliciesService.CreateAsync(dto, cancellationToken);

            if (policy.adminPolicyDto is null)
            {
                return BadRequest(new
                {
                    message = $"{policy.errorMessage}"
                });
            }

            return CreatedAtAction(nameof(GetById),
                                   new { id = policy.adminPolicyDto.Id }, 
                                   policy.adminPolicyDto);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<AdminPolicyDto>> Update(
            int id,
            [FromBody] AdminPolicyUpdateDto dto,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var policy = await _adminPoliciesService.UpdateAsync(id, dto, cancellationToken);

            if (policy is null)
            {
                return NotFound();
            }

            return Ok(policy);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var isDelete = await _adminPoliciesService.DeleteAsync(id, cancellationToken);

            if (!isDelete)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
