using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using HtmlAgilityPack;
using Automatronus.Models;

namespace Automatronus.Services
{
    public class WebScrapingService
    {
        private readonly ProjectService _projectService;

        public WebScrapingService(ProjectService projectService)
        {
            _projectService = projectService;
        }

        public async Task<List<Project>> ScrapeFreelancerMapAsync(List<string> skills, int scrapeSessionId)
        {
            var projects = new List<Project>();
            
            try
            {
                var options = new ChromeOptions();
                options.AddArgument("--headless");
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");
                
                using var driver = new ChromeDriver(options);
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
                
                foreach (var skill in skills.Take(5))
                {
                    var skillProjects = await ScrapeSkillProjectsAsync(driver, skill, scrapeSessionId);
                    projects.AddRange(skillProjects);
                    
                    await Task.Delay(2000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during web scraping: {ex.Message}");
            }
            
            return projects.DistinctBy(p => p.ProjectId).ToList();
        }

        private async Task<List<Project>> ScrapeSkillProjectsAsync(IWebDriver driver, string skill, int scrapeSessionId)
        {
            var projects = new List<Project>();
            
            try
            {
                var searchUrl = $"https://www.freelancermap.de/projektboerse.html?query={Uri.EscapeDataString(skill)}";
                driver.Navigate().GoToUrl(searchUrl);
                
                await Task.Delay(3000);
                
                var projectElements = driver.FindElements(By.CssSelector(".project-item, .project-card, [data-testid='project-item']"));
                
                foreach (var element in projectElements.Take(10))
                {
                    try
                    {
                        var project = ExtractProjectData(element, scrapeSessionId);
                        if (project != null)
                        {
                            projects.Add(project);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error extracting project data: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scraping skill '{skill}': {ex.Message}");
            }
            
            return projects;
        }

        private Project? ExtractProjectData(IWebElement element, int scrapeSessionId)
        {
            try
            {
                var titleElement = element.FindElement(By.CssSelector("h3, .project-title, [data-testid='project-title']"));
                var title = titleElement?.Text?.Trim();
                
                if (string.IsNullOrEmpty(title))
                    return null;

                var project = new Project
                {
                    Name = title,
                    ScrapeSessionId = scrapeSessionId,
                    DateScraped = DateTime.Now,
                    Status = ProjectStatus.New
                };

                try
                {
                    var descriptionElement = element.FindElement(By.CssSelector(".project-description, .description, p"));
                    project.Description = descriptionElement?.Text?.Trim();
                }
                catch { }

                try
                {
                    var companyElement = element.FindElement(By.CssSelector(".company-name, .client-name, .company"));
                    project.CompanyName = companyElement?.Text?.Trim();
                }
                catch { }

                try
                {
                    var locationElement = element.FindElement(By.CssSelector(".location, .project-location"));
                    project.Location = locationElement?.Text?.Trim();
                }
                catch { }

                try
                {
                    var budgetElement = element.FindElement(By.CssSelector(".budget, .price, .project-budget"));
                    var budgetText = budgetElement?.Text?.Trim();
                    if (!string.IsNullOrEmpty(budgetText))
                    {
                        var budgetMatch = System.Text.RegularExpressions.Regex.Match(budgetText, @"(\d+(?:\.\d+)?)\s*€");
                        if (budgetMatch.Success && decimal.TryParse(budgetMatch.Groups[1].Value, out decimal budget))
                        {
                            project.Budget = budget;
                            project.Currency = "EUR";
                        }
                    }
                }
                catch { }

                try
                {
                    var linkElement = element.FindElement(By.CssSelector("a"));
                    var href = linkElement?.GetAttribute("href");
                    if (!string.IsNullOrEmpty(href))
                    {
                        project.Url = href.StartsWith("http") ? href : $"https://www.freelancermap.de{href}";
                    }
                }
                catch { }

                var projectIdMatch = System.Text.RegularExpressions.Regex.Match(project.Url ?? "", @"(\d+)");
                if (projectIdMatch.Success)
                {
                    project.ProjectId = projectIdMatch.Groups[1].Value;
                }

                return project;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting project data: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Project>> ScrapeProjectsAsync(List<string> skills)
        {
            var session = await _projectService.CreateScrapeSessionAsync(string.Join(", ", skills));
            var projects = await ScrapeFreelancerMapAsync(skills, session.Id);
            
            if (projects.Any())
            {
                await _projectService.SaveProjectsAsync(projects);
                session.ProjectsFound = projects.Count;
            }
            
            return projects;
        }
    }
}