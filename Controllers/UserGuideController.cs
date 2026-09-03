using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TrackerKerja.Controllers
{
    [Authorize]
    public class UserGuideController : Controller
    {
        [HttpGet("/UserGuide")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Panduan Pengguna (User Guide)";
            return View();
        }

        [HttpGet("/UserGuide/ModalContent")]
        public IActionResult ModalContent()
        {
            return PartialView("_UserGuideContent");
        }
    }
}
