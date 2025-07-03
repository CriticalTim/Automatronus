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
        public async Task<(List<ExtractedSkill> skills, List<string> projects, string? errorMessage)> ExtractSkillsAndProjectsAsync(string pdfPath)
        {
            var skills = new List<ExtractedSkill>();
            var projects = new List<string>();
            string? errorMessage = null;
            
            try
            {
                if (!File.Exists(pdfPath))
                {
                    errorMessage = $"PDF file not found: {pdfPath}";
                    return (skills, projects, errorMessage);
                }
                
                string text = await ExtractTextFromPdfAsync(pdfPath);
                
                if (string.IsNullOrWhiteSpace(text))
                {
                    errorMessage = "PDF appears to be empty or text extraction failed";
                    return (skills, projects, errorMessage);
                }
                
                skills = ExtractSkillsWithDetails(text);
                projects = ExtractProjects(text);
                
                if (skills.Count == 0)
                {
                    errorMessage = "No skills could be extracted from the PDF. Please ensure the PDF contains a skills section with the expected format.";
                }
            }
            catch (FileNotFoundException ex)
            {
                errorMessage = $"PDF file not found: {ex.Message}";
            }
            catch (UnauthorizedAccessException ex)
            {
                errorMessage = $"Access denied to PDF file: {ex.Message}";
            }
            catch (Exception ex)
            {
                errorMessage = $"Error extracting from PDF: {ex.Message}";
                Console.WriteLine($"Detailed error: {ex}");
            }
            
            return (skills, projects, errorMessage);
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
            
            try
            {
                // Extract skills from the specific PDF format where skills are listed with years and proficiency
                var lines = text.Split('\n').Select(line => line.Trim()).Where(line => !string.IsNullOrEmpty(line)).ToList();
                
                // Find skills sections - looking for patterns like "Kompetenzen", "Programmierung", "Datenbankenkenntnisse"
                var skillSections = new[] { "Kompetenzen", "Programmierung", "Datenbankenkenntnisse", "Software Entwicklung", "Web Entwicklung" };
                
                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    
                    // Check if this line contains a skill section header
                    if (skillSections.Any(section => line.Contains(section, StringComparison.OrdinalIgnoreCase)))
                    {
                        // Process the following lines until we hit another section or end
                        i++; // Move to next line after header
                        
                        while (i < lines.Count)
                        {
                            var skillLine = lines[i].Trim();
                            
                            // Break if we hit another major section
                            if (skillLine.Contains("Jahre") && skillLine.Contains("+") && 
                                i + 1 < lines.Count && lines[i + 1].Contains("Jahre") && lines[i + 1].Contains("+"))
                            {
                                // This looks like a years/proficiency line, skip it
                                i++;
                                continue;
                            }
                            
                            // Break if we hit a major section header
                            if (skillLine.All(char.IsUpper) || skillLine.Contains("Branche") || 
                                skillLine.Contains("Projektumfang") || skillLine.Contains("Zeitraum") ||
                                skillLine.Contains("Zertifikate") || skillLine.Contains("Fremdsprachen"))
                            {
                                break;
                            }
                            
                            // Try to extract skill name with years and proficiency
                            var skill = ParseSkillLine(skillLine, lines, i);
                            if (skill != null && !extractedSkills.Any(s => s.Name.Equals(skill.Name, StringComparison.OrdinalIgnoreCase)))
                            {
                                extractedSkills.Add(skill);
                            }
                            
                            i++;
                        }
                        i--; // Adjust for the outer loop increment
                    }
                }
                
                // If no skills found with the section approach, try the generic approach
                if (extractedSkills.Count == 0)
                {
                    // Look for lines that match the pattern: SkillName followed by Years and Proficiency
                    var skillPattern = @"^([A-Za-z][A-Za-z0-9#\.\+\-\s/]*?)\s*$";
                    var yearPattern = @"^(\d+)\s*Jahre?\s*$";
                    var proficiencyPattern = @"^(\+{1,4})\s*$";
                    
                    for (int i = 0; i < lines.Count - 2; i++)
                    {
                        var skillMatch = Regex.Match(lines[i], skillPattern);
                        if (skillMatch.Success)
                        {
                            var skillName = skillMatch.Groups[1].Value.Trim();
                            
                            // Check if next lines contain years and proficiency
                            if (i + 1 < lines.Count && Regex.IsMatch(lines[i + 1], yearPattern) &&
                                i + 2 < lines.Count && Regex.IsMatch(lines[i + 2], proficiencyPattern))
                            {
                                var yearMatch = Regex.Match(lines[i + 1], yearPattern);
                                var profMatch = Regex.Match(lines[i + 2], proficiencyPattern);
                                
                                if (yearMatch.Success && profMatch.Success &&
                                    int.TryParse(yearMatch.Groups[1].Value, out int years))
                                {
                                    var skill = new ExtractedSkill
                                    {
                                        Name = skillName,
                                        YearsOfExperience = years,
                                        Proficiency = (SkillProficiency)Math.Min(profMatch.Groups[1].Value.Length, 4)
                                    };
                                    
                                    if (!extractedSkills.Any(s => s.Name.Equals(skill.Name, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        extractedSkills.Add(skill);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing skills: {ex.Message}");
                // Return empty list if parsing fails
                return new List<ExtractedSkill>();
            }

            return extractedSkills.DistinctBy(s => s.Name).ToList();
        }
        
        private ExtractedSkill? ParseSkillLine(string line, List<string> allLines, int currentIndex)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;
                
            // Skip lines that are clearly not skills
            if (line.All(char.IsUpper) || line.Contains("Jahre") || line.Contains("+") ||
                line.Contains("Grundkenntnisse") || line.Contains("Basiskenntnisse") ||
                line.Contains("Fortgeschritten") || line.Contains("Expertenkenntnisse"))
            {
                return null;
            }
            
            try
            {
                // Clean up the skill name
                var skillName = line.Trim();
                
                // Remove common suffixes that aren't part of skill names
                var suffixesToRemove = new[] { "2014 - 2022", "2022", "Associate", "Developer", ".NET" };
                foreach (var suffix in suffixesToRemove)
                {
                    if (skillName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    {
                        skillName = skillName.Substring(0, skillName.Length - suffix.Length).Trim();
                    }
                }
                
                // Skip if skill name is too short or contains invalid characters
                if (skillName.Length < 2 || skillName.All(char.IsDigit))
                {
                    return null;
                }
                
                // Try to find years and proficiency in the surrounding lines
                int? years = null;
                SkillProficiency proficiency = SkillProficiency.Beginner;
                
                // Look for years in the next few lines
                for (int i = currentIndex + 1; i < Math.Min(currentIndex + 3, allLines.Count); i++)
                {
                    var nextLine = allLines[i].Trim();
                    if (Regex.IsMatch(nextLine, @"^\d+\s*Jahre?$"))
                    {
                        if (int.TryParse(Regex.Match(nextLine, @"(\d+)").Groups[1].Value, out int extractedYears))
                        {
                            years = extractedYears;
                            break;
                        }
                    }
                }
                
                // Look for proficiency in the next few lines
                for (int i = currentIndex + 1; i < Math.Min(currentIndex + 3, allLines.Count); i++)
                {
                    var nextLine = allLines[i].Trim();
                    if (Regex.IsMatch(nextLine, @"^\+{1,4}$"))
                    {
                        var plusCount = nextLine.Length;
                        proficiency = (SkillProficiency)Math.Min(plusCount, 4);
                        break;
                    }
                }
                
                return new ExtractedSkill
                {
                    Name = skillName,
                    YearsOfExperience = years,
                    Proficiency = proficiency
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing skill line '{line}': {ex.Message}");
                return null;
            }
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