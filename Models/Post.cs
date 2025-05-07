using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillSwap.Models
{
    public class Post
    {
        [Key]
        public int Post_id { get; set; }
        
        [Required]
        public string Title { get; set; }
        
        public string Description { get; set; }
        
        public string Image { get; set; }
        
        [ForeignKey("User")]
        public int User_id { get; set; }
        
        // Navigation properties
        public ApplicationUser User { get; set; }
        public ICollection<Request> Requests { get; set; }
    }
}