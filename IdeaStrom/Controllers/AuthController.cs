using Microsoft.AspNetCore.Mvc;

namespace IdeaStrom.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
