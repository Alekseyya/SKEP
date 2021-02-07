using System;
using Core.BL;
using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;

namespace BL.Implementation
{
    public class CostSubItemService : RepositoryAwareServiceBase<CostSubItem, int, ICostSubItemRepository>, ICostSubItemService
    {
        private readonly (string, string) _user;

        public CostSubItemService(IRepositoryFactory repositoryFactory, IUserService userService) : base(repositoryFactory)
        {
            _user = userService.GetUserDataForVersion();
        }

        public override CostSubItem Add(CostSubItem costSubItem)
        {
            if (costSubItem == null)
                throw new ArgumentNullException();

            var costSubItemRepository = RepositoryFactory.GetRepository<ICostSubItemRepository>();
            costSubItem.InitBaseFields(Tuple.Create(_user.Item1, _user.Item2));
            return costSubItemRepository.Add(costSubItem);
        }

        public override CostSubItem Update(CostSubItem costSubItem)
        {
            if (costSubItem == null) throw new ArgumentNullException(nameof(costSubItem));
            var costSubItemRepository = RepositoryFactory.GetRepository<ICostSubItemRepository>();

            var originalItem = costSubItemRepository.FindNoTracking(costSubItem.ID);

            costSubItem.UpdateBaseFields(Tuple.Create(_user.Item1, _user.Item2), originalItem.ID, originalItem);
            originalItem.FreeseVersion(originalItem.ID);


            costSubItemRepository.Add(originalItem);
            return costSubItemRepository.Update(costSubItem);
        }
    }
}
