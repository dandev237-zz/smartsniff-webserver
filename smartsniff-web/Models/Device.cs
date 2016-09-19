using System;
using System.Collections.Generic;

namespace smartsniff_web.Models
{
    public partial class Device
    {
        public Device()
        {
            AsocSessionDevice = new HashSet<AsocSessionDevice>();
        }

        public int Id { get; set; }
        public string Ssid { get; set; }
        public string Bssid { get; set; }
        public string Manufacturer { get; set; }
        public string Characteristics { get; set; }
        public string Type { get; set; }
        public string ChannelWidth { get; set; }
        public short? Frequency { get; set; }
        public short? SignalIntensity { get; set; }

        public virtual ICollection<AsocSessionDevice> AsocSessionDevice { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Device && ((Device)obj).Bssid.Equals(this.Bssid);
        }

        public override int GetHashCode()
        {
            return this.Bssid.GetHashCode();
        }
    }
}
