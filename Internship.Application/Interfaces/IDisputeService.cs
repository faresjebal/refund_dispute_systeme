using Internship.Application.DTOs.Dispute;

namespace Internship.Application.Interfaces
{
    public interface IDisputeService
    {
        Task<DisputeResponse> CreateDisputeAsync(CreateDisputeDto request, string userId);
        Task<DisputeResponse> ProcessDisputeAsync(ProcessDisputeDto request, string adminUserId);
        Task<DisputeResponse?> GetDisputeAsync(string disputeId);
        Task<IEnumerable<DisputeResponse>> GetUserDisputesAsync(string userId, int page = 1, int pageSize = 10);
        Task<IEnumerable<DisputeResponse>> GetAllDisputesAsync(int page = 1, int pageSize = 10);
        Task<IEnumerable<DisputeResponse>> GetDisputesByStatusAsync(Domain.Entities.DisputeStatus status, int page = 1, int pageSize = 10);
        Task<IEnumerable<DisputeResponse>> GetAssignedDisputesAsync(string assignedToUserId, int page = 1, int pageSize = 10);
        Task<DisputeResponse> AssignDisputeAsync(string disputeId, string assignedToUserId, string adminUserId);
    }
}