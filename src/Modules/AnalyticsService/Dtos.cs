namespace Barakah.AnalyticsService.Dtos;

public record SalesSummaryResponse(
    int TotalOrders,
    decimal TotalRevenue,
    decimal AvgOrderValue,
    decimal TotalPaymentsReceived,
    DateTime From,
    DateTime To);
