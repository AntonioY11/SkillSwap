using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillSwap.Models
{
    public class Post
    {
        [Key]
        public int Post_id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(255, ErrorMessage = "Title cannot be longer than 255 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        public string? Image { get; set; }

        [ForeignKey("User")]
        public int User_id { get; set; }

        // Navigation properties
        public ApplicationUser? User { get; set; }
        
        // Collection navigation property for requests
        public virtual ICollection<Request> Requests { get; set; } = new List<Request>();
    }
}