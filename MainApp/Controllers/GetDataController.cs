using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Core.Models.RBAC;
using Data;
using MainApp.RBAC.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace MainApp.Controllers
{
    /// <summary>
    /// Контроллен нужен для того, чтобы перебросить базу из MySql в PosgreSQL
    /// </summary>
    public class GetDataController : Controller
    {
        private readonly DbContextOptions<RPCSContext> _dbOptions;
        private RPCSContext _db;

        public GetDataController(DbContextOptions<RPCSContext> dbOptions)
        {
            _dbOptions = dbOptions;
            _db = new RPCSContext(_dbOptions);
        }

        //Расскоментировать, если надо залить данные
        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        //        public ActionResult SetDataToPostgres()
        //        {
        //            var projectstatusrecord = new List<ProjectStatusRecord>(){
        //     new ProjectStatusRecord(){ID = 2, StatusPeriodName = "12.02 - 16.02", ProjectStatusBeginDate = DateTime.Parse("12.02.2019 0:00:00"), ProjectStatusEndDate = DateTime.Parse("16.02.2019 0:00:00"), SupervisorComments = "", ProblemsText = "erwer", ProposedSolutionText = "rw", ProjectID = 22, ContractReceivedMoneyAmountActual = 150000.00m, PaidToSubcontractorsAmountActual = 45000.00m, EmployeePayrollAmountActual = 156000.00m, OtherCostsAmountActual = 512000.00m, StatusText = "Второе вложение денег", RiskIndicatorFlag = ProjectStatusRiskIndicatorFlag.Yellow, RiskIndicatorComments = "", PlannedReleaseInfo = "", ExternalDependenciesInfo = "", ItemID = 2, IsVersion = false, VersionNumber = 1, Created = DateTime.Parse("16.02.2019 17:54:50"), Modified = DateTime.Parse("12.04.2019 15:44:12"), Author = "Ярчук Алексей Александрович", AuthorSID = "", Editor = "Ярчук Алексей Александрович", EditorSID = "S-1-5-21-949748521-2455742171-1266078757", IsDeleted = false, DeletedDate = null, DeletedBy = "", DeletedBySID = ""
        //    },
        //     new ProjectStatusRecord(){ID = 3, StatusPeriodName = "16.02 - 25.02", ProjectStatusBeginDate = DateTime.Parse("16.02.2019 0:00:00"), ProjectStatusEndDate = DateTime.Parse("25.02.2019 0:00:00"), SupervisorComments = "", ProblemsText = "erwerw", ProposedSolutionText = "werwer", ProjectID = 22, ContractReceivedMoneyAmountActual = 195000.00m, PaidToSubcontractorsAmountActual = 95000.00m, EmployeePayrollAmountActual = 196000.00m, OtherCostsAmountActual = 596000.00m, StatusText = "Третье вложение денег", RiskIndicatorFlag = ProjectStatusRiskIndicatorFlag.All, RiskIndicatorComments = "Без рисков", PlannedReleaseInfo = "Какая-то информация", ExternalDependenciesInfo = "", ItemID = null, IsVersion = false, VersionNumber = 0, Created = DateTime.Parse("25.02.2019 17:56:22"), Modified = null, Author = "Ярчук Алексей Александрович", AuthorSID = "", Editor = "", EditorSID = "", IsDeleted = false, DeletedDate = null, DeletedBy = "", DeletedBySID = ""
        //    },
        //     new ProjectStatusRecord(){ID = 21, StatusPeriodName = "22.04 - 24.04", ProjectStatusBeginDate = DateTime.Parse("22.04.2019 0:00:00"), ProjectStatusEndDate = DateTime.Parse("24.04.2019 0:00:00"), SupervisorComments = "", ProblemsText = "wer", ProposedSolutionText = "we", ProjectID = 22, ContractReceivedMoneyAmountActual = 169000.00m, PaidToSubcontractorsAmountActual = 189000.00m, EmployeePayrollAmountActual = null, OtherCostsAmountActual = null, StatusText = "енгкенг", RiskIndicatorFlag = ProjectStatusRiskIndicatorFlag.Green, RiskIndicatorComments = "", PlannedReleaseInfo = "", ExternalDependenciesInfo = "", ItemID = 21, IsVersion = false, VersionNumber = 1, Created = DateTime.Parse("12.04.2019 15:52:38"), Modified = DateTime.Parse("12.04.2019 15:59:13"), Author = "Ярчук Алексей Александрович", AuthorSID = "S-1-5-21-949748521-2455742171-1266078757", Editor = "Ярчук Алексей Александрович", EditorSID = "S-1-5-21-949748521-2455742171-1266078757", IsDeleted = false, DeletedDate = null, DeletedBy = "", DeletedBySID = ""
        //    },
        //     new ProjectStatusRecord(){ID = 24, StatusPeriodName = "11.03 - 14.03", ProjectStatusBeginDate = DateTime.Parse("11.03.2019 0:00:00"), ProjectStatusEndDate = DateTime.Parse("14.04.2019 0:00:00"), SupervisorComments = "", ProblemsText = "we", ProposedSolutionText = "erwer", ProjectID = 22, ContractReceivedMoneyAmountActual = null, PaidToSubcontractorsAmountActual = null, EmployeePayrollAmountActual = null, OtherCostsAmountActual = null, StatusText = "куцуцккуцкуцкуц", RiskIndicatorFlag = ProjectStatusRiskIndicatorFlag.Green, RiskIndicatorComments = "куцкуцкуцкуцкуц", PlannedReleaseInfo = "йукуцкуц", ExternalDependenciesInfo = "куцкуцкуц", ItemID = null, IsVersion = false, VersionNumber = 0, Created = DateTime.Parse("15.04.2019 15:27:21"), Modified = DateTime.Parse("15.04.2019 15:27:21"), Author = "Ярчук Алексей Александрович", AuthorSID = "S-1-5-21-949748521-2455742171-1266078757", Editor = "Ярчук Алексей Александрович", EditorSID = "S-1-5-21-949748521-2455742171-1266078757", IsDeleted = false, DeletedDate = null, DeletedBy = "", DeletedBySID = ""
        //    },
        //     new ProjectStatusRecord(){ID = 25, StatusPeriodName = "22.04 - 25.04", ProjectStatusBeginDate = DateTime.Parse("22.04.2019 0:00:00"), ProjectStatusEndDate = DateTime.Parse("25.04.2019 0:00:00"), SupervisorComments = "", ProblemsText = "fghfgh678678", ProposedSolutionText = "fghfu678678", ProjectID = 22, ContractReceivedMoneyAmountActual = null, PaidToSubcontractorsAmountActual = null, EmployeePayrollAmountActual = null, OtherCostsAmountActual = null, StatusText = "fghfgh67867", RiskIndicatorFlag = ProjectStatusRiskIndicatorFlag.Green, RiskIndicatorComments = "678678678", PlannedReleaseInfo = "h';dgfj;dmgfjh;ldmfjh", ExternalDependenciesInfo = "gfhfgh", ItemID = null, IsVersion = false, VersionNumber = 0, Created = DateTime.Parse("23.04.2019 19:38:28"), Modified = DateTime.Parse("23.04.2019 19:38:28"), Author = "Ярчук Алексей Александрович", AuthorSID = "S-1-5-21-949748521-2455742171-1266078757", Editor = "Ярчук Алексей Александрович", EditorSID = "S-1-5-21-949748521-2455742171-1266078757", IsDeleted = false, DeletedDate = null, DeletedBy = "", DeletedBySID = ""
        //    },
        //     new ProjectStatusRecord(){ID = 26, StatusPeriodName = "24.04.19 - 25.04.19", ProjectStatusBeginDate = DateTime.Parse("24.04.2019 0:00:00"), ProjectStatusEndDate = DateTime.Parse("25.04.2019 0:00:00"), SupervisorComments = "", ProblemsText = "567567", ProposedSolutionText = "567567", ProjectID = 22, ContractReceivedMoneyAmountActual = 110000.00m, PaidToSubcontractorsAmountActual = 269000.00m, EmployeePayrollAmountActual = null, OtherCostsAmountActual = null, StatusText = "567567", RiskIndicatorFlag = ProjectStatusRiskIndicatorFlag.Green, RiskIndicatorComments = "", PlannedReleaseInfo = "", ExternalDependenciesInfo = "", ItemID = 26, IsVersion = false, VersionNumber = 1, Created = DateTime.Parse("23.04.2019 19:40:01"), Modified = DateTime.Parse("23.04.2019 19:42:58"), Author = "Ярчук Алексей Александрович", AuthorSID = "S-1-5-21-949748521-2455742171-1266078757", Editor = "Ярчук Алексей Александрович", EditorSID = "S-1-5-21-949748521-2455742171-1266078757", IsDeleted = false, DeletedDate = null, DeletedBy = "", DeletedBySID = ""
        //    },
        //     new ProjectStatusRecord(){ID = 28, StatusPeriodName = "24.04.19 - 25.04.19", ProjectStatusBeginDate = DateTime.Parse("24.04.2019 0:00:00"), ProjectStatusEndDate = DateTime.Parse("25.04.2019 0:00:00"), SupervisorComments = "", ProblemsText = "ytuty", ProposedSolutionText = "tyutyu", ProjectID = 128, ContractReceivedMoneyAmountActual = 50000.00m, PaidToSubcontractorsAmountActual = null, EmployeePayrollAmountActual = null, OtherCostsAmountActual = null, StatusText = "fhfghfgh", RiskIndicatorFlag = ProjectStatusRiskIndicatorFlag.Green, RiskIndicatorComments = "", PlannedReleaseInfo = "", ExternalDependenciesInfo = "", ItemID = null, IsVersion = false, VersionNumber = 0, Created = DateTime.Parse("26.04.2019 17:08:36"), Modified = DateTime.Parse("26.04.2019 17:08:49"), Author = "", AuthorSID = "", Editor = "", EditorSID = "", IsDeleted = false, DeletedDate = null, DeletedBy = "", DeletedBySID = ""
        //    },
        //     new ProjectStatusRecord(){ID = 32, StatusPeriodName = "29.04.19 - 30.04.19", ProjectStatusBeginDate = DateTime.Parse("15.04.2019 0:00:00"), ProjectStatusEndDate = DateTime.Parse("30.04.2019 0:00:00"), SupervisorComments = "", ProblemsText = "234", ProposedSolutionText = "ыцваы", ProjectID = 22, ContractReceivedMoneyAmountActual = null, PaidToSubcontractorsAmountActual = null, EmployeePayrollAmountActual = null, OtherCostsAmountActual = null, StatusText = "34234", RiskIndicatorFlag = ProjectStatusRiskIndicatorFlag.Green, RiskIndicatorComments = "ываыва", PlannedReleaseInfo = "йцуйц", ExternalDependenciesInfo = "у2342", ItemID = null, IsVersion = false, VersionNumber = 0, Created = DateTime.Parse("29.04.2019 16:59:20"), Modified = DateTime.Parse("29.04.2019 16:59:20"), Author = "Ярчук Алексей Александрович", AuthorSID = "S-1-5-21-949748521-2455742171-1266078757", Editor = "Ярчук Алексей Александрович", EditorSID = "S-1-5-21-949748521-2455742171-1266078757", IsDeleted = false, DeletedDate = null, DeletedBy = "", DeletedBySID = ""
        //    },
        //     new ProjectStatusRecord(){ID = 33, StatusPeriodName = "06.05.19 - 09.05.19", ProjectStatusBeginDate = DateTime.Parse("06.05.2019 0:00:00"), ProjectStatusEndDate = DateTime.Parse("09.05.2019 0:00:00"), SupervisorComments = "", ProblemsText = "234234", ProposedSolutionText = "234", ProjectID = 22, ContractReceivedMoneyAmountActual = null, PaidToSubcontractorsAmountActual = null, EmployeePayrollAmountActual = null, OtherCostsAmountActual = null, StatusText = "234", RiskIndicatorFlag = ProjectStatusRiskIndicatorFlag.Green, RiskIndicatorComments = "234234", PlannedReleaseInfo = "234", ExternalDependenciesInfo = "234234", ItemID = null, IsVersion = false, VersionNumber = 0, Created = DateTime.Parse("06.05.2019 16:57:06"), Modified = DateTime.Parse("06.05.2019 16:57:06"), Author = "Ярчук Алексей Александрович", AuthorSID = "S-1-5-21-949748521-2455742171-1266078757", Editor = "Ярчук Алексей Александрович", EditorSID = "S-1-5-21-949748521-2455742171-1266078757", IsDeleted = false, DeletedDate = null, DeletedBy = "", DeletedBySID = ""
        //    },
        //     new ProjectStatusRecord(){ID = 34, StatusPeriodName = "01.05.19 - 03.05.19", ProjectStatusBeginDate = DateTime.Parse("01.05.2019 0:00:00"), ProjectStatusEndDate = DateTime.Parse("03.05.2019 0:00:00"), SupervisorComments = "", ProblemsText = "546756", ProposedSolutionText = "7567", ProjectID = 22, ContractReceivedMoneyAmountActual = null, PaidToSubcontractorsAmountActual = null, EmployeePayrollAmountActual = null, OtherCostsAmountActual = null, StatusText = "3456", RiskIndicatorFlag = ProjectStatusRiskIndicatorFlag.Green, RiskIndicatorComments = "567567", PlannedReleaseInfo = "re", ExternalDependenciesInfo = "356345345", ItemID = null, IsVersion = false, VersionNumber = 0, Created = DateTime.Parse("06.05.2019 17:32:35"), Modified = DateTime.Parse("06.05.2019 17:32:35"), Author = "Ярчук Алексей Александрович", AuthorSID = "S-1-5-21-949748521-2455742171-1266078757", Editor = "Ярчук Алексей Александрович", EditorSID = "S-1-5-21-949748521-2455742171-1266078757", IsDeleted = false, DeletedDate = null, DeletedBy = "", DeletedBySID = ""
        //    },
        //     new ProjectStatusRecord(){ID = 35, StatusPeriodName = "17.06.19 - 21.06.19", ProjectStatusBeginDate = DateTime.Parse("17.06.2019 0:00:00"), ProjectStatusEndDate = DateTime.Parse("21.06.2019 0:00:00"), SupervisorComments = "", ProblemsText = "qweqwe", ProposedSolutionText = "qweqwe", ProjectID = 85, ContractReceivedMoneyAmountActual = null, PaidToSubcontractorsAmountActual = null, EmployeePayrollAmountActual = null, OtherCostsAmountActual = null, StatusText = "qweqwe", RiskIndicatorFlag = ProjectStatusRiskIndicatorFlag.Green, RiskIndicatorComments = "", PlannedReleaseInfo = "", ExternalDependenciesInfo = "qweqwe", ItemID = null, IsVersion = false, VersionNumber = 0, Created = DateTime.Parse("17.06.2019 18:13:48"), Modified = DateTime.Parse("17.06.2019 18:13:48"), Author = "Ярчук Алексей Александрович", AuthorSID = "S-1-5-21-949748521-2455742171-1266078757", Editor = "Ярчук Алексей Александрович", EditorSID = "S-1-5-21-949748521-2455742171-1266078757", IsDeleted = false, DeletedDate = null, DeletedBy = "", DeletedBySID = ""
        //    },
        //     new ProjectStatusRecord(){ID = 38, StatusPeriodName = "17.06.19 - 21.06.19", ProjectStatusBeginDate = DateTime.Parse("17.06.2019 0:00:00"), ProjectStatusEndDate = DateTime.Parse("21.06.2019 0:00:00"), SupervisorComments = "", ProblemsText = "tert", ProposedSolutionText = "ertert", ProjectID = 22, ContractReceivedMoneyAmountActual = null, PaidToSubcontractorsAmountActual = null, EmployeePayrollAmountActual = null, OtherCostsAmountActual = null, StatusText = "ter", RiskIndicatorFlag = ProjectStatusRiskIndicatorFlag.Green, RiskIndicatorComments = "ertert", PlannedReleaseInfo = "erter", ExternalDependenciesInfo = "tert", ItemID = null, IsVersion = false, VersionNumber = 0, Created = DateTime.Parse("27.06.2019 12:50:54"), Modified = DateTime.Parse("27.06.2019 12:50:54"), Author = "Ярчук Алексей Александрович", AuthorSID = "S-1-5-21-949748521-2455742171-1266078757", Editor = "Ярчук Алексей Александрович", EditorSID = "S-1-5-21-949748521-2455742171-1266078757", IsDeleted = false, DeletedDate = null, DeletedBy = "", DeletedBySID = ""
        //    }
        //};

        //            //Пример

        //            try
        //            {
        //                foreach (var entity in projectstatusrecord)
        //                {
        //                    _db.ProjectStatusRecords.Add(entity);
        //                    _db.SaveChanges();
        //                }
        //            }
        //            catch (Exception e)
        //            {
        //                Console.WriteLine(e);
        //                throw;
        //            }

        //            return Content("Миграция для бд " + projectstatusrecord.GetType().ToString());
        //        }

        [OperationActionFilter(nameof(Operation.AdminFullAccess))]
        public string GetListData()
        {

            var result = "";
            var employee = _db.Employees.ToList();
            result += Create(employee);
            var employeeposition = _db.EmployeePositions.ToList();
            result += Create(employeeposition);
            var department = _db.Departments.ToList();
            result += Create(department);
            var organisation = _db.Organisations.ToList();
            result += Create(organisation);
            var project = _db.Projects.ToList();
            result += Create(project);
            var employeegrad = _db.EmployeeGrads.ToList();
            result += Create(employeegrad);
            var employeegradassignment = _db.EmployeeGradAssignments.ToList();
            result += Create(employeegradassignment);
            var employeedepartmentassignment = _db.EmployeeDepartmentAssignments.ToList();
            result += Create(employeedepartmentassignment);
            var employeepositionassignment = _db.EmployeePositionAssignments.ToList();
            result += Create(employeepositionassignment);
            var rpcsuser = _db.RPCSUsers.ToList();
            result += Create(rpcsuser);
            var projectreportrecord = _db.ProjectReportRecords.ToList();
            result += Create(projectreportrecord);
            var projectstatusrecord = _db.ProjectStatusRecords.ToList();
            result += Create(projectstatusrecord);
            var employeelocation = _db.EmployeeLocations.ToList();
            result += Create(employeelocation);
            var projectrole = _db.ProjectRoles.ToList();
            result += Create(projectrole);
            var projectmember = _db.ProjectMembers.ToList();
            result += Create(projectmember);
            var productioncalendarrecord = _db.ProductionCalendarRecords.ToList();
            result += Create(productioncalendarrecord);
            var employeepositionofficial = _db.EmployeePositionOfficials.ToList();
            result += Create(employeepositionofficial);
            var tshoursrecord = _db.TSHoursRecords.ToList();
            result += Create(tshoursrecord);
            var employeepositionofficialassignment = _db.EmployeePositionOfficialAssignments.ToList();
            result += Create(employeepositionofficialassignment);
            var projecttype = _db.ProjectTypes.ToList();
            result += Create(projecttype);
            var appproperty = _db.AppProperties.ToList();
            result += Create(appproperty);
            var employeecategory = _db.EmployeeCategories.ToList();
            result += Create(employeecategory);
            var tsautohoursrecord = _db.TSAutoHoursRecords.ToList();
            result += Create(tsautohoursrecord);
            var costitem = _db.CostItems.ToList();
            result += Create(costitem);
            var costsubitem = _db.CostSubItems.ToList();
            result += Create(costsubitem);
            var budgetlimit = _db.BudgetLimits.ToList();
            result += Create(budgetlimit);
            var expensesrecord = _db.ExpensesRecords.ToList();
            result += Create(expensesrecord);
            var vacationrecord = _db.VacationRecords.ToList();
            result += Create(vacationrecord);
            var reportingperiod = _db.ReportingPeriods.ToList();
            result += Create(reportingperiod);
            return result;
        }

        private string GetCodeAddToDatabase()
        {
            var stringList = "";
            var properties = typeof(RPCSContext).GetProperties().Select(s => new { s.PropertyType, s.Name }).ToList();

            foreach (var property in properties)
            {
                try
                {
                    var className = property.PropertyType.GetGenericArguments()[0].Name;
                    var propName = property.Name;
                    stringList += "foreach(var entity in " + className.ToLower() + ")\n" +
                              "{\n" +
                              "\t _db." + propName + ".Add(entity);\n" +
                              "}\n";
                }
                catch (IndexOutOfRangeException)
                {
                    stringList += "";
                }
            }

            return stringList;
        }

        private string GetCodeGetAllTablesInDatabase()
        {
            var stringList = "";
            var properties = typeof(RPCSContext).GetProperties().Select(s => new { s.PropertyType, s.Name }).ToList();
            foreach (var property in properties)
            {
                try
                {
                    var className = property.PropertyType.GetGenericArguments()[0].Name;
                    var propName = property.Name;
                    stringList += "var " + className.ToLower() + " = _db." + propName + ".ToList();\n" +
                        "result += Create(" + className.ToLower() + ");\n";
                }
                catch (IndexOutOfRangeException)
                {
                    stringList += "";
                }
            }
            return stringList;
        }

        private string GetAllEnumInProject()
        {
            var result = "";
            try
            {
                var listEnums = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(t => t.GetTypes())
                    .Where(t => t.IsEnum && t.Namespace == "RPCSWebApp.Models")
                    .ToList();

                foreach (var currentEnum in listEnums)
                {
                    result += "|| .PropertyType == typeof(" + currentEnum.Name + ")\n";
                }

            }
            catch (Exception ex)
            {
                if (ex is System.Reflection.ReflectionTypeLoadException)
                {
                    var typeLoadException = ex as ReflectionTypeLoadException;
                    var loaderExceptions = typeLoadException.LoaderExceptions;
                }
            }


            return "!!!!!!!!!!!!!";
        }

        private string Create<T>(List<T> listModels)
        {
            if (listModels.Count > 0)
            {
                //Получить все проперти кроме ма
                var obj = listModels[0];

                var className = obj.GetType().Name.Substring(0,
                    obj.GetType().Name.IndexOf('_') == -1 ? obj.GetType().Name.Length : obj.GetType().Name.IndexOf('_'));

                //Запись текста начала листа
                string resultString = CreateStringList(className, className);
                //Todo по всему массиву
                for (int i = 0; i < listModels.Count; i++)
                {
                    var currentObject = listModels[i];
                    //Для последнего элемента не ставить ,
                    if (listModels.Count - 1 == i)
                        resultString += "\t new " + className + "(){" + SetPropertiesToParameter(currentObject) + "}";
                    else
                    {
                        resultString += "\t new " + className + "(){" + SetPropertiesToParameter(currentObject) + "},\n";
                    }
                }
                resultString += CreateEndStringList();
                return resultString;
            }

            return "";
        }

        private string SetPropertiesToParameter<T>(T src)
        {
            //Все проперти
            var allPropertiesName = src.GetType().GetProperties()
                .Where(s => s.PropertyType == typeof(int)
                            || s.PropertyType == typeof(int?)
                            || s.PropertyType == typeof(string)
                            || s.PropertyType == typeof(decimal)
                            || s.PropertyType == typeof(decimal?)
                            || s.PropertyType == typeof(double)
                            || s.PropertyType == typeof(double?)
                            || s.PropertyType == typeof(bool)
                            || s.PropertyType == typeof(DateTime)
                            || s.PropertyType == typeof(DateTime?)
                            || s.PropertyType.IsEnum
                ).Where(s => PropertyHaveGetAndSet(s) == true)
                .Select(n => n.Name).ToList();

            var resultString = "";
            var countProperty = 0;
            foreach (var propertyName in allPropertiesName)
            {
                countProperty++;
                var propertyValue = src.GetType().GetProperty(propertyName).GetValue(src, null);
                var propertyType = src.GetType().GetProperty(propertyName).PropertyType;
                //работа со cтрингами
                if (propertyType == typeof(string))
                {
                    if (propertyValue == null || (string)propertyValue == "")
                    {
                        propertyValue = "\"\"";
                    }//если в значении есть типа ad\aa -> изменить на ad\\aa
                    else if (propertyValue.ToString().ToLower().Contains("\\"))
                    {
                        propertyValue = "\"" + propertyValue.ToString().Replace("\\", "\\" + "\\") + "\"";
                    }
                    else
                    {
                        if (propertyValue.ToString().Contains("\""))
                            propertyValue = propertyValue.ToString().Replace("\"", "\\\"");
                        propertyValue = "\"" + propertyValue + "\"";
                    }
                }
                //работа с детималами
                if (propertyType == typeof(decimal) || propertyType == typeof(decimal?))
                {
                    if (propertyValue == null)
                        propertyValue = "null";
                    else
                        propertyValue = propertyValue.ToString().Replace(",", ".") + "m";
                }
                //работа с интами
                if (propertyValue == null && propertyType == typeof(int?))
                {
                    propertyValue = "null";
                }
                //работа с буллеанами
                if (propertyType == typeof(bool))
                {
                    propertyValue = propertyValue.ToString().ToLower();
                }
                //для дататайма
                if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
                {
                    if (propertyValue == null)
                        propertyValue = "null";
                    else
                        propertyValue = "DateTime.Parse(" + "\"" + propertyValue.ToString() + "\"" + ")";
                }
                //для енумов
                if (propertyType.IsEnum)
                { //RPCSWebApp.Models.VacationRecordType - измениние имени до VacationRecordType
                    var stringProperty = propertyType.ToString();
                    var stringEnum = stringProperty.Substring(stringProperty.LastIndexOf(".") + 1,
                        stringProperty.Length - 1 - stringProperty.LastIndexOf("."));
                    propertyValue = stringEnum + "." + propertyValue.ToString();
                }

                //для дабла
                if (propertyType == typeof(double) || propertyType == typeof(double?)) //TODO тут надо доделать будет!!
                {
                    if (propertyValue == null)
                        propertyValue = "null";
                    else
                        propertyValue = propertyValue.ToString().Replace(",", ".");
                }


                //Если последняя проперти чтобы не ставить в конче ,
                if (allPropertiesName.Count == countProperty)
                {
                    resultString += propertyName + " = " + propertyValue + "";
                }
                else
                {
                    resultString += propertyName + " = " + propertyValue + ", ";
                }
            }
            return resultString;
        }

        private string CreateStringList(string listName, string className)
        {
            return "var " + listName.ToLower() + " = new List<" + className + ">(){\n";
        }

        private string CreateEndStringList()
        {
            return "\n};\n";
        }

        private bool PropertyHaveGetAndSet(PropertyInfo property)
        {
            var setter = property.GetSetMethod(true);
            var getter = property.GetGetMethod(true);
            if (setter != null && getter != null)
            {
                return true;
            }

            return false;
        }


    }
}
