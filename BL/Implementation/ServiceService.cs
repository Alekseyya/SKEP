using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Core.BL;
using Microsoft.Extensions.DependencyInjection;

using Core.BL.Interfaces;
using Core.Models;
using Data;

namespace BL.Implementation
{
    public class ServiceService : IServiceService
    {
        private readonly IServiceProvider _serviceProvider;

        public ServiceService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        /// <summary>
        /// Существуют ли связи в связанных сущностях по Id входного типа
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entry"></param>
        /// <returns></returns>
        public (bool hasRelated, string relatedInDBClassId) HasRecycleBinInDBRelation<T>(T entry)
        {
            var hasRelated = false;
            var relatedInDBClassId = string.Empty;
            foreach (var propertyContext in typeof(RPCSContext).GetProperties().Where(x => x.Name != "Database" && x.Name != "ChangeTracker" && x.Name != "Configuration" && x.Name != entry.GetType().Name + "s"))
            {
                foreach (var property in propertyContext.PropertyType.GenericTypeArguments.First().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    //Todo если указывать как new Project() - то entry.getType(), если это энтити класс - entry.GetType().BaseType
                    if (property.PropertyType == entry.GetType() || property.PropertyType == entry.GetType().BaseType)
                    {
                        var entryIdValue = (int)entry.GetType().GetProperty("ID").GetValue(entry, null);
                        var foundInType = propertyContext.PropertyType.GenericTypeArguments.First();
                        var propertyId = entry.GetType().Name + "ID";
                        //если запись ProjectID найден в других бд
                        //type - может быть любая модель! Надо перечислять все модели!
                        var entryIDInTableRelationNotDeleted = HasEntryIDInTableRelationNotDeleted(entry, foundInType, entryIdValue);
                        if (entryIDInTableRelationNotDeleted.isRowNoRecycleBin)
                        {
                            //связанная запись найдена - true -значит выходим и выдаем ошибку!
                            hasRelated = true;
                            relatedInDBClassId = foundInType.Name + ".ID = " + entryIDInTableRelationNotDeleted.relatedEntryId;
                            break;
                        }
                    }
                }
            }
            return (hasRelated, relatedInDBClassId);
        }

        private bool HasRecycleRecycleBinInDbRelation<T>(T entry)
        {
            var hasRow = false;
            foreach (var propertyContext in typeof(RPCSContext).GetProperties().Where(x => x.Name != "Database" && x.Name != "ChangeTracker" && x.Name != "Configuration" && x.Name != entry.GetType().Name + "s"))
            {
                foreach (var property in propertyContext.PropertyType.GenericTypeArguments.First().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    //Todo проверить!!
                    if (property.PropertyType == entry.GetType() && property.PropertyType.BaseType == typeof(BaseModel))
                    {
                        var entryIdValue = (int)entry.GetType().GetProperty("ID").GetValue(entry, null);
                        var foundInType = propertyContext.PropertyType.GenericTypeArguments.First();

                        if (HasEntryIDInRecyсleBin(entry, foundInType, entryIdValue))
                        {
                            //связанная запись найдена(связанная запись в корзине) - true -значит выходим и выдаем ошибку!
                            hasRow = true;
                            break;
                        }
                    }
                }
            }
            return hasRow;
        }

        private bool HasEntryIDInRecyсleBin<T>(T entry, Type table, int entryId)
        {
            //Запись не в корзине - значить можно восстанавливать!
            var isNotRecycleBin = false;
            switch (table)
            {
                case Type _ when table == typeof(BudgetLimit):
                    switch (entry)
                    {
                        case Department _:
                            isNotRecycleBin = _serviceProvider.GetService<IBudgetLimitService>().Get(sh => sh.Where(p => p.DepartmentID == entryId && p.IsDeleted).ToList(), GetEntityMode.Deleted).Any();
                            break;
                        case Project _:
                            isNotRecycleBin = _serviceProvider.GetService<IBudgetLimitService>().Get(sh => sh.Where(p => p.ProjectID == entryId && p.IsDeleted).ToList(), GetEntityMode.Deleted).Any();
                            break;
                        case CostSubItem _:
                            isNotRecycleBin = _serviceProvider.GetService<IBudgetLimitService>().Get(sh => sh.Where(p => p.CostSubItemID == entryId && p.IsDeleted).ToList(), GetEntityMode.Deleted).Any();
                            break;
                    }
                    break;

                case { } _ when table == typeof(CostSubItem):
                    switch (entry)
                    {
                        case CostItem _:
                            isNotRecycleBin = _serviceProvider.GetService<ICostSubItemService>().Get(sh => sh.Where(p => p.CostItemID == entryId && p.IsDeleted).ToList(), GetEntityMode.Deleted).Any();
                            break;
                    }
                    break;

                case Type _ when table == typeof(Department):
                    switch (entry)
                    {
                        case Organisation _:
                            isNotRecycleBin = _serviceProvider.GetService<IDepartmentService>().Get(sh => sh.Where(p => p.OrganisationID == entryId && p.IsDeleted).ToList(), GetEntityMode.Deleted).Any();
                            break;
                    }
                    break;

                case Type _ when table == typeof(Employee):
                    switch (entry)
                    {
                        case EmployeePosition _:
                            isNotRecycleBin = _serviceProvider.GetService<IEmployeeService>().Get(sh =>
                                sh.Where(p =>
                                    p.EmployeePositionID == entryId ||
                                    p.EmployeePositionOfficialID == entryId && p.IsDeleted).ToList(), GetEntityMode.Deleted).Any();
                            break;
                        case Department _:
                            isNotRecycleBin = _serviceProvider.GetService<IEmployeeService>().Get(sh =>
                                sh.Where(p => p.DepartmentID == entryId && p.IsDeleted).ToList(), GetEntityMode.Deleted).Any();
                            break;
                        case Organisation _:
                            isNotRecycleBin = _serviceProvider.GetService<IEmployeeService>().Get(sh =>
                                sh.Where(p => p.OrganisationID == entryId && p.IsDeleted).ToList(), GetEntityMode.Deleted).Any();
                            break;
                        case EmployeeGrad _:
                            isNotRecycleBin = _serviceProvider.GetService<IEmployeeService>().Get(sh =>
                                sh.Where(p => p.EmployeeGradID == entryId && p.IsDeleted).ToList(), GetEntityMode.Deleted).Any();
                            break;
                        case EmployeeLocation _:
                            isNotRecycleBin = _serviceProvider.GetService<IEmployeeService>().Get(sh =>
                                sh.Where(p => p.EmployeeLocationID == entryId && p.IsDeleted).ToList(), GetEntityMode.Deleted).Any();
                            break;
                        case EmployeePositionOfficial _:
                            isNotRecycleBin = _serviceProvider.GetService<IEmployeeService>().Get(sh =>
                                sh.Where(p => p.EmployeePositionOfficialID == entryId && p.IsDeleted).ToList(), GetEntityMode.Deleted).Any();
                            break;
                    }
                    break;

                case Type _ when table == typeof(Project):
                    switch (entry)
                    {
                        case Employee _:
                            isNotRecycleBin = _serviceProvider.GetService<IProjectService>().Get(p => p
                                .Where(x => x.ApproveHoursEmployeeID == entryId || x.EmployeeCAMID == entryId ||
                                            x.EmployeePMID == entryId || x.EmployeePAID == entryId && x.IsDeleted)
                                .ToList(), GetEntityMode.Deleted).Any();
                            break;
                        case Department _:
                            isNotRecycleBin = _serviceProvider.GetService<IProjectService>().Get(p =>
                                p.Where(x =>
                                        x.ProductionDepartmentID == entryId ||
                                        x.DepartmentID == entryId && x.IsDeleted)
                                    .ToList(), GetEntityMode.Deleted).Any();
                            break;
                        case Organisation _:
                            isNotRecycleBin = _serviceProvider.GetService<IProjectService>().Get(p =>
                                p.Where(x => x.OrganisationID == entryId && x.IsDeleted).ToList(), GetEntityMode.Deleted).Any();
                            break;
                        case ProjectType _:
                            isNotRecycleBin = _serviceProvider.GetService<IProjectService>().Get(p =>
                                p.Where(x => x.ProjectTypeID == entryId && x.IsDeleted).ToList(), GetEntityMode.Deleted).Any();
                            break;
                    }
                    break;

                case Type _ when table == typeof(ProjectScheduleEntry):
                    switch (entry)
                    {
                        case Project _:
                            isNotRecycleBin = _serviceProvider.GetService<IProjectScheduleEntryService>().Get(sh =>
                                sh.Where(p => p.ProjectID == entryId && p.IsDeleted).ToList(), GetEntityMode.Deleted).Any();
                            break;
                        case ProjectScheduleEntryType _:
                            isNotRecycleBin = _serviceProvider.GetService<IProjectScheduleEntryService>().Get(sh =>
                                sh.Where(p => p.ProjectScheduleEntryTypeID == entryId && p.IsDeleted).ToList(), GetEntityMode.Deleted).Any();
                            break;

                    }
                    break;

                case Type _ when table == typeof(ProjectScheduleEntryType):
                    switch (entry)
                    {
                        case ProjectType _:
                            isNotRecycleBin = _serviceProvider.GetService<IProjectScheduleEntryTypeService>().Get(sh =>
                                sh.Where(p => p.ProjectTypeID == entryId && p.IsDeleted).ToList(), GetEntityMode.Deleted).Any();
                            break;

                    }
                    break;

                case Type _ when table == typeof(ProjectStatusRecord):
                    switch (entry)
                    {
                        case Project _:
                            isNotRecycleBin = _serviceProvider.GetService<IProjectStatusRecordService>().Get(sh =>
                                sh.Where(p => p.ProjectID == entryId && p.IsDeleted).ToList(), GetEntityMode.Deleted).Any();
                            break;

                    }
                    break;

                case Type _ when table == typeof(TSAutoHoursRecord):
                    switch (entry)
                    {
                        case Employee _:
                            isNotRecycleBin = _serviceProvider.GetService<ITSAutoHoursRecordService>().Get(sh =>
                                sh.Where(p => p.EmployeeID == entryId && p.IsDeleted).ToList(), GetEntityMode.Deleted).Any();
                            break;
                        case Project _:
                            isNotRecycleBin = _serviceProvider.GetService<ITSAutoHoursRecordService>().Get(sh =>
                                sh.Where(p => p.ProjectID == entryId && p.IsDeleted).ToList(), GetEntityMode.Deleted).Any();
                            break;

                    }
                    break;

                case Type _ when table == typeof(TSHoursRecord):
                    switch (entry)
                    {
                        case Employee _:
                            isNotRecycleBin = _serviceProvider.GetService<ITSHoursRecordService>().Get(sh =>
                                sh.Where(p => p.EmployeeID == entryId && p.IsDeleted).ToList(), GetEntityMode.Deleted).Any();
                            break;
                        case Project _:
                            isNotRecycleBin = _serviceProvider.GetService<ITSHoursRecordService>().Get(sh =>
                                sh.Where(p => p.ProjectID == entryId && p.IsDeleted).ToList(), GetEntityMode.Deleted).Any();
                            break;
                        case TSAutoHoursRecord _:
                            isNotRecycleBin = _serviceProvider.GetService<ITSHoursRecordService>().Get(sh =>
                                sh.Where(p => p.ParentTSAutoHoursRecordID == entryId && p.IsDeleted).ToList(), GetEntityMode.Deleted).Any();
                            break;
                        case VacationRecord _:
                            isNotRecycleBin = _serviceProvider.GetService<ITSHoursRecordService>()
                                .Get(sh => sh.Where(p => p.ParentVacationRecordID == entryId && p.IsDeleted).ToList(), GetEntityMode.Deleted).Any();
                            break;

                    }
                    break;
                case Type _ when table == typeof(VacationRecord):
                    switch (entry)
                    {
                        case Employee _:
                            isNotRecycleBin = _serviceProvider.GetService<IVacationRecordService>().Get(sh =>
                                sh.Where(p => p.EmployeeID == entryId && p.IsDeleted).ToList(), GetEntityMode.Deleted).Any();
                            break;

                    }
                    break;
            }
            return isNotRecycleBin;
        }


        public bool RecycleBinDeleteInTable(string tableName, int deletedId)
        {
            var isDeleted = true;
            switch (tableName)
            {
                case { } _ when tableName == nameof(BudgetLimit) && HasRecycleBinInDBRelation(new BudgetLimit() { ID = deletedId }).hasRelated == false:
                    if (_serviceProvider.GetService<IBudgetLimitService>().Get(x => x.Where(b => b.ID == (int)deletedId).ToList(), GetEntityMode.Deleted).FirstOrDefault() != null)
                        _serviceProvider.GetService<IBudgetLimitService>().Delete((int)deletedId);
                    _serviceProvider.GetService<IBudgetLimitService>().DeleteRelatedEntries((int)deletedId);
                    break;
                case { } _ when tableName == nameof(CostItem) && HasRecycleBinInDBRelation(new CostItem() { ID = deletedId }).hasRelated == false:

                    if (_serviceProvider.GetService<ICostItemService>().Get(x => x.Where(b => b.ID == (int)deletedId).ToList(), GetEntityMode.Deleted).FirstOrDefault() != null)
                        _serviceProvider.GetService<ICostItemService>().Delete((int)deletedId);
                    _serviceProvider.GetService<ICostItemService>().DeleteRelatedEntries((int)deletedId);
                    break;
                case { } _ when tableName == nameof(CostSubItem) && HasRecycleBinInDBRelation(new CostSubItem() { ID = deletedId }).hasRelated == false:

                    if (_serviceProvider.GetService<ICostSubItemService>().Get(x => x.Where(b => b.ID == (int)deletedId).ToList(), GetEntityMode.Deleted).FirstOrDefault() != null)
                        _serviceProvider.GetService<ICostSubItemService>().Delete((int)deletedId);
                    _serviceProvider.GetService<ICostSubItemService>().DeleteRelatedEntries((int)deletedId);

                    break;
                case { } _ when tableName == nameof(Department) && HasRecycleBinInDBRelation(new Department() { ID = deletedId }).hasRelated == false:

                    if (_serviceProvider.GetService<IDepartmentService>().Get(x => x.Where(b => b.ID == (int)deletedId).ToList(), GetEntityMode.Deleted).FirstOrDefault() != null)
                        _serviceProvider.GetService<IDepartmentService>().Delete((int)deletedId);
                    _serviceProvider.GetService<IDepartmentService>().DeleteRelatedEntries((int)deletedId);

                    break;
                case { } _ when tableName == nameof(Employee) && HasRecycleBinInDBRelation(new Employee() { ID = deletedId }).hasRelated == false:
                    if (HasRecycleBinInDBRelation(new Employee() { ID = deletedId }).hasRelated == false)
                    {
                        if (_serviceProvider.GetService<IEmployeeService>().Get(x => x.Where(b => b.ID == (int)deletedId).ToList(), GetEntityMode.Deleted).FirstOrDefault() != null)
                            _serviceProvider.GetService<IEmployeeService>().Delete((int)deletedId);
                        _serviceProvider.GetService<IEmployeeService>().DeleteRelatedEntries((int)deletedId);
                    }
                    break;
                case { } _ when tableName == nameof(ProjectStatusRecord) && HasRecycleBinInDBRelation(new ProjectStatusRecord() { ID = deletedId }).hasRelated == false:

                    if (_serviceProvider.GetService<IProjectStatusRecordService>().GetByIdWithDeleteFilter((int)deletedId) != null)
                    {
                        _serviceProvider.GetService<IProjectStatusRecordService>().Delete((int)deletedId);
                        _serviceProvider.GetService<IProjectStatusRecordService>().DeleteRelatedEntries((int)deletedId);
                    }

                    break;
                case { } _ when tableName == nameof(ProjectScheduleEntry) && HasRecycleBinInDBRelation(new ProjectScheduleEntry() { ID = deletedId }).hasRelated == false:
                    if (HasRecycleBinInDBRelation(new ProjectScheduleEntry() { ID = deletedId }).hasRelated == false)
                    {
                        if (_serviceProvider.GetService<IProjectScheduleEntryService>().Get(x => x.Where(b => b.ID == (int)deletedId).ToList(), GetEntityMode.Deleted).FirstOrDefault() != null)
                            _serviceProvider.GetService<IProjectScheduleEntryService>().Delete((int)deletedId);
                        _serviceProvider.GetService<IProjectScheduleEntryService>().DeleteRelatedEntries((int)deletedId);
                    }
                    break;
                case { } _ when tableName == nameof(ProjectScheduleEntryType) && HasRecycleBinInDBRelation(new ProjectScheduleEntryType() { ID = deletedId }).hasRelated == false:

                    if (_serviceProvider.GetService<IProjectScheduleEntryTypeService>().Get(x => x.Where(b => b.ID == (int)deletedId).ToList(), GetEntityMode.Deleted).FirstOrDefault() != null)
                        _serviceProvider.GetService<IProjectScheduleEntryTypeService>().Delete((int)deletedId);
                    _serviceProvider.GetService<IProjectScheduleEntryTypeService>().DeleteRelatedEntries((int)deletedId);

                    break;
                case { } _ when tableName == nameof(TSHoursRecord) && HasRecycleBinInDBRelation(new TSHoursRecord() { ID = deletedId }).hasRelated == false:

                    if (_serviceProvider.GetService<ITSHoursRecordService>().Get(x => x.Where(b => b.ID == (int)deletedId).ToList(), GetEntityMode.Deleted).FirstOrDefault() != null)
                        _serviceProvider.GetService<ITSHoursRecordService>().Delete((int)deletedId);
                    _serviceProvider.GetService<ITSHoursRecordService>().DeleteRelatedEntries((int)deletedId);

                    break;
                case { } _ when tableName == nameof(TSAutoHoursRecord) && HasRecycleBinInDBRelation(new TSAutoHoursRecord() { ID = deletedId }).hasRelated == false:

                    if (_serviceProvider.GetService<ITSAutoHoursRecordService>().Get(x => x.Where(b => b.ID == (int)deletedId).ToList(), GetEntityMode.Deleted).FirstOrDefault() != null)
                        _serviceProvider.GetService<ITSAutoHoursRecordService>().Delete((int)deletedId);
                    _serviceProvider.GetService<ITSAutoHoursRecordService>().DeleteRelatedEntries((int)deletedId);

                    break;
                case { } _ when tableName == nameof(Project) && HasRecycleBinInDBRelation(new Project() { ID = deletedId }).hasRelated == false:

                    if (_serviceProvider.GetService<IProjectService>().Get(x => x.Where(b => b.ID == (int)deletedId).ToList(), GetEntityMode.Deleted).FirstOrDefault() != null)
                        _serviceProvider.GetService<IProjectService>().Delete((int)deletedId);
                    _serviceProvider.GetService<IProjectService>().DeleteRelatedEntries((int)deletedId);

                    break;
                case { } _ when tableName == nameof(VacationRecord) && HasRecycleBinInDBRelation(new VacationRecord() { ID = deletedId }).hasRelated == false:

                    if (_serviceProvider.GetService<IVacationRecordService>().Get(x => x.Where(b => b.ID == (int)deletedId).ToList(), GetEntityMode.Deleted).FirstOrDefault() != null)
                        _serviceProvider.GetService<IVacationRecordService>().Delete((int)deletedId);
                    _serviceProvider.GetService<IVacationRecordService>().DeleteRelatedEntries((int)deletedId);

                    break;
                default:
                    isDeleted = false;
                    break;
            }

            return isDeleted;

        }

        public bool RecycleBinRestoreInTable(string tableName, int restoreId)
        {
            var isRestore = true;
            switch (tableName)
            {
                case { } _ when tableName == nameof(BudgetLimit) && HasRecycleRecycleBinInDbRelation(new BudgetLimit() { ID = restoreId }) == false:
                    _serviceProvider.GetService<IBudgetLimitService>().RestoreFromRecycleBin(restoreId);
                    break;
                case { } _ when tableName == nameof(CostItem) && HasRecycleRecycleBinInDbRelation(new CostItem() { ID = restoreId }) == false:
                    _serviceProvider.GetService<ICostItemService>().RestoreFromRecycleBin(restoreId);
                    break;
                case { } _ when tableName == nameof(CostSubItem) && HasRecycleRecycleBinInDbRelation(new CostSubItem() { ID = restoreId }) == false:
                    _serviceProvider.GetService<ICostSubItemService>().RestoreFromRecycleBin(restoreId);
                    break;
                case { } _ when tableName == nameof(Department) && HasRecycleRecycleBinInDbRelation(new Department() { ID = restoreId }) == false:
                    _serviceProvider.GetService<IDepartmentService>().RestoreFromRecycleBin(restoreId);
                    break;
                case { } _ when tableName == nameof(Employee) && HasRecycleRecycleBinInDbRelation(new Employee() { ID = restoreId }) == false:
                    _serviceProvider.GetService<IEmployeeService>().RestoreFromRecycleBin(restoreId);
                    break;
                case { } _ when tableName == nameof(ProjectStatusRecord) && HasRecycleRecycleBinInDbRelation(new ProjectStatusRecord() { ID = restoreId }) == false:
                    _serviceProvider.GetService<IProjectStatusRecordService>().RestoreFromRecycleBin(restoreId);
                    break;
                case { } _ when tableName == nameof(ProjectScheduleEntry) && HasRecycleRecycleBinInDbRelation(new ProjectScheduleEntry() { ID = restoreId }) == false:
                    _serviceProvider.GetService<IBudgetLimitService>().RestoreFromRecycleBin(restoreId);
                    break;
                case { } _ when tableName == nameof(ProjectScheduleEntryType) && HasRecycleRecycleBinInDbRelation(new ProjectScheduleEntryType() { ID = restoreId }) == false:
                    _serviceProvider.GetService<IProjectScheduleEntryTypeService>().RestoreFromRecycleBin(restoreId);
                    break;
                case { } _ when tableName == nameof(TSHoursRecord) && HasRecycleRecycleBinInDbRelation(new TSHoursRecord() { ID = restoreId }) == false:
                    _serviceProvider.GetService<ITSHoursRecordService>().RestoreFromRecycleBin(restoreId);
                    break;
                case { } _ when tableName == nameof(TSAutoHoursRecord) && HasRecycleRecycleBinInDbRelation(new TSAutoHoursRecord() { ID = restoreId }) == false:
                    _serviceProvider.GetService<ITSAutoHoursRecordService>().RestoreFromRecycleBin(restoreId);
                    break;
                case { } _ when tableName == nameof(Project) && HasRecycleRecycleBinInDbRelation(new Project() { ID = restoreId }) == false:
                    _serviceProvider.GetService<IProjectService>().RestoreFromRecycleBin(restoreId);
                    break;
                case { } _ when tableName == nameof(VacationRecord) && HasRecycleRecycleBinInDbRelation(new VacationRecord() { ID = restoreId }) == false:
                    _serviceProvider.GetService<IVacationRecordService>().RestoreFromRecycleBin(restoreId);
                    break;
                default:
                    isRestore = false;
                    break;
            }
            return isRestore;
        }

        /// <summary>
        /// Связанная запись найдена в других БД, но не помещена в корзину
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entry"></param>
        /// <param name="table"></param>
        /// <param name="entryId"></param>
        /// <returns></returns>
        private (bool isRowNoRecycleBin, int relatedEntryId) HasEntryIDInTableRelationNotDeleted<T>(T entry, Type table, int entryId)
        {
            //False - если запись в корзине!
            var isRowNoRecycleBin = false;
            object relatedEntry = null;
            //Если хоть где-то есть связь будет установлен флаг на true!
            switch (table)
            {
                case Type _ when table == typeof(BudgetLimit):
                    switch (entry)
                    {
                        case Department _:
                            relatedEntry = _serviceProvider.GetService<IBudgetLimitService>().Get(sh => sh.Where(p => p.DepartmentID == entryId && !p.IsDeleted).ToList()).FirstOrDefault();
                            break;
                        case Project _:
                            relatedEntry = _serviceProvider.GetService<IBudgetLimitService>().Get(sh => sh.Where(p => p.ProjectID == entryId && !p.IsDeleted).ToList()).FirstOrDefault();
                            break;
                        case CostSubItem _:
                            relatedEntry = _serviceProvider.GetService<IBudgetLimitService>().Get(sh => sh.Where(p => p.CostSubItemID == entryId && !p.IsDeleted).ToList()).FirstOrDefault();
                            break;
                    }
                    break;
                case { } _ when table == typeof(CostSubItem):
                    switch (entry)
                    {
                        case CostItem _:
                            relatedEntry = _serviceProvider.GetService<ICostSubItemService>().Get(sh => sh.Where(p => p.CostItemID == entryId && !p.IsDeleted).ToList()).FirstOrDefault();
                            break;
                    }
                    break;
                case Type _ when table == typeof(Department):
                    switch (entry)
                    {
                        case Organisation _:
                            relatedEntry = _serviceProvider.GetService<IDepartmentService>().Get(sh => sh.Where(p => p.OrganisationID == entryId && !p.IsDeleted).ToList()).FirstOrDefault();
                            break;
                    }
                    break;
                case Type _ when table == typeof(Employee):
                    switch (entry)
                    {
                        case EmployeePosition _:
                            relatedEntry = _serviceProvider.GetService<IEmployeeService>().Get(sh => sh.Where(p => p.EmployeePositionID == entryId || p.EmployeePositionOfficialID == entryId && !p.IsDeleted).ToList()).FirstOrDefault();
                            break;
                        case Department _:
                            relatedEntry = _serviceProvider.GetService<IEmployeeService>().Get(sh => sh.Where(p => p.DepartmentID == entryId && !p.IsDeleted).ToList()).FirstOrDefault();
                            break;
                        case Organisation _:
                            relatedEntry = _serviceProvider.GetService<IEmployeeService>().Get(sh => sh.Where(p => p.OrganisationID == entryId && !p.IsDeleted).ToList()).FirstOrDefault();
                            break;
                        case EmployeeGrad _:
                            relatedEntry = _serviceProvider.GetService<IEmployeeService>().Get(sh => sh.Where(p => p.EmployeeGradID == entryId && !p.IsDeleted).ToList()).FirstOrDefault();
                            break;
                        case EmployeeLocation _:
                            relatedEntry = _serviceProvider.GetService<IEmployeeService>().Get(sh => sh.Where(p => p.EmployeeLocationID == entryId && !p.IsDeleted).ToList()).FirstOrDefault();
                            break;
                        case EmployeePositionOfficial _:
                            relatedEntry = _serviceProvider.GetService<IEmployeeService>().Get(sh => sh.Where(p => p.EmployeePositionOfficialID == entryId && !p.IsDeleted).ToList()).FirstOrDefault();
                            break;
                    }
                    break;
                case Type _ when table == typeof(EmployeeCategory):
                    switch (entry)
                    {
                        case Employee _:
                            relatedEntry = _serviceProvider.GetService<IEmployeeCategoryService>().Get(sh => sh.Where(p => p.EmployeeID == entryId).ToList()).FirstOrDefault();
                            break;
                    }
                    break;
                case Type _ when table == typeof(EmployeeDepartmentAssignment):
                    switch (entry)
                    {
                        case Employee _:
                            relatedEntry = _serviceProvider.GetService<IEmployeeDepartmentAssignmentService>().Get(sh => sh.Where(p => p.EmployeeID == entryId).ToList()).FirstOrDefault();
                            break;
                        case Department _:
                            relatedEntry = _serviceProvider.GetService<IEmployeeDepartmentAssignmentService>().Get(sh => sh.Where(p => p.DepartmentID == entryId).ToList()).FirstOrDefault();
                            break;
                    }
                    break;
                case Type _ when table == typeof(EmployeeGradAssignment):
                    switch (entry)
                    {
                        case Employee _:
                            relatedEntry = _serviceProvider.GetService<IEmployeeGradAssignmentService>().Get(sh => sh.Where(p => p.EmployeeID == entryId).ToList()).FirstOrDefault();
                            break;
                        case EmployeeGrad _:
                            relatedEntry = _serviceProvider.GetService<IEmployeeGradAssignmentService>().Get(sh => sh.Where(p => p.EmployeeGradID == entryId).ToList()).FirstOrDefault();
                            break;
                    }
                    break;
                case Type _ when table == typeof(EmployeeOrganisation):
                    switch (entry)
                    {
                        case Employee _:
                            relatedEntry = _serviceProvider.GetService<IEmployeeOrganisationService>().Get(sh => sh.Where(p => p.EmployeeID == entryId).ToList()).FirstOrDefault();
                            break;
                        case Organisation _:
                            relatedEntry = _serviceProvider.GetService<IEmployeeOrganisationService>().Get(sh => sh.Where(p => p.OrganisationID == entryId).ToList()).FirstOrDefault();
                            break;
                        case EmployeePositionOfficial _:
                            relatedEntry = _serviceProvider.GetService<IEmployeeOrganisationService>().Get(sh => sh.Where(p => p.EmployeePositionOfficialID == entryId).ToList()).FirstOrDefault();
                            break;
                    }
                    break;
                case Type _ when table == typeof(EmployeePositionAssignment):
                    switch (entry)
                    {
                        case Employee _:
                            relatedEntry = _serviceProvider.GetService<IEmployeePositionAssignmentService>().Get(sh => sh.Where(p => p.EmployeeID == entryId).ToList()).FirstOrDefault();
                            break;
                        case EmployeePosition _:
                            relatedEntry = _serviceProvider.GetService<IEmployeePositionAssignmentService>().Get(sh => sh.Where(p => p.EmployeePositionID == entryId).ToList()).FirstOrDefault();
                            break;
                    }
                    break;
                case Type _ when table == typeof(EmployeePositionOfficial):
                    switch (entry)
                    {
                        case Organisation _:
                            relatedEntry = _serviceProvider.GetService<IEmployeePositionOfficialService>().Get(sh => sh.Where(p => p.OrganisationID == entryId).ToList()).FirstOrDefault();
                            break;
                    }
                    break;
                case Type _ when table == typeof(EmployeePositionOfficialAssignment):
                    switch (entry)
                    {
                        case Employee _:
                            relatedEntry = _serviceProvider.GetService<IEmployeePositionOfficialAssignmentService>().Get(sh => sh.Where(p => p.EmployeeID == entryId).ToList()).FirstOrDefault();
                            break;
                        case EmployeePositionOfficial _:
                            relatedEntry = _serviceProvider.GetService<IEmployeePositionOfficialAssignmentService>().Get(sh => sh.Where(p => p.EmployeePositionOfficialID == entryId).ToList()).FirstOrDefault();
                            break;
                    }
                    break;
                case Type _ when table == typeof(EmployeeQualifyingRole):
                    switch (entry)
                    {
                        case Employee _:
                            relatedEntry = _serviceProvider.GetService<IEmployeeQualifyingRoleService>().Get(sh => sh.Where(p => p.EmployeeID == entryId).ToList()).FirstOrDefault();
                            break;
                        case QualifyingRole _:
                            relatedEntry = _serviceProvider.GetService<IEmployeeQualifyingRoleService>().Get(sh => sh.Where(p => p.QualifyingRoleID == entryId).ToList()).FirstOrDefault();
                            break;
                    }
                    break;
                case Type _ when table == typeof(ExpensesRecord):
                    switch (entry)
                    {
                        case Department _:
                            relatedEntry = _serviceProvider.GetService<IExpensesRecordService>().Get(sh => sh.Where(p => p.DepartmentID == entryId).ToList()).FirstOrDefault();
                            break;
                        case Project _:
                            relatedEntry = _serviceProvider.GetService<IExpensesRecordService>().Get(sh => sh.Where(p => p.ProjectID == entryId).ToList()).FirstOrDefault();
                            break;
                        case CostSubItem _:
                            relatedEntry = _serviceProvider.GetService<IExpensesRecordService>().Get(sh => sh.Where(p => p.CostSubItemID == entryId).ToList()).FirstOrDefault();
                            break;
                    }
                    break;
                case Type _ when table == typeof(Project):
                    switch (entry)
                    {
                        case Employee _:
                            relatedEntry = _serviceProvider.GetService<IProjectService>().Get(p => p
                                .Where(x => x.ApproveHoursEmployeeID == entryId || x.EmployeeCAMID == entryId || x.EmployeePMID == entryId || x.EmployeePAID == entryId && !x.IsDeleted).ToList()).FirstOrDefault();
                            break;
                        case Department _:
                            relatedEntry = _serviceProvider.GetService<IProjectService>().Get(p =>
                                p.Where(x => x.ProductionDepartmentID == entryId || x.DepartmentID == entryId && !x.IsDeleted).ToList()).FirstOrDefault();
                            break;
                        case Organisation _:
                            relatedEntry = _serviceProvider.GetService<IProjectService>().Get(p => p.Where(x => x.OrganisationID == entryId && !x.IsDeleted).ToList()).FirstOrDefault();
                            break;
                        case ProjectType _:
                            relatedEntry = _serviceProvider.GetService<IProjectService>().Get(p => p.Where(x => x.ProjectTypeID == entryId && !x.IsDeleted).ToList()).FirstOrDefault();
                            break;
                    }
                    break;
                case Type _ when table == typeof(ProjectExternalWorkspace):
                    switch (entry)
                    {
                        case Project _:
                            relatedEntry = _serviceProvider.GetService<IProjectExternalWorkspaceService>().Get(sh => sh.Where(p => p.ProjectID == entryId).ToList()).FirstOrDefault();
                            break;
                    }
                    break;
                case Type _ when table == typeof(ProjectMember):
                    switch (entry)
                    {
                        case Employee _:
                            relatedEntry = _serviceProvider.GetService<IProjectMembershipService>().Get(sh => sh.Where(p => p.EmployeeID == entryId).ToList()).FirstOrDefault();
                            break;
                        case Project _:
                            relatedEntry = _serviceProvider.GetService<IProjectMembershipService>().Get(sh => sh.Where(p => p.ProjectID == entryId).ToList()).FirstOrDefault();
                            break;
                        case ProjectRole _:
                            relatedEntry = _serviceProvider.GetService<IProjectMembershipService>().Get(sh => sh.Where(p => p.ProjectRoleID == entryId).ToList()).FirstOrDefault();
                            break;
                    }
                    break;
                case Type _ when table == typeof(ProjectReportRecord):
                    switch (entry)
                    {
                        case Employee _:
                            relatedEntry = _serviceProvider.GetService<IProjectReportRecordService>().Get(sh => sh.Where(p => p.EmployeeID == entryId).ToList()).FirstOrDefault();
                            break;
                        case Project _:
                            relatedEntry = _serviceProvider.GetService<IProjectReportRecordService>().Get(sh => sh.Where(p => p.ProjectID == entryId).ToList()).FirstOrDefault();
                            break;

                    }
                    break;
                case Type _ when table == typeof(ProjectScheduleEntry):
                    switch (entry)
                    {
                        case Project _:
                            relatedEntry = _serviceProvider.GetService<IProjectScheduleEntryService>().Get(sh => sh.Where(p => p.ProjectID == entryId && !p.IsDeleted).ToList()).FirstOrDefault();
                            break;
                        case ProjectScheduleEntryType _:
                            relatedEntry = _serviceProvider.GetService<IProjectScheduleEntryService>().Get(sh => sh.Where(p => p.ProjectScheduleEntryTypeID == entryId && !p.IsDeleted).ToList()).FirstOrDefault();
                            break;

                    }
                    break;
                case Type _ when table == typeof(ProjectScheduleEntryType):
                    switch (entry)
                    {
                        case ProjectType _:
                            relatedEntry = _serviceProvider.GetService<IProjectScheduleEntryTypeService>().Get(sh => sh.Where(p => p.ProjectTypeID == entryId && !p.IsDeleted).ToList()).FirstOrDefault();
                            break;

                    }
                    break;
                case Type _ when table == typeof(ProjectStatusRecord):
                    switch (entry)
                    {
                        case Project _:
                            relatedEntry = _serviceProvider.GetService<IProjectStatusRecordService>().Get(sh => sh.Where(p => p.ProjectID == entryId && !p.IsDeleted).ToList()).FirstOrDefault();
                            break;

                    }
                    break;
                case Type _ when table == typeof(ProjectStatusRecordEntry):
                    switch (entry)
                    {
                        case ProjectStatusRecord _:
                            relatedEntry = _serviceProvider.GetService<IProjectStatusRecordEntryService>().Get(sh => sh.Where(p => p.ProjectStatusRecordID == entryId).ToList()).FirstOrDefault();
                            break;

                    }
                    break;
                case Type _ when table == typeof(ProjectType):
                    switch (entry)
                    {
                        case CostSubItem _:
                            relatedEntry = _serviceProvider.GetService<IProjectTypeService>().Get(sh => sh.Where(p => p.BusinessTripCostSubItemID == entryId).ToList()).FirstOrDefault();
                            break;

                    }
                    break;
                case Type _ when table == typeof(QualifyingRoleRate):
                    switch (entry)
                    {
                        case Department _:
                            relatedEntry = _serviceProvider.GetService<IQualifyingRoleRateService>().Get(sh => sh.Where(p => p.DepartmentID == entryId).ToList()).FirstOrDefault();
                            break;
                        case QualifyingRole _:
                            relatedEntry = _serviceProvider.GetService<IQualifyingRoleRateService>().Get(sh => sh.Where(p => p.QualifyingRoleID == entryId).ToList()).FirstOrDefault();
                            break;

                    }
                    break;
                case Type _ when table == typeof(ReportingPeriod):
                    switch (entry)
                    {
                        case Project _:
                            relatedEntry = _serviceProvider.GetService<IReportingPeriodService>().Get(sh => sh
                                .Where(p => p.VacationProjectID == entryId || p.VacationNoPaidProjectID == entryId).ToList()).FirstOrDefault();
                            break;

                    }
                    break;
                case Type _ when table == typeof(TSAutoHoursRecord):
                    switch (entry)
                    {
                        case Employee _:
                            relatedEntry = _serviceProvider.GetService<ITSAutoHoursRecordService>().Get(sh => sh.Where(p => p.EmployeeID == entryId && !p.IsDeleted).ToList()).FirstOrDefault();
                            break;
                        case Project _:
                            relatedEntry = _serviceProvider.GetService<ITSAutoHoursRecordService>().Get(sh => sh.Where(p => p.ProjectID == entryId && !p.IsDeleted).ToList()).FirstOrDefault();
                            break;

                    }
                    break;
                case Type _ when table == typeof(TSHoursRecord):
                    switch (entry)
                    {
                        case Employee _:
                            relatedEntry = _serviceProvider.GetService<ITSHoursRecordService>().Get(sh => sh.Where(p => p.EmployeeID == entryId && !p.IsDeleted).ToList()).FirstOrDefault();
                            break;
                        case Project _:
                            relatedEntry = _serviceProvider.GetService<ITSHoursRecordService>().Get(sh => sh.Where(p => p.ProjectID == entryId && !p.IsDeleted).ToList()).FirstOrDefault();
                            break;
                        case TSAutoHoursRecord _:
                            relatedEntry = _serviceProvider.GetService<ITSHoursRecordService>().Get(sh => sh.Where(p => p.ParentTSAutoHoursRecordID == entryId && !p.IsDeleted).ToList()).FirstOrDefault();
                            break;
                        case VacationRecord _:
                            relatedEntry = _serviceProvider.GetService<ITSHoursRecordService>().Get(sh => sh.Where(p => p.ParentVacationRecordID == entryId && !p.IsDeleted).ToList()).FirstOrDefault();
                            break;

                    }
                    break;
                case Type _ when table == typeof(VacationRecord):
                    switch (entry)
                    {
                        case Employee _:
                            relatedEntry = _serviceProvider.GetService<IVacationRecordService>().Get(sh => sh.Where(p => p.EmployeeID == entryId && !p.IsDeleted).ToList()).FirstOrDefault();
                            break;

                    }
                    break;
            }
            isRowNoRecycleBin = relatedEntry != null;
            return (isRowNoRecycleBin, relatedEntry == null ? 0 : (int)relatedEntry.GetType().GetProperty("ID").GetValue(relatedEntry, null));
        }
    }
}
