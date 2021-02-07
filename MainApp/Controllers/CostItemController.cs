using System.Collections.Generic;
using System.Linq;
using Core.BL;
using Core.BL.Interfaces;
using Core.Models;
using Core.Models.RBAC;
using Core.RecordVersionHistory;
using MainApp.RBAC.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;






namespace MainApp.Controllers
{
    public class CostItemController : Controller
    {
        private readonly ICostItemService _costItemService;
        private readonly ILogger<CostItemController> _logger;
        private readonly IUserService _userService;
        private readonly IServiceService _serviceService;

        public CostItemController(ICostItemService costItemService, ILogger<CostItemController> logger, IUserService userService, IServiceService serviceService)
        {
            _costItemService = costItemService;
            _logger = logger;
            _userService = userService;
            _serviceService = serviceService;
        }

        [OperationActionFilter(nameof(Operation.FinDataView))]
        public ActionResult Index()
        {
            return View(_costItemService.Get(x => x.ToList().OrderBy(ci => ci.ShortName).ToList()));
        }

        [OperationActionFilter(nameof(Operation.FinDataView))]
        public ActionResult Details(int? id, int? version)
        {
            if (id == null)
            {
                _logger.LogWarning(LoggingEvents.GetItemNotFound, "CostItem({id}) not found", id);
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            if (version != null && version.HasValue)
            {
                int ID = id.Value;
                var recordVersion = _costItemService.Get(y => y.Where(x => x.ItemID == ID
                                                                              && ((x.VersionNumber == version.Value)
                                                                                  || (version.Value == 0 &&
                                                                                      x.VersionNumber == null)))
                    .ToList(), GetEntityMode.VersionAndOther).FirstOrDefault();
                if (recordVersion == null)
                    return StatusCode(StatusCodes.Status404NotFound);

                recordVersion.Versions = new List<CostItem>().AsEnumerable();
                return View(recordVersion);
            }

            var record = _costItemService.Get(x => x.Where(c => c.ID == id.Value).ToList()).FirstOrDefault();
            if (record == null)
            {
                _logger.LogWarning(LoggingEvents.GetItemNotFound, "CostItem not found");
                return StatusCode(StatusCodes.Status404NotFound);
            }

            record.Versions = _costItemService.Get(x => x.Where(p => p.ItemID == record.ID || p.ID == record.ID).OrderByDescending(p => p.VersionNumber).ToList(), GetEntityMode.VersionAndOther);

            int versionsCount = record.Versions.Count();
            for (int i = 0; i < versionsCount; i++)
            {
                if (i == versionsCount - 1)
                    continue;

                var changes = ChangedRecordsFiller.GetChangedData(record.Versions.ElementAt(i), record.Versions.ElementAt(i + 1));
                record.Versions.ElementAt(i).ChangedRecords = changes;
            }

            return View(record);
        }

        [OperationActionFilter(nameof(Operation.FinDataCreateUpdate))]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.FinDataCreateUpdate))]
        public ActionResult Create(CostItem costItem)
        {
            if (ModelState.IsValid)
            {
                _costItemService.Add(costItem);
                return RedirectToAction("Index");
            }

            return View(costItem);
        }

        [OperationActionFilter(nameof(Operation.FinDataCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            var costItem = _costItemService.GetById(id.Value);
            if (costItem == null)
                return StatusCode(StatusCodes.Status404NotFound);

            if (costItem.IsVersion)
                return StatusCode(StatusCodes.Status403Forbidden);

            return View(costItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.FinDataCreateUpdate))]
        public ActionResult Edit(CostItem costItem)
        {
            if (ModelState.IsValid)
            {
                _costItemService.Update(costItem);
                return RedirectToAction("Index");
            }
            return View(costItem);
        }

        [OperationActionFilter(nameof(Operation.FinDataDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            CostItem costItem = _costItemService.GetById(id.Value);
            if (costItem == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(costItem);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.FinDataDelete))]
        public ActionResult DeleteConfirmed(int id)
        {
            CostItem costItem = _costItemService.GetById(id);
            var user = _userService.GetUserDataForVersion();
            var recycleBinInDBRelation = _serviceService.HasRecycleBinInDBRelation(costItem);
            if (recycleBinInDBRelation.hasRelated == false)
            {
                var recycleToRecycleBin = _costItemService.RecycleToRecycleBin(costItem.ID, user.Item1, user.Item2);
                if (!recycleToRecycleBin.toRecycleBin)
                {
                    ViewBag.RecycleBinError =
                        "Невозможно удалить, так как на удаляемый элемент ссылаются другие элементы в системе." +
                        "Сначала необходимо удалить элементы, которые ссылаются на данный элемент. " +
                        recycleToRecycleBin.relatedClassId;
                    return View(costItem);
                }
            }
            else
            {
                ViewBag.RecycleBinError =
                    "Невозможно удалить, так как на удаляемый элемент ссылаются другие элементы в системе." +
                    $"Сначала необходимо удалить элементы, которые ссылаются на данный элемент. {recycleBinInDBRelation.relatedInDBClassId}";
                return View(costItem);
            }
            return RedirectToAction("Index");
        }
    }
}
