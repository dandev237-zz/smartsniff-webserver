using System;
using System.Collections.Generic;

namespace smartsniff_api
{
    public partial class Session
    {
        public Session()
        {
            AsocSessionDevice = new HashSet<AsocSessionDevice>();
        }

        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string MacAddress { get; set; }

        public virtual ICollection<AsocSessionDevice> AsocSessionDevice { get; set; }
    }
}
