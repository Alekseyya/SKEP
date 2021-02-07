using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using Core.Models;
using Core.Validation;

namespace BL.Validation
{
    public class ProjectValidator : EntityValidatorBase<Project>
    {
        private readonly IQueryable<Project> _projects;

        protected IQueryable<Project> Projects
        {
            get { return _projects; }
        }

        private Project _parentProject;

        protected Project ParentProject
        {
            get { return _parentProject; }
        }

        private IQueryable<Project> _childProjects;

        protected IQueryable<Project> ChildProjects
        {
            get { return _childProjects; }
        }

        private IQueryable<Project> _siblingProjects;

        protected IQueryable<Project> SiblingProjects
        {
            get { return _siblingProjects; }
        }

        private bool _relationshipsLoaded = false;

        public ProjectValidator(Project project, IValidationRecipient recipient, IQueryable<Project> projects) : base(project, recipient)
        {
            if (projects == null)
                throw new ArgumentNullException(nameof(projects));

            _projects = projects;
        }

        public override void Validate()
        {
            LoadRelationships();
            ValidateProperties();
            ValidateSingleLevelHierarchy();
            if (ParentProject != null)
                ValidateParentRelationships();
            if (ChildProjects.Count() > 0)
                ValidateChildrenRelationships();
        }

        #region Properties validation

        private bool IsShortNameIsUnique()
        {
            int count = Projects.Count(p => p.ShortName == Entity.ShortName && p.ID != Entity.ID);
            return count == 0;
        }

        private void ValidateProperties()
        {
            if (!IsShortNameIsUnique())
                Recipient.SetError(nameof(Entity.ShortName), "Карточка проекта с указанным кодом проекта уже существует");
            if (Entity.BeginDate.HasValue && Entity.EndDate.HasValue && Entity.BeginDate.Value > Entity.EndDate.Value)
                Recipient.SetError(nameof(Entity.EndDate), "Дата закрытия не может быть раньше даты открытия");
            if (Entity.ParentProjectID == Entity.ID)
                Recipient.SetError(nameof(Entity.ParentProjectID), "Проект не может быть дочерним к самому себе");
        }

        #endregion

        #region Hierarchy validation

        private void ValidateSingleLevelHierarchy()
        {
            if (ParentProject == null)
                return;

            if (ChildProjects.Count() > 0)
            {
                Recipient.SetError(nameof(Entity.ParentProjectID), "Нельзя указать родительский проект, так как у текущего проекта есть дочерние");
                return;
            }

            if (ParentProject.ParentProject != null || ParentProject.ParentProjectID.HasValue)
                Recipient.SetError(nameof(Entity.ParentProjectID), "Нельзя указать выбранный проект в качестве родительского, так как он является дочерним к другому проекту");
        }

        #endregion

        #region Parent relationships validation

        private void ValidateParentRelationships()
        {
            if (ParentProject != null)
            {
                if (ParentProject.BeginDate.HasValue && Entity.BeginDate.HasValue && Entity.BeginDate.Value < ParentProject.BeginDate.Value)
                    Recipient.SetError(nameof(Entity.BeginDate),
                        $"Дата открытия проекта не может быть раньше даты открытия родительского проекта ({ParentProject.BeginDate.Value.ToShortDateString()})");
                if (ParentProject.EndDate.HasValue && Entity.EndDate.HasValue && Entity.EndDate.Value > ParentProject.EndDate.Value)
                    Recipient.SetError(nameof(Entity.EndDate),
                        $"Дата закрытия проекта не может быть позже даты закрытия родительского проекта ({ParentProject.EndDate.Value.ToShortDateString()})");

                ValidateParentContractAmount();
                ValidateParentSubcontractorsAmountBudget();
                ValidateParentOrganisationAmountBudget();
                ValidateParentEmployeePayrollBudget();
                ValidateParentOtherCostsBudget();
            }
        }

        private void ValidateParentContractAmount()
        {
            ValidateParentBudgetPropertyAmount(nameof(Project.ContractAmount),
                "Общее значение суммы по договору дочерних проектов не может быть больше, чем у родительского проекта ({0})");
        }

        private void ValidateParentSubcontractorsAmountBudget()
        {
            ValidateParentBudgetPropertyAmount(nameof(Project.SubcontractorsAmountBudget),
                "Общее значение суммы субподрядчиков дочерних проектов не может быть больше, чем у родительского проекта ({0})");
        }

        private void ValidateParentOrganisationAmountBudget()
        {
            ValidateParentBudgetPropertyAmount(nameof(Project.OrganisationAmountBudget),
                "Общее значение суммы компании дочерних проектов не может быть больше, чем у родительского проекта ({0})");
        }

        private void ValidateParentEmployeePayrollBudget()
        {
            ValidateParentBudgetPropertyAmount(nameof(Project.EmployeePayrollBudget),
                "Общее значение планового ФОТ дочерних проектов не может быть больше, чем у родительского проекта ({0})");
        }

        private void ValidateParentOtherCostsBudget()
        {
            ValidateParentBudgetPropertyAmount(nameof(Project.OtherCostsBudget),
                "Общее значение прочих затрат дочерних проектов не может быть больше, чем у родительского проекта ({0})");
        }

        #endregion

        #region Children relationships validation

        private void ValidateChildrenRelationships()
        {
            if (ChildProjects.Count() > 0)
            {
                if (Entity.BeginDate.HasValue && ChildProjects.Count(p => p.BeginDate.HasValue) > 0)
                {
                    DateTime minBeginDate = ChildProjects.Where(p => p.BeginDate.HasValue).Min(p => p.BeginDate.Value);
                    if (Entity.BeginDate.Value > minBeginDate)
                        Recipient.SetError(nameof(Entity.BeginDate),
                            $"Дата открытия проекта не может быть позже, чем самая ранняя дата открытия дочерних проектов ({minBeginDate.ToShortDateString()})");
                }
                if (Entity.EndDate.HasValue && ChildProjects.Count(p => p.EndDate.HasValue) > 0)
                {
                    DateTime maxEndDate = ChildProjects.Where(p => p.EndDate.HasValue).Max(p => p.EndDate.Value);
                    if (Entity.EndDate.Value < maxEndDate)
                        Recipient.SetError(nameof(Entity.EndDate),
                            $"Дата закрытия проекта не может быть раньше, чем самая поздня дата закрытия дочерних проектов ({maxEndDate.ToShortDateString()})");
                }

                ValidateChildrenContractAmount();
                ValidateChildrenSubcontractorsAmountBudget();
                ValidateChildrenOrganisationAmountBudget();
                ValidateChildrenEmployeePayrollBudget();
                ValidateChildrenOtherCostsBudget();
            }

        }

        private void ValidateChildrenContractAmount()
        {
            ValidateChildrenBudgetPropertyAmount(nameof(Project.ContractAmount),
                "Сумма по договору не может быть меньше, чем общее значение дочерних проектов ({0})");
        }

        private void ValidateChildrenSubcontractorsAmountBudget()
        {
            ValidateChildrenBudgetPropertyAmount(nameof(Project.SubcontractorsAmountBudget),
                "Суммы субподрядчиков не может быть меньше, чем общее значение дочерних проектов ({0})");
        }

        private void ValidateChildrenOrganisationAmountBudget()
        {
            ValidateChildrenBudgetPropertyAmount(nameof(Project.OrganisationAmountBudget),
                "Сумма компании не может быть меньше, чем общее значение дочерних проектов ({0})");
        }

        private void ValidateChildrenEmployeePayrollBudget()
        {
            ValidateChildrenBudgetPropertyAmount(nameof(Project.EmployeePayrollBudget),
                "Плановый ФОТ не может быть меньше, чем общее значение дочерних проектов ({0})");
        }

        private void ValidateChildrenOtherCostsBudget()
        {
            ValidateChildrenBudgetPropertyAmount(nameof(Project.OtherCostsBudget),
                "Прочие затраты не могут быть меньше, чем общее значение дочерних проектов ({0})");
        }

        #endregion

        #region Budget properties helper methods

        private Expression<Func<Project, bool>> GetBudgetPropertyHasValueExpression(string propertyName)
        {
            var paramExpr = Expression.Parameter(typeof(Project), "p");
            var propertyExpr = Expression.Property(paramExpr, propertyName);
            var propertyHasValueBodyExpr = Expression.Property(propertyExpr, "HasValue");
            var propertyHasValueLambdaExpr = Expression.Lambda<Func<Project, bool>>(propertyHasValueBodyExpr, new ParameterExpression[] { paramExpr });
            return propertyHasValueLambdaExpr;
        }

        private Expression<Func<Project, decimal>> GetBudgetPropertyValueExpression(string propertyName)
        {
            var paramExpr = Expression.Parameter(typeof(Project), "p");
            var propertyExpr = Expression.Property(paramExpr, propertyName);
            var propertyValueBodyExpr = Expression.Property(propertyExpr, "Value");
            var propertyValueLambdaExpr = Expression.Lambda<Func<Project, decimal>>(propertyValueBodyExpr, new ParameterExpression[] { paramExpr });
            return propertyValueLambdaExpr;
        }

        private Func<Project, bool> GetBudgetPropertyHasValueFunc(string propertyName)
        {
            var paramExpr = Expression.Parameter(typeof(Project), "p");
            var propertyExpr = Expression.Property(paramExpr, propertyName);
            var propertyHasValueBodyExpr = Expression.Property(propertyExpr, "HasValue");
            var propertyHasValueLambdaExpr = GetBudgetPropertyHasValueExpression(propertyName);
            return propertyHasValueLambdaExpr.Compile();
        }

        private void ValidateParentBudgetPropertyAmount(string propertyName, string errorMessageTemplate)
        {
            LoadRelationships();
            if (ParentProject == null)
                return;
            var hasBudgetValue = GetBudgetPropertyHasValueFunc(propertyName);
            if (hasBudgetValue(Entity) && hasBudgetValue(ParentProject))
            {
                var conditionExpression = GetBudgetPropertyHasValueExpression(propertyName);
                var valueExpression = GetBudgetPropertyValueExpression(propertyName);
                var filtered = SiblingProjects.Where(conditionExpression).ToList();
                decimal total = filtered.AsQueryable().Sum(valueExpression);
                var valueFunc = valueExpression.Compile();
                total += valueFunc(Entity);
                if (total > valueFunc(ParentProject))
                    Recipient.SetError(propertyName, string.Format(errorMessageTemplate, valueFunc(ParentProject)));
            }
        }

        private void ValidateChildrenBudgetPropertyAmount(string propertyName, string errorMessageTemplate)
        {
            LoadRelationships();
            var hasBudgetValue = GetBudgetPropertyHasValueFunc(propertyName);
            if (hasBudgetValue(Entity))
            {
                var conditionExpression = GetBudgetPropertyHasValueExpression(propertyName);
                var valueExpression = GetBudgetPropertyValueExpression(propertyName);
                var filtered = ChildProjects.Where(conditionExpression).ToList();
                decimal total = filtered.AsQueryable().Sum(valueExpression);
                var valueFunc = valueExpression.Compile();
                if (total > valueFunc(Entity))
                    Recipient.SetError(propertyName, string.Format(errorMessageTemplate, total));
            }
        }

        #endregion

        #region Data loading

        private void LoadRelationships()
        {
            if (!_relationshipsLoaded)
            {
                if (Entity.ParentProject != null)
                    _parentProject = Entity.ParentProject;
                else if (Entity.ParentProjectID.HasValue)
                    _parentProject = Projects.Single(p => p.ID == Entity.ParentProjectID.Value);

                if (Entity.ChildProjects != null)
                    _childProjects = Entity.ChildProjects.AsQueryable();
                else
                    _childProjects = Projects.Where(p => p.ParentProjectID == Entity.ID);

                if (_parentProject != null)
                    _siblingProjects = Projects.Where(p => p.ParentProjectID == _parentProject.ID && p.ID != Entity.ID);
                else
                    _siblingProjects = new List<Project>(0).AsQueryable();

                _relationshipsLoaded = true;
            }
        }

        #endregion

    }
}