using Microsoft.AspNetCore.Mvc;
using smartsniff_api.Models;
using System.Collections.Generic;
using System.Linq;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace smartsniff_api.Controllers
{
    [Route("api/[controller]")]
    public class DeviceController : Controller
    {
        private SmartsniffDbContext context;

        public DeviceController(SmartsniffDbContext _context)
        {
            context = _context;
        }

        // GET: api/device
        [HttpGet]
        public IEnumerable<Device> Get()
        {
            return context.Device.AsEnumerable();
        }

        // GET api/device/5
        [HttpGet("{id}", Name = "GetDevice")]
        public IActionResult GetDeviceById(int id)
        {
            List<Device> deviceList = context.Device.ToList();

            foreach (Device d in deviceList)
            {
                if (d.Id == id)
                    return new ObjectResult(d);
            }

            return NotFound();
        }

        // POST api/device/createdevice
        [HttpPost("CreateDevice")]
        public IActionResult Post([FromBody]Device device)
        {
            if (device != null)
            {
                context.Device.Add(device);
                context.SaveChanges();
                //201 Response
                return CreatedAtRoute("GetDevice", new { id = device.Id }, device);
            }

            //400 Response
            return BadRequest();
        }
    }
}