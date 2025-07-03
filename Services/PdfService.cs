using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text.RegularExpressions;
using Automatronus.Models;

namespace Automatronus.Services
{
    public class ExtractedSkill
    {
        public string Name { get; set; } = string.Empty;
        public int? YearsOfExperience { get; set; }
        public SkillProficiency Proficiency { get; set; } = SkillProficiency.Beginner;
        
        public string GetProficiencySymbols()
        {
            return new string('+', (int)Proficiency);
        }
    }

    public class PdfService
    {
        public async Task<(List<ExtractedSkill> skills, List<string> projects)> ExtractSkillsAndProjectsAsync(string pdfPath)
        {
            var skills = new List<ExtractedSkill>();
            var projects = new List<string>();
            
            try
            {
                string text = await ExtractTextFromPdfAsync(pdfPath);
                
                skills = ExtractSkillsWithDetails(text);
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

        private List<ExtractedSkill> ExtractSkillsWithDetails(string text)
        {
            var extractedSkills = new List<ExtractedSkill>();
            var commonSkills = new[]
            {
                "C#", "JavaScript", "TypeScript", "Python", "Java", "C++", "React", "Angular", "Vue.js",
                "Node.js", "ASP.NET", "Blazor", "Entity Framework", "SQL Server", "MySQL", "PostgreSQL",
                "MongoDB", "Redis", "Docker", "Kubernetes", "AWS", "Azure", "Git", "HTML", "CSS",
                "Bootstrap", "Tailwind", "REST API", "GraphQL", "Microservices", "DevOps", "CI/CD",
                "Agile", "Scrum", "TDD", "Unit Testing", "Integration Testing", "Selenium", "Jest",
                "Mocha", "Cypress", "Webpack", "Vite", "npm", "Yarn", "Linux", "Windows", "macOS"
            };

            foreach (var skillName in commonSkills)
            {
                if (text.Contains(skillName, StringComparison.OrdinalIgnoreCase))
                {
                    var skill = new ExtractedSkill
                    {
                        Name = skillName,
                        YearsOfExperience = ExtractYearsOfExperience(text, skillName),
                        Proficiency = ExtractProficiency(text, skillName)
                    };
                    extractedSkills.Add(skill);
                }
            }

            // Extract skills from skills section with context
            var skillSectionMatch = Regex.Match(text, @"(?i)(skills?|technologies?|expertise|competencies?)[\s\S]*?(?=\n\n|\n[A-Z]|$)", RegexOptions.IgnoreCase);
            if (skillSectionMatch.Success)
            {
                var skillSection = skillSectionMatch.Value;
                
                // Look for patterns like "C# (5 years)", "JavaScript +++", etc.
                var skillWithDetailsPattern = @"([A-Za-z][A-Za-z0-9#\.\+\-]*)\s*(?:\((\d+)\s*years?\)|\s*(\+{1,4})|(\d+)\s*years?)";
                var matches = Regex.Matches(skillSection, skillWithDetailsPattern, RegexOptions.IgnoreCase);
                
                foreach (Match match in matches)
                {
                    var skillName = match.Groups[1].Value.Trim();
                    if (skillName.Length > 2 && !extractedSkills.Any(s => s.Name.Equals(skillName, StringComparison.OrdinalIgnoreCase)))
                    {
                        var skill = new ExtractedSkill
                        {
                            Name = skillName,
                            YearsOfExperience = null,
                            Proficiency = SkillProficiency.Beginner
                        };

                        // Extract years from parentheses
                        if (match.Groups[2].Success && int.TryParse(match.Groups[2].Value, out int years))
                        {
                            skill.YearsOfExperience = years;
                        }
                        // Extract years from separate number
                        else if (match.Groups[4].Success && int.TryParse(match.Groups[4].Value, out int years2))
                        {
                            skill.YearsOfExperience = years2;
                        }

                        // Extract proficiency from + symbols
                        if (match.Groups[3].Success)
                        {
                            var plusCount = match.Groups[3].Value.Length;
                            skill.Proficiency = (SkillProficiency)Math.Min(plusCount, 4);
                        }

                        extractedSkills.Add(skill);
                    }
                }
            }

            return extractedSkills.DistinctBy(s => s.Name).ToList();
        }

        private int? ExtractYearsOfExperience(string text, string skillName)
        {
            // Look for patterns like "5 years of C#", "C# for 3 years", etc.
            var patterns = new[]
            {
                $@"(\d+)\s*years?\s+(?:of\s+)?{Regex.Escape(skillName)}",
                $@"{Regex.Escape(skillName)}\s+(?:for\s+)?(\d+)\s*years?",
                $@"{Regex.Escape(skillName)}\s*\((\d+)\s*years?\)"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int years))
                {
                    return years;
                }
            }

            return null;
        }

        private SkillProficiency ExtractProficiency(string text, string skillName)
        {
            // Look for patterns like "C# +++", "Expert in C#", etc.
            var expertPattern = $@"(?:expert|advanced|senior|lead)\s+(?:in\s+)?{Regex.Escape(skillName)}|{Regex.Escape(skillName)}\s*\+{{3,4}}";
            var intermediatePattern = $@"(?:intermediate|proficient)\s+(?:in\s+)?{Regex.Escape(skillName)}|{Regex.Escape(skillName)}\s*\+{{2}}";
            var beginnerPattern = $@"(?:beginner|basic|junior)\s+(?:in\s+)?{Regex.Escape(skillName)}|{Regex.Escape(skillName)}\s*\+{{1}}";

            if (Regex.IsMatch(text, expertPattern, RegexOptions.IgnoreCase))
                return SkillProficiency.Expert;
            if (Regex.IsMatch(text, intermediatePattern, RegexOptions.IgnoreCase))
                return SkillProficiency.Intermediate;
            if (Regex.IsMatch(text, beginnerPattern, RegexOptions.IgnoreCase))
                return SkillProficiency.Beginner;

            return SkillProficiency.Intermediate; // Default to intermediate
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