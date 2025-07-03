using System.ComponentModel.DataAnnotations;

namespace Automatronus.Models
{
    public class ProjectSkill
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string SkillName { get; set; } = string.Empty;
        
        public int ProjectId { get; set; }
        
        public virtual Project Project { get; set; } = null!;
    }
}