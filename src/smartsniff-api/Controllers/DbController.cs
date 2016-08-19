using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using smartsniff_api.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NpgsqlTypes;

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
                    //Coger todas las localizaciones del JSON
                    var allLocations = jsonObject["locations"].Children();

                    //Para cada localización
                    foreach(JToken token in allLocations)
                    {
                        //Coger el token de las coordenadas y, de ese token, latitud y longitud
                        JToken coordinatesToken = token.SelectToken("coordinates");
                        double latitude = coordinatesToken.SelectToken("latitude").ToObject<Double>();
                        double longitude = coordinatesToken.SelectToken("longitude").ToObject<Double>();

                        //Coger, del token principal, la fecha
                        DateTime locationDate = Convert.ToDateTime(token.SelectToken("date").ToString());
                        
                        //Crear la localización a añadir a la base de datos
                        Location locationToAdd = new Location
                        {
                            Date = locationDate,
                            Coordinates = new NpgsqlPoint(latitude, longitude)
                        };

                        _context.Location.Add(locationToAdd);
                    }

                    _context.SaveChanges();

                    //_context.Location.AddRange(data.locations);
                }

                if(data.asocsessiondevices.Any())
                {
                    //Por cada asociación
                    foreach(AsocSessionDevice a in data.asocsessiondevices)
                    {
                        //Buscar la ID de la sesión (Por fecha de inicio)
                        Session queriedSession = _context.Session.Where(ID => a.session.StartDate == ID.StartDate).First();

                        //Buscar la ID del dispositivo 
                        Device queriedDevice = _context.Device.Where(ID => a.device.Bssid == ID.Bssid).First();

                        //Buscar la ID de la localización
                        //Iterar en las location de las asociaciones 
                        Location queriedLocation = _context.Location.Where(ID => 
                        a.location.Coordinates.X == ID.Coordinates.X && a.location.Coordinates.Y == ID.Coordinates.Y)
                        .First();

                        //Añadir la fila con las tres IDs
                        AsocSessionDevice association = new AsocSessionDevice
                        {
                            session = queriedSession,
                            device = queriedDevice,
                            location = queriedLocation
                        };

                        queriedSession.AsocSessionDevice.Add(association);
                        queriedDevice.AsocSessionDevice.Add(association);
                        queriedLocation.AsocSessionDevice.Add(association);
                    }
                    _context.SaveChanges();

                    //_context.AsocSessionDevice.AddRange(data.asocsessiondevices);

                }
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
