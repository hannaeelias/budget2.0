using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.Exchange.WebServices.Data;

namespace budget.models
{
    public class AppDbContext
    {
        private const string DB_name = "budget_local_db.db3";
        private readonly SQLiteAsyncConnection _connection;

        public AppDbContext()
        {
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, DB_name);
            _connection = new SQLiteAsyncConnection(dbPath);
            _connection.ExecuteAsync("PRAGMA foreign_keys = ON;").Wait(); 

            _connection.CreateTableAsync<AppUser>().Wait();
            _connection.CreateTableAsync<Item>().Wait();
        }

        public async Task<List<Item>> GetItemsForUser(string UserId)
        {
            return await _connection.Table<Item>().Where(i => i.UserId == UserId).ToListAsync();
        }

        public async Task<AppUser> GetUser(string userId)
        {
            return await _connection.Table<AppUser>().FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<AppUser> GetDefaultUser()
        {
            return await _connection.Table<AppUser>().FirstOrDefaultAsync();
        }

        public async Task<int> CreateUser(AppUser appUser)
        {
            try
            {
                return await _connection.InsertAsync(appUser);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating user: {ex.Message}");
                return 0;
            }
        }

        public async Task<int> UpdateUser(AppUser user)
        {
            try
            {
                if (user.Email == null)
                {
                    throw new InvalidOperationException("User must have a valid Email to be updated.");
                }

                var result = await _connection.UpdateAsync(user);  

                if (result > 0)
                {
                    Debug.WriteLine("User updated successfully.");
                }
                else
                {
                    Debug.WriteLine("No changes were made to the database.");
                }
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating user: {ex.Message}");
                return 0;  
            }
        }



        public async Task<int> DeleteItem(Item item)
        {
            try
            {
                return await _connection.DeleteAsync(item);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting item: {ex.Message}");
                return 0;
            }
        }

        public async Task<int> CreateItem(Item item)
        {
            try
            {
                return await _connection.InsertAsync(item);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating item: {ex.Message}");
                return 0;
            }
        }

        public async System.Threading.Tasks.Task SeedData()
        {
            var adminUser = await _connection.Table<AppUser>().FirstOrDefaultAsync(u => u.UserName == "admin@example.com");
            if (adminUser == null)
            {
                var user = new AppUser
                {
                    UserName = "admin@example.com",
                    Email = "admin@example.com",
                    FirstName = "Admin",
                    LastName = "User",
                    BirthDate = new DateTime(1980, 1, 1),
                    Salary = 5000,
                    Balance = 1000,
                    SavingsBalance = 200,
                    ProfilePicture = "",
                    LastUpdated = DateTime.Now,
                    PasswordHash = new PasswordHasher<AppUser>().HashPassword(null, "Admin123!") 
                };
                await CreateUser(user);
            }

            adminUser = await _connection.Table<AppUser>()
                                 .Where(u => u.Email == "admin@example.com")
                                 .FirstOrDefaultAsync();


            if (adminUser != null)
            {
                var items = await _connection.Table<Item>().ToListAsync();
                if (items.Count == 0)
                {
                    var newItem = new Item
                    {
                        Name = "Sample Item",
                        Category = "General",
                        Priority = "High",
                        EstimatedCost = 100,
                        UserId = adminUser.Id,
                    };
                    await CreateItem(newItem);
                }
            }

        }

        public async Task<int> Update<T>(T entity) where T : class
        {
            try
            {
                return await _connection.UpdateAsync(entity);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating entity: {ex.Message}");
                return 0;
            }
        }

        internal async Task<AppUser?> GetUserByEmail(string email)
        {
            try
            {
                return await _connection.Table<AppUser>().FirstOrDefaultAsync(u => u.Email == email);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving user by email: {ex.Message}");
                return null;

            }
        }

        public async Task<List<AppUser>> GetAllUsersAsync()
        {
            try
            {
                return await _connection.Table<AppUser>().ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving all users: {ex.Message}");
                return new List<AppUser>();
            }
        }


        public async Task<int> DeleteUser(AppUser appUser)
        {
            try
            {
                return await _connection.DeleteAsync(appUser);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting item: {ex.Message}");
                return 0;
            }
        }

    }
}


