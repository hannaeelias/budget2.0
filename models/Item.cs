using SQLite;
using System.ComponentModel.DataAnnotations.Schema;

namespace budget.models
{
    [System.ComponentModel.DataAnnotations.Schema.Table("Item")]
    public class Item
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Indexed] 
        public string UserId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public double EstimatedCost { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsSelected { get; set; }
        public bool IsRecurring { get; set; } 
        public string RecurrenceInterval { get; set; } = "None"; 
        public DateTime? NextDueDate { get; set; }
        public bool IsProcessed { get; set; }

     
    }
}
