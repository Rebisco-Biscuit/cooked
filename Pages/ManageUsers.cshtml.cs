using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using MySql.Data.MySqlClient;

namespace YourNamespace.Pages
{
    public class ManageUsersModel : PageModel
    {
        public string connectionString = "Server=localhost;Port=3306;Database=cis2103_pcf;User ID=root;Password=;";

        public List<User> Users { get; set; } = new();
        public List<string> RolesList { get; set; } = new();

        public void OnGet()
        {
            PopulateUserTable();
            RolesList = GetEnumValuesFromDatabase(); 
        }

        private void PopulateUserTable()
        {
            Users.Clear();

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT username, role, created_at FROM users";

                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Users.Add(new User
                        {
                            Username = reader.GetString("username"),
                            Role = reader.GetString("role"),
                            JoinedDate = reader.GetDateTime("created_at")
                        });
                    }
                }
            }
        }

        public IActionResult OnPostDelete(string username)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string deleteQuery = "DELETE FROM users WHERE username = @username";

                using (var cmd = new MySqlCommand(deleteQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToPage();
        }

        public IActionResult OnPostChangeRole(string username, string newRole)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string updateQuery = "UPDATE users SET role = @newRole WHERE username = @username";

                using (var cmd = new MySqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@newRole", newRole);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToPage();
        }

        public class User
        {
            public string Username { get; set; }
            public string Role { get; set; }
            public DateTime JoinedDate { get; set; }
        }
        private List<string> GetEnumValuesFromDatabase()
        {
            var roles = new List<string>();

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                var query = "SHOW COLUMNS FROM users LIKE 'role';";
                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var typeInfo = reader["Type"].ToString(); 
                        if (typeInfo.StartsWith("enum("))
                        {
                            var rawRoles = typeInfo
                                .Replace("enum(", "")
                                .TrimEnd(')')
                                .Replace("'", "");
                            roles = rawRoles.Split(',').ToList();
                        }
                    }
                }
            }

            return roles;
        }

    }
}
