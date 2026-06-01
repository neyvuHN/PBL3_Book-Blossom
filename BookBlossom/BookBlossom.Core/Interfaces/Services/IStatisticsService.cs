using BookBlossom.Core.DTOs;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface IStatisticsService
    {
        Task<DashboardDataDto> GetDashboardDataAsync(DateTime from, DateTime to, string model = "All");
        Task<byte[]> GenerateDashboardPdfAsync(DateTime from, DateTime to, string model = "All");
    }
}