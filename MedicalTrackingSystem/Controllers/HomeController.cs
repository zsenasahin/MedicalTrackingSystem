using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MedicalTrackingSystem.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "AccountView");
            }

            var userType = User.FindFirst(ClaimTypes.Role)?.Value;

            switch (userType)
            {
                case "Patient":
                    return View("PatientDashboard");
                case "Doctor":
                    return View("DoctorDashboard");
                case "Admin":
                    return View("AdminDashboard");
                default:
                    return RedirectToAction("Login", "AccountView");
            }
        }
    }
}
