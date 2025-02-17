namespace BEPrj3.Models.DTO
{
    public class UserDTO
    {
        public int Id { get; set; }

        public string Username { get; set; } = null!;

        public string Password { get; set; } = null!;

        public string Name { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string? Phone { get; set; }

        public string? Address { get; set; }

        public string? IdCard { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        public string? Avatar { get; set; }

        public int RoleId { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string? RoleName { get; set; }

       
    }
}
