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
        
        public int? YearsOfExperience { get; set; }
        
        public SkillProficiency Proficiency { get; set; } = SkillProficiency.Beginner;
        
        public int ProfileId { get; set; }
        
        public virtual Profile Profile { get; set; } = null!;
    }
    
    public enum SkillProficiency
    {
        Beginner = 1,      // +
        Intermediate = 2,  // ++
        Advanced = 3,      // +++
        Expert = 4         // ++++
    }
}