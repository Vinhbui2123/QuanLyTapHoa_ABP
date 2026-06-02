using System.ComponentModel.DataAnnotations;

namespace InternProject.Users.Dto;

public class ChangeUserLanguageDto
{
    [Required]
    public string LanguageName { get; set; }
}