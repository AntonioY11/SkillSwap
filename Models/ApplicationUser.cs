using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace SkillSwap.Models
{
    [Table("User")]
    public class ApplicationUser
    {
        [Key]
        public int User_id { get; set; }

        [Required]
        [StringLength(255)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Password { get; set; } = string.Empty;
        
        public string? Bio { get; set; }
        
        public string? ProfilePicture { get; set; }
        
        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
        public virtual ICollection<Request> Requests { get; set; } = new List<Request>();
    }
}