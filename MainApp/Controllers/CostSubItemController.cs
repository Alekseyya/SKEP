using System;
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
using Microsoft.AspNetCore.Mvc.Rendering;






namespace MainApp.Controllers
{
    public class CostSubItemController : Controller
    {
        private readonly ICostSubItemService _costSubItemService;
        private readonly ICostItemService _costItemService;
        private readonly IUserService _userService;
        private readonly IServiceService _serviceService;

        public CostSubItemController(ICostSubItemService costSubItemService, ICostItemService costItemService, IUserService userService, IServiceService serviceService)
        {
            _costSubItemService = costSubItemService;
            _costItemService = costItemService;
            _userService = userService;
            _serviceService = serviceService;
        }

        private void SetViewBag(CostSubItem item)
        {
            ViewBag.CostItemID = new SelectList(_costItemService.Get(x => x.ToList()), "ID", "FullName", item?.CostItemID);
        }

        [OperationActionFilter(nameof(Operation.FinDataView))]
        public ActionResult Index()
        {
            return View(_costSubItemService.Get(x => x.ToList().OrderBy(csi => csi.ShortName).ToList()));
        }


        [OperationActionFilter(nameof(Operation.FinDataView))]
        public ActionResult Details(int? id, int? version)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            if (version != null && version.HasValue)
            {
                int ID = id.Value;
                var recordVersion = _costSubItemService.Get(c => c.Where(x => x.ItemID == ID
                                                                              && ((x.VersionNumber == version.Value)
                                                                                  || (version.Value == 0 &&
                                                                                      x.VersionNumber == null)))
                    .ToList(), GetEntityMode.VersionAndOther).FirstOrDefault();
                if (recordVersion == null)
                    return StatusCode(StatusCodes.Status404NotFound);

                recordVersion.Versions = new List<CostSubItem>().AsEnumerable();
                return View(recordVersion);
            }

            var record = _costSubItemService.GetById(id.Value);
            if (record == null)
                return StatusCode(StatusCodes.Status404NotFound);

            record.Versions = _costSubItemService.Get(x => x
                .Where(p => /*p.IsVersion == true &&*/ p.ItemID == record.ID || p.ID == record.ID)
                .OrderByDescending(p => p.VersionNumber).ToList(), GetEntityMode.VersionAndOther);

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
            SetViewBag(null);
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.FinDataCreateUpdate))]
        public ActionResult Create(CostSubItem costSubItem)
        {
            if (ModelState.IsValid)
            {
                _costSubItemService.Add(costSubItem);
                return RedirectToAction("Index");
            }

            SetViewBag(costSubItem);
            return View(costSubItem);
        }


        [OperationActionFilter(nameof(Operation.FinDataCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (id.HasValue == false)
                return StatusCode(StatusCodes.Status400BadRequest);

            CostSubItem costSubItem = _costSubItemService.GetById(id.Value);
            if (costSubItem == null)
                return StatusCode(StatusCodes.Status404NotFound);

            if (costSubItem.IsVersion)
                return StatusCode(StatusCodes.Status403Forbidden);

            SetViewBag(costSubItem);
            return View(costSubItem);
        }


        [OperationActionFilter(nameof(Operation.FinDataCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CostSubItem costSubItem)
        {
            if (ModelState.IsValid)
            {
                _costSubItemService.Update(costSubItem);
                return RedirectToAction("Index");
            }

            SetViewBag(costSubItem);
            return View(costSubItem);
        }


        [OperationActionFilter(nameof(Operation.FinDataDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            CostSubItem costSubItem = _costSubItemService.GetById(id.Value);
            if (costSubItem == null)
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
            return View(costSubItem);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [OperationActionFilter(nameof(Operation.FinDataDelete))]
        public ActionResult DeleteConfirmed(int id)
        {
            CostSubItem costSubItem = _costSubItemService.GetById(id);
            var user = _userService.GetUserDataForVersion();
            var recycleBinInDBRelation = _serviceService.HasRecycleBinInDBRelation(costSubItem);
            if (recycleBinInDBRelation.hasRelated == false)
            {
                var recycleToRecycleBin = _costSubItemService.RecycleToRecycleBin(costSubItem.ID, user.Item1, user.Item2);
                if (!recycleToRecycleBin.toRecycleBin)
                {
                    ViewBag.RecycleBinError =
                        "Невозможно удалить, так как на удаляемый элемент ссылаются другие элементы в системе." +
                        "Сначала необходимо удалить элементы, которые ссылаются на данный элемент. " +
                        recycleToRecycleBin.relatedClassId;
                    return View(costSubItem);
                }
            }
            else
            {
                ViewBag.RecycleBinError =
                    "Невозможно удалить, так как на удаляемый элемент ссылаются другие элементы в системе." +
                    $"Сначала необходимо удалить элементы, которые ссылаются на данный элемент. {recycleBinInDBRelation.relatedInDBClassId}";
                return View(costSubItem);
            }
            return RedirectToAction("Index");
        }
    }
}
