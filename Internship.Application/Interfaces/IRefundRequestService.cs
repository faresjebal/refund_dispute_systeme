using Internship.Application.DTOs.RefundRequest;
using Internship.Domain.Entities;

namespace Internship.Application.Interfaces
{
    public interface IRefundRequestService
    {
        Task<RefundRequestResponse> CreateRefundRequestAsync(CreateRefundRequestDto request, string userId);
        Task<RefundRequestResponse> ProcessRefundRequestAsync(ProcessRefundRequestDto request, string adminUserId);
        Task<RefundRequestResponse?> GetRefundRequestAsync(string refundRequestId);
        Task<IEnumerable<RefundRequestResponse>> GetUserRefundRequestsAsync(string userId, int page = 1, int pageSize = 10);
        Task<IEnumerable<RefundRequestResponse>> GetAllRefundRequestsAsync(int page = 1, int pageSize = 10);
        Task<IEnumerable<RefundRequestResponse>> GetRefundRequestsByStatusAsync(Domain.Entities.RefundStatus status, int page = 1, int pageSize = 10);
        Task<bool> RefundExistsForTransactionAsync(string transactionId);
        Task<RefundRequestResponse> UpdateRefundStatusAsync(string refundRequestId, RefundStatus newStatus, string? adminNotes = null);


    }
}
