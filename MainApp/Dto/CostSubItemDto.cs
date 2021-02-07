using System.Collections.Generic;


namespace MainApp.Dto
{
    public class CostSubItemDto
    {
        public int ID { get; set; }
        public string ShortName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int CostItemID { get; set; }
        public virtual CostItemDto CostItem { get; set; }
        public string FullName
        {
            get
            {
                return ((ShortName != null) ? ShortName.Trim() + ". " : "") + ((Title != null) ? Title.Trim() : "");
            }
        }

        public IEnumerable<CostSubItemDto> Versions { get; set; }
    }
}