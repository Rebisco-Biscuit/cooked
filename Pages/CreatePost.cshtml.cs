using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;

public class CreatePostModel : PageModel
{
    private string connectionString = "Server=localhost;Port=3306;Database=cis2103_pcf;User ID=root;Password=;";

    [BindProperty]
    public string PostTitle { get; set; }

    [BindProperty]
    public string PostContent { get; set; }

    [BindProperty]
    public string TopicTag { get; set; } = "Random";

    [BindProperty]
    public IFormFile AttachedImage { get; set; }

    public int UserId { get; set; }

    public string SuccessMessage { get; set; }
    public string ErrorMessage { get; set; }

    public void OnPost()
    {
        UserId = HttpContext.Session.GetInt32("UserId") ?? 0;
        if (UserId == 0)
        {
            ErrorMessage = "You must be logged in to create a post.";
            return;
        }

        if (string.IsNullOrWhiteSpace(PostTitle) || string.IsNullOrWhiteSpace(PostContent))
        {
            ErrorMessage = "Title and content cannot be empty.";
            return;
        }

        string imagePath = null;

        if (AttachedImage != null)
        {
            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            imagePath = Path.Combine("uploads", AttachedImage.FileName);
            string fullPath = Path.Combine(uploadsFolder, AttachedImage.FileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                AttachedImage.CopyTo(stream);
            }
        }

        string query = "INSERT INTO posts (user_id, title, content, attached_img, created_at, topic_tag) VALUES (@UserId, @Title, @Content, @AttachedImage, NOW(), @TopicTag)";

        try
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@UserId", UserId);
                    cmd.Parameters.AddWithValue("@Title", PostTitle);
                    cmd.Parameters.AddWithValue("@Content", PostContent);
                    cmd.Parameters.AddWithValue("@AttachedImage", imagePath ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@TopicTag", TopicTag);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        SuccessMessage = "Post created successfully!";
                        RedirectToPage("/MainPage");
                    }
                    else
                    {
                        ErrorMessage = "Failed to create the post. Please try again.";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
    }
}
