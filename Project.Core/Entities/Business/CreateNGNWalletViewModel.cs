using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Core.Entities.Business
{
    public class CreateNGNWalletViewModel
    {
        [Required(ErrorMessage = "Please Enter Customer ID.")]
        [Range(1, 9999999999, ErrorMessage = "Customer ID must be between 1 and 10 digits.")]
        public int? Customer_ID { get; set; }

        [Required(ErrorMessage = "Please Enter Branch ID.")]
        [Range(1, 9999999999, ErrorMessage = "Branch ID must be between 1 and 10 digits.")]
        public int? Branch_ID { get; set; }

        [Required(ErrorMessage = "Please Enter Client ID.")]
        [Range(1, 9999999999, ErrorMessage = "Client ID must be between 1 and 10 digits.")]
        public int? Client_ID { get; set; }
    }
}
