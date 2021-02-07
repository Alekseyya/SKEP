using System;
using System.Linq;
using Core.Models;
using Core.Validation;

namespace BL.Validation
{
    public class ProjectMemberValidator : EntityValidatorBase<ProjectMember>
    {
        private readonly IQueryable<Project> _projects;
        protected IQueryable<Project> Projects
        {
            get { return _projects; }
        }

        public ProjectMemberValidator(ProjectMember projectMember, IValidationRecipient recipient, IQueryable<Project> projects) : base(projectMember, recipient)
        {
            if (projects == null)
                throw new ArgumentNullException(nameof(projects));

            _projects = projects;
        }

        public override void Validate()
        {
            var project = Projects.FirstOrDefault(x => x.ID == Entity.ProjectID);
            if (project == null)
                Recipient.SetError(nameof(Entity.ProjectID), "Указанный проект не найден");

            if (project?.BeginDate > Entity.MembershipDateBegin)
                Recipient.SetError(nameof(Entity.MembershipDateBegin), "Дата начала вступления в роль не должна быть ранее даты начала проекта.");

            if (project?.EndDate < Entity.MembershipDateEnd)
                Recipient.SetError(nameof(Entity.MembershipDateEnd), "Дата окончания участия в роли не должна быть позднее даты окончания проекта.");

            if (Entity.MembershipDateBegin > Entity.MembershipDateEnd)
                Recipient.SetError(nameof(Entity.MembershipDateBegin), "Дата начала вступления в роль не должна быть позднее даты окончания участия роли.");
        }
    }
}