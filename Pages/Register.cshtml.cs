using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;

public class RegisterModel : PageModel
{
    [BindProperty]
    public string Username { get; set; }
    [BindProperty]
    public string Password { get; set; }
    [BindProperty]
    public string ConfirmPassword { get; set; }
    public string ErrorMessage { get; set; }

    private readonly string connectionString = "Server=localhost;Database=cis2103_pcf;User=root;Password=;";

    public void OnGet() { }

    public IActionResult OnPost()
    {
        if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password) || string.IsNullOrEmpty(ConfirmPassword))
        {
            ErrorMessage = "All fields are required.";
            return Page();
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match.";
            return Page();
        }

        try
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                if (UsernameExists(Username, connection))
                {
                    ErrorMessage = "Username already exists.";
                    return Page();
                }

                string hashedPassword = HashPassword(Password);
                string insertQuery = "INSERT INTO users (username, password) VALUES (@Username, @Password)";
                using (var cmd = new MySqlCommand(insertQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@Username", Username);
                    cmd.Parameters.AddWithValue("@Password", hashedPassword);

                    int rowsInserted = cmd.ExecuteNonQuery();
                    if (rowsInserted > 0)
                    {
                        return RedirectToPage("/Index");
                    }
                    else
                    {
                        ErrorMessage = "Registration failed. Please try again.";
                        return Page();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
            return Page();
        }
    }

    private bool UsernameExists(string username, MySqlConnection connection)
    {
        string sql = "SELECT COUNT(*) FROM users WHERE username = @Username";
        using (var cmd = new MySqlCommand(sql, connection))
        {
            cmd.Parameters.AddWithValue("@Username", username);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }
    }

    private static string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            foreach (byte b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
