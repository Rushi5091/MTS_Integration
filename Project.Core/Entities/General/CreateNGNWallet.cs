using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Core.Entities.General
{
    public class CreateNGNWallet
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

        [Required(ErrorMessage = "Please Enter Currency ID.")]
        [Range(1, 9999999999, ErrorMessage = "Currency ID must be between 1 and 10 digits.")]
        public int? Currency_Id { get; set; }

        [Required(ErrorMessage = "Please Enter User ID.")]
        [Range(1, 9999999999, ErrorMessage = "User ID must be between 1 and 10 digits.")]
        public int? User_ID { get; set; }

        [Required(ErrorMessage = "Please Enter Bank Verification Number .")]
        public string? BankVerificationNumber { get; set; }

        [Required(ErrorMessage = "Please Enter Wallet Transaction Reference Number.")]
        public string? Wallet_Transaction_Reference { get; set; }


    }
}
