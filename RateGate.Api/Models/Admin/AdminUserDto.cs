namespace RateGate.Api.Models.Admin
{
    public class AdminUserDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Email { get; set; }

        public string? Plan { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public int ApiKeysCount { get; set; }

        public int PoliciesCount { get; set; }
    }
}
