using System.Collections.Generic;

namespace BookBlossom.Web.ViewModels.Admin
{
    /// <summary>
    /// ViewModel for the Admin User & Staff Management Dashboard
    /// </summary>
    public class UserManagementViewModel
    {
        public List<AdminUserItemViewModel> Buyers { get; set; } = new();
        public List<AdminUserItemViewModel> Staff { get; set; } = new();
        public List<AdminUserItemViewModel> Banned { get; set; } = new();
    }

    /// <summary>
    /// Models user data columns for list rendering (both Buyers and Staff)
    /// </summary>
    public class AdminUserItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "User"; // e.g. "Admin", "Moderator", "User"
        
        /// <summary>
        /// Plan subscription: "Free", "Basic", "Pro"
        /// Only applicable or visible for Buyers/Users
        /// </summary>
        public string Plan { get; set; } = "Free"; 
        
        /// <summary>
        /// Renamed from Reputation Score to Internal Score (Điểm nội bộ)
        /// </summary>
        public int InternalScore { get; set; } = 100;
        
        public string JoinDate { get; set; } = string.Empty;
        public string Status { get; set; } = "Active"; // "Active", "Banned"
        public string AvatarUrl { get; set; } = string.Empty;
    }
}
