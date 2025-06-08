using VnPay.Model;

namespace VnPay.Services
{
    public interface IVnPayService
    {
        Task<string> CreatePayment(PaymentRequestModel request, HttpContext context);
        Task<PaymentResponseModel> PaymentExecute(IQueryCollection collections);

    }
}
