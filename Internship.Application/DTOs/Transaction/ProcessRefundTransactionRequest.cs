using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Internship.Application.DTOs.Transaction
{
    public class ProcessRefundTransactionRequest
    {
        public decimal Amount { get; set; }
        public string? Reason { get; set; }
        public string? RefundRequestId { get; set; }
    }
}
