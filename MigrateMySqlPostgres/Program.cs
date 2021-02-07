using System;
using System.Collections.Generic;
using System.Linq;
using Core.Extensions;
using Core.Models;
using Data;
using Microsoft.EntityFrameworkCore;



namespace RPCSWebApp.MigrateMySqlPostgres
{
    class Program
    {
        static void Main(string[] args)
        {
            //Подключение к Postgress
            var obPostgresContext = new DbContextOptionsBuilder<RPCSContext>();
            obPostgresContext.UseNpgsql("");

            //Подключение к Mysql
            var obMysqlContext = new DbContextOptionsBuilder<RPCSContextMysql>();
            obMysqlContext.UseMySql("");


            //Ошибки в ProjectStatusRecord, TSHoursRecord, TSAutoHoursRecord, VacationRecord
            //public IEnumerable<TSHoursRecord> Versions { get; set; }  - закоментить в классах

            var listTextDatabase = new List<string>();
            try
            {
                var rpcsUsers = GetDataFromMysql(new RPCSUser(), obMysqlContext);
                SaveChangesInPostgres(rpcsUsers, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(rpcsUsers.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(rpcsUsers.GetType(), true));

                var organisations = GetDataFromMysql(new Organisation(), obMysqlContext);
                SaveChangesInPostgres(organisations, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(organisations.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(organisations.GetType(), true));

                var projectRoles = GetDataFromMysql(new ProjectRole(), obMysqlContext);
                SaveChangesInPostgres(projectRoles, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(projectRoles.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(projectRoles.GetType(), true));

                var productionCalendarRecords = GetDataFromMysql(new ProductionCalendarRecord(), obMysqlContext);
                SaveChangesInPostgres(productionCalendarRecords, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(productionCalendarRecords.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(productionCalendarRecords.GetType(), true));

                var employeeGrads = GetDataFromMysql(new EmployeeGrad(), obMysqlContext);
                SaveChangesInPostgres(employeeGrads, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(employeeGrads.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(employeeGrads.GetType(), true));

                var employeePositions = GetDataFromMysql(new EmployeePosition(), obMysqlContext);
                SaveChangesInPostgres(employeePositions, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(employeePositions.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(employeePositions.GetType(), true));

                var costitems = GetDataFromMysql(new CostItem(), obMysqlContext);
                SaveChangesInPostgres(costitems, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(costitems.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(costitems.GetType(), true));

                var costSubItems = GetDataFromMysql(new CostSubItem(), obMysqlContext);
                SaveChangesInPostgres(costSubItems, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(costSubItems.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(costSubItems.GetType(), true));

                var projectTypes = GetDataFromMysql(new ProjectType(), obMysqlContext);
                SaveChangesInPostgres(projectTypes, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(projectTypes.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(projectTypes.GetType(), true));

                var employeePositionOfficials = GetDataFromMysql(new EmployeePositionOfficial(), obMysqlContext);
                SaveChangesInPostgres(employeePositionOfficials, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(employeePositionOfficials.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(employeePositionOfficials.GetType(), true));

                var employeeLocations = GetDataFromMysql(new EmployeeLocation(), obMysqlContext);
                SaveChangesInPostgres(employeeLocations, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(employeeLocations.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(employeeLocations.GetType(), true));

                //Todo Непонятно как он загрузит департамент, там ссылки на Employee есть. Возможно надо сразу две таблицы грузить и
                //сохранять в одном saveChanges

                #region Сохранение двух таблиц
                //var departments = GetDataFromMysql(new Department(), obMysqlContext);
                //SaveChangesInPostgres(departments, obPostgresContext);
                //listTextDatabase.Add(GetCSharpRepresentation(departments.GetType(), true));
                //Console.WriteLine(GetCSharpRepresentation(departments.GetType(), true));

                //var employees = GetDataFromMysql(new Employee(), obMysqlContext);
                //SaveChangesInPostgres(employees, obPostgresContext);
                //listTextDatabase.Add(GetCSharpRepresentation(employees.GetType(), true));
                //Console.WriteLine(GetCSharpRepresentation(employees.GetType(), true));
                #endregion

                var employees = GetDataFromMysql(new Employee(), obMysqlContext);
                var departments = GetDataFromMysql(new Department(), obMysqlContext);
                SaveChangesInPostgres(employees, departments, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(employees.GetType(), true));
                listTextDatabase.Add(GetCSharpRepresentation(departments.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(employees.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(departments.GetType(), true));

                var employeeCategories = GetDataFromMysql(new EmployeeCategory(), obMysqlContext);
                SaveChangesInPostgres(employeeCategories, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(employeeCategories.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(employeeCategories.GetType(), true));

                var employeePositionOfficialAssignments = GetDataFromMysql(new EmployeePositionOfficialAssignment(), obMysqlContext);
                SaveChangesInPostgres(employeePositionOfficialAssignments, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(employeePositionOfficialAssignments.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(employeePositionOfficialAssignments.GetType(), true));

                var vacationRecords = GetDataFromMysql(new VacationRecord(), obMysqlContext);
                SaveChangesInPostgres(vacationRecords, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(vacationRecords.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(vacationRecords.GetType(), true));

                var employeeGradAssignments = GetDataFromMysql(new EmployeeGradAssignment(), obMysqlContext);
                SaveChangesInPostgres(employeeGradAssignments, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(employeeGradAssignments.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(employeeGradAssignments.GetType(), true));

                var employeeDepartmentAssignments = GetDataFromMysql(new EmployeeDepartmentAssignment(), obMysqlContext);
                SaveChangesInPostgres(employeeDepartmentAssignments, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(employeeDepartmentAssignments.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(employeeDepartmentAssignments.GetType(), true));

                var employeePositionAssignments = GetDataFromMysql(new EmployeePositionAssignment(), obMysqlContext);
                SaveChangesInPostgres(employeePositionAssignments, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(employeePositionAssignments.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(employeePositionAssignments.GetType(), true));

                var projects = GetDataFromMysql(new Project(), obMysqlContext);
                SaveChangesInPostgres(projects, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(projects.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(projects.GetType(), true));

                var tsAutoHoursRecords = GetDataFromMysql(new TSAutoHoursRecord(), obMysqlContext);
                SaveChangesInPostgres(tsAutoHoursRecords, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(tsAutoHoursRecords.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(tsAutoHoursRecords.GetType(), true));

                var tsHoursRecords = GetDataFromMysql(new TSHoursRecord(), obMysqlContext);
                SaveChangesInPostgres(tsHoursRecords, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(tsHoursRecords.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(tsHoursRecords.GetType(), true));

                var projectStatusRecords = GetDataFromMysql(new ProjectStatusRecord(), obMysqlContext);
                SaveChangesInPostgres(projectStatusRecords, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(projectStatusRecords.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(projectStatusRecords.GetType(), true));

                var projectReportRecords = GetDataFromMysql(new ProjectReportRecord(), obMysqlContext);
                SaveChangesInPostgres(projectReportRecords, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(projectReportRecords.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(projectReportRecords.GetType(), true));

                var expensesRecords = GetDataFromMysql(new ExpensesRecord(), obMysqlContext);
                SaveChangesInPostgres(expensesRecords, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(expensesRecords.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(expensesRecords.GetType(), true));

                var reportingPeriods = GetDataFromMysql(new ReportingPeriod(), obMysqlContext);
                SaveChangesInPostgres(reportingPeriods, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(reportingPeriods.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(reportingPeriods.GetType(), true));

                var budgetLimits = GetDataFromMysql(new BudgetLimit(), obMysqlContext);
                SaveChangesInPostgres(budgetLimits, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(budgetLimits.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(budgetLimits.GetType(), true));

                var projectMembers = GetDataFromMysql(new ProjectMember(), obMysqlContext);
                SaveChangesInPostgres(projectMembers, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(projectMembers.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(projectMembers.GetType(), true));

                var appProperties = GetDataFromMysql(new AppProperty(), obMysqlContext);
                SaveChangesInPostgres(appProperties, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(appProperties.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(appProperties.GetType(), true));

                var qualifyingRoles = GetDataFromMysql(new QualifyingRole(), obMysqlContext);
                SaveChangesInPostgres(qualifyingRoles, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(qualifyingRoles.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(qualifyingRoles.GetType(), true));

                var qualifyingRoleRates = GetDataFromMysql(new QualifyingRoleRate(), obMysqlContext);
                SaveChangesInPostgres(qualifyingRoleRates, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(qualifyingRoleRates.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(qualifyingRoleRates.GetType(), true));

                var employeeQualifyingRoles = GetDataFromMysql(new EmployeeQualifyingRole(), obMysqlContext);
                SaveChangesInPostgres(employeeQualifyingRoles, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(employeeQualifyingRoles.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(employeeQualifyingRoles.GetType(), true));

                var projectScheduleEntryTypes = GetDataFromMysql(new ProjectScheduleEntryType(), obMysqlContext);
                SaveChangesInPostgres(projectScheduleEntryTypes, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(projectScheduleEntryTypes.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(projectScheduleEntryTypes.GetType(), true));

                var projectScheduleEntries = GetDataFromMysql(new ProjectScheduleEntry(), obMysqlContext);
                SaveChangesInPostgres(projectScheduleEntries, obPostgresContext);
                listTextDatabase.Add(GetCSharpRepresentation(projectScheduleEntries.GetType(), true));
                Console.WriteLine(GetCSharpRepresentation(projectScheduleEntries.GetType(), true));

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                Console.WriteLine("-------------------------------------------------");
                Console.WriteLine("Отсортированный список таблиц");
                foreach (var dbText in listTextDatabase.OrderBy(x => x))
                {
                    Console.WriteLine(dbText);
                }
                Console.WriteLine("Конец выгрузки");
            }
            Console.ReadKey();
        }
        static string GetCSharpRepresentation(Type t, bool trimArgCount)
        {
            if (t.IsGenericType)
            {
                var genericArgs = t.GetGenericArguments().ToList();

                return GetCSharpRepresentation(t, trimArgCount, genericArgs);
            }

            return t.Name;
        }

        static string GetCSharpRepresentation(Type t, bool trimArgCount, List<Type> availableArguments)
        {
            if (t.IsGenericType)
            {
                string value = t.Name;
                if (trimArgCount && value.IndexOf("`") > -1)
                {
                    value = value.Substring(0, value.IndexOf("`"));
                }

                if (t.DeclaringType != null)
                {
                    // This is a nested type, build the nesting type first
                    value = GetCSharpRepresentation(t.DeclaringType, trimArgCount, availableArguments) + "+" + value;
                }

                // Build the type arguments (if any)
                string argString = "";
                var thisTypeArgs = t.GetGenericArguments();
                for (int i = 0; i < thisTypeArgs.Length && availableArguments.Count > 0; i++)
                {
                    if (i != 0) argString += ", ";

                    argString += GetCSharpRepresentation(availableArguments[0], trimArgCount);
                    availableArguments.RemoveAt(0);
                }

                // If there are type arguments, add them with < >
                if (argString.Length > 0)
                {
                    value += "<" + argString + ">";
                }

                return value;
            }

            return t.Name;
        }

        static IList<T> GetDataFromMysql<T>(T entity, DbContextOptionsBuilder<RPCSContextMysql> dbContextOptionsBuilder) where T : class
        {
            var list = new List<T>();
            using (var rpcsMySql = new RPCSContextMysql(dbContextOptionsBuilder.Options))
            {
                list = rpcsMySql.Set<T>().ToList();
            }
            return list;
        }

        static void SaveChangesInPostgres<T>(IList<T> list, DbContextOptionsBuilder<RPCSContext> dbContextOptionsBuilderPostgres) where T : class
        {
            using (var rpcsContextPostgres = new RPCSContext(dbContextOptionsBuilderPostgres.Options))
            {
                rpcsContextPostgres.Set<T>().Clear();
                rpcsContextPostgres.SaveChanges();
                foreach (var item in list)
                {
                    rpcsContextPostgres.Add(item);
                }
                rpcsContextPostgres.SaveChanges();
            }
        }

        static void SaveChangesInPostgres(IList<Employee> employees, IList<Department> departments, DbContextOptionsBuilder<RPCSContext> dbContextOptionsBuilderPostgres)
        {
            var saveListEmployee = new List<Employee>();
            var saveListDepartments = new List<Department>();

            using (var rpcsContextPostgres = new RPCSContext(dbContextOptionsBuilderPostgres.Options))
            {
                rpcsContextPostgres.Set<Employee>().Clear();
                rpcsContextPostgres.Set<Department>().Clear();
                rpcsContextPostgres.SaveChanges();

                foreach (var item in employees)
                {
                    saveListEmployee.Add(new Employee() { ID = item.ID, DepartmentID = item.DepartmentID });
                    item.DepartmentID = null;
                    rpcsContextPostgres.Add(item);
                }
                rpcsContextPostgres.SaveChanges();

                foreach (var item in departments)
                {
                    saveListDepartments.Add(new Department()
                    {
                        ID = item.ID,
                        DepartmentManagerID = item.DepartmentManagerID,
                        DepartmentManagerAssistantID = item.DepartmentManagerAssistantID,
                        DepartmentPAID = item.DepartmentPAID
                    });
                    item.DepartmentManagerID = null;
                    item.DepartmentManagerAssistantID = null;
                    item.DepartmentPAID = null;
                    rpcsContextPostgres.Add(item);
                }
                rpcsContextPostgres.SaveChanges();


                //https://stackoverflow.com/questions/40073149/entity-framework-circular-dependency-for-last-entity
                //using (var scope = new TransactionScope())
                //{
                //    foreach (var item in firstList)
                //    {
                //        rpcsContextPostgres.Add(item);
                //    }
                //    rpcsContextPostgres.SaveChanges();
                //    scope.Complete();
                //}

                //using (var scope = new TransactionScope())
                //{
                //    foreach (var item in secondList)
                //    {
                //        rpcsContextPostgres.Add(item);
                //    }
                //    rpcsContextPostgres.SaveChanges();
                //    scope.Complete();
                //}
            }

            using (var rpcsContextPostgres = new RPCSContext(dbContextOptionsBuilderPostgres.Options))
            {
                //обновление пустых полей
                foreach (var employee in saveListEmployee)
                {
                    rpcsContextPostgres.Employees.Attach(employee);
                    rpcsContextPostgres.Entry(employee).Property(x => x.DepartmentID).IsModified = true;
                }
                rpcsContextPostgres.SaveChanges();


                foreach (var department in saveListDepartments)
                {
                    rpcsContextPostgres.Departments.Attach(department);
                    rpcsContextPostgres.Entry(department).Property(x => x.DepartmentManagerID).IsModified = true;
                    rpcsContextPostgres.Entry(department).Property(x => x.DepartmentManagerAssistantID).IsModified = true;
                    rpcsContextPostgres.Entry(department).Property(x => x.DepartmentPAID).IsModified = true;
                }
                rpcsContextPostgres.SaveChanges();

            }
        }
    }
}
