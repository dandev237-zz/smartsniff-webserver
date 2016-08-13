using System;
using System.Collections.Generic;
using NpgsqlTypes;

namespace smartsniff_api.Models
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
