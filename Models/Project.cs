using System.ComponentModel.DataAnnotations;

namespace Automatronus.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string? ProjectId { get; set; }
        
        public string? Description { get; set; }
        
        public string? PhoneNumber { get; set; }
        
        public string? Email { get; set; }
        
        public string? CompanyName { get; set; }
        
        public string? Location { get; set; }
        
        public decimal? Budget { get; set; }
        
        public string? Currency { get; set; }
        
        public DateTime? Deadline { get; set; }
        
        public DateTime DatePosted { get; set; } = DateTime.Now;
        
        public DateTime DateScraped { get; set; } = DateTime.Now;
        
        public string? Url { get; set; }
        
        public ProjectStatus Status { get; set; } = ProjectStatus.New;
        
        public DateTime? ApplicationDate { get; set; }
        
        public string? Notes { get; set; }
        
        public int ScrapeSessionId { get; set; }
        
        public virtual ScrapeSession ScrapeSession { get; set; } = null!;
        
        public virtual ICollection<ProjectSkill> RequiredSkills { get; set; } = new List<ProjectSkill>();
    }
    
    public enum ProjectStatus
    {
        New,
        Applied,
        Denied,
        Success
    }
}