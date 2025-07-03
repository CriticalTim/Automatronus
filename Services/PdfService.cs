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
                var lines = text.Split('\n').Select(line => line.Trim()).Where(line => !string.IsNullOrEmpty(line)).ToList();
                
                // Define the skill section headers based on the PDF structure
                var skillSections = new[] { "Kompetenzen", "Datenbankenkenntnisse", "Programmierung", "Zertifikate", "Branchenkenntnisse", "Fremdsprachen" };
                
                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    
                    // Check if this line is a skill section header
                    if (skillSections.Any(section => line.Equals(section, StringComparison.OrdinalIgnoreCase)))
                    {
                        Console.WriteLine($"Found skill section: {line}");
                        i++; // Move to the next line after the header
                        
                        // Process skills in this section
                        while (i < lines.Count)
                        {
                            var currentLine = lines[i].Trim();
                            
                            // Break if we hit another major section
                            if (skillSections.Any(s => s.Equals(currentLine, StringComparison.OrdinalIgnoreCase)) ||
                                currentLine.StartsWith("Branche", StringComparison.OrdinalIgnoreCase) ||
                                currentLine.All(char.IsUpper) && currentLine.Length > 8)
                            {
                                i--; // Step back so the outer loop can process this line
                                break;
                            }
                            
                            // Skip legend lines
                            if (currentLine.Contains("Grundkenntnisse") || currentLine.Contains("Basiskenntnisse") ||
                                currentLine.Contains("Fortgeschritten") || currentLine.Contains("Expertenkenntnisse") ||
                                currentLine.StartsWith("+") || currentLine.StartsWith("Profil von"))
                            {
                                i++;
                                continue;
                            }
                            
                            // Parse skill with the specific format: skill name on one line, years and proficiency on the next lines
                            var skill = ParseSkillWithFormat(currentLine, lines, i);
                            if (skill != null && !extractedSkills.Any(s => s.Name.Equals(skill.Name, StringComparison.OrdinalIgnoreCase)))
                            {
                                extractedSkills.Add(skill);
                                Console.WriteLine($"Extracted skill: {skill.Name}, {skill.YearsOfExperience} years, {skill.GetProficiencySymbols()}");
                            }
                            
                            i++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing skills: {ex.Message}");
                return new List<ExtractedSkill>();
            }

            return extractedSkills.DistinctBy(s => s.Name).ToList();
        }
        
        private ExtractedSkill? ParseSkillWithFormat(string skillLine, List<string> allLines, int currentIndex)
        {
            if (string.IsNullOrWhiteSpace(skillLine))
                return null;
            
            try
            {
                // Clean up the skill name
                var skillName = skillLine.Trim();
                
                // Skip if this looks like a years or proficiency line
                if (Regex.IsMatch(skillName, @"^\d+\s*Jahre?$") || Regex.IsMatch(skillName, @"^\+{1,4}$"))
                    return null;
                
                // Skip legend and section identifiers
                if (skillName.Contains("von") && skillName.Contains("8") || 
                    skillName.Length < 2 || 
                    skillName.All(char.IsDigit))
                    return null;
                
                // Look ahead to find years and proficiency in the same line or next lines
                int? years = null;
                SkillProficiency proficiency = SkillProficiency.Beginner;
                
                // Check if years and proficiency are in the same line (format: "SkillName    5 Jahre    ++++")
                var sameLineMatch = Regex.Match(skillLine, @"^(.+?)\s+(\d+)\s*Jahre?\s+(\+{1,4})\s*$");
                if (sameLineMatch.Success)
                {
                    skillName = sameLineMatch.Groups[1].Value.Trim();
                    if (int.TryParse(sameLineMatch.Groups[2].Value, out int extractedYears))
                    {
                        years = extractedYears;
                    }
                    proficiency = (SkillProficiency)Math.Min(sameLineMatch.Groups[3].Value.Length, 4);
                }
                else
                {
                    // Look for years and proficiency in subsequent lines
                    for (int i = currentIndex + 1; i < Math.Min(currentIndex + 4, allLines.Count); i++)
                    {
                        var nextLine = allLines[i].Trim();
                        
                        // Try to extract years
                        if (years == null && Regex.IsMatch(nextLine, @"^\d+\s*Jahre?$"))
                        {
                            var yearMatch = Regex.Match(nextLine, @"^(\d+)\s*Jahre?$");
                            if (yearMatch.Success && int.TryParse(yearMatch.Groups[1].Value, out int extractedYears))
                            {
                                years = extractedYears;
                            }
                        }
                        
                        // Try to extract proficiency
                        if (Regex.IsMatch(nextLine, @"^\+{1,4}$"))
                        {
                            proficiency = (SkillProficiency)Math.Min(nextLine.Length, 4);
                        }
                    }
                }
                
                // Only return skill if we have a reasonable skill name
                if (skillName.Length >= 2)
                {
                    return new ExtractedSkill
                    {
                        Name = skillName,
                        YearsOfExperience = years,
                        Proficiency = proficiency
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing skill line '{skillLine}': {ex.Message}");
            }
            
            return null;
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
            
            try
            {
                var lines = text.Split('\n').Select(line => line.Trim()).Where(line => !string.IsNullOrEmpty(line)).ToList();
                
                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    
                    // Look for project sections that start with "Branche"
                    if (line.StartsWith("Branche", StringComparison.OrdinalIgnoreCase))
                    {
                        var projectInfo = new List<string>();
                        projectInfo.Add(line); // Add the Branche line
                        
                        i++; // Move to next line
                        
                        // Collect project information until we hit the next "Branche" or end
                        while (i < lines.Count && !lines[i].StartsWith("Branche", StringComparison.OrdinalIgnoreCase))
                        {
                            var currentLine = lines[i].Trim();
                            
                            // Skip empty lines and page indicators
                            if (!string.IsNullOrEmpty(currentLine) && 
                                !currentLine.StartsWith("Profil von") &&
                                !Regex.IsMatch(currentLine, @"^\d+\s*von\s*\d+$"))
                            {
                                // Include important project details
                                if (currentLine.StartsWith("Aufgabe", StringComparison.OrdinalIgnoreCase) ||
                                    currentLine.StartsWith("Rolle", StringComparison.OrdinalIgnoreCase) ||
                                    currentLine.StartsWith("Projektumfang", StringComparison.OrdinalIgnoreCase) ||
                                    currentLine.StartsWith("Zeitraum", StringComparison.OrdinalIgnoreCase) ||
                                    currentLine.StartsWith("Technologien", StringComparison.OrdinalIgnoreCase))
                                {
                                    projectInfo.Add(currentLine);
                                }
                                // Include task descriptions (usually after "Aufgabe")
                                else if (projectInfo.Count > 0 && projectInfo.Last().StartsWith("Aufgabe"))
                                {
                                    projectInfo.Add(currentLine);
                                }
                            }
                            
                            i++;
                        }
                        
                        // Create a formatted project description
                        if (projectInfo.Count > 1)
                        {
                            var projectDescription = string.Join(" | ", projectInfo);
                            if (projectDescription.Length > 10)
                            {
                                projects.Add(projectDescription.Substring(0, Math.Min(projectDescription.Length, 300)));
                            }
                        }
                        
                        i--; // Step back so we can process the next "Branche" line
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting projects: {ex.Message}");
            }

            return projects.Take(10).ToList();
        }
    }
}