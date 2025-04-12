using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Core.Entities.General
{
    public class TransactionStatus
    {
        [Required(ErrorMessage = "Please Enter Client ID.")]
        [Range(1, 9999999999, ErrorMessage = "Client ID must be between 1 and 10 digits.")]
        public int? Client_ID { get; set; }

        [Required(ErrorMessage = "Please Enter Branch ID.")]
        [Range(1, 9999999999, ErrorMessage = "Branch ID must be between 1 and 10 digits.")]
        public int? Branch_ID { get; set; }


        [Required(ErrorMessage = "Please Enter Transaction ID.")]
        [Range(1, 9999999999, ErrorMessage = "Transaction ID must be between 1 and 10 digits.")]
        public int? Transaction_ID { get; set; }

        [Required(ErrorMessage = "Please Enter BranchList API ID.")]
        [Range(1, 9999999999, ErrorMessage = "BranchList API ID must be between 1 and 10 digits.")]
        public int? BranchListAPI_ID { get; set; }

        public string? APIBranch_Details { get; set; }

        [Required(ErrorMessage = "Please Enter User ID.")]
        [Range(1, 9999999999, ErrorMessage = "User ID must be between 1 and 10 digits.")]
        public int? user_id { get; set; }
    }
}
