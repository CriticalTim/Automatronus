using System.ComponentModel.DataAnnotations;

namespace Automatronus.Models
{
    public class Skill
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string? Category { get; set; }
        
        public int ProfileId { get; set; }
        
        public virtual Profile Profile { get; set; } = null!;
    }
}