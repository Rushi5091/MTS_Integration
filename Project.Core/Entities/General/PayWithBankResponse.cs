using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Core.Entities.General
{
    public class PayWithBankResponse
    {
        public string? Status { get; set; }
        public int? StatusCode { get; set; }
        public string? Message { get; set; }
        public int? ApiId { get; set; }

        public string? Return_Url { get; set; }
        public List<string>? ExtraFields { get; set; }
    }
}
