
using Internship.Domain.Entities;

namespace Internship.Application.DTOs;
public class PaginatedAuditLogResponse
{
    public IEnumerable<object> Items { get; set; } = new List<object>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}