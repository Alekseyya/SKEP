using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Core.BL.Interfaces;
using Core.Models;
using Core.Models.RBAC;
using MainApp.RBAC.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;





namespace MainApp.Controllers
{
    public class EmployeeOrganisationController : Controller
    {
        private readonly IEmployeeOrganisationService _employeeOrganisationService;
        private readonly IOrganisationService _organisationService;
        private readonly IEmployeePositionOfficialService _employeePositionOfficialService;
        private readonly IEmployeeService _employeeService;

        public EmployeeOrganisationController(IEmployeeOrganisationService employeeOrganisationService,
            IOrganisationService organisationService,
            IEmployeePositionOfficialService employeePositionOfficialService,
            IEmployeeService employeeService)
        {
            _employeeOrganisationService = employeeOrganisationService;
            _organisationService = organisationService;
            _employeePositionOfficialService = employeePositionOfficialService;
            _employeeService = employeeService;
        }

        [OperationActionFilter(nameof(Operation.EmployeeOrganisationView))]
        public ActionResult Index() => View(_employeeOrganisationService.Get(eos => eos/*.Include(eo => eo.Organisation)*/.ToList()));


        [OperationActionFilter(nameof(Operation.EmployeeOrganisationView))]
        public ActionResult Details(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            EmployeeOrganisation employeeOrganisation = _employeeOrganisationService.GetById(id.Value);
            if (employeeOrganisation == null)
                return StatusCode(StatusCodes.Status404NotFound);

            return View(employeeOrganisation);
        }

        [OperationActionFilter(nameof(Operation.EmployeeOrganisationCreateUpdate))]
        public ActionResult Create(int? employeeid)
        {
            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.ToList()), "ID", "FullName");
            ViewBag.OrganisationID = new SelectList(_organisationService.Get(x => x.ToList()), "ID", "FullName");
            ViewBag.EmployeePositionOfficialID = new SelectList(_employeePositionOfficialService.Get(x => x.ToList()), "ID", "FullName");
            return View();
        }

        [OperationActionFilter(nameof(Operation.EmployeeOrganisationCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EmployeeOrganisation employeeOrganisation)
        {
            if (employeeOrganisation.OrganisationDateEnd.HasValue && employeeOrganisation.OrganisationDateEnd <= employeeOrganisation.OrganisationDateBegin)
            {
                ModelState.AddModelError("OrganisationDateEnd", "Дата окончания должна быть больше даты начала.");
            }
            else
            {
                var employeeOrganisationList = _employeeOrganisationService.Get(x => x.Where(eo => eo.EmployeeID == employeeOrganisation.EmployeeID
                    && (!eo.OrganisationDateEnd.HasValue || eo.OrganisationDateEnd >= employeeOrganisation.OrganisationDateBegin)
                    && (!employeeOrganisation.OrganisationDateEnd.HasValue || eo.OrganisationDateBegin <= employeeOrganisation.OrganisationDateEnd)).ToList()); // сравнить по дате пересечения

                EmployeeOrganisation existEmployeeOrganisation = null;

                if (employeeOrganisation.IsMainPlaceWork && (existEmployeeOrganisation = employeeOrganisationList.FirstOrDefault(eo => eo.IsMainPlaceWork)) != null)
                {
                    string errorValue = existEmployeeOrganisation.Organisation.FullName + "\\" + existEmployeeOrganisation.EmployeePositionOfficial.FullName;
                    ModelState.AddModelError("IsMainPlaceWork", "Основным местом работы сотрудника на выбранный период является \"" + errorValue + "\". ");
                    // Ошибка о том, что уже есть основная работа
                }
                else if ((existEmployeeOrganisation = employeeOrganisationList.FirstOrDefault(eo => eo.OrganisationID == employeeOrganisation.OrganisationID)) != null)
                {
                    // генерируем сообщение об ошибки, что - В период с "Дата начала" сотрудник работает в "Организация" в должности "Должность по труд.кн.". Скорректируйте даты"
                    // "В период с "Дата начала" по "Дата окончания" сотрудник работает в "Организация" в должности "Должность по труд.кн.". Скорректируйте даты".
                    string errorValue = "";
                    if (existEmployeeOrganisation.OrganisationDateEnd.HasValue)
                    {
                        errorValue = "В период с \"" + existEmployeeOrganisation.OrganisationDateBegin.Value.ToString("dd-MM-yyyy") +
                            "\" по \"" + existEmployeeOrganisation.OrganisationDateEnd.Value.ToString("dd-MM-yyyy") +
                            "\" сотрудник работает в \"" + existEmployeeOrganisation.Organisation.FullName +
                            "\" в должности \"" + existEmployeeOrganisation.EmployeePositionOfficial.FullName + "\". Скорректируйте даты";
                    }
                    else
                    {
                        errorValue = "В период с \"" + existEmployeeOrganisation.OrganisationDateBegin.Value.ToString("dd-MM-yyyy") + "\"" +
                            " по \"" + existEmployeeOrganisation.OrganisationDateEnd.Value.ToString("dd-MM-yyyy") + "\"" +
                            "сотрудник работает в \"" + existEmployeeOrganisation.Organisation.FullName + "\"" +
                            " в должности \"" + existEmployeeOrganisation.EmployeePositionOfficial.FullName + "\". Скорректируйте даты";
                    }
                    ModelState.AddModelError("OrganisationDateBegin", errorValue);
                }
            }

            if (ModelState.IsValid)
            {
                _employeeOrganisationService.Add(employeeOrganisation);

                if (employeeOrganisation.IsMainPlaceWork
                    && employeeOrganisation.OrganisationDateBegin <= DateTime.Today
                    && (!employeeOrganisation.OrganisationDateEnd.HasValue || employeeOrganisation.OrganisationDateEnd >= DateTime.Today))
                {
                    var employee = _employeeService.GetById(employeeOrganisation.EmployeeID.Value);
                    if (employee.OrganisationID != employeeOrganisation.OrganisationID
                        || employee.EmployeePositionOfficialID != employeeOrganisation.EmployeePositionOfficialID)
                    {
                        employee.OrganisationID = employeeOrganisation.OrganisationID;
                        employee.EmployeePositionOfficialID = employeeOrganisation.EmployeePositionOfficialID;
                        _employeeService.Update(employee);
                    }
                }

                string returnUrl = Url.Action("Details", "Employee", new { id = employeeOrganisation.EmployeeID + "#employeeorganisation" }).Replace("%23", "#");
                return new RedirectResult(returnUrl);
            }

            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.ToList()), "ID", "FullName");
            ViewBag.OrganisationID = new SelectList(_organisationService.Get(x => x.ToList()), "ID", "FullName");
            ViewBag.EmployeePositionOfficialID = new SelectList(_employeePositionOfficialService.Get(x => x.ToList()), "ID", "FullName");
            return View(employeeOrganisation);
        }

        [OperationActionFilter(nameof(Operation.EmployeeOrganisationCreateUpdate))]
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            EmployeeOrganisation employeeOrganisation = _employeeOrganisationService.GetById(id.Value);
            if (employeeOrganisation == null)
                return StatusCode(StatusCodes.Status404NotFound);

            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.ToList()), "ID", "FullName");
            ViewBag.OrganisationID = new SelectList(_organisationService.Get(x => x.ToList()), "ID", "FullName");
            ViewBag.EmployeePositionOfficialID = new SelectList(_employeePositionOfficialService.Get(x => x.ToList()), "ID", "FullName");
            return View(employeeOrganisation);
        }

        [OperationActionFilter(nameof(Operation.EmployeeOrganisationCreateUpdate))]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EmployeeOrganisation employeeOrganisation)
        {
            if (employeeOrganisation.OrganisationDateEnd.HasValue && employeeOrganisation.OrganisationDateEnd <= employeeOrganisation.OrganisationDateBegin)
            {
                ModelState.AddModelError("OrganisationDateEnd", "Дата окончания должна быть больше даты начала.");
            }
            else
            {
                var employeeOrganisationList = _employeeOrganisationService.Get(x => x.Where(eo => eo.EmployeeID == employeeOrganisation.EmployeeID
                    && eo.ID != employeeOrganisation.ID
                    && (!eo.OrganisationDateEnd.HasValue || eo.OrganisationDateEnd >= employeeOrganisation.OrganisationDateBegin)
                    && (!employeeOrganisation.OrganisationDateEnd.HasValue || eo.OrganisationDateBegin <= employeeOrganisation.OrganisationDateEnd)).ToList());

                EmployeeOrganisation existEmployeeOrganisation = null;

                if (employeeOrganisation.IsMainPlaceWork && (existEmployeeOrganisation = employeeOrganisationList.FirstOrDefault(eo => eo.IsMainPlaceWork)) != null)
                {
                    string errorValue = existEmployeeOrganisation.Organisation.FullName + "\\" + existEmployeeOrganisation.EmployeePositionOfficial.FullName;
                    ModelState.AddModelError("IsMainPlaceWork", "Основным местом работы сотрудника на выбранный период является \"" + errorValue + "\". ");
                    // Ошибка о том, что уже есть основная работа
                }
                else if ((existEmployeeOrganisation = employeeOrganisationList.FirstOrDefault(eo => eo.OrganisationID == employeeOrganisation.OrganisationID)) != null)
                {
                    // генерируем сообщение об ошибки, что - В период с "Дата начала" сотрудник работает в "Организация" в должности "Должность по труд.кн.". Скорректируйте даты"
                    // "В период с "Дата начала" по "Дата окончания" сотрудник работает в "Организация" в должности "Должность по труд.кн.". Скорректируйте даты".
                    string errorValue = "";
                    if (existEmployeeOrganisation.OrganisationDateEnd.HasValue)
                    {
                        errorValue = "В период с \"" + existEmployeeOrganisation.OrganisationDateBegin.Value.ToString("dd-MM-yyyy") +
                            "\" по \"" + existEmployeeOrganisation.OrganisationDateEnd.Value.ToString("dd-MM-yyyy") +
                            "\" сотрудник работает в \"" + existEmployeeOrganisation.Organisation.FullName +
                            "\" в должности \"" + existEmployeeOrganisation.EmployeePositionOfficial.FullName + "\". Скорректируйте даты";
                    }
                    else
                    {
                        errorValue = "В период с \"" + existEmployeeOrganisation.OrganisationDateBegin.Value.ToString("dd-MM-yyyy") + "\"" +
                            " по \"" + existEmployeeOrganisation.OrganisationDateEnd.Value.ToString("dd-MM-yyyy") + "\"" +
                            "сотрудник работает в \"" + existEmployeeOrganisation.Organisation.FullName + "\"" +
                            " в должности \"" + existEmployeeOrganisation.EmployeePositionOfficial.FullName + "\". Скорректируйте даты";
                    }
                    ModelState.AddModelError("OrganisationDateBegin", errorValue);
                }
            }
            if (ModelState.IsValid)
            {
                _employeeOrganisationService.Update(employeeOrganisation);

                if (employeeOrganisation.IsMainPlaceWork
                    && employeeOrganisation.OrganisationDateBegin <= DateTime.Today
                    && (!employeeOrganisation.OrganisationDateEnd.HasValue || employeeOrganisation.OrganisationDateEnd >= DateTime.Today))
                {
                    var employee = _employeeService.GetById(employeeOrganisation.EmployeeID.Value);
                    if (employee.OrganisationID != employeeOrganisation.OrganisationID
                        || employee.EmployeePositionOfficialID != employeeOrganisation.EmployeePositionOfficialID)
                    {
                        employee.OrganisationID = employeeOrganisation.OrganisationID;
                        employee.EmployeePositionOfficialID = employeeOrganisation.EmployeePositionOfficialID;
                        _employeeService.Update(employee);
                    }
                }

                string returnUrl = Url.Action("Details", "Employee", new { id = employeeOrganisation.EmployeeID + "#employeeorganisation" }).Replace("%23", "#");
                return new RedirectResult(returnUrl);
            }

            ViewBag.EmployeeID = new SelectList(_employeeService.Get(x => x.ToList()), "ID", "FullName");
            ViewBag.OrganisationID = new SelectList(_organisationService.Get(x => x.ToList()), "ID", "FullName");
            ViewBag.EmployeePositionOfficialID = new SelectList(_employeePositionOfficialService.Get(x => x.ToList()), "ID", "FullName");
            return View(employeeOrganisation);
        }

        [OperationActionFilter(nameof(Operation.EmployeeOrganisationDelete))]
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return StatusCode(StatusCodes.Status400BadRequest);

            EmployeeOrganisation employeeOrganisation = _employeeOrganisationService.GetById(id.Value);
            if (employeeOrganisation == null)
                return StatusCode(StatusCodes.Status404NotFound);

            return View(employeeOrganisation);
        }

        [OperationActionFilter(nameof(Operation.EmployeeOrganisationDelete))]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            EmployeeOrganisation employeeOrganisation = _employeeOrganisationService.GetById(id);
            int employeeID = employeeOrganisation.EmployeeID.Value;
            _employeeOrganisationService.Delete(employeeOrganisation.ID);
            string returnUrl = Url.Action("Details", "Employee", new { id = employeeID + "#employeeorganisation" }).Replace("%23", "#");
            return new RedirectResult(returnUrl);
        }
    }
}
