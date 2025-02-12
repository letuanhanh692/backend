using Microsoft.AspNetCore.Http;
using BEPrj3.Models.Vnpay;

namespace BEPrj3.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(PaymentInformationModel model, HttpContext context);
        PaymentResponseModel PaymentExecute(IQueryCollection collections);
    }
}
