namespace BEPrj3.Models
{
    public class RegisterUserModel
    {
        
        public string Email { get; set; } = null!;

        public string Password { get; set; } = null!;

        public string Name { get; set; } = null!;

        public string? Phone { get; set; }

        public string? Address { get; set; }

        public string? IdCard { get; set; }

        public DateOnly? DateOfBirth { get; set; }
    }
}
