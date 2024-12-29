using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;

public class IndexModel : PageModel
{
    [BindProperty]
    public string Username { get; set; }

    [BindProperty]
    public string Password { get; set; }

    public string ErrorMessage { get; set; }

    public void OnGet()
    {
        //HttpContext.Session.Clear();
    }

    public IActionResult OnPost()
    {
        if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "Username and Password are required!";
            return Page();
        }

        string connectionString = "Server=localhost;Database=cis2103_pcf;User=root;Password=;";

        try
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT user_id, password FROM users WHERE username = @Username";

                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", Username);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int userId = reader.GetInt32("user_id");
                            string storedPassword = reader.GetString("password");

                            if (VerifyPassword(Password, storedPassword))
                            {
                                HttpContext.Session.SetInt32("UserId", userId);
                                HttpContext.Session.SetString("Username", Username);

                                return RedirectToPage("/MainPage");
                            }
                            else
                            {
                                ErrorMessage = "Invalid password!";
                                return Page();
                            }
                        }
                        else
                        {
                            ErrorMessage = "Username not found!";
                            return Page();
                        }
                    }
                }
            }
        }
        catch (MySqlException ex)
        {
            ErrorMessage = "Database error: " + ex.Message;
            return Page();
        }
    }

    private bool VerifyPassword(string inputPassword, string storedPassword)
    {
        string hashedInputPassword = HashPassword(inputPassword);
        return hashedInputPassword == storedPassword;
    }

    private string HashPassword(string password)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashedBytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
