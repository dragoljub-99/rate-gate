namespace RateGate.Domain.Entities
{
    public class ApiKey
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Key { get; set; } = null!;

        public bool IsActive { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? LastUsedAtUtc { get; set; }

        public User User { get; set; } = null!;
    }
}
