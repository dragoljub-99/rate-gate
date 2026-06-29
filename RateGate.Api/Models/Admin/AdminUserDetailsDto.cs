namespace RateGate.Api.Models.Admin
{
    public class AdminUserDetailsDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Email { get; set; }

        public string? Plan { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public List<AdminApiKeyDto> ApiKeys { get; set; } = new();

        public List<AdminPolicyDto> Policies { get; set; } = new();
    }
}
