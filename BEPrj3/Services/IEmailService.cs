using System.Threading.Tasks;

namespace BEPrj3.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}
