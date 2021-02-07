using System.Linq;


namespace Core.Models.RBAC
{
    public class Operation
    {
        private long _operationValue;

        public Operation(int value)
        {
            _operationValue = value;
        }

        public static implicit operator Operation(int value)
        {
            return new Operation(value);
        }

        public static implicit operator int(Operation op)
        {
            return (int)op._operationValue;
        }

        public override bool Equals(object obj)
        {
            Operation otherObj = (Operation)obj;
            return otherObj._operationValue.Equals(this._operationValue);
        }

        public override int GetHashCode()
        {
            return (int)_operationValue;
        }

        public static bool operator ==(Operation op1, Operation op2)
        {
            if (object.ReferenceEquals(op1, null) || object.ReferenceEquals(op2, null))
                return (object.ReferenceEquals(op1, null) && object.ReferenceEquals(op2, null));
            else
                return op1.Equals(op2);

        }
        public static bool operator !=(Operation op1, Operation op2)
        {
            return !(op1 == op2);
        }

        public static OperationSet operator |(Operation op1, Operation op2)
        {
            OperationSet value1 = new OperationSet(op1);
            OperationSet value2 = new OperationSet(op2);

            if (value2?.Operations != null)
            {
                value1.Operations.AddRange(value2.Operations);
                var all = value1.Operations.Distinct().ToList();
                value1.Operations = all;
            }
            return value1;
        }

        public static readonly int MinValue = 0;

        public static readonly Operation AdminFullAccess = 10;

        public static readonly Operation RPCSUserView = 20;
        public static readonly Operation RPCSUserCreateUpdate = 21;
        public static readonly Operation RPCSUserDelete = 22;
        public static readonly Operation ServiceTablesView = 23;
        public static readonly Operation ADSyncAccess = 24;
        public static readonly Operation BitrixSyncAccess = 25;
        public static readonly Operation TimesheetProcessingAccess = 26;
        public static readonly Operation ProductionCalendarRecordCreateUpdate = 27;
        public static readonly Operation ProductionCalendarRecordDelete = 28;
        public static readonly Operation AppPropertiesAccess = 29;
        public static readonly Operation ApiAccessDataView = 30;
        public static readonly Operation ApiAccessDataCreateUpdate = 31; //пока не используется

        public static readonly Operation EmployeeView = 40;
        public static readonly Operation EmployeeCreateUpdate = 41;
        public static readonly Operation EmployeeDelete = 42;
        public static readonly Operation EmployeeExcelExport = 43;
        public static readonly Operation OrgChartView = 44;
        public static readonly Operation EmployeeSelfUpdate = 45;
        public static readonly Operation EmployeeADUpdate = 46;
        public static readonly Operation EmployeePersonalDataView = 47;
        public static readonly Operation EmployeeSubEmplPersonalDataView = 48;
        public static readonly Operation EmployeeFullListView = 49;

        public static readonly Operation EmployeePayrollReportView = 50;
        public static readonly Operation EmployeeIDServiceAccess = 51;

        public static readonly Operation EmployeeIdentityDocsView = 52;
        public static readonly Operation EmployeeIdentityDocsUpdate = 53;

        public static readonly Operation DepartmentView = 60;
        public static readonly Operation DepartmentCreateUpdate = 61;
        public static readonly Operation DepartmentDelete = 62;
        public static readonly Operation DepartmentListView = 63;

        public static readonly Operation PositionView = 70;
        public static readonly Operation PositionCreateUpdate = 71;
        public static readonly Operation PositionDelete = 72;

        public static readonly Operation GradView = 80;
        public static readonly Operation GradCreateUpdate = 81;
        public static readonly Operation GradDelete = 82;

        public static readonly Operation OrganizationView = 90;
        public static readonly Operation OrganizationCreateUpdate = 91;
        public static readonly Operation OrganizationDelete = 92;

        public static readonly Operation EmployeeLocationView = 100;
        public static readonly Operation EmployeeLocationCreateUpdate = 101;
        public static readonly Operation EmployeeLocationDelete = 102;

        public static readonly Operation ProjectListView = 110;
        public static readonly Operation ProjectView = 111;
        public static readonly Operation ProjectMyProjectView = 112;
        public static readonly Operation ProjectCreateUpdate = 113;
        public static readonly Operation ProjectDelete = 114;
        public static readonly Operation ProjectsHoursReportView = 115;
        public static readonly Operation ProjectsCostsReportView = 116;
        public static readonly Operation ProjectsHoursReportViewForManagedEmployees = 117;
        public static readonly Operation ProjectExcelExport = 118;
        public static readonly Operation ProjectMyDepartmentProjectView = 119;

        public static readonly Operation ProjectMemberView = 130;
        public static readonly Operation ProjectMemberCreateUpdate = 131;
        public static readonly Operation ProjectMemberDelete = 132;
        public static readonly Operation ProjectMemberWorkloadView = 133;
        public static readonly Operation ProjectMemberMyPeopleView = 134;

        public static readonly Operation ProjectRoleCreateUpdate = 140;
        public static readonly Operation ProjectRoleView = 141;
        public static readonly Operation ProjectRoleDelete = 142;

        public static readonly Operation ProjectTypeCreateUpdate = 150;
        public static readonly Operation ProjectTypeView = 151;
        public static readonly Operation ProjectTypeDelete = 152;

        // доступа к oostorage
        public static readonly Operation OOAccessAllow = 160;
        public static readonly Operation OOAccessSubEmplReadPayrollAccess = 161;
        public static readonly Operation OOAccessFullPayrollAccess = 162;
        public static readonly Operation OOAccessFullReadPayrollAccess = 163;
        public static readonly Operation OOAccessSubEmplPayrollChangeAccess = 164;
        public static readonly Operation OOAccessFullPayrollChangeAccess = 165;
        public static readonly Operation OOAccessFullReadPayrollChangeAccess = 166;

        public static readonly Operation FinReportView = 170;
        public static readonly Operation FinDataView = 171;
        public static readonly Operation FinDataCreateUpdate = 172;
        public static readonly Operation FinDataDelete = 173;

        public static readonly Operation FinDataViewForMyDepartments = 174;

        public static readonly Operation TSAutoHoursRecordCreateUpdate = 181;
        public static readonly Operation TSAutoHoursRecordView = 182;
        public static readonly Operation TSAutoHoursRecordDelete = 183;

        public static readonly Operation VacationRecordCreateUpdate = 190;
        public static readonly Operation VacationRecordView = 191;
        public static readonly Operation VacationRecordDelete = 192;

        public static readonly Operation ReportingPeriodCreateUpdate = 210;
        public static readonly Operation ReportingPeriodView = 211;
        public static readonly Operation ReportingPeriodDelete = 212;

        public static readonly Operation QualifyingRoleCreateUpdate = 220;
        public static readonly Operation QualifyingRoleView = 221;
        public static readonly Operation QualifyingRoleDelete = 222;

        public static readonly Operation QualifyingRoleRateCreateUpdate = 230;
        public static readonly Operation QualifyingRoleRateView = 231;
        public static readonly Operation QualifyingRoleRateDelete = 232;

        public static readonly Operation EmployeeQualifyingRoleCreateUpdate = 240;
        public static readonly Operation EmployeeQualifyingRoleView = 241;
        public static readonly Operation EmployeeQualifyingRoleDelete = 242;

        public static readonly Operation EmployeeCategoryCreateUpdate = 250;
        public static readonly Operation EmployeeCategoryView = 251;
        public static readonly Operation EmployeeCategoryDelete = 252;

        // Операции над Вехами проектов
        public static readonly Operation ProjectScheduleEntryCreateUpdate = 260;
        public static readonly Operation ProjectScheduleEntryView = 261;
        public static readonly Operation ProjectScheduleEntryDelete = 262;

        // Операции над типами Вех
        public static readonly Operation ProjectScheduleEntryTypeCreateUpdate = 270;
        public static readonly Operation ProjectScheduleEntryTypeView = 271;
        public static readonly Operation ProjectScheduleEntryTypeDelete = 272;

        //Таймшит
        public static readonly Operation TSHoursRecordCreateUpdate = 281;
        public static readonly Operation TSHoursRecordDelete = 282;
        public static readonly Operation TSHoursRecordView = 283;
        public static readonly Operation TSHoursRecordCreateUpdateMyHours = 284;
        public static readonly Operation TSHoursRecordDeleteMyHours = 285;
        public static readonly Operation TSHoursRecordPMApproveHours = 286;
        public static readonly Operation TSCompletenessReportView = 287;
        public static readonly Operation TSCompletenessReportViewForManagedEmployees = 288;
        public static readonly Operation TSApproveHoursReportView = 289;
        public static readonly Operation TSApproveHoursReportViewForManagedEmployees = 290;
        public static readonly Operation TSHoursUtilizationReportView = 291;

        public static readonly Operation ProjectExternalWorkspaceCreateUpdate = 310;
        public static readonly Operation ProjectExternalWorkspaceView = 311;
        public static readonly Operation ProjectExternalWorkspaceDelete = 312;

        // Трудоустройство сотрудника
        public static readonly Operation EmployeeOrganisationCreateUpdate = 320;
        public static readonly Operation EmployeeOrganisationView = 321;
        public static readonly Operation EmployeeOrganisationDelete = 322;
        
        // Операции над статус-отчетами
        public static readonly Operation ProjectStatusRecordCreateUpdate = 330;
        public static readonly Operation ProjectStatusRecordView = 331;
        public static readonly Operation ProjectStatusRecordDelete = 332;
        
        public static readonly int MaxValue = 350;
    }
}
