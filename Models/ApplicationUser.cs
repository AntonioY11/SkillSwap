using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkillSwap.Models
{
    [Table("User")]
    public class ApplicationUser
    {
        [Key]
        public int User_id { get; set; }
        
        [Required]
        [StringLength(255)]
        public string FullName { get; set; }
        
        [Required]
        [StringLength(255)]
        [EmailAddress]
        public string Email { get; set; }
        
        [Required]
        [StringLength(255)]
        public string Password { get; set; }
        
        // Navigation properties
        public virtual ICollection<Post> Posts { get; set; }
        public virtual ICollection<Request> Requests { get; set; }
    }
}