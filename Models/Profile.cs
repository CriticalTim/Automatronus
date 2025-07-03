using System.ComponentModel.DataAnnotations;

namespace Automatronus.Models
{
    public class Profile
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string AppName { get; set; } = string.Empty;
        
        public string? Name { get; set; }
        
        public string? PdfFileName { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        public virtual ICollection<Skill> Skills { get; set; } = new List<Skill>();
    }
}