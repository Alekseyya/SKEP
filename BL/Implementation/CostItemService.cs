using System;
using Core.BL;
using Core.BL.Interfaces;
using Core.Data;
using Core.Data.Interfaces;
using Core.Models;

namespace BL.Implementation
{
    public class CostItemService : RepositoryAwareServiceBase<CostItem, int, ICostItemRepository>, ICostItemService
    {
        private readonly (string, string) _user;

        public CostItemService(IRepositoryFactory repositoryFactory, IUserService userService) : base(repositoryFactory)
        {
            _user = userService.GetUserDataForVersion();
        }

        public override CostItem Add(CostItem costItem)
        {
            if (costItem == null)
                throw new ArgumentNullException();

            var tsHoursRecordRepository = RepositoryFactory.GetRepository<ICostItemRepository>();
            costItem.InitBaseFields(Tuple.Create(_user.Item1, _user.Item2));
            return tsHoursRecordRepository.Add(costItem);
        }

        public override CostItem Update(CostItem costItem)
        {
            if (costItem == null) throw new ArgumentNullException(nameof(costItem));
            var costItemRepository = RepositoryFactory.GetRepository<ICostItemRepository>();

            var originalItem = costItemRepository.FindNoTracking(costItem.ID);

            costItem.UpdateBaseFields(Tuple.Create(_user.Item1, _user.Item2), originalItem.ID, originalItem);
            originalItem.FreeseVersion(originalItem.ID);


            costItemRepository.Add(originalItem);
            return costItemRepository.Update(costItem);
        }
    }
}
