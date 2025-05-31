using Microsoft.Maui.Controls;
using budget.models;
using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity;

namespace budget
{
    public partial class SignUpPage : ContentPage
    {
        private readonly AppDbContext _dbContext;
        private readonly ApiService _apiService;

        public SignUpPage()
        {
            InitializeComponent();
            _dbContext = new AppDbContext();
            _apiService = new ApiService(_dbContext);

        }

        private async void OnSignUpClicked(object sender, EventArgs e)
        {
            string username = NameEntry.Text;
            string email = EmailEntry.Text;
            string password = PasswordEntry.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Error", "Please fill in all fields.", "OK");
                return;
            }

            var existingUser = await _dbContext.GetUserByEmail(email);
            if (existingUser != null)
            {
                await DisplayAlert("Error", "User with this email already exists.", "OK");
                return;
            }

            
            var user = new AppUser
            {
                FirstName = username,
                UserName = username,
                Email = email,
                PasswordHash = password,
                BirthDate = DateTime.Now
            };


            Debug.WriteLine($"User Data: FirstName: {user.FirstName}, UserName: {user.UserName}, Email: {user.Email}");

            var isUserCreated = await _apiService.CreateUserAsync(user, password);
            if (!isUserCreated)
            {
                await DisplayAlert("Error", "Failed to create user. api is not fully linked", "OK");
                return;
            }

            await _dbContext.CreateUser(user);
            await DisplayAlert("Success", "Account created successfully!", "OK");

            await Navigation.PushAsync(new LoginPage());
        }


        private async void OnLoginClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new LoginPage());
        }
    }
}
