namespace RateGate.Api.Models.Admin
{
    public class AdminApiKeyDto
    {
        public int Id {get; set;}
        public string Key {get; set;} = null!;
        public bool IsActive {get; set;}
        public DateTime CreatedAtUtc {get; set;}
        public DateTime? LastUsedAtUtc {get; set;}
        public int UserId {get; set;}
    }
}