using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Core.Entities.General
{
    public class ProceedTransactionDetails
    {
        public string TransactionRef { get; set; }
        public string BicCode { get; set; }
        public string SenderCountryCode { get; set; }
        public string DocIdCode { get; set; }
        public string BankId { get; set; }
        public string SenderNameOnID { get; set; }
        public string AppId { get; set; }
        public DateTime SenderDOBmdy { get; set; }
        public DateTime SenderIDExpiryDatemdy { get; set; }
        public DateTime IssueDatemdy { get; set; }
        public string SenderStreet { get; set; }
        public string SenderNationality { get; set; }
        public string SenderHouseNumber { get; set; }
        public string CurrentDate1 { get; set; }
        public DateTime SenderIDExpiryDateymd { get; set; }
        public DateTime BenefDOBymd { get; set; }
        public DateTime BenefExpiryDateymd { get; set; }
        public DateTime BenefIssueymd { get; set; }
        public DateTime BenefDocDOBymd { get; set; }
        public DateTime SenderDOBymd { get; set; }
        public string Relation { get; set; }
        public string Comment { get; set; }
        public string APITransaction_ID { get; set; }
        public DateTime SenderIDExpiryDatedmy { get; set; }
        public DateTime IssueDatedmy { get; set; }
        public string BeneficiaryName { get; set; }
        public string ProviderName { get; set; }
        public string BankCode { get; set; }
        public string FromCurrencyISOCode { get; set; }
        public string ToCurrencyISOCode { get; set; }
        public string RDA_Code { get; set; }
        public DateTime TransactionDateTime { get; set; }
        public string BenefISOCode { get; set; }
        public string IbanID { get; set; }
        public string IfscCode { get; set; }
        public string AbcBankName { get; set; }
        public int CustomerAPIID { get; set; }
        public int BeneficiaryAPIID { get; set; }
        public int PaymentTypeID { get; set; }
        public string EmailID { get; set; }
        public int TransMapID { get; set; }
        public string WireTransferReferenceNo { get; set; }
        public string Purpose { get; set; }
        public string ReferenceNo { get; set; }
        public decimal AmountInGBP { get; set; }
        public decimal AmountInPKR { get; set; }
        public decimal ExchangeRate { get; set; }
        public decimal TransferFees { get; set; }
        public int CustomerID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int CityID { get; set; }
        public int CountryID { get; set; }
        public string PostCode { get; set; }
        public string PhoneNumber { get; set; }
        public string MobileNumber { get; set; }
        public string Password { get; set; }
        public int SecurityQuestionID { get; set; }
        public string SecurityQuestionAnswer { get; set; }
        public string ProfileImage { get; set; }
        public DateTime RecordInsertDateTime { get; set; }
        public bool DeleteStatus { get; set; }
        public string RegularCustomerID { get; set; }
        public string HouseNumber { get; set; }
        public string Street { get; set; }
        public string Gender { get; set; }
        public string AddressLine2 { get; set; }
        public int AgentMappingID { get; set; }
        public string Nationality { get; set; }
        public string ExceededAmount { get; set; }
        public DateTime InactivateDate { get; set; }
        public bool VerificationFlag { get; set; }
        public string Profession { get; set; }
        public string CompanyName { get; set; }
        public string CustLimit { get; set; }
        public int HeardFromID { get; set; }
        public string HeardFromEvent { get; set; }
        public string SourceOfRegistration { get; set; }
        public string CommentUserID { get; set; }
        public DateTime RemindDate { get; set; }
        public int BranchID { get; set; }
        public int ClientID { get; set; }
        public int TitleID { get; set; }
        public bool BlacklistedFlag { get; set; }
        public string ReasonForBlacklist { get; set; }
        public bool BlockLoginFlag { get; set; }
        public string ReasonForBlockLogin { get; set; }
        public bool SuspiciousFlag { get; set; }
        public string SuspiciousReason { get; set; }
        public int FaceMatchScore { get; set; }
        public int NationalityID { get; set; }
        public bool SuspendedFlag { get; set; }
        public string SuspendReason { get; set; }
        public string MiddleName { get; set; }
        public string FileReference { get; set; }
        public int EmploymentID { get; set; }
        public bool IsMerged { get; set; }
        public string MergedWith { get; set; }
        public bool ProbableMatchFlag { get; set; }
        public string MatchingID { get; set; }
        public DateTime DateOfBirth { get; set; }
        public bool MobileVerificationFlag { get; set; }
        public int MobileRetryFlag { get; set; }
        public int EmailRetryFlag { get; set; }
        public DateTime RetryDate { get; set; }
        public int SecurityFlag { get; set; }
        public DateTime SecurityDate { get; set; }
        public string CheckRules { get; set; }
        public string MobileNumberCode { get; set; }
        public string PhoneNumberCode { get; set; }
        public int UserID { get; set; }
        public bool WatchlistFlag { get; set; }
        public string ReasonForWatchlist { get; set; }
        public string ProvinceID { get; set; }
        public int NameMatchFlag { get; set; }
        public bool UnsubscribeZoho { get; set; }
        public int ReferredByAgent { get; set; }
        public int ReKycEligibility { get; set; }
        public int AssignedUserID { get; set; }
        public int AssignedBranchID { get; set; }
        public int RegStep { get; set; }
        public string PayoutCountries { get; set; }
        public string PlaceOfBirth { get; set; }
        public string TotalMatch { get; set; }
        public string CountryOfBirth { get; set; }
        public int AnnualSalaryID { get; set; }
        public int BeneficiaryID { get; set; }
        public string BeneficiaryAddress { get; set; }
        public string BeneficiaryCity { get; set; }
        public string BeneficiaryCountry { get; set; }
        public string BeneficiaryMobile { get; set; }
        public DateTime BeneficiaryRecordInsertDateTime { get; set; }
        public int CreatedByUserID { get; set; }
        public string BeneficiaryPostCode { get; set; }
        public bool CashCollectionFlag { get; set; }

        public int CollectionTypeId { get; set; }
        public string MobileProvider { get; set; }
        public string WalletProvider { get; set; }
        public string WalletId { get; set; }
        public string BenfTranSign { get; set; }
        public bool PEPAndSanctions { get; set; }
        public string BeneficiaryGender { get; set; }
        public string BeneficiaryState { get; set; }


        // Customer Information
        public string CustomerName { get; set; }
        public string Customer_ID { get; set; }
        public string Country_ID { get; set; } 
        public string Beneficiary_Country_ID { get; set; }
        public string Bank_Name { get; set; }
        public string Provider_name { get; set; } 
        public string Beneficiary_City { get; set; }  
        public string Beneficiary_Name { get; set; }
        public string FromCurrency_Code { get; set; }
        public string Currency_Code { get; set; }
        public int PaymentDepositType_ID { get; set; }
        public string City_Name { get; set; }
        public string NationalityCountry { get; set; } 
        public string Transfer_Fees { get; set; }
        public string ID_Name { get; set; } 
        public string First_Name { get; set; }
        public string Middle_Name { get; set; }
        public string Last_Name { get; set; }
        public string BCountry_Code { get; set; }
        public string Mobile_Number { get; set; }
        public string Phone_Number { get; set; }
        public string sender_address { get; set; }
        public string Post_Code { get; set; }
        public string Nationality_Country { get; set; }
        public string Email_ID { get; set; }
        public string Sender_DOBmdy { get; set; }
        public string Beneficiary_Mobile { get; set; }
        public string Beneficiary_Address { get; set; }
        public string current_date1 { get; set; }
        public string SenderID_Number { get; set; }
        public string Country_Name { get; set; }
        public string Issue_Datemdy { get; set; }  
        public string SenderID_ExpiryDatemdy { get; set; }
        public string Account_Number { get; set; }
        public string TransMap_ID { get; set; }
        public string SenderID_ExpiryDatedmy { get; set; }
        public string Issue_Datedmy { get; set; }
        public string bank_code { get; set; }
        public string benf_ISO_Code { get; set; }
        public string Exchange_Rate { get; set; }
        public string Customer_Name { get; set; }
        public string Country_Code { get; set; }
        public string BISO_Code_Three { get; set; }


        // Sender Information
        public string SenderGender { get; set; }
        public string SenderAddress { get; set; }
        public string SenderDOB { get; set; }
        public string SenderDateOfBirth { get; set; }
        public string SenderIDExpiryDate { get; set; }
        public string SenderIDNumber { get; set; }

        // Location Details
    
        public string CityName { get; set; }
        public string CountryName { get; set; }

        // ISO & Currency Information
        public string ISOCode { get; set; }
        public string BISOCode { get; set; }
        public string CountryCode { get; set; }
        public string BCountryCode { get; set; }
        public string ISOCodeThree { get; set; }
        public string NISOCodeThree { get; set; }
        public string BISOCodeThree { get; set; }
        public string FromCurrencyCode { get; set; }
        public string CurrencyCode { get; set; }

        // Identification & Banking Information
        public string IssueDate { get; set; }
        public string IDName { get; set; }
        public string BIDNumber { get; set; }
        public string BankName { get; set; }
        public string AccountHolderName { get; set; }
        public string AccountNumber { get; set; }
        public string BranchCode { get; set; }
        public string Branch { get; set; }
        public string BICCode { get; set; }
        public string BBankID { get; set; }
        public string SortCode { get; set; }
        public string IFSCLabel { get; set; }

        // Transaction Details
        public string TransactionDate { get; set; }
        public string TransactionTime { get; set; }

        // Miscellaneous
        public string PaymentDepositTypeID { get; set; }
        public string RelationCode { get; set; }
        public string PurposeCode { get; set; }
        public string IDNameCode { get; set; }
        public string BIDNameCode { get; set; }
        public string BIDName { get; set; }
        public int Deliverytype_Id { get; set; }
       

    }

}
