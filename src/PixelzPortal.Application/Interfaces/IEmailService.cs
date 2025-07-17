using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelzPortal.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendOrderConfirmationAsync(string email, string orderName, decimal amount);
    }
}
