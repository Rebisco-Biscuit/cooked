using System;
using System.IO;
using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PCF.Pages
{
    public class ProfileModel : PageModel
    {
        public string connectionString = "Server=localhost;Port=3306;Database=cis2103_pcf;User ID=root;Password=;";
        public UserProfile gprof { get; set; }
        public int UserId { get; set; }

        public class UserProfile
        {
            public string Username { get; set; }
            public string PetInfo { get; set; }
            public string ProfilePicUrl { get; set; }
            public string Role { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public void OnGet()
        {
            UserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (UserId == 0)
            {
                Console.WriteLine("No UserId found in session");
            }
            gprof = GetUserProfile(UserId); // Fetch profile from DB
        }

public async Task<IActionResult> OnPostSaveProfile(string petInfo, IFormFile ProfilePicUrl)
{
    string imagePath = null;
    string profPicToDB = null;

    if(ProfilePicUrl == null){
        Console.WriteLine("@@@@Prof Pic is null");
    }
    if (ProfilePicUrl != null && ProfilePicUrl.Length > 0)
    {
         // Extract the file extension (e.g., ".jpg", ".png")
        var fileExtension = Path.GetExtension(ProfilePicUrl.FileName);

        // Generate a unique filename using a GUID or timestamp
        var uniqueFileName = $"{Path.GetFileNameWithoutExtension(ProfilePicUrl.FileName)}_{Guid.NewGuid()}{fileExtension}";

        string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "profpics");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        imagePath = Path.Combine("profpics", uniqueFileName);
        string fullPath = Path.Combine(uploadsFolder, uniqueFileName);
        profPicToDB = imagePath;

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            ProfilePicUrl.CopyTo(stream);
        }
    }
    int? userId = HttpContext.Session.GetInt32("UserId");
    string profilePicUrlFromDB;
    if (!userId.HasValue)
    {
        Console.WriteLine("User not logged in.");
        return RedirectToPage("/Profile"); // Redirect if no user is logged in
    }

    using (var conn = new MySqlConnection(connectionString))
        {
            try
            {
                conn.Open();
                // Fetch current profile data
                var currentProfileQuery = "SELECT pet_info, profile_pic_url FROM users WHERE user_id = @UserId";
                using (var fetchCmd = new MySqlCommand(currentProfileQuery, conn))
                {
                    fetchCmd.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = fetchCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var currentPetInfo = reader["pet_info"]?.ToString();
                            profilePicUrlFromDB = reader["profile_pic_url"]?.ToString();
                        }
                    }
                }
            }
            catch (MySqlException sqlEx)
            {
                Console.WriteLine($"Database error: {sqlEx.Message}"); // Log MySQL-specific errors
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}"); // Log other exceptions
            }

        SaveProfile(userId.Value, petInfo, profPicToDB);
    }
    return RedirectToPage("/Profile");
}

public void SaveProfile(int userId, string petInfo, string profPicToDB)
{
    Console.WriteLine($"==== SaveProfile PetInfo: {petInfo}");
    Console.WriteLine($"==== SaveProfile PicToDB: {profPicToDB}");

    using (var conn = new MySqlConnection(connectionString))
    {
        try
        {
            conn.Open();

            // Fetch current profile data
            var currentProfileQuery = "SELECT pet_info, profile_pic_url FROM users WHERE user_id = @UserId";
            using (var fetchCmd = new MySqlCommand(currentProfileQuery, conn))
            {
                fetchCmd.Parameters.AddWithValue("@UserId", userId);

                using (var reader = fetchCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var currentPetInfo = reader["pet_info"]?.ToString();
                        var currentProfilePicUrl = reader["profile_pic_url"]?.ToString();

                        // If no changes detected, return early
                        if (string.Equals(currentPetInfo, petInfo) && string.Equals(currentProfilePicUrl, profPicToDB))
                        {
                            Console.WriteLine("No changes detected. Exiting.");
                            return;
                        }
                    }
                }
            }

            // Update the profile if there are changes
            var updateQuery = @"
                UPDATE users 
                SET pet_info = @PetInfo, 
                    profile_pic_url = @ProfilePicUrl 
                WHERE user_id = @UserId";

            using (var updateCmd = new MySqlCommand(updateQuery, conn))
            {
                updateCmd.Parameters.AddWithValue("@PetInfo", string.IsNullOrWhiteSpace(petInfo) ? (object)DBNull.Value : petInfo);
                updateCmd.Parameters.AddWithValue("@ProfilePicUrl", string.IsNullOrWhiteSpace(profPicToDB) ? (object)DBNull.Value : profPicToDB);
                updateCmd.Parameters.AddWithValue("@UserId", userId);

                int rowsAffected = updateCmd.ExecuteNonQuery();
                Console.WriteLine(rowsAffected > 0 ? "Profile updated successfully." : "No changes made to the profile.");
            }
        }
        catch (MySqlException sqlEx)
        {
            Console.WriteLine($"Database error: {sqlEx.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
    }
}


        public UserProfile GetUserProfile(int userId)
        {
            UserProfile userProfile = null;

            using (var conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    var query = @"SELECT 
                                    username, 
                                    pet_info, 
                                    profile_pic_url, 
                                    role, 
                                    created_at 
                                  FROM users 
                                  WHERE user_id = @UserId";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                userProfile = new UserProfile
                                {
                                    Username = reader["username"]?.ToString(),
                                    PetInfo = reader["pet_info"]?.ToString(),
                                    Role = reader["role"]?.ToString(),
                                    ProfilePicUrl = reader["profile_pic_url"]?.ToString(),
                                    CreatedAt = reader["created_at"] != DBNull.Value
                                        ? Convert.ToDateTime(reader["created_at"])
                                        : DateTime.MinValue
                                };
                            }
                            else
                            {
                                Console.WriteLine($"No user found for ID: {userId}"); // Log if no user is found
                            }
                        }
                    }
                }
                catch (MySqlException sqlEx)
                {
                    Console.WriteLine($"Database error: {sqlEx.Message}"); // Log MySQL-specific errors
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error: {ex.Message}"); // Log other exceptions
                }
            }

            return userProfile;
        }
    }
}
