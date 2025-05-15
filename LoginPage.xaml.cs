using Microsoft.AspNetCore.Identity;
using Microsoft.Maui.Controls;
using budget.models;
using System;
using System.Diagnostics;

namespace budget
{
    public partial class LoginPage : ContentPage
    {
        private readonly ApiService _apiService;
        private readonly AppDbContext _dbContext;
        private readonly PasswordHasher<AppUser> _passwordHasher;

        public LoginPage()
        {
            InitializeComponent();
            _dbContext = new AppDbContext();
            _passwordHasher = new PasswordHasher<AppUser>();
            _apiService = new ApiService(_dbContext);
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            try
            {
                if (EmailEntry == null || PasswordEntry == null)
                {
                    await DisplayAlert("Error", "Email or password field is not initialized.", "OK");
                    return;
                }

                string email = EmailEntry.Text;
                string password = PasswordEntry.Text;

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    await DisplayAlert("Error", "Please enter both email and password.", "OK");
                    return;
                }

                var user = await _dbContext.GetUserByEmail(email);
                if (user == null)
                {
                    var apiUser = await _apiService.GetUserFromApiByEmailAsync(email);
                    if (apiUser == null)
                    {
                        await DisplayAlert("Error", "Invalid email or password.", "OK");
                        return;
                    }

                    var result = _passwordHasher.VerifyHashedPassword(apiUser, apiUser.PasswordHash, password);
                    if (result == PasswordVerificationResult.Failed)
                    {
                        await DisplayAlert("Error", "Invalid email or password.", "OK");
                        return;
                    }

                    await _dbContext.CreateUser(apiUser);

                    Preferences.Set("UserId", apiUser.Id);
                }
                else
                {
                    var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
                    if (result == PasswordVerificationResult.Failed)
                    {
                        await DisplayAlert("Error", "Invalid email or password.", "OK");
                        return;
                    }

                    Preferences.Set("Email", user.Email);
                }

                await DisplayAlert("Success", "You are now logged in!", "OK");
                await Navigation.PushAsync(new MainPage());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Login error: {ex.Message}");
                await DisplayAlert("Error", "An unexpected error occurred during login.", "OK");
            }
        }


        private async void OnSignUpClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SignUpPage());
        }
    }
}
