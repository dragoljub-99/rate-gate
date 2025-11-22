namespace RateGate.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Email { get; set; }

        public string? Plan { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();

        public ICollection<Policy> Policies { get; set; } = new List<Policy>();
    }
}