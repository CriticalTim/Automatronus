using System.ComponentModel.DataAnnotations;

namespace Automatronus.Models
{
    public class ScrapeSession
    {
        [Key]
        public int Id { get; set; }
        
        public DateTime SessionDate { get; set; } = DateTime.Now;
        
        public string? SearchTerms { get; set; }
        
        public int ProjectsFound { get; set; }
        
        public string? Notes { get; set; }
        
        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}