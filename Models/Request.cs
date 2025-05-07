using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillSwap.Models
{
    public class Request
    {
        [Key]
        public int Request_id { get; set; }
        
        // Null for pending, 0 for rejected, 1 for accepted
        public bool? Status { get; set; }
        
        public string Comment { get; set; }
        
        [ForeignKey("User")]
        public int User_id { get; set; }
        
        [ForeignKey("Post")]
        public int Post_id { get; set; }
        
        // Navigation properties
        public ApplicationUser User { get; set; }
        public Post Post { get; set; }
    }
}