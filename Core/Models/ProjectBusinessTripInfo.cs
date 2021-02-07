namespace Core.Models
{
    public class ProjectBusinessTripInfo
    {
        public int Id { get; set; }

        public string ShortName { get; set; }

        public string Title { get; set; }

        public string FullName { get; set; }

        public int? ProjectTypeId { get; set; }
        public string ProjectTypeShortName { get; set; }

        public int? DepartmentId { get; set; }
        public string DepartmentShortName { get; set; }
        public string DepartmentShortTitle { get; set; }
        public string DepartmentTitle { get; set; }

        public int? BusinessTripCostSubItemId { get; set; }
        public string BusinessTripCostSubItemShortName { get; set; }
        public string BusinessTripCostSubItemTitle { get; set; }
    }
}
