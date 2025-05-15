using CommunityToolkit.Maui.Views;

namespace budget;

public partial class EditSalaryPopup : CommunityToolkit.Maui.Views.Popup
{
    public EditSalaryPopup() => InitializeComponent();

public event EventHandler<double>? SalarySaved;

    private void OnSaveClicked(object sender, EventArgs e)
    {
        if (double.TryParse(SalaryEntry.Text, out double salary) && salary > 0)
        {
            SalarySaved?.Invoke(this, salary);
            Close();
        }
        else
        {
            App.Current.MainPage.DisplayAlert("Error", "Please enter a valid salary.", "OK");
        }
    }
}
