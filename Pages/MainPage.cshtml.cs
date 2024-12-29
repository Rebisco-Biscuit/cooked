using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;

namespace PCF.Pages
{
    public class MainPageModel : PageModel
    {
        public string connectionString = "Server=localhost;Port=3306;Database=cis2103_pcf;User ID=root;Password=;";

        public List<Post> Posts { get; set; }
        public List<Comment> Comments { get; set; }
        public int UserId { get; set; }
        public string LoggedInUsername { get; set; }
        public string LoggedInRole { get; set; }

        public void OnGet()
        {
            UserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            (LoggedInUsername, LoggedInRole) = GetUserDetailsFromDatabase(UserId);
            Posts = GetPostsFromDatabase();

            foreach (var post in Posts)
            {
                post.Likes = GetLikeCount(post.PostId);
                post.Comments = GetComments(post.PostId);
            }
        }


private (string username, string role) GetUserDetailsFromDatabase(int userId)
{
    string username = string.Empty;
    string role = string.Empty;

    using (var connection = new MySqlConnection(connectionString))
    {
        try
        {
            connection.Open();
            string query = "SELECT username, role FROM users WHERE user_id = @UserId";

            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        username = reader["username"].ToString();
                        role = reader["role"].ToString();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    return (username, role);
}



    private List<Post> GetPostsFromDatabase()
    {
        List<Post> posts = new List<Post>();

        using (var connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();

                string query = "SELECT posts.post_id, users.role, users.username, users.profile_pic_url, posts.title, posts.content, " +
                            "posts.topic_tag, posts.created_at, posts.attached_img " +
                            "FROM posts " +
                            "JOIN users ON posts.user_id = users.user_id " +
                            "ORDER BY posts.created_at DESC";

                MySqlCommand cmd = new MySqlCommand(query, connection);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var post = new Post
                        {
                            PostId = reader.GetInt32("post_id"),
                            Username = reader.GetString("username"),
                            ProfilePicUrl = reader.GetString("profile_pic_url"),
                            Title = reader.GetString("title"),
                            Content = reader.GetString("content"),
                            TopicTag = reader.GetString("topic_tag"),
                            UserRole = reader.GetString("role"),
                            CreatedAt = reader.GetDateTime("created_at"),
                            
                            AttachedImgPath = reader.IsDBNull(reader.GetOrdinal("attached_img")) 
                                ? null 
                                : reader.GetString("attached_img")
                        };
                        posts.Add(post);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        return posts;
    }

    private int GetLikeCount(int postId)
    {
        string queryLikes = "SELECT COUNT(like_id) FROM likes WHERE post_id = @PostId";
        int numLikes = 0;

        using (var connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                using (var cmd = new MySqlCommand(queryLikes, connection))
                {
                    cmd.Parameters.AddWithValue("@PostId", postId);

                    var result = cmd.ExecuteScalar();
                    numLikes = Convert.ToInt32(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        return numLikes;
    }

    public bool CheckIfUserLikedPost(int userId, int postId)
    {
        string query = "SELECT COUNT(*) FROM likes WHERE user_id = @UserId AND post_id = @PostId";

        using (var connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@PostId", postId);

                    var result = Convert.ToInt32(cmd.ExecuteScalar());
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        return false;
    }

    public IActionResult OnPostLikePost(int postId)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");

        if (!userId.HasValue)  
        {
            return RedirectToPage("/Index"); 
        }

        LikePost(userId.Value, postId);  
        return RedirectToPage(); 
    }

    public IActionResult OnPostUnlikePost(int postId)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");

        if (!userId.HasValue) 
        {
            return RedirectToPage("/Index");
        }

        UnlikePost(userId.Value, postId);
        return RedirectToPage();
    }


    public void LikePost(int userId, int postId)
    {
        string connectionString = "Server=localhost;Port=3306;Database=cis2103_pcf;User ID=root;Password=;";
        string query = "INSERT INTO likes (post_id, user_id, created_at) VALUES (@PostId, @UserId, NOW())";
        using (var connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@PostId", postId);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }


    public void UnlikePost(int userId, int postId)
    {
        string connectionString = "Server=localhost;Port=3306;Database=cis2103_pcf;User ID=root;Password=;";
        string query = "DELETE FROM likes WHERE user_id = @UserId AND post_id = @PostId";
        using (var connection = new MySqlConnection(connectionString))

        {
            try
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@PostId", postId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    public IActionResult OnPostDeletePost(int postId)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToPage("/MainPage");
        }
        DeletePost(postId);
        return RedirectToPage();
    }

    public void DeletePost(int postId)
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();
                string deleteQuery = "DELETE FROM posts WHERE post_id = @PostId";

                using (var cmd = new MySqlCommand(deleteQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@PostId", postId);
                    cmd.ExecuteNonQuery();
                }

                Posts = GetPostsFromDatabase();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting post: {ex.Message}");
            }
        }
    }
        public List<Comment> GetComments(int postId)
        {
            List<Comment> comments = new List<Comment>();
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT c.comment_id, u.username, u.profile_pic_url, c.content, c.created_at
                    FROM comments c
                    JOIN users u ON c.user_id = u.user_id
                    WHERE c.post_id = @PostId
                    ORDER BY c.created_at ASC";
                
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PostId", postId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var comment = new Comment
                            {
                                CommentId = reader.GetInt32("comment_id"),
                                Username = reader.GetString("username"),
                                ProfilePicUrl = reader.GetString("profile_pic_url"),
                                Content = reader.GetString("content"),
                                CreatedAt = reader.GetDateTime("created_at")
                            };
                            comments.Add(comment);
                        }
                    }
                }
            }
            return comments;
        }

public IActionResult OnPost()
{
    // Get the postId and content from the form
    var postId = Request.Form["postId"];
    var content = Request.Form["content"];

    // Check if the user is logged in (userId exists in session)
    int? userId = HttpContext.Session.GetInt32("UserId");

    if (userId.HasValue && !string.IsNullOrWhiteSpace(content))
    {
        AddCommentToDatabase(int.Parse(postId), userId.Value, content);
    }

    Comments = GetComments(int.Parse(postId));
    if (!userId.HasValue)
    {
        return RedirectToPage("/MainPage");
    }
    return RedirectToPage();
}

public void AddCommentToDatabase(int postId, int userId, string content)
{
    string query = "INSERT INTO comments (post_id, user_id, content, created_at) VALUES (@PostId, @UserId, @Content, NOW())";
    using (var connection = new MySqlConnection(connectionString))
    {
        try
        {
            connection.Open();
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@PostId", postId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Content", content);
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}

        
        public IActionResult OnPostDeleteComment(int commentId)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToPage("/MainPage");
            }
            Console.WriteLine(commentId);
            DeleteComment(commentId);
            return RedirectToPage();
        }


        public void DeleteComment(int commentId)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string deleteQuery = "DELETE FROM comments WHERE comment_id = @CommentId";

                    using (var cmd = new MySqlCommand(deleteQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@CommentId", commentId);
                        cmd.ExecuteNonQuery();
                    }

                    Posts = GetPostsFromDatabase();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting post: {ex.Message}");
                }
            }
        }


        public class Post
        {
            public int PostId { get; set; }
            public string Username { get; set; }
            public string UserRole { get; set; }
            public string ProfilePicUrl { get; set; }
            public string Title { get; set; }
            public string Content { get; set; }
            public string TopicTag { get; set; }
            public DateTime CreatedAt { get; set; }
            public string AttachedImgPath { get; set; }
            public int Likes { get; set; }
            public List<Comment> Comments { get; set; }
        }

        public class Comment
        {
            public int CommentId { get; set; }
            public string ProfilePicUrl { get; set; }
            public string Username { get; set; }
            public string Content { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }
}
