using Microsoft.AspNetCore.Mvc;

namespace MainApp.Areas.Timesheet.Controllers
{
    [Area("Timesheet")]
    public class TimesheetController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}