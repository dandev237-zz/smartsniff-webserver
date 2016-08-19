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
                        //Buscar la sesión (Por fecha de inicio y MAC)
                        Session queriedSession = _context.Session.Where(ID => 
                        a.session.StartDate == ID.StartDate
                        && a.session.MacAddress == ID.MacAddress).First();

                        //Buscar la ID del dispositivo 
                        Device queriedDevice = _context.Device.Where(ID => a.device.Bssid == ID.Bssid).First();

                        //Buscar la ID de la localización
                        Location queriedLocation = getMatchingLocation(queriedSession, queriedDevice, jsonObject, dateTimeConverter);

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
                }
                return StatusCode(201);
            }

            //400 Response
            return BadRequest();
        }

        public Location getMatchingLocation(Session queriedSession, Device queriedDevice, JObject jsonObject, IsoDateTimeConverter dateTimeConverter)
        {
            Location resultLocation = new Location();

            //Coger la location de la asociación en el JSON
            var allAssociations = jsonObject["asocsessiondevices"].Children();
            double[] associationCoordinates = new double[2];

            foreach (JToken token in allAssociations)
            {
                JToken tokenizedSession = token.SelectToken("session");
                Session session = JsonConvert.DeserializeObject<Session>(tokenizedSession.ToString(), dateTimeConverter);

                JToken tokenizedDevice = token.SelectToken("device");
                Device device = tokenizedDevice.ToObject<Device>();

                if (queriedSession.Equals(session))
                {
                    if (queriedDevice.Equals(device))
                    {
                        //We have found the correct association! Get the location coordinates to compare them later
                        //to the locations contained in the JSON file
                        JToken coordinatesToken = token.SelectToken("location");
                        associationCoordinates[0] = coordinatesToken.SelectToken("latitude").ToObject<Double>();
                        associationCoordinates[1] = coordinatesToken.SelectToken("longitude").ToObject<Double>();

                        break;
                    }
                }

            }

            var allLocations = jsonObject["locations"].Children();
            foreach (JToken token in allLocations)
            {
                JToken coordinatesToken = token.SelectToken("coordinates");
                double latitude = coordinatesToken.SelectToken("latitude").ToObject<Double>();
                double longitude = coordinatesToken.SelectToken("longitude").ToObject<Double>();

                if (associationCoordinates[0] == latitude && associationCoordinates[1] == longitude)
                {
                    //We have found the correct location!
                    NpgsqlPoint locationCoordinates = new NpgsqlPoint(latitude, longitude);

                    resultLocation = _context.Location.Where(Location => 
                    locationCoordinates.X == Location.Coordinates.X &&
                    locationCoordinates.Y == Location.Coordinates.Y).First();

                    break;
                }
            }
            return resultLocation;
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
