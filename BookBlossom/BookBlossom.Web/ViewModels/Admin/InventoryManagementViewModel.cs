namespace BookBlossom.Web.ViewModels.Admin
{
    public class InventoryManagementViewModel
    {
        public List<InventoryBookItemViewModel> Books { get; set; } = new();
        public List<InventoryCategoryViewModel> Categories { get; set; } = new();
    }

    public class InventoryBookItemViewModel
    {
        public long BookID { get; set; }
        public long CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? SampleFilePath { get; set; }
        public double Weight { get; set; }
        public int UnitsInStock { get; set; }
        public int ReservedQuantity { get; set; }
        public bool IsContinued { get; set; }
        public int PublishYear { get; set; }
        public string Authors { get; set; } = string.Empty;
        public string MainImageUrl { get; set; } = string.Empty;
    }



    public class InventoryCategoryViewModel
    {
        public long CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public int BookCount { get; set; }
    }
}
