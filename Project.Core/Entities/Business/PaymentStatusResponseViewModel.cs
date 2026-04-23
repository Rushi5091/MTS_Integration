using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Core.Entities.Business
{
    public class PaymentStatusResponseViewModel
    {
        public string? Status { get; set; }
        public int? StatusCode { get; set; }
        public string? Message { get; set; }
        public int? ApiId { get; set; }

        public string? PaymentStatus { get; set; }
        public List<string>? ExtraFields { get; set; }
    }
}
