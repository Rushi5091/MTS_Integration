using Dapper;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Project.API.Configuration;
using Project.Core.Entities.Business;
using Project.Core.Entities.General;
using Project.Core.Interfaces.IRepositories;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Project.Infrastructure.Repositories
{
    public class PaymentStatusRepository : BaseRepository<PaymentStatus>, IPaymentStatusRepository
    {
        private readonly AppSettings _appSettings;

        public PaymentStatusRepository(IDbConnection dbConnection, IOptions<AppSettings> appSettings) : base(dbConnection)
        {
            _appSettings = appSettings.Value;
        }

        public async Task<PaymentStatusResponseViewModel> PaymentStatus(PaymentStatus entity)
        {
            int? PaymentGetwayID = entity.PaymentGetwayID;
            int? Client_ID = entity.Client_ID;
            string? Transaction_Ref = entity.Transaction_Ref;
            int? apistatus = 1;
            string payment_status = "";
            string Message = ""; string Message1 = "";
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

            int api_id = 0; string reference = "", currency = "", applicationId = "", merchantName = "";
            api_id = Convert.ToInt32(apidetail.bank_api_id);
            apiurl = Convert.ToString(apidetail.API_URL);
            apiuser = Convert.ToString(apidetail.UserName);
            apipass = Convert.ToString(apidetail.Password);

            string API_Codes = Convert.ToString(apidetail.APIUnique_Codes);
            if (api_id_active_bnk_id > 0)
            {
                if (api_id_active_bnk_id == 4)
                {

                    try
                    {

                        int Customer_ID = Convert.ToInt32(entity.Customer_ID);
                        await SaveActivityLogTracker(" VolumePay Start execution  : <br/>" + Transaction_Ref, 0, DateTime.Now, 0, Transaction_Ref.ToString(), 0, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);

                        Newtonsoft.Json.Linq.JObject o = Newtonsoft.Json.Linq.JObject.Parse(API_Codes);
                        string x_application_secret = "";
                        applicationId = Convert.ToString(o["applicationId"]);
                        x_application_secret = Convert.ToString(o["x_application_secret"]);


                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                            | SecurityProtocolType.Tls11
                            | SecurityProtocolType.Tls12;
                        string statusAPIURL = apipass;
                        var options = new RestClientOptions(statusAPIURL + "status?merchantPaymentId=" + Transaction_Ref)
                        {
                            MaxTimeout = -1
                        };
                        var clientVolumePay = new RestClient(options);
                        var requestVolumePay = new RestRequest()
                        {
                            Method = Method.Get
                        };
                        requestVolumePay.AddHeader("x-application-id", applicationId);
                        requestVolumePay.AddHeader("x-application-secret", x_application_secret);
                        await SaveActivityLogTracker(" Get Status VolumePay StatusRequest URL  : <br/>" + statusAPIURL + "status?merchantPaymentId=" + Transaction_Ref, 0, DateTime.Now, 0, Transaction_Ref.ToString(), 0, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);
                        RestResponse responseVolumePay = clientVolumePay.Execute(requestVolumePay);
                        await SaveActivityLogTracker(" Get Status VolumePay Request URL Response  : <br/>" + responseVolumePay.Content, 0, DateTime.Now, 0, Transaction_Ref.ToString(), 0, Convert.ToInt32(Customer_ID), "Proceed", entity.Branch_ID, Client_ID);
                        string paymentId = "", volumestatus = "";
                        if (responseVolumePay != null)
                        {
                            string resresult = responseVolumePay.Content;

                            if (resresult != "" && resresult != null)
                            {
                                JArray arr = JArray.Parse(responseVolumePay.Content);

                                if (arr.Count > 0)
                                {
                                    JObject obj2 = (JObject)arr[0]; // Get the first object in the array

                                    paymentId = Convert.ToString(obj2["paymentId"]);
                                    volumestatus = Convert.ToString(obj2["paymentStatus"]).ToUpper();

                                }
                            }
                        }

                        if ((volumestatus != "AWAITING_AUTHORIZATION" || volumestatus != "PENDING" || volumestatus != "COMPLETED" || volumestatus != "FAILED") && volumestatus != "")
                        {

                            try
                            {
                                string where = "";
                                if (Client_ID != null && Client_ID != -1)
                                {
                                    where = where + " and aa.Client_ID = " + Client_ID;
                                }
                                if (Transaction_Ref != "" && Transaction_Ref != null)
                                {
                                    where = where + " and ReferenceNo like '%" + Transaction_Ref + "%'";
                                }
                                if (Customer_ID > 0)
                                {
                                    where = where + " and  aa.customer_Id=" + Customer_ID;
                                }

                                var storedProcedureName2 = "View_IncompleteTransfer";
                                var values2 = new
                                {
                                    _whereclause = where,
                                };

                                var apidetails2 = await _dbConnection.QueryAsync(storedProcedureName2, values2, commandType: CommandType.StoredProcedure);
                                dynamic IncompleteTransfer = apidetails2.FirstOrDefault();

                                int transactionId = 0;
                                try
                                {
                                    transactionId = Convert.ToInt32(IncompleteTransfer.Transaction_ID);
                                }
                                catch (Exception ex)
                                {
                                    Message1 = ex.Message;
                                    await SaveErrorLogAsync("VolumePay Get Transaction ID From Temprory Transaction Error: <br/>" + ex.ToString() + "", DateTime.Now, "VolumePay", 0, entity.Branch_ID, Client_ID, 0);
                                }

                                var parameters = new
                                {
                                    _ReferenceNo = Transaction_Ref,
                                    _Client_ID = Client_ID,
                                    _Transaction_ID = transactionId,
                                };

                                var rowsAffected = await _dbConnection.ExecuteAsync("Inactive_TempTransaction", parameters, commandType: CommandType.StoredProcedure);

                            }
                            catch (Exception ex)
                            {
                                Message1 = ex.Message;
                                await SaveErrorLogAsync("VolumePay Transaction Inactive Error: <br/>" + ex.ToString() + "", DateTime.Now, "VolumePay", 0, entity.Branch_ID, Client_ID, 0);
                            }


                        }


                        if (volumestatus.ToLower() == "settled" || volumestatus.ToLower() == "completed")
                        {
                            payment_status = "SUCESS";
                            apistatus = 0;
                        }
                        else if (volumestatus == "PENDING")
                        {
                            payment_status = "PENDING";
                            apistatus = 0;
                        }
                        else
                        {
                            payment_status = "Failed";
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
                return new PaymentStatusResponseViewModel
                {
                    Status = "Success",
                    StatusCode = 0,
                    Message = Message,
                    ApiId = 0,
                    PaymentStatus = payment_status,
                    ExtraFields = new List<string> { "", "" }
                };
            }
            else
            {
                Message = "Description :" + Message1;
                return new PaymentStatusResponseViewModel
                {
                    Status = "Failed",
                    StatusCode = 2,
                    Message = Message,
                    ApiId = 0,
                    PaymentStatus = payment_status,
                    ExtraFields = new List<string> { "", "" }
                };
            }
        }
    }
}

