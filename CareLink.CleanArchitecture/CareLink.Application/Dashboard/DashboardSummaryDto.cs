namespace CareLink.Application.Dashboard;

public record DashboardSummaryDto(
    int NewPatientsCount,
    int DisconnectedMonitorsCount,
    int TotalActivePatientsCount);
