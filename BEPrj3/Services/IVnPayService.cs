using BEPrj3.Models.DTO;

namespace BEPrj3.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(PaymentInformationModel model, string baseUrl, string paymentCode);
        PaymentResponseModel ProcessPaymentResponse(Dictionary<string, string> responseParams);
    }

}
