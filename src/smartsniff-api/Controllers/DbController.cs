using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using NpgsqlTypes;
using smartsniff_api.Models;
using System;
using System.Collections.Generic;
using System.Linq;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace smartsniff_api.Controllers
{
    [Route("api/[controller]")]
    public class DbController : Controller
    {
        private SmartsniffDbContext context;

        public DbController(SmartsniffDbContext _context)
        {
            context = _context;
        }

        // POST api/db/storedata
        [HttpPost("StoreData")]
        public IActionResult Post([FromBody] JObject jsonObject)
        {
            var format = "dd/MM/yyyy HH:mm:ss";
            var dateTimeConverter = new IsoDateTimeConverter { DateTimeFormat = format };

            RootObject data = JsonConvert.DeserializeObject<RootObject>(jsonObject.ToString(), dateTimeConverter);

            if (data.sessions.Any())
            {
                foreach (Session s in data.sessions)
                {
                    if (context.Session.Any(session => session.StartDate == s.StartDate && session.MacAddress == s.MacAddress)) continue;

                    context.Session.Add(s);
                    context.SaveChanges();
                }

                if (data.devices.Any())
                {
                    foreach (Device d in data.devices)
                    {
                        if (context.Device.Any(device => device.Bssid.Equals(d.Bssid))) continue;

                        context.Device.Add(d);
                        context.SaveChanges();
                    }
                }
                if (data.locations.Any())
                {
                    //Grab all locations from JSON
                    var allLocations = jsonObject["locations"].Children();

                    foreach (JToken token in allLocations)
                    {
                        //Grab coordinates token
                        JToken coordinatesToken = token.SelectToken("coordinates");
                        double latitude = coordinatesToken.SelectToken("latitude").ToObject<Double>();
                        double longitude = coordinatesToken.SelectToken("longitude").ToObject<Double>();

                        //Grab the date from the main token
                        DateTime locationDate = Convert.ToDateTime(token.SelectToken("date").ToString());

                        if (context.Location.Any(location => location.Coordinates.X == latitude &&
                                                  location.Coordinates.Y == longitude &&
                                                  location.Date.Equals(locationDate))) continue;

                        //If the location is not stored in the databse, then add it
                        Location locationToAdd = new Location
                        {
                            Date = locationDate,
                            Coordinates = new NpgsqlPoint(latitude, longitude)
                        };

                        context.Location.Add(locationToAdd);
                        context.SaveChanges();
                    }
                }

                if (data.asocsessiondevices.Any())
                {
                    foreach (AsocSessionDevice a in data.asocsessiondevices)
                    {
                        //Look for the session (startDate & Mac)
                        Session queriedSession = context.Session.Where(ID =>
                        a.session.StartDate == ID.StartDate
                        && a.session.MacAddress == ID.MacAddress).First();

                        //Look for the ID of the device
                        Device queriedDevice = context.Device.Where(dev => a.device.Equals(dev)).First();

                        //Look for the location
                        Location queriedLocation = getMatchingLocation(queriedSession, queriedDevice, jsonObject, dateTimeConverter, context);

                        var query = context.AsocSessionDevice.Where(assoc =>
                         assoc.IdSession == queriedSession.Id &&
                         assoc.IdDevice == queriedDevice.Id &&
                         assoc.IdLocation == queriedLocation.Id);

                        if (query.Any()) continue;

                        /*if (_context.AsocSessionDevice.Any(assoc =>
                         assoc.IdSession == queriedSession.Id &&
                         assoc.IdDevice == queriedDevice.Id &&
                         assoc.IdLocation == queriedLocation.Id)) continue;*/

                        //In the event the association does not exist, then add it
                        AsocSessionDevice association = new AsocSessionDevice
                        {
                            session = queriedSession,
                            device = queriedDevice,
                            location = queriedLocation
                        };

                        /*queriedSession.AsocSessionDevice.Add(association);
                        queriedDevice.AsocSessionDevice.Add(association);
                        queriedLocation.AsocSessionDevice.Add(association);*/

                        context.AsocSessionDevice.Add(association);

                        context.SaveChanges();
                    }
                }

                return StatusCode(201);
            }

            //400 Response
            return BadRequest();
        }

        public Location getMatchingLocation(Session queriedSession, Device queriedDevice, JObject jsonObject, IsoDateTimeConverter dateTimeConverter, SmartsniffDbContext context)
        {
            Location resultLocation = new Location();

            //Grab location info from the JSON (association section)
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

            //All JSON locations are stored in the DB. We can search for the exact location
            //inside the DB at this point
            resultLocation = context.Location.Where(location =>
                    associationCoordinates[0] == location.Coordinates.X &&
                    associationCoordinates[1] == location.Coordinates.Y).First();

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