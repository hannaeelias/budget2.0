using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace budget.models
{
    [System.ComponentModel.DataAnnotations.Schema.Table("Transaction")]
    public class Transaction
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public AppUser? User { get; set; } 

        public int ItemId { get; set; } 
        [ForeignKey("ItemId")]
        public Item? Item { get; set; } 

        public double Amount { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string Type { get; set; } = "Expense"; 
        public bool IsDeleted { get; set; } = false; 
    }
}