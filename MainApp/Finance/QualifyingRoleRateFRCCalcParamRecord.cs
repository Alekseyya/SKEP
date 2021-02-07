

using Core.Models;

namespace MainApp.Finance
{
    public class QualifyingRoleRateFRCCalcParamRecord
    {
        public int ID { get; set; }
        public int DepartmentID { get; set; }
        public Department Department { get; set; }
        public double FRCCorrectionFactor { get; set; }
        public double FRCInflationRate { get; set; }
    }
}
