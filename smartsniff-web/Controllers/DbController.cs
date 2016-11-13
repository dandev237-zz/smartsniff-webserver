using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using NpgsqlTypes;
using smartsniff_web;
using smartsniff_web.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace smartsniff_api.Controllers
{
    [Route("api/[controller]")]
    public class DbController : Controller
    {
        private SmartsniffDbContext context;

        public DbController(SmartsniffDbContext dbcontext)
        {
            context = dbcontext;
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
                        Location queriedLocation = GetMatchingLocation(queriedSession, queriedDevice, jsonObject, dateTimeConverter, context);

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

        private Location GetMatchingLocation(Session queriedSession, Device queriedDevice, JObject jsonObject, IsoDateTimeConverter dateTimeConverter, SmartsniffDbContext context)
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

        public struct HeatmapPoint
        {
            public double lat { get; set; }
            public double lng { get; set; }
            public int count { get; set; }

            public HeatmapPoint(double latitude, double longitude, int countValue)
            {
                lat = latitude;
                lng = longitude;
                count = countValue;
            }
        }

        // GET api/db/getheatmapdata
        [HttpGet("GetHeatmapData")]
        public IActionResult GetHeatmapData()
        {
            List<Location> locations = context.Location.AsEnumerable().ToList();

            HeatmapPoint[] heatmapData = new HeatmapPoint[locations.Count];
            foreach (Location loc in locations)
            {
                var count = context.AsocSessionDevice.Where(o => o.IdLocation == loc.Id && o.device.Type.Equals("WIFI")).Count();

                heatmapData[locations.IndexOf(loc)] = new HeatmapPoint(loc.Coordinates.X, loc.Coordinates.Y, count);
            }

            JsonResult heatmapDataJson = Json(heatmapData);
            return heatmapDataJson;
        }

        public struct StatData
        {
            public string label { get; set; }
            public string value { get; set; }

            public StatData(String dataLabel, int dataValue)
            {
                label = dataLabel;
                value = dataValue.ToString();
            }
        }

        public struct StatArray
        {
            public StatData[] statDataArray { get; set; }

            public StatArray(StatData[] dataArray)
            {
                statDataArray = dataArray;
            }
        }

        //GET api/db/getstatisticsdata
        [HttpGet("GetStatisticsData/{startDate?}/{endDate?}")]
        public IActionResult GetStatisticsData(DateTime? startDate = null, DateTime? endDate = null)
        {
            List<Session> sessions = context.Session.Include(a => a.AsocSessionDevice).AsEnumerable().ToList();
            List<Device> devices = new List<Device>();

            List<String> manufacturers = new List<String>();
            List<String> channelWidths = new List<String>();
            List<int> frequencies = new List<int>();
            List<String> bluetoothSubtypes = new List<String>();

            //Get all the dates within the time interval from the sessions table
            //Once we have all the dates, we get all the devices from the sessions
            List<String> dates = new List<String>();
            foreach (Session s in sessions)
            {
                String sessionDate = s.StartDate.Date.ToString("dd/M/yyyy");

                if (dates.Contains(sessionDate) || !DateInRange(sessionDate, startDate, endDate)) continue;
                else
                {
                    dates.Add(sessionDate);
                    foreach(AsocSessionDevice a in s.AsocSessionDevice)
                    {
                        Device d = context.Device.Where(o => o.Id == a.IdDevice).First();
                        if (!devices.Contains(d)) devices.Add(d);
                    }
                }
            }
            dates.Sort();

            //Device data required to fill the charts
            foreach (Device d in devices)
            {
                //Manufacturer data
                if (manufacturers.Contains(d.Manufacturer)) continue;
                else
                {
                    manufacturers.Add(d.Manufacturer);
                }

                if (d.Type.Equals("WIFI"))
                {
                    //Channel width data
                    if (channelWidths.Contains(d.ChannelWidth)) continue;
                    else
                    {
                        channelWidths.Add(d.ChannelWidth);
                    }

                    //Frequency data
                    if (GetFirstDigit(d.Frequency) == 2 && !frequencies.Contains(2))
                    {
                        frequencies.Add(2);
                    }
                    else if (GetFirstDigit(d.Frequency) == 5 && !frequencies.Contains(5))
                    {
                        frequencies.Add(5);
                    }
                }
                else //Bluetooth
                {
                    //Bluetooth subtypes data
                    if (bluetoothSubtypes.Contains(d.Characteristics)) continue;
                    else
                    {
                        bluetoothSubtypes.Add(d.Characteristics);
                    }
                }
            }


            //First chart (Manufacturers)
            if (manufacturers.Contains("NotFound")) manufacturers.Remove("NotFound");

            StatData[] manufacturerData = new StatData[manufacturers.Count];
            foreach(String manufacturer in manufacturers)
            {
                    var count = context.Device.Where(o => o.Manufacturer.Equals(manufacturer)).Count();
                    
                    if(count > 0)
                    {
                        manufacturerData[manufacturers.IndexOf(manufacturer)] = new StatData(manufacturer, count);
                    }
                    
            }

            //Second chart (Channel Width)
            StatData[] channelWidthData = new StatData[channelWidths.Count];
            foreach(String channelWidth in channelWidths)
            {
                var count = context.Device.Where(o => o.ChannelWidth.Equals(channelWidth)).Count();

                if (count > 0)
                {
                    channelWidthData[channelWidths.IndexOf(channelWidth)] = new StatData(channelWidth, count);
                }
            }

            //Third chart (Frequencies)
            StatData[] frequenciesData = new StatData[frequencies.Count];
            foreach(int frequency in frequencies)
            {
                var count = context.Device.Where(o => GetFirstDigit(o.Frequency) == frequency).Count();

                if(count > 0)
                {
                    if (frequency == 2)
                    {
                        frequenciesData[frequencies.IndexOf(frequency)] = new StatData("2,4 GHz", count);
                    }
                    else if (frequency == 5)
                    {
                        frequenciesData[frequencies.IndexOf(frequency)] = new StatData("5 GHz", count);
                    }
                }
            }

            //Fourth chart (Devices by date)
            StatData[] datesData = new StatData[dates.Count];
            foreach(String date in dates)
            {
                var count = context.AsocSessionDevice.Where(o => o.session.StartDate.Date.ToString("dd/M/yyyy").Equals(date)).Count();

                if(count > 9)
                {
                    datesData[dates.IndexOf(date)] = new StatData(date, count);
                }
            }

            //Fifth chart (Bluetooth subtypes)
            StatData[] bluetoothSubtypesData = new StatData[bluetoothSubtypes.Count];
            foreach(String bluetoothSubtype in bluetoothSubtypes)
            {
                var count = context.Device.Where(o => o.Characteristics.Equals(bluetoothSubtype)).Count();

                if(count > 0)
                {
                    bluetoothSubtypesData[bluetoothSubtypes.IndexOf(bluetoothSubtype)] = new StatData(bluetoothSubtype, count);
                }
            }

            //JSON Object
            StatArray[] statisticsArray = new StatArray[5];
            statisticsArray[0] = new StatArray(manufacturerData);
            statisticsArray[1] = new StatArray(channelWidthData);
            statisticsArray[2] = new StatArray(frequenciesData);
            statisticsArray[3] = new StatArray(datesData);
            statisticsArray[4] = new StatArray(bluetoothSubtypesData);

            JsonResult statisticsDataJson = Json(statisticsArray);
            return statisticsDataJson;
        }

        //GET api/db/gettabledata
        [HttpGet("GetTableData")]
        public IActionResult GetTableData()
        {
            int[] tableDataArray = new int[6];

            //Contributors count
            tableDataArray[0] = context.Session.Select(o => o.MacAddress).Distinct().Count();

            //Sessions count
            tableDataArray[1] = context.Session.Count();

            //Locations count
            tableDataArray[2] = context.Location.Count();

            //Total devices count
            tableDataArray[3] = context.Device.Count();

            //Wifi devices count
            tableDataArray[4] = context.Device.Where(o => o.Type.Equals("WIFI")).Count();

            //Bluetooth devices count
            tableDataArray[5] = context.Device.Where(o => o.Type.Equals("BLUETOOTH")).Count();

            JsonResult tableDataJson = Json(tableDataArray);
            return tableDataJson;
        }

        public int GetFirstDigit(short? number)
        {
            int r = (Int16)number;
            while (r >= 10)
            {
                r /= 10;
            }

            return r;
        }

        public bool DateInRange(String date, DateTime? start, DateTime? end)
        {
            DateTime parsedDate = DateTime.ParseExact(date, "dd/M/yyyy", CultureInfo.InvariantCulture);

            if (start != null && end != null)
                return start <= parsedDate && end >= parsedDate;
            else
                return true;
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