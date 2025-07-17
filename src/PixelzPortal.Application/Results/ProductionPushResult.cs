using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelzPortal.Application.Results
{
    public class ProductionPushResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }

        public static ProductionPushResult Ok() => new() { Success = true };
        public static ProductionPushResult Fail(string reason) => new() { Success = false, Error = reason };
    }
}
