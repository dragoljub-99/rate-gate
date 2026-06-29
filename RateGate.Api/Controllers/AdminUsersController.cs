using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RateGate.Api.Models.Admin;
using RateGate.Api.Services.Admin;
using RateGate.Infrastructure.Data;

namespace RateGate.Api.Controllers
{
    [ApiController]
    [Route("admin/users")]
    public class AdminUsersController : ControllerBase
    {
        private readonly AdminUsersService _adminUsersService;

        public AdminUsersController(AdminUsersService adminUsersService)
        {
            _adminUsersService = adminUsersService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AdminUserDto>>> GetAll(CancellationToken cancellationToken)
        {
            var users = await _adminUsersService.GetAllAsync(cancellationToken);

            return Ok(users);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<AdminUserDetailsDto>?> GetById(int id, CancellationToken cancellationToken)
        {
          
            var user = await _adminUsersService.GetByIdAsync(id, cancellationToken);

            if (user is null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<AdminUserDto>> Create(
            [FromBody] AdminUserCreateDto dto,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _adminUsersService.CreateAsync(dto, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        [HttpGet("{id:int}/apikeys")]
        public async Task<ActionResult<IEnumerable<AdminApiKeyDto>>> GetApiKeysForUser(int id, CancellationToken cancellationToken)
        {
           var apiKeys = await _adminUsersService.GetApiKeysForUserAsync(id, cancellationToken);

            return Ok(apiKeys);
        }

        [HttpGet("{id:int}/policies")]
        public async Task<ActionResult<IEnumerable<AdminPolicyDto>>> GetPoliciesForUser(int id, CancellationToken cancellationToken)
        {
            var policies = await _adminUsersService.GetPoliciesForUserAsync(id, cancellationToken);

            return Ok(policies);
        }
    }
}
