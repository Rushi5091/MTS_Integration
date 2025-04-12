using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Core.Entities.Business
{
    public class ProceedWithWalletViewModel
    {
        [Required(ErrorMessage = "Please enter TransactionRef")]
        [StringLength(10, MinimumLength = 5, ErrorMessage = "TransactionRef must be between 5 and 10 characters.")]
        [RegularExpression("^[a-zA-Z0-9]*$", ErrorMessage = "TransactionRef must be alphanumeric.")]
        public string? TransactionRef { get; set; }

        [Required(ErrorMessage = "Please enter AmountInGBP")]
        [Range(0.01, double.MaxValue, ErrorMessage = "AmountInGBP must be greater than zero.")]
        public double AmountInGBP { get; set; }

        [Required(ErrorMessage = "Please enter AmountInPKR")]
        [Range(0.01, double.MaxValue, ErrorMessage = "AmountInPKR must be greater than zero.")]
        public double AmountInPKR { get; set; }


        [Required(ErrorMessage = "Please Enter FromCurrency_Code")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "FromCurrency_Code must be exactly 3 characters.")]
        public string? FromCurrency_Code { get; set; }

        [Required(ErrorMessage = "Please Enter ToCurrency_Code")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "ToCurrency_Code must be exactly 3 characters.")]
        public string? ToCurrency_Code { get; set; }


        [Required(ErrorMessage = "Please Enter Customer ID.")]
        [Range(1, 9999999999, ErrorMessage = "Customer ID must be between 1 and 10 digits.")]
        public int? Customer_ID { get; set; }

        [Required(ErrorMessage = "Please Enter Beneficiary ID.")]
        [Range(1, 9999999999, ErrorMessage = "Beneficiary ID must be between 1 and 10 digits.")]
        public int? Beneficiary_ID { get; set; }


        [Required(ErrorMessage = "Please Enter BeneficiaryName")]
        [StringLength(100, ErrorMessage = "BeneficiaryName can't be longer than 100 characters.")]
        [RegularExpression("^[a-zA-Z ]+$", ErrorMessage = "BeneficiaryName must contain only letters and spaces.")]
        public string? BeneficiaryName { get; set; }

        [Required(ErrorMessage = "Please Enter PaymentType ID.")]
        [Range(1, 9999999999, ErrorMessage = "PaymentType ID must be between 1 and 10 digits.")]
        public int? PaymentType_ID { get; set; }


        [Required(ErrorMessage = "Please Enter Client ID.")]
        [Range(1, 9999999999, ErrorMessage = "Client ID must be between 1 and 10 digits.")]
        public int? Client_ID { get; set; }

        [Required(ErrorMessage = "Please Enter Branch ID.")]
        [Range(1, 9999999999, ErrorMessage = "Branch ID must be between 1 and 10 digits.")]
        public int? Branch_ID { get; set; }

        //[Required(ErrorMessage = "Please Enter Transaction ID.")]
        //[Range(1, 9999999999, ErrorMessage = "Transaction ID must be between 1 and 10 digits.")]
        //public int? Transaction_ID { get; set; }

        //[Required(ErrorMessage = "Please Enter BranchList API ID.")]
        //[Range(1, 9999999999, ErrorMessage = "BranchList API ID must be between 1 and 10 digits.")]
        //public int? BranchListAPI_ID { get; set; }

        //public string? APIBranch_Details { get; set; }

        //[Required(ErrorMessage = "Please Enter User ID.")]
        //[Range(1, 9999999999, ErrorMessage = "User ID must be between 1 and 10 digits.")]
        //public int? user_id { get; set; }
    }
}
