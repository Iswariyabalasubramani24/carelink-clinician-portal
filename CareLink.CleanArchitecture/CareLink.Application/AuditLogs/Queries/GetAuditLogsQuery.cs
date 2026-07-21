using CareLink.Application.Common.Interfaces;
using MediatR;

namespace CareLink.Application.AuditLogs.Queries;

public class GetAuditLogsQuery : IRequest<PagedAuditLogsDto>
{
    public int TenantId { get; set; }

    public string? Action { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 25;

    public GetAuditLogsQuery() { }

    public GetAuditLogsQuery(int tenantId, string? action = null, int page = 1, int pageSize = 25)
    {
        TenantId = tenantId;
        Action = action;
        Page = page;
        PageSize = pageSize;
    }
}

public class GetAuditLogsQueryHandler(IAuditLogRepository auditLogRepository)
    : IRequestHandler<GetAuditLogsQuery, PagedAuditLogsDto>
{
    private const int MaxPageSize = 100;

    public async Task<PagedAuditLogsDto> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var (items, totalCount) = await auditLogRepository.GetByTenantIdAsync(request.TenantId, request.Action, page, pageSize);

        return new PagedAuditLogsDto(items.Select(AuditLogDto.FromEntity).ToList(), totalCount, page, pageSize);
    }
}
