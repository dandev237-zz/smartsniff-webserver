using Microsoft.AspNetCore.Mvc;

namespace smartsniff_web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            
            return View();
        }

        public IActionResult Stats()
        {
            return View();
        }

        public IActionResult DisplayDatesChart()
        {
            return View();
        }

        public IActionResult DisplayBluetoothChart()
        {
            return View();
        }

        public IActionResult DisplayChannelWidthChart()
        {
            return View();
        }

        public IActionResult DisplayFrequenciesChart()
        {
            return View();
        }

        public IActionResult DisplayManufacturerChart()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
