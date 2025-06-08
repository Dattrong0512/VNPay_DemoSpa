using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Web;
using VnPay.Model;
using VnPay.Services;
using VnPay.TemplateReader;

namespace VnPay.Controllers
{
    [Route("api/v0/payment")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
       
        private readonly ILogger<PaymentController> _logger;
        private readonly ITemplateReader _templateReader;
        private readonly IVnPayService _vnPay;

        public PaymentController(ILogger<PaymentController> logger, ITemplateReader templateReader, IVnPayService vnPay)
        {
            _logger = logger;
            _templateReader = templateReader;
            _vnPay = vnPay;
        }

        [HttpPost("")]
        public async Task<IActionResult> CreatePaymentPlansUrlVnPay([FromBody] PaymentRequestModel request)
        {
            string url = await _vnPay.CreatePayment(request,HttpContext);
            _logger.LogInformation(url);
            return Ok(new { paymentUrl = url });
        }


        #region VnPayCallback
        /// <summary>
        /// Handles the VnPay payment callback and displays the payment result.
        /// Used only by back-end, front-end doesn't care     
        /// </summary>
        [HttpGet("vnpay/callback")]
        public async Task<IActionResult> PaymentCallBackVnPay()
        {
            var response = await _vnPay.PaymentExecute(Request.Query);

            _logger.LogInformation("Response from PaymentExecute: Success={Success}, ErrorMessage={ErrorMessage}, ResponseCode={ResponseCode}",
                response.Success, response.ErrorMessage, response.VnPayResponseCode);

            // Ghi log kết quả
            if (response.Success)
            {
                _logger.LogInformation("Payment successful. OrderId: {OrderId}, TransactionId: {TransactionId}",
                    response.OrderId, response.TransactionId);
            }
            else
            {
                _logger.LogWarning("Payment failed. ResponseCode: {ResponseCode}, ErrorMessage: {ErrorMessage}",
                    response.VnPayResponseCode, response.ErrorMessage);
            }

            // Đọc nội dung file HTML từ TemplateReader
            string htmlContent = await _templateReader.GetTemplate("Template/PaymentNotification.html");

            // Lấy tất cả tham số từ Request.Query
            var query = Request.Query;
            var className = response.Success ? "success" : "failure";
            var message = response.Success ? "Thanh toán thành công" : "Thanh toán thất bại";
            var errorMessage = !string.IsNullOrEmpty(response.ErrorMessage) && !response.Success
                ? $"<div class=\"payment-result failure\" style=\"margin-top: 10px;\">{response.ErrorMessage}</div>"
                : "";
            var orderId = response.OrderId != null
                ? $"<p><span class=\"label\">Mã đơn hàng:</span><span class=\"value\">{response.OrderId}</span></p>"
                : "";
            var transactionId = response.TransactionId != null
                ? $"<p><span class=\"label\">Mã giao dịch:</span><span class=\"value\">{response.TransactionId}</span></p>"
                : "";
            var vnp_Amount = query.ContainsKey("vnp_Amount")
                ? long.TryParse(query["vnp_Amount"].ToString(), out long amount)
                    ? $"<p><span class=\"label\">Số tiền:</span><span class=\"value\">{amount / 100} VND</span></p>"
                    : "<p><span class=\"label\">Số tiền:</span><span class=\"value\">Không hợp lệ</span></p>"
                : "";
            var vnp_BankCode = query.ContainsKey("vnp_BankCode")
                ? $"<p><span class=\"label\">Ngân hàng:</span><span class=\"value\">{query["vnp_BankCode"]}</span></p>"
                : "";
            var vnp_BankTranNo = query.ContainsKey("vnp_BankTranNo")
                ? $"<p><span class=\"label\">Mã giao dịch ngân hàng:</span><span class=\"value\">{query["vnp_BankTranNo"]}</span></p>"
                : "";
            var vnp_CardType = query.ContainsKey("vnp_CardType")
                ? $"<p><span class=\"label\">Loại thẻ:</span><span class=\"value\">{query["vnp_CardType"]}</span></p>"
                : "";
            var vnp_OrderInfo = query.ContainsKey("vnp_OrderInfo")
                ? $"<p><span class=\"label\">Thông tin đơn hàng:</span><span class=\"value\">{HttpUtility.UrlDecode(query["vnp_OrderInfo"])}</span></p>"
                : "";
            var vnp_PayDate = query.ContainsKey("vnp_PayDate")
                ? DateTime.TryParseExact(query["vnp_PayDate"].ToString(), "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out var payDate)
                    ? $"<p><span class=\"label\">Thời gian thanh toán:</span><span class=\"value\">{payDate:dd/MM/yyyy HH:mm:ss}</span></p>"
                    : "<p><span class=\"label\">Thời gian thanh toán:</span><span class=\"value\">Không hợp lệ</span></p>"
                : "";
            var vnp_TransactionStatus = query.ContainsKey("vnp_TransactionStatus")
                ? $"<p><span class=\"label\">Trạng thái giao dịch:</span><span class=\"value\">{query["vnp_TransactionStatus"]}</span></p>"
                : "";
            var vnp_TxnRef = query.ContainsKey("vnp_TxnRef")
                ? $"<p><span class=\"label\">Mã tham chiếu:</span><span class=\"value\">{query["vnp_TxnRef"]}</span></p>"
                : "";

            // Thay thế placeholder trong HTML
            htmlContent = htmlContent.Replace("{0}", className)
                                    .Replace("{1}", message)
                                    .Replace("{2}", orderId)
                                    .Replace("{3}", transactionId)
                                    .Replace("{4}", vnp_Amount)
                                    .Replace("{5}", vnp_BankCode)
                                    .Replace("{6}", vnp_BankTranNo)
                                    .Replace("{7}", vnp_CardType)
                                    .Replace("{8}", vnp_OrderInfo)
                                    .Replace("{9}", vnp_PayDate)
                                    .Replace("{10}", vnp_TransactionStatus)
                                    .Replace("{11}", vnp_TxnRef)
                                    .Replace("{12}", errorMessage);

            return Content(htmlContent, "text/html");
        }
        #endregion
    }
}
