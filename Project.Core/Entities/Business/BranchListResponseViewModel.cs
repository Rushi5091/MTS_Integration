using Project.Core.Entities.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Core.Entities.Business
{
    public class BranchListResponseViewModel
    {
        public string? Status { get; set; }
        public int? StatusCode { get; set; }
        public string? Message { get; set; }
        public int? ApiId { get; set; }
        public double? AgentRate { get; set; }
        public int? ApiStatus { get; set; }
        public List<string>? ExtraFields { get; set; }
        public List<BranchDetailViewModel>? BranchDetails { get; set; }
    }
}
