using Microsoft.EntityFrameworkCore;
using Automatronus.Data;
using Automatronus.Models;

namespace Automatronus.Services
{
    public class ProfileService
    {
        private readonly AutomatronusContext _context;

        public ProfileService(AutomatronusContext context)
        {
            _context = context;
        }

        public async Task<Profile?> GetCurrentProfileAsync()
        {
            return await _context.Profiles
                .Include(p => p.Skills)
                .OrderByDescending(p => p.UpdatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<Profile> CreateOrUpdateProfileAsync(Profile profile)
        {
            var existingProfile = await GetCurrentProfileAsync();
            
            if (existingProfile != null)
            {
                existingProfile.Email = profile.Email;
                existingProfile.AppName = profile.AppName;
                existingProfile.Name = profile.Name;
                existingProfile.PdfFileName = profile.PdfFileName;
                existingProfile.UpdatedAt = DateTime.Now;
                
                _context.Update(existingProfile);
                await _context.SaveChangesAsync();
                return existingProfile;
            }
            else
            {
                _context.Profiles.Add(profile);
                await _context.SaveChangesAsync();
                return profile;
            }
        }

        public async Task AddSkillsToProfileAsync(int profileId, List<string> skillNames)
        {
            var existingSkills = await _context.Skills
                .Where(s => s.ProfileId == profileId)
                .ToListAsync();
            
            _context.Skills.RemoveRange(existingSkills);
            
            var skills = skillNames.Select(name => new Skill
            {
                Name = name,
                ProfileId = profileId
            }).ToList();
            
            _context.Skills.AddRange(skills);
            await _context.SaveChangesAsync();
        }

        public async Task<List<string>> GetSkillNamesAsync(int profileId)
        {
            return await _context.Skills
                .Where(s => s.ProfileId == profileId)
                .Select(s => s.Name)
                .ToListAsync();
        }
    }
}