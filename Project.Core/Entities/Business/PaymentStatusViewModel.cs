using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Core.Entities.Business
{
    public class PaymentStatusViewModel
    {
        [Required(ErrorMessage = "Please Enter Client ID.")]
        [Range(1, 9999999999, ErrorMessage = "Client ID must be between 1 and 10 digits.")]
        public int? Client_ID { get; set; }

        [Required(ErrorMessage = "Please Enter Branch ID.")]
        [Range(1, 9999999999, ErrorMessage = "Branch ID must be between 1 and 10 digits.")]
        public int? Branch_ID { get; set; }


        [Required(ErrorMessage = "Please Enter Transaction Reference Number.")]
        public string? Transaction_Ref { get; set; }

        [Required(ErrorMessage = "Please Enter Payment Getway API ID.")]
        [Range(1, 9999999999, ErrorMessage = "Payment Getway API ID must be between 1 and 10 digits.")]
        public int? PaymentGetwayID { get; set; }

        [Required(ErrorMessage = "Please Enter if Payment is Pay With Bank then 1, if Payment is Pay By Card then 2.")]
        [Range(1, 9999999999, ErrorMessage = "Payment PayWithBankORPayByCardFlag must be between 1 and 10 digits.")]
        public int? PayWithBankORPayByCardFlag { get; set; }


        [Required(ErrorMessage = "Please Enter Customer ID.")]
        [Range(1, 9999999999, ErrorMessage = "Customer ID must be between 1 and 10 digits.")]
        public int? Customer_ID { get; set; }

        [Required(ErrorMessage = "Please Enter Payment Parter Transaction Reference Number.")]
        public string? payvynetransid { get; set; }
    }
}
