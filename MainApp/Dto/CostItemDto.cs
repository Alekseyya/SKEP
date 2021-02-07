using System.Collections.Generic;


namespace MainApp.Dto
{
    public class CostItemDto
    {
        public int ID { get; set; }
        public string ShortName { get; set; }
        public string Title { get; set; }
        public string FullName
        {
            get
            {
                return ((ShortName != null) ? ShortName.Trim() + ". " : "") + ((Title != null) ? Title.Trim() : "");
            }
        }

        public IEnumerable<CostItemDto> Versions { get; set; }
    }
}