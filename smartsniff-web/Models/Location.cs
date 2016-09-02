using NpgsqlTypes;
using System;
using System.Collections.Generic;

namespace smartsniff_web.Models
{
    public partial class Location
    {
        public Location()
        {
            AsocSessionDevice = new HashSet<AsocSessionDevice>();
        }

        public int Id { get; set; }
        public DateTime Date { get; set; }
        public NpgsqlPoint Coordinates { get; set; }

        public virtual ICollection<AsocSessionDevice> AsocSessionDevice { get; set; }
    }
}