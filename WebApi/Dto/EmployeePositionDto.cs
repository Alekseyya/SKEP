
namespace WebApi.Dto
{
    public class EmployeePositionDto
    {
        public int ID { get; set; }
        public string ShortName { get; set; }
        public string Title { get; set; }
        public string FullName
        {
            get
            {
                return ((ShortName != null) ? ShortName.Trim() + " - " : "") + ((Title != null) ? Title.Trim() : "");
            }
        }
    }
}
