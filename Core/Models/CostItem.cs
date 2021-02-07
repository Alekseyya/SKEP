using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Core.Models.Attributes;
using Core.Models.RBAC;


namespace Core.Models
{
    [AllowRecycleBin]
    [DisplayTableName("Статьи затрат")]
    public class CostItem : BaseModel
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Display(Name = "Код статьи")]
        [Required]
        public string ShortName { get; set; }

        [Display(Name = "Наименование статьи")]
        [Required]
        public string Title { get; set; }

        [Display(Name = "Статья")]
        public string FullName
        {
            get
            {
                return ((ShortName != null) ? ShortName.Trim() + ". " : "") + ((Title != null) ? Title.Trim() : "");
            }
        }

        public IEnumerable<CostItem> Versions { get; set; }
    }
}