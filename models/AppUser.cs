﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNetCore.Identity;
using SQLite;

namespace budget.models
{
    [System.ComponentModel.DataAnnotations.Schema.Table("AppUser")]
    public class AppUser : IdentityUser
    {
        [SQLite.PrimaryKey, SQLite.AutoIncrement]
        public String Id { get; set; }
        public String password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public double Salary { get; set; } = 0;
        public double Balance { get; set; } = 0;
        public double SavingsBalance { get; set; }
        public string ProfilePicture { get; set; } = string.Empty;
        public DateTime? LastUpdated { get; set; }
        public string? Role { get; set; }

    }

}