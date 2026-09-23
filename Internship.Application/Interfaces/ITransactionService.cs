using Internship.Application.DTOs.RefundRequest;
using Internship.Application.DTOs.Transaction;
using Internship.Domain.Entities;

namespace Internship.Application.Interfaces
{
    public interface ITransactionService
    {
        Task<TransactionResponse> CreateTransactionAsync(CreateTransactionRequest request, string userId);
        Task<TransactionResponse> ProcessTransactionAsync(string transactionId);
        Task<TransactionResponse?> GetTransactionAsync(string transactionId);
        Task<TransactionResponse?> GetTransactionByReferenceAsync(string transactionId);
        Task<IEnumerable<TransactionResponse>> GetUserTransactionsAsync(string userId, int page = 1, int pageSize = 10);
        Task<IEnumerable<TransactionResponse>> GetAllTransactionsAsync(int page = 1, int pageSize = 10);
        Task<bool> RefundTransactionAsync(string transactionId, decimal amount, string reason);
        Task<TransactionResponse> SimulateTransactionOutcomeAsync(string transactionId, TransactionStatus desiredStatus);

    }
}