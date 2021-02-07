using System.ComponentModel.DataAnnotations;

namespace Core.JIRA
{
    public enum ErrorTypesJira
    {
        [Display(Name = "Код проекта не определен")]
        ProjectCodeNotDefined = 0,
        [Display(Name = "Не найден в СКИПР")]
        ProjectCodeNotFound = 1
    }
}
