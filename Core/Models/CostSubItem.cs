using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Core.Models.Attributes;
using Core.Models.RBAC;


namespace Core.Models
{
    [AllowRecycleBin]
    [DisplayTableName("Подстатьи затрат")]
    public class CostSubItem : BaseModel
    {
        [Display(Name = "ИД")]
        public int ID { get; set; }

        [Required]
        [Display(Name = "Код подстатьи")]
        public string ShortName { get; set; }

        [Required]
        [Display(Name = "Наименование подстатьи")]
        public string Title { get; set; }

        [Required]
        [Display(Name = "Содержание подстатьи")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Cтатья затрат")]
        public int CostItemID { get; set; }
        public virtual CostItem CostItem { get; set; }

        [Display(Name = "Прочие затраты")]
        public bool IsProjectOtherCosts { get; set; }

        [Display(Name = "Затраты на Performance Bonus")]
        public bool IsProjectPerformanceBonusCosts { get; set; }

        [Display(Name = "Затраты на оборудование для перепродажи")]
        public bool IsProjectEquipmentCostsForResale { get; set; }

        [Display(Name = "Затраты на субподрядчиков")]
        public bool IsProjectSubcontractorsCosts { get; set; }

        [Display(Name = "Затраты на командировки")]
        public bool IsProjectBusinessTripCosts { get; set; }

        [Display(Name = "Затраты на плановые изменения ФОТ")]
        public bool IsEmployeePayrollCosts { get; set; }

        [Display(Name = "Подстатья")]
        public string FullName
        {
            get
            {
                return ((ShortName != null) ? ShortName.Trim() + ". " : "") + ((Title != null) ? Title.Trim() : "");
            }
        }

        public IEnumerable<CostSubItem> Versions { get; set; }
    }
}