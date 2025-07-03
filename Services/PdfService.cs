using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text.RegularExpressions;

namespace Automatronus.Services
{
    public class PdfService
    {
        public async Task<(List<string> skills, List<string> projects)> ExtractSkillsAndProjectsAsync(string pdfPath)
        {
            var skills = new List<string>();
            var projects = new List<string>();
            
            try
            {
                string text = await ExtractTextFromPdfAsync(pdfPath);
                
                skills = ExtractSkills(text);
                projects = ExtractProjects(text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting from PDF: {ex.Message}");
            }
            
            return (skills, projects);
        }

        private async Task<string> ExtractTextFromPdfAsync(string pdfPath)
        {
            return await Task.Run(() =>
            {
                using var reader = new PdfReader(pdfPath);
                using var document = new PdfDocument(reader);
                var text = string.Empty;
                
                for (int page = 1; page <= document.GetNumberOfPages(); page++)
                {
                    var strategy = new SimpleTextExtractionStrategy();
                    text += PdfTextExtractor.GetTextFromPage(document.GetPage(page), strategy);
                }
                
                return text;
            });
        }

        private List<string> ExtractSkills(string text)
        {
            var skills = new List<string>();
            var commonSkills = new[]
            {
                "C#", "JavaScript", "TypeScript", "Python", "Java", "C++", "React", "Angular", "Vue.js",
                "Node.js", "ASP.NET", "Blazor", "Entity Framework", "SQL Server", "MySQL", "PostgreSQL",
                "MongoDB", "Redis", "Docker", "Kubernetes", "AWS", "Azure", "Git", "HTML", "CSS",
                "Bootstrap", "Tailwind", "REST API", "GraphQL", "Microservices", "DevOps", "CI/CD",
                "Agile", "Scrum", "TDD", "Unit Testing", "Integration Testing", "Selenium", "Jest",
                "Mocha", "Cypress", "Webpack", "Vite", "npm", "Yarn", "Linux", "Windows", "macOS"
            };

            foreach (var skill in commonSkills)
            {
                if (text.Contains(skill, StringComparison.OrdinalIgnoreCase))
                {
                    skills.Add(skill);
                }
            }

            var skillSectionMatch = Regex.Match(text, @"(?i)(skills?|technologies?|expertise|competencies?)[\s\S]*?(?=\n\n|\n[A-Z]|$)", RegexOptions.IgnoreCase);
            if (skillSectionMatch.Success)
            {
                var skillSection = skillSectionMatch.Value;
                var additionalSkills = Regex.Matches(skillSection, @"\b[A-Z][a-z]+(?:\.[a-z]+)*\b")
                    .Cast<Match>()
                    .Select(m => m.Value)
                    .Where(s => s.Length > 2 && !skills.Contains(s))
                    .ToList();
                
                skills.AddRange(additionalSkills);
            }

            return skills.Distinct().ToList();
        }

        private List<string> ExtractProjects(string text)
        {
            var projects = new List<string>();
            
            var projectSectionMatch = Regex.Match(text, @"(?i)(projects?|work experience|experience|portfolio)[\s\S]*?(?=\n\n|\n[A-Z]|$)", RegexOptions.IgnoreCase);
            if (projectSectionMatch.Success)
            {
                var projectSection = projectSectionMatch.Value;
                var projectMatches = Regex.Matches(projectSection, @"(?i)(?:project|developed|built|created|implemented)[\s\S]*?(?=\n\n|\n(?=\w)|$)");
                
                foreach (Match match in projectMatches)
                {
                    var projectText = match.Value.Trim();
                    if (projectText.Length > 10)
                    {
                        projects.Add(projectText.Substring(0, Math.Min(projectText.Length, 200)));
                    }
                }
            }

            return projects.Take(10).ToList();
        }
    }
}