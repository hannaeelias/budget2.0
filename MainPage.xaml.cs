using budget.models;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.ApplicationModel.Communication;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Item = budget.models.Item;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Controls;

namespace budget
{
    public partial class MainPage : ContentPage
    {
        private double _salary = 0;
        private double _remainingBalance = 0;
        private readonly AppDbContext _dbContext;
        private ObservableCollection<Item> _items;
        private readonly ApiService _apiService;

        private int SavingsPercentage { get; set; }

        public MainPage()
        {
            try
            {
                InitializeComponent();
                _items = new ObservableCollection<Item>();
                ItemsListView.ItemsSource = _items;
                _dbContext = new AppDbContext();
                LoadItems();
                LoadUserData();
                SavingsSlider.ValueChanged += OnSavingsSliderChanged;
                _apiService = new ApiService(_dbContext);
                OnAppearing();

                var categories = new List<string> { "Bill", "Food", "Extra" };

                CategoryEntry.ItemsSource = categories;

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during initialization: {ex.Message}");
            }
        }

        protected override async void OnAppearing()
        {
            try
            {
                base.OnAppearing();

                if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                {
                    await _apiService.SyncItemsWithApiAsync();
                }
                else
                {
                    await DisplayAlert("Offline", "The app is offline. Local data will sync when a connection is available.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during OnAppearing: {ex.Message}");
                await DisplayAlert("Error", "An error occurred while syncing data.", "OK");
            }
        }

        private async void LoadItems()
        {
            try
            {
                string? userId = Preferences.Get("UserId", null); 
                if (string.IsNullOrEmpty(userId))
                {
                    await DisplayAlert("Error", "User ID is not available.", "OK");
                    return;
                }

                var user = await _dbContext.GetUser(userId);
                if (user == null)
                {
                    await DisplayAlert("Error", "User not found.", "OK");
                    return;
                }

                var itemsFromDb = await _dbContext.GetItemsForUser(user.Id);
                _items.Clear();
                foreach (var item in itemsFromDb)
                {
                    _items.Add(item);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading items: {ex.Message}");
                await DisplayAlert("Error", "An error occurred while loading items.", "OK");
            }
        }

        private void OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            try
            {
                if (e.Item is Item tappedItem)
                {
                    tappedItem.IsSelected = !tappedItem.IsSelected;
                    var index = _items.IndexOf(tappedItem);
                    _items.Remove(tappedItem);
                    _items.Insert(index, tappedItem);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during item tap: {ex.Message}");
            }
        }

        private async void OnSaveItemClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(NameEntry.Text))
                {
                    await DisplayAlert("Error", "Name is required.", "OK");
                    return;
                }

                if (!double.TryParse(EstimatedCostEntry.Text, out double estimatedCost))
                {
                    await DisplayAlert("Error", "Estimated Cost must be a valid number.", "OK");
                    return;
                }

                var userId = Preferences.Get("UserId", null);
                if (string.IsNullOrEmpty(userId))
                {
                    await DisplayAlert("Error", "User ID is not available.", "OK");
                    return;
                }

                var user = await _dbContext.GetUser(userId);
                if (user == null)
                {
                    await DisplayAlert("Error", "User not found.", "OK");
                    return;
                }

                var item = new Item
                {
                    UserId = user.Id,
                    Name = NameEntry.Text,
                    Description = DescriptionEntry.Text,
                    Category = CategoryEntry.SelectedItem?.ToString() ?? "Bill",
                    Priority = PriorityEntry.Text,
                    EstimatedCost = estimatedCost,
                    CreatedAt = CreatedAtPicker.Date,
                    IsSelected = false,
                    Status = StatusPicker.SelectedItem?.ToString() ?? "Not Paid",
                    IsRecurring = IsRecurringSwitch.IsToggled,
                    RecurrenceInterval = RecurrencePicker.SelectedItem?.ToString() ?? "None",
                    NextDueDate = IsRecurringSwitch.IsToggled ? NextDueDatePicker.Date : (DateTime?)null
                };

                var result = await _dbContext.CreateItem(item);
                if (result > 0)
                {
                    _items.Add(item);

                    _remainingBalance -= estimatedCost;
                    RemainingBalanceLabel.Text = $"Remaining: ${_remainingBalance:F2}";

                    await DisplayAlert("Success", "Item saved successfully.", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "Failed to save item.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving item: {ex.Message}");
                await DisplayAlert("Error", "An error occurred while saving the item.", "OK");
            }
        }

        private async void OnDeleteItemClicked(object sender, EventArgs e)
        {
            try
            {
                var itemsToDelete = _items.Where(item => item.IsSelected).ToList();

                if (!itemsToDelete.Any())
                {
                    await DisplayAlert("Error", "No items selected for deletion.", "OK");
                    return;
                }

                bool confirm = await DisplayAlert("Confirm Delete", $"Are you sure you want to delete {itemsToDelete.Count} item(s)?", "Yes", "No");
                if (!confirm) return;

                double totalDeletedCost = 0;

                foreach (var item in itemsToDelete)
                {
                    await _dbContext.DeleteItem(item);
                    _items.Remove(item);
                    totalDeletedCost += item.EstimatedCost;
                }

                _remainingBalance += totalDeletedCost;
                RemainingBalanceLabel.Text = $"Remaining: ${_remainingBalance:F2}";

                await DisplayAlert("Success", "Selected items deleted successfully.", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during item deletion: {ex.Message}");
                await DisplayAlert("Error", "An error occurred while deleting the items.", "OK");
            }
        }

        private async void OnNavigateToOtherPageClicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new Itemviewing());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error navigating to other page: {ex.Message}");
                await DisplayAlert("Error", "An error occurred while navigating.", "OK");
            }
        }



        private async void LoadUserData()
        {
            try
            {
                string? userId = Preferences.Get("UserId", null);
                string? userEmail = Preferences.Get("Email", null);

                Debug.WriteLine($"Loaded from preferences: UserId = '{userId}', Email = '{userEmail}'");

                if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(userEmail))
                {
                    await DisplayAlert("Error", "User ID or Email is not available. Please log in again.", "OK");
                    return;
                }

                AppUser? user = null;

                if (!string.IsNullOrEmpty(userId))
                {
                    user = await _dbContext.GetUser(userId);
                    Debug.WriteLine(user == null
                        ? $"No local user found by Id: {userId}"
                        : $"Local user found by Id: {userId}, Name: {user.FirstName}");
                }

                if (user == null && !string.IsNullOrEmpty(userEmail))
                {
                    user = await _dbContext.GetUserByEmail(userEmail);
                    Debug.WriteLine(user == null
                        ? $"No local user found by Email: {userEmail}"
                        : $"Local user found by Email: {userEmail}, Name: {user.FirstName}");
                }

                if (user == null && !string.IsNullOrEmpty(userEmail))
                {
                    Debug.WriteLine($"Fetching user from API by email: {userEmail}");
                    user = await _apiService.GetUserFromApiByEmailAsync(userEmail);

                    if (user == null)
                    {
                        Debug.WriteLine("User not found on API by email.");
                    }
                    else
                    {
                        Debug.WriteLine($"User fetched from API: {user.FirstName} ({user.Id})");
                        // Save user locally and update prefs
                        await _dbContext.CreateUser(user);
                        Preferences.Set("UserId", user.Id);
                        Preferences.Set("Email", user.Email);
                    }
                }

                if (user == null)
                {
                    Debug.WriteLine("User not found anywhere; creating default user.");
                    user = new AppUser
                    {
                        FirstName = "Default User",
                        BirthDate = DateTime.Now,
                        Salary = 0,
                        Balance = 0,
                        LastUpdated = DateTime.Now
                    };
                    await _dbContext.CreateUser(user);
                    Preferences.Set("Email", user.Email ?? "");
                    Preferences.Set("UserId", user.Id ?? "");
                }

                double totalExpenses = await CalculateTotalExpenses(user.Id);

                _salary = user.Salary > 0 ? user.Salary : 1000;

                if (user.Balance == null)
                {
                    user.Balance = 0;
                    await _dbContext.UpdateUser(user);
                }

                _remainingBalance = user.Salary - totalExpenses;

                NameLabel.Text = $"Welcome, {user.FirstName}!";
                SalaryEntry.Text = _salary > 0 ? _salary.ToString("F2") : "";
                RemainingBalanceLabel.Text = $"Remaining: ${_remainingBalance:F2}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading user data: {ex.Message}");
                await DisplayAlert("Error", "An error occurred while loading user data.", "OK");
            }
        }


        private async Task<double> CalculateTotalExpenses(string userId)
        {
            try
            {
                var items = await _dbContext.GetItemsForUser(userId);
                double total = items.Sum(item => item.EstimatedCost);
                return total;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating total expenses: {ex.Message}");
                return 0;
            }
        }

        private void OnSavingsSliderChanged(object sender, ValueChangedEventArgs e)
        {
            try
            {
               SavingsPercentage = (int)e.NewValue;
                SavingsBalanceLabel.Text = $"Savings: {SavingsPercentage}%";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during savings slider change: {ex.Message}");
            }
        }


        private async void OnApplySavingsClicked(object sender, EventArgs e)
        {
            try
            {
                if (SavingsPercentage < 0 || SavingsPercentage > 100)
                {
                    await DisplayAlert("Error", "Invalid savings percentage. It must be between 0% and 100%.", "OK");
                    return;
                }

                double salary = _salary;

                double savingsAmount = (salary * SavingsPercentage) / 100.0;
                _remainingBalance = salary - savingsAmount;

                SavingsBalanceLabel.Text = $"Savings: ${savingsAmount:F2}";
                RemainingBalanceLabel.Text = $"Remaining: ${_remainingBalance:F2}";

                await DisplayAlert("Success", $"Savings: ${savingsAmount:F2}. Remaining Balance: ${_remainingBalance:F2}", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error calculating savings: {ex.Message}");
                await DisplayAlert("Error", "An error occurred while calculating savings.", "OK");
            }
        }






        private void OnDistributeWeeklyClicked(object sender, EventArgs e)
        {
            try
            {
                double weeklyAllowance = _remainingBalance / 4;
                AllowanceLabel.Text = $"Weekly Allowance: ${weeklyAllowance:F2}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during weekly distribution: {ex.Message}");
                DisplayAlert("Error", "An error occurred while calculating weekly allowance.", "OK");
            }
        }

        private void OnDistributeMonthlyClicked(object sender, EventArgs e)
        {
            try
            {
                double monthAllowance = _remainingBalance / 1;
                AllowanceLabel.Text = $"Weekly Allowance: ${monthAllowance:F2}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during weekly distribution: {ex.Message}");
                DisplayAlert("Error", "An error occurred while calculating weekly allowance.", "OK");
            }
        }



        private async void OnEditSalaryClicked(object sender, EventArgs e)
        {
            try
            {
                var popup = new EditSalaryPopup();
                popup.SalarySaved += async (s, newSalary) =>
                {
                    if (newSalary <= 0)
                    {
                        await DisplayAlert("Error", "Please enter a valid salary.", "OK");
                        return;
                    }

                    string? email = Preferences.Get("Email", null);
                    if (string.IsNullOrEmpty(email))
                    {
                        await DisplayAlert("Error", "User Email is not available. Please log in again.", "OK");
                        return;
                    }

                    var user = await _dbContext.GetUserByEmail(email); 
                    if (user == null)
                    {
                        user = new AppUser
                        {
                            FirstName = "Default User",
                            Email = email,
                            UserName = email,
                            BirthDate = DateTime.Now,
                            Salary = newSalary,
                            Balance = newSalary,
                            LastUpdated = DateTime.Now
                        };
                        await _dbContext.CreateUser(user);
                        Preferences.Set("Email", user.Email);
                    }
                    else
                    {
                        user.Salary = newSalary;
                        user.Balance = newSalary;
                        user.LastUpdated = DateTime.Now;
                        await _dbContext.UpdateUser(user);

                    }

                    RemainingBalanceLabel.Text = $"Remaining: ${user.Balance:F2}";
                    await DisplayAlert("Success", "Salary updated successfully.", "OK");

                    LoadUserData();
                };

                this.ShowPopup(popup);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting salary: {ex.Message}");
                await DisplayAlert("Error", "An error occurred while setting the salary.", "OK");
            }
        }

        
    }
}