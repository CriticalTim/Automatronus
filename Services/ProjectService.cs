using Microsoft.EntityFrameworkCore;
using Automatronus.Data;
using Automatronus.Models;

namespace Automatronus.Services
{
    public class ProjectService
    {
        private readonly AutomatronusContext _context;

        public ProjectService(AutomatronusContext context)
        {
            _context = context;
        }

        public async Task<List<Project>> GetProjectsAsync(int? scrapeSessionId = null)
        {
            var query = _context.Projects
                .Include(p => p.RequiredSkills)
                .Include(p => p.ScrapeSession)
                .AsQueryable();

            if (scrapeSessionId.HasValue)
            {
                query = query.Where(p => p.ScrapeSessionId == scrapeSessionId.Value);
            }

            return await query
                .OrderByDescending(p => p.DateScraped)
                .ToListAsync();
        }

        public async Task<Project> UpdateProjectStatusAsync(int projectId, ProjectStatus status)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project != null)
            {
                project.Status = status;
                if (status == ProjectStatus.Applied)
                {
                    project.ApplicationDate = DateTime.Now;
                }
                await _context.SaveChangesAsync();
            }
            return project!;
        }

        public async Task<List<Project>> SearchProjectsAsync(string searchTerm)
        {
            return await _context.Projects
                .Include(p => p.RequiredSkills)
                .Include(p => p.ScrapeSession)
                .Where(p => p.Name.Contains(searchTerm) || 
                           p.Description!.Contains(searchTerm) ||
                           p.CompanyName!.Contains(searchTerm))
                .OrderByDescending(p => p.DateScraped)
                .ToListAsync();
        }

        public async Task<ScrapeSession> CreateScrapeSessionAsync(string searchTerms)
        {
            var session = new ScrapeSession
            {
                SearchTerms = searchTerms,
                SessionDate = DateTime.Now
            };

            _context.ScrapeSessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        public async Task<List<ScrapeSession>> GetScrapeSessionsAsync()
        {
            return await _context.ScrapeSessions
                .OrderByDescending(s => s.SessionDate)
                .ToListAsync();
        }

        public async Task SaveProjectsAsync(List<Project> projects)
        {
            _context.Projects.AddRange(projects);
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetProjectCountByStatusAsync(ProjectStatus status)
        {
            return await _context.Projects.CountAsync(p => p.Status == status);
        }
    }
}