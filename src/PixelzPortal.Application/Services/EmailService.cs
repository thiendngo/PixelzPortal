using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelzPortal.Application.Services
{
    public interface IEmailService
    {
        Task SendOrderConfirmationAsync(string email, string orderName, decimal amount);
    }
    public class EmailService : IEmailService
    {
        public async Task SendOrderConfirmationAsync(string email, string orderName, decimal amount)
        {
            // This is a mock/stub. Replace with real SMTP or SendGrid integration
            Console.WriteLine($"[EmailService] Email sent to {email} confirming order '{orderName}' of ${amount}.");
            await Task.CompletedTask;
        }
    }
}
