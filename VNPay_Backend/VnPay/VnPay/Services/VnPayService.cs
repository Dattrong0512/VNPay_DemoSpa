using System.Diagnostics;
using VnPay.Libraries;
using VnPay.Model;

namespace VnPay.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<VnPayService> _logger;


        public VnPayService(IConfiguration config ,ILogger<VnPayService> logger)
        {
            _config = config;
            _logger = logger;
        }

        #region CreatePayment
        public async Task<string> CreatePayment(PaymentRequestModel request, HttpContext context)
        {
            var vnpay = new VnPayLibrary();
            var vnp_TxnRef = DateTimeOffset.Now.ToUnixTimeSeconds().ToString(); // Sử dụng timestamp giống PHP
            var startTime = DateTime.Now;
            var expireTime = startTime.AddMinutes(15); // Hết hạn sau 15 phút


            vnpay.AddRequestData("vnp_Version", _config["VnPay:Version"]);
            vnpay.AddRequestData("vnp_Command", _config["VnPay:Command"]);
            vnpay.AddRequestData("vnp_TmnCode", _config["VnPay:TmnCode"]); 

            vnpay.AddRequestData("vnp_Amount", (request.Price*100).ToString());
            vnpay.AddRequestData("vnp_CreateDate", startTime.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", _config["VnPay:CurrCode"]);
            vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress(context));
            vnpay.AddRequestData("vnp_Locale", _config["VnPay:Locale"]);
            vnpay.AddRequestData("vnp_OrderInfo", request.OrderInfo);
            vnpay.AddRequestData("vnp_OrderType", "billpayment");
            vnpay.AddRequestData("vnp_ReturnUrl", _config["VnPay:PaymentBackReturnUrl"]);
            vnpay.AddRequestData("vnp_TxnRef", vnp_TxnRef);
            vnpay.AddRequestData("vnp_ExpireDate", expireTime.ToString("yyyyMMddHHmmss"));

            var paymentUrl = vnpay.CreateRequestUrl(_config["VnPay:BaseUrl"], _config["VnPay:HashSecret"]);
            Debug.WriteLine("Payment URL: " + paymentUrl);
            return paymentUrl;
        }
        #endregion


        #region PaymentExecute
        public async Task<PaymentResponseModel> PaymentExecute(IQueryCollection collections)
        {
            var vnpay = new VnPayLibrary();
            foreach (var (key, value) in collections)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, value.ToString());
                }
            }

            var vnp_TxnRef = vnpay.GetResponseData("vnp_TxnRef");
            var vnp_OrderId = vnp_TxnRef; 
            var vnp_TransactionID = vnpay.GetResponseData("vnp_TransactionNo");
            var vnp_SecureHash = collections.FirstOrDefault(p => p.Key == "vnp_SecureHash").Value;
            var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            var vnp_OrderInfo = vnpay.GetResponseData("vnp_OrderInfo");
            var vnp_Amount = vnpay.GetResponseData("vnp_Amount");

            string errorMessage = string.Empty;
            bool isSuccess = false;
            string status = "Failed";

            switch (vnp_ResponseCode)
            {
                case "00":
                    isSuccess = true;
                    status = "Success";
                    break;
                case "07":
                    errorMessage = "Trừ tiền thành công nhưng giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường).";
                    break;
                case "09":
                    errorMessage = "Thẻ/Tài khoản chưa đăng ký dịch vụ Internet Banking.";
                    break;
                case "10":
                    errorMessage = "Xác thực thông tin thẻ/tài khoản không đúng quá 3 lần.";
                    break;
                case "11":
                    errorMessage = "Đã hết hạn chờ thanh toán. Vui lòng thực hiện lại giao dịch.";
                    break;
                case "12":
                    errorMessage = "Thẻ/Tài khoản bị khóa.";
                    break;
                case "13":
                    errorMessage = "Nhập sai mật khẩu xác thực giao dịch (OTP). Vui lòng thực hiện lại.";
                    break;
                case "24":
                    errorMessage = "Khách hàng hủy giao dịch.";
                    break;
                case "51":
                    errorMessage = "Tài khoản không đủ số dư để thực hiện giao dịch.";
                    break;
                case "65":
                    errorMessage = "Tài khoản đã vượt quá hạn mức giao dịch trong ngày.";
                    break;
                case "75":
                    errorMessage = "Ngân hàng thanh toán đang bảo trì.";
                    break;
                case "79":
                    errorMessage = "Nhập sai mật khẩu thanh toán quá số lần quy định. Vui lòng thực hiện lại.";
                    break;
                default:
                    errorMessage = "Lỗi không xác định. Vui lòng liên hệ hỗ trợ.";
                    break;
            }

            var checkSignature = vnpay.ValidateSignature(vnp_SecureHash, _config["VnPay:HashSecret"]);
            if (!checkSignature)
            {
                return new PaymentResponseModel { Success = false };
            }


            if (isSuccess)
            {
                _logger.LogInformation("VnPay payment successful: OrderId={OrderId}, TransactionId={TransactionId}, Amount={Amount}, ResponseCode={ResponseCode}, OrderInfo={OrderInfo}",
                    vnp_OrderId, vnp_TransactionID, vnp_Amount, vnp_ResponseCode, vnp_OrderInfo);
                return new PaymentResponseModel
                {
                    Success = isSuccess,
                    PaymentMethod = "VnPay",
                    OrderDescription = vnp_OrderInfo,
                    OrderId = vnp_OrderId,
                    TransactionId = vnp_TransactionID,
                    Token = vnp_SecureHash,
                    VnPayResponseCode = vnp_ResponseCode,
                    ErrorMessage = errorMessage
                };
            }
            return new PaymentResponseModel { Success = false };

        }
        #endregion

    }
}
