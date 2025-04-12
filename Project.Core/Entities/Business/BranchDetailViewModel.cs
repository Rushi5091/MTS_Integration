using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Core.Entities.Business
{
    public class BranchDetailViewModel
    {
        public string? BranchCode { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Address { get; set; }
        public int? ApiId { get; set; }
    }
}
