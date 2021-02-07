using System;
using BL.Validation;
using Core.BL.Interfaces;
using Core.Models;
using Core.Models.RBAC;
using Core.Web;
using MainApp.RBAC.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;




using X.PagedList;


namespace MainApp.Controllers
{
    public class ProductionCalendarRecordController : Controller
    {
        private IProductionCalendarService _productionCalendarSvc;

        public ProductionCalendarRecordController(IProductionCalendarService productionCalendarSvc)
        {
            if (productionCalendarSvc == null)
                throw new ArgumentNullException(nameof(productionCalendarSvc));

            _productionCalendarSvc = productionCalendarSvc;
        }

        [OperationActionFilter(nameof(Operation.ServiceTablesView))]
        public ActionResult Index(int? page)
        {
            var records = _productionCalendarSvc.GetAllRecords();

            int pageSize = 20;
            int pageNumber = (page ?? 1);
            return View(records.ToPagedList(pageNumber, pageSize));
        }

        [OperationActionFilter(nameof(Operation.ServiceTablesView))]
        public ActionResult Details(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            var productionCalendarRecord = _productionCalendarSvc.GetRecordById(id.Value);
            if (productionCalendarRecord == null)
                return StatusCode(StatusCodes.Status404NotFound);

            return View(productionCalendarRecord);
        }

        [OperationActionFilter(nameof(Operation.ProductionCalendarRecordCreateUpdate))]
        public ActionResult Create()
        {
            return View();
        }

        [OperationActionFilter(nameof(Operation.ProductionCalendarRecordCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [TransactionalActionMvc]
        public ActionResult Create(ProductionCalendarRecord productionCalendarRecord)
        {
            _productionCalendarSvc.Validate(productionCalendarRecord, new ModelStateValidationRecipient(ModelState));
            if (ModelState.IsValid)
            {
                _productionCalendarSvc.AddRecord(productionCalendarRecord);
                return RedirectToAction("Index");
            }

            return View(productionCalendarRecord);
        }

        [OperationActionFilter(nameof(Operation.ProductionCalendarRecordCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            var productionCalendarRecord = _productionCalendarSvc.GetRecordById(id.Value);
            if (productionCalendarRecord == null)
                return StatusCode(StatusCodes.Status404NotFound);

            return View(productionCalendarRecord);
        }

        [OperationActionFilter(nameof(Operation.ProductionCalendarRecordCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [TransactionalActionMvc]
        public ActionResult Edit(ProductionCalendarRecord productionCalendarRecord)
        {
            _productionCalendarSvc.Validate(productionCalendarRecord, new ModelStateValidationRecipient(ModelState));
            if (ModelState.IsValid)
            {
                _productionCalendarSvc.UpdateRecord(productionCalendarRecord);
                return RedirectToAction("Index");
            }
            return View(productionCalendarRecord);
        }

        [OperationActionFilter(nameof(Operation.ProductionCalendarRecordDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            var productionCalendarRecord = _productionCalendarSvc.GetRecordById(id.Value);
            if (productionCalendarRecord == null)
                return StatusCode(StatusCodes.Status404NotFound);

            return View(productionCalendarRecord);
        }

        // POST: ProductionCalendarRecord/Delete/5
        [OperationActionFilter(nameof(Operation.ProductionCalendarRecordDelete))]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [TransactionalActionMvc]
        public ActionResult DeleteConfirmed(int id)
        {
            _productionCalendarSvc.DeleteRecord(id);
            return RedirectToAction("Index");
        }
    }
}
