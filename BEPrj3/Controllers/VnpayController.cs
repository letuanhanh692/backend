using Azure.Core;
using BEPrj3.Models.Vnpay;
using BEPrj3.Services;
using Microsoft.AspNetCore.Mvc;

namespace BEPrj3.Controllers
{
    public class VnpayController : Controller
    {

        private readonly IVnPayService _vnPayService;
        public VnpayController(IVnPayService vnPayService)
        {

            _vnPayService = vnPayService;
        }

        public IActionResult CreatePaymentUrlVnpay(PaymentInformationModel model)
        {
            var url = _vnPayService.CreatePaymentUrl(model, HttpContext);

            return Redirect(url);
        }
        [HttpGet]
        public IActionResult PaymentCallbackVnpay()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            return Json(response);
        }


    }

}
