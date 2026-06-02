using Abp.Localization;
using Abp.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace InternProject.EntityFrameworkCore.Seed.Host;

public class DefaultLanguagesCreator
{
    public static List<ApplicationLanguage> InitialLanguages => GetInitialLanguages();

    private readonly InternProjectDbContext _context;

    private static List<ApplicationLanguage> GetInitialLanguages()
    {
        var tenantId = InternProjectConsts.MultiTenancyEnabled ? null : (int?)MultiTenancyConsts.DefaultTenantId;
        return new List<ApplicationLanguage>
        {
            new ApplicationLanguage(tenantId, "en", "English", "famfamfam-flags us"),
            new ApplicationLanguage(tenantId, "vi", "Tiếng Việt", "famfamfam-flags vn")
        };
    }

    public DefaultLanguagesCreator(InternProjectDbContext context)
    {
        _context = context;
    }

    public void Create()
    {
        CreateLanguages();
        DeleteUnusedLanguages();
    }

    private void CreateLanguages()
    {
        foreach (var language in InitialLanguages)
        {
            AddLanguageIfNotExists(language);
        }
    }

    private void AddLanguageIfNotExists(ApplicationLanguage language)
    {
        if (_context.Languages.IgnoreQueryFilters().Any(l => l.TenantId == language.TenantId && l.Name == language.Name))
        {
            return;
        }

        _context.Languages.Add(language);
        _context.SaveChanges();
    }

    private void DeleteUnusedLanguages()
    {
        var activeLanguageNames = InitialLanguages.Select(l => l.Name).ToList();
        var languagesToDelete = _context.Languages
            .IgnoreQueryFilters()
            .Where(l => !activeLanguageNames.Contains(l.Name))
            .ToList();

        if (languagesToDelete.Any())
        {
            _context.Languages.RemoveRange(languagesToDelete);
            _context.SaveChanges();
        }
    }
}
