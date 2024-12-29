using System;

namespace PCF.Pages
{
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
    }
}
