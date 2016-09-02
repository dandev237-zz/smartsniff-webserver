using System;
using System.Collections.Generic;

namespace smartsniff_web.Models
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

        public override bool Equals(object obj)
        {
            return obj is Session && ((Session)obj).StartDate.Equals(this.StartDate)
                && ((Session)obj).EndDate.Equals(this.EndDate)
                && ((Session)obj).MacAddress.Equals(this.MacAddress);
        }

        public override int GetHashCode()
        {
            return (this.StartDate.GetHashCode() + this.EndDate.GetHashCode() + this.MacAddress.GetHashCode()).GetHashCode();
        }
    }
}