using Dapper;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Project.Core.Entities.General;
using Project.Core.Interfaces.IRepositories;
using RestSharp;
using System.Data;
using Project.API.Configuration;
using Project.Core.Entities.Business;



namespace Project.Infrastructure.Repositories
{
    public class PayWithBankRepository : BaseRepository<PayWithBank>, IPayWithBankRepository // Assuming BaseRepository is the intended base class
    {
        private readonly AppSettings _appSettings;

        public PayWithBankRepository(IDbConnection dbConnection, IOptions<AppSettings> appSettings) : base(dbConnection) // Ensure BaseRepository has a constructor accepting IDbConnection
        {
            _appSettings = appSettings.Value;
        }

        public async Task<PayWithBankResponseViewModel> PayWithBank(PayWithBank entity)
        {
            int? PaymentGetwayID = entity.PaymentGetwayID;
            int? Client_ID = entity.Client_ID;
            string? Transaction_Ref = entity.Transaction_Ref;
            int? apistatus = 1;
            double? AgentRateapi = 0;
            string Message = ""; string Message1 = "";
            string returnURL = "";
            string apibankname = "", apiurl = "", apiuser = "", apipass = "", accesscode = "", apicompany_id = "", api_fields = "";
            string? SecurityKey = _appSettings.SecurityKey;

            var storedProcedureName = "Get_activeinstantBankAPIDetails";
            var values = new
            {
                _Client_ID = Client_ID,
                _status = 0,
                _payWithBankGatewayId = PaymentGetwayID
            };

            var apidetails = await _dbConnection.QueryAsync(storedProcedureName, values, commandType: CommandType.StoredProcedure);
            dynamic dttcmdactive_instantbnkapi = apidetails.FirstOrDefault();



            int api_id_active_bnk_id = 0;
            try
            {
                api_id_active_bnk_id = Convert.ToInt32(dttcmdactive_instantbnkapi.bank_api_id);
            }
            catch (Exception ex) { }



            var storedProcedureName1 = "Get_instantBankAPIDetails";
            var values1 = new
            {
                _API_ID = api_id_active_bnk_id,
                _Client_ID = Client_ID,
                _status = 0,
            };

            var apidetails1 = await _dbConnection.QueryAsync(storedProcedureName1, values1, commandType: CommandType.StoredProcedure);
            dynamic apidetail = apidetails1.FirstOrDefault();


            if (api_id_active_bnk_id > 0)
            {
                if (api_id_active_bnk_id == 4)
                {

                    try
                    {

                        int api_id = 0; string reference = "", currency = "", applicationId = "", merchantName = "";
                        string AmountInGBP = "";
                        int Customer_ID = Convert.ToInt32(entity.Customer_ID);
                        await SaveActivityLogTracker(" VolumePay Start execution  : <br/>" + Transaction_Ref, 0, DateTime.Now, 0, Transaction_Ref.ToString(), 0, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                        api_id = Convert.ToInt32(apidetail.bank_api_id);
                        apiurl = Convert.ToString(apidetail.API_URL);
                        apiuser = Convert.ToString(apidetail.UserName);
                        apipass = Convert.ToString(apidetail.Password);

                        string API_Codes = Convert.ToString(apidetail.APIUnique_Codes);
                        Newtonsoft.Json.Linq.JObject o = Newtonsoft.Json.Linq.JObject.Parse(API_Codes);
                        reference = Convert.ToString(Transaction_Ref);
                        currency = Convert.ToString(o["currency"]);
                        applicationId = Convert.ToString(o["applicationId"]);
                        merchantName = Convert.ToString(o["merchantName"]);


                        // Wallet Scenario

                        double? transfer_cost = entity.Amount;
                        if (entity.Wallet_Perm != null && entity.Wallet_Perm != -1)
                        {
                            if (Convert.ToString(entity.Wallet_Perm) == "0")
                            {
                                if (Convert.ToString(entity.Transfer_Cost) != "" && Convert.ToString(entity.Transfer_Cost) != null)
                                {
                                    transfer_cost = entity.Transfer_Cost;
                                }
                            }
                        }

                        if (entity.Discount_Perm != null && entity.Discount_Perm != -1)
                        {
                            if (Convert.ToString(entity.Discount_Perm) == "0")
                            {
                                if (Convert.ToString(entity.Transfer_Cost) != "" && Convert.ToString(entity.Transfer_Cost) != null)
                                {
                                    transfer_cost = entity.Transfer_Cost;
                                }
                            }
                        }
                        AmountInGBP = Convert.ToString(transfer_cost);


                        string requestdata = "{\r\n    \"applicationId\": \"" + applicationId + "\",\r\n    \"merchantPaymentId\": \"" + Transaction_Ref + "\",\r\n    \"merchantPaymentId\": \"" + Transaction_Ref + "\",\r\n    \"merchantName\": \"" + merchantName + "\" ,\r\n    \"showCancelButton\":  true  ,\r\n    \"paymentRequest\": {\r\n        \"amount\": " + AmountInGBP + ",\r\n        \"currency\": \"" + currency + "\",\r\n        \"reference\": \"" + reference + "\"\r\n    },\r\n    \"metadata\": {\r\n        \"mydata\": \"myvalue\"\r\n    }\r\n}";
                        await SaveActivityLogTracker("VolumePay Request Data : <br/>" + requestdata, 0, DateTime.Now, 0, Transaction_Ref.ToString(), 0, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes("{\r\n    \"applicationId\": \"" + applicationId + "\",\r\n    \"merchantPaymentId\": \"" + Transaction_Ref + "\",\r\n    \"merchantPaymentId\": \"" + Transaction_Ref + "\",\r\n    \"merchantName\": \"" + merchantName + "\",\r\n    \"showCancelButton\":  true  ,\r\n    \"paymentRequest\": {\r\n        \"amount\": " + AmountInGBP + ",\r\n        \"currency\": \"" + currency + "\",\r\n        \"reference\": \"" + reference + "\"\r\n    },\r\n    \"metadata\": {\r\n        \"mydata\": \"myvalue\"\r\n    }\r\n}");
                        await SaveActivityLogTracker("VolumePay Request URL  : <br/>" + plainTextBytes, 0, DateTime.Now, 0, Transaction_Ref.ToString(), 0, Convert.ToInt32(Customer_ID), "VolumePay", entity.Branch_ID, Client_ID);

                        string base64URL = System.Convert.ToBase64String(plainTextBytes);
                        returnURL = apiurl + "?ref=" + base64URL;
                        await SaveActivityLogTracker("VolumePay Request returnURL Response  : <br/>" + returnURL, 0, DateTime.Now, 0, Transaction_Ref.ToString(), 0, Convert.ToInt32(Customer_ID), "VolumePay", entity.Branch_ID, Client_ID);

                        if (returnURL != "")
                        {
                            apistatus = 0;
                        }

                    }
                    catch (Exception ex)
                    {
                        Message1 = ex.Message;
                        await SaveErrorLogAsync("VolumePay BankTransfer Error: <br/>" + ex.ToString() + "", DateTime.Now, "VolumePay", 0, entity.Branch_ID, Client_ID, 0);
                    }

                }
            }




            if (apistatus == 0)
            {
                return new PayWithBankResponseViewModel
                {
                    Status = "Success",
                    StatusCode = 0,
                    Message = Message,
                    ApiId = 0,
                    Return_Url = returnURL,
                    ExtraFields = new List<string> { "", "" }
                };
            }
            else
            {
                Message = "Description : " + Message1;
                return new PayWithBankResponseViewModel
                {
                    Status = "Failed",
                    StatusCode = 2,
                    Message = Message,
                    ApiId = 0,
                    Return_Url = returnURL,
                    ExtraFields = new List<string> { "", "" }
                };
            }
        }
    }
}
