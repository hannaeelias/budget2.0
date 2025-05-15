using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using budget.models;
using Newtonsoft.Json;
using SQLite;
using System.Diagnostics;
using System.Net.Http.Json;

namespace budget
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _dbContext;

        public ApiService(AppDbContext dbContext)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost:5255/api/") 
            };
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _dbContext = dbContext;
        }


        public async Task<List<AppUser>> GetUsersFromApiAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("users");

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var users = JsonConvert.DeserializeObject<List<AppUser>>(jsonResponse);

                    Debug.WriteLine($"Successfully fetched {users.Count} users from the API.");
                    return users;
                }
                else
                {
                    Debug.WriteLine($"Failed to fetch users from the API. Status code: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetUsersFromApiAsync: {ex.Message}");
                return null;
            }
        }


        public async Task<bool> CreateUserAsync(AppUser user, string password)
        {
            try
            {
                var jsonContent = JsonConvert.SerializeObject(new
                {
                    email = user.Email,
                    userName = user.UserName,  
                    firstName = user.FirstName,
                    password = password
                });

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("users/register", content);

                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($" Successfully created user: {user.UserName}");
                    return true;
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($" Failed to create user: {user.UserName}. Status: {response.StatusCode}, Error: {errorResponse}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($" Error in CreateUserAsync: {ex.Message}");
                return false;
            }
        }




        public async Task SyncItemsWithApiAsync()
        {
            try
            {
                var localItems = await _dbContext.GetItemsForUser("userId"); 
                Debug.WriteLine($"Fetched {localItems.Count} items from local database.");

                foreach (var localItem in localItems)
                {
                    var success = await CreateItemAsync(localItem);

                    if (success)
                    {
                        localItem.IsProcessed = true;

                        await _dbContext.Update(localItem);
                        Debug.WriteLine($"Successfully updated item with ID {localItem.Id}.");
                    }
                    else
                    {
                        Debug.WriteLine($"Failed to create item with ID {localItem.Id} on the API.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error syncing items: {ex.Message}");
            }
        }



        public async Task SyncUsersWithApiAsync()
        {
            try
            {
                var apiUsers = await GetUsersFromApiAsync();

                if (apiUsers == null)
                {
                    Debug.WriteLine("No users fetched from the API.");
                    return;
                }

                foreach (var apiUser in apiUsers)
                {
                    var existingUser = await _dbContext.GetUser(apiUser.Id);

                    if (existingUser == null)
                    {
                        await _dbContext.CreateUser(apiUser);
                        Debug.WriteLine($"Successfully added user {apiUser.UserName} to the local database.");
                    }
                    else
                    {
                        if (existingUser.UserName != apiUser.UserName ||
                            existingUser.Email != apiUser.Email ||
                            existingUser.Salary != apiUser.Salary)
                        {
                            existingUser.UserName = apiUser.UserName;
                            existingUser.Email = apiUser.Email;
                            existingUser.Salary = apiUser.Salary;

                            await _dbContext.UpdateUser(existingUser);
                            Debug.WriteLine($"Successfully updated user {apiUser.UserName} in the local database.");
                        }
                    }
                }

                var localUsers = await _dbContext.GetAllUsersAsync(); 

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SyncUsersWithApiAsync: {ex.Message}");
            }
        }



        public async Task<bool> CreateItemAsync(Item item)
        {
            try
            {
                var jsonContent = JsonConvert.SerializeObject(item);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("items", content);

                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"Successfully created item with ID {item.Id} on the API.");
                    return true;
                }
                else
                {
                    Debug.WriteLine($"Failed to create item with ID {item.Id} on the API. Status code: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CreateItemAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<AppUser> GetUserFromApiByEmailAsync(string email)
        {

            try
            {
                var response = await _httpClient.GetAsync($"users/email/{email}");

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"API call failed with status code: {response.StatusCode}");
                    throw new Exception($"API call failed with status code: {response.StatusCode}");
                }

                var user = await response.Content.ReadFromJsonAsync<AppUser>();
                return user;
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"HTTP Request Error: {ex.Message}");
                throw new Exception("An error occurred while calling the API.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"General Error: {ex.Message}");
                throw;
            }
        }

    }
}
