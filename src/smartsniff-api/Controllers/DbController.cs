using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using smartsniff_api.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace smartsniff_api.Controllers
{
    [Route("api/[controller]")]
    public class DbController : Controller
    {
        private smartsniff_dbContext _context;

        public DbController(smartsniff_dbContext context)
        {
            _context = context;
        }

        // POST api/db/createdevice
        [HttpPost("StoreData")]
        public IActionResult Post([FromBody] JObject jsonObject)
        {
            var format = "dd/MM/yyyy HH:mm:ss";
            var dateTimeConverter = new IsoDateTimeConverter { DateTimeFormat = format };

            RootObject data = JsonConvert.DeserializeObject<RootObject>(jsonObject.ToString(), dateTimeConverter);

            if(data.sessions.Any())
            {
                foreach(Session s in data.sessions)
                {
                    _context.Session.Add(s);
                    _context.SaveChanges();
                }
                //_context.Session.AddRange(data.sessions);

                if (data.devices.Any())
                {
                    foreach (Device d in data.devices)
                    {
                        _context.Device.Add(d);
                        _context.SaveChanges();
                    }
                    //_context.Device.AddRange(data.devices);
                }
                if (data.locations.Any())
                {
                    foreach(Location l in data.locations)
                    {
                        _context.Location.Add(l);
                        _context.SaveChanges();
                    }

                    //_context.Location.AddRange(data.locations);
                }

                /*if (data.asocsessiondevices.Any())
                {
                    _context.AsocSessionDevice.AddRange(data.asocsessiondevices);
                    _context.SaveChanges();
                }*/
                return StatusCode(201);
            }

            //400 Response
            return BadRequest();
        }
    }

    public class RootObject
    {
        public List<Session> sessions { get; set; }
        public List<Device> devices { get; set; }
        public List<Location> locations { get; set; }
        public List<AsocSessionDevice> asocsessiondevices { get; set; }

        public RootObject(List<Session> s, List<Device> d, List<Location> l, List<AsocSessionDevice> a)
        {
            sessions = s;
            devices = d;
            locations = l;
            asocsessiondevices = a;
        }
    }
}
