using System;
using System.Collections.Generic;
using Core.BL.Interfaces;
using Core.Models;
using Core.Models.RBAC;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Dto;
using WebApi.RBAC.Attributes;

namespace WebApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class BudgetLimitController : Controller
    {
        private IBudgetLimitService _budgetLimitService;
        public BudgetLimitController(IBudgetLimitService budgetLimitService)
        {
            _budgetLimitService = budgetLimitService ?? throw new ArgumentNullException(nameof(budgetLimitService));
        }

        [HttpGet]
        [Route("api/budgetlimit/amount")]
        [OperationApiActionFilter(nameof(Operation.ApiAccessDataView))]
        public IActionResult GetBudgetLimitData([FromQuery]int costSubItemtID, [FromQuery()]int departmentID, [FromQuery]int year, [FromQuery]int month, [FromQuery]int? projectID = null)
        {
            var data = _budgetLimitService.GetLimitData(costSubItemtID, departmentID, projectID, year, month);
            return Ok(new ServiceResultDTO<Limit> { Result = data });

        }

        [HttpGet]
        [Route("api/budgetlimit/amountforbusinesstrip")]
        [OperationApiActionFilter(nameof(Operation.ApiAccessDataView))]
        public IActionResult GetBudgetLimitData([FromQuery]int year, [FromQuery]int month, [FromQuery]int projectID)
        {
            var data = _budgetLimitService.GetLimitDataForBusinessTrip(projectID, year, month);
            return Ok(new ServiceResultDTO<Limit> { Result = data });
        }


        [Route("api/budgetlimit/summary")]
        [OperationApiActionFilter(nameof(Operation.ApiAccessDataView))]
        public IActionResult GetBudgetLimitDataSummary([FromQuery]int costSubItemtID, [FromQuery()]int departmentID, [FromQuery]int year, [FromQuery]int? projectID = null)
        {
            var data = _budgetLimitService.GetLimitDataSummary(costSubItemtID, departmentID, projectID, year);
            return Ok(new ServiceResultDTO<IEnumerable<Summary>> { Result = data });
        }

        [Route("api/budgetlimit/summaryforbusinesstrip")]
        [OperationApiActionFilter(nameof(Operation.ApiAccessDataView))]
        public IActionResult GetBudgetLimitDataSummaryForBusinessTrip([FromQuery]int year, [FromQuery]int projectID)
        {
            var data = _budgetLimitService.GetLimitDataSummaryForBusinessTrip(projectID, year);
            return Ok(new ServiceResultDTO<IEnumerable<Summary>> { Result = data });
        }

        [Route("api/budgetlimit/projectbusinesstripinfo")]
        [OperationApiActionFilter(nameof(Operation.ApiAccessDataView))]
        public IActionResult GetProjectBusinessTripInfo([FromQuery]int projectID)
        {
            var data = _budgetLimitService.GetProjectBusinessTripInfo(projectID);
            return Ok(new ServiceResultDTO<ProjectBusinessTripInfo> { Result = data });
        }
    }
}
