using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(long orderId, decimal amount, HttpContext context);
        bool ValidateSignature(IQueryCollection query);
    }
}
