namespace BookBlossom.Core.Entities;
using System.Collections.Generic;
using BookBlossom.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Category
{
    public long CategoryID { get; set; } 
    public string CategoryName{ get; set; } = string.Empty;
    public string Description{get; set; } = string.Empty;
    public CategoryStatus Status {get; set; }
    public ICollection<CustomerPreference> CustomerPreferences {get; set;} = new List<CustomerPreference>();
    public ICollection<RealBook> RealBooks {get; set; } = new List<RealBook>();
}