using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using smartsniff_api.Models;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace smartsniff_api.Controllers
{
    [Route("api/[controller]")]
    public class DeviceController : Controller
    {
        private smartsniff_dbContext _context;

        public DeviceController(smartsniff_dbContext context)
        {
            _context = context;
        }

        // GET: api/device
        [HttpGet]
        public IEnumerable<Device> Get()
        {
            return _context.Device.AsEnumerable();
        }

        // GET api/device/5
        [HttpGet("{id}", Name = "GetDevice")]
        public IActionResult GetDeviceById(int id)
        {
            List<Device> deviceList = _context.Device.ToList();

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
                _context.Device.Add(device);
                _context.SaveChanges();
                //201 Response
                return CreatedAtRoute("GetDevice", new { id = device.Id }, device);
            }

            //400 Response
            return BadRequest();
        }
    }
}
