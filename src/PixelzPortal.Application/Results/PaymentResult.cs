using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelzPortal.Application.Results
{
    public class PaymentResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }

        public static PaymentResult Success(Guid id) => new() { IsSuccess = true };
        public static PaymentResult Fail(string error) => new() { IsSuccess = false, ErrorMessage = error };
    }
}
