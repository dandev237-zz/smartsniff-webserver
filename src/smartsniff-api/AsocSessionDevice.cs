using System;
using System.Collections.Generic;

namespace smartsniff_api
{
    public partial class AsocSessionDevice
    {
        public int IdSession { get; set; }
        public int IdDevice { get; set; }
        public int IdLocation { get; set; }

        public virtual Device IdDeviceNavigation { get; set; }
        public virtual Location IdLocationNavigation { get; set; }
        public virtual Session IdSessionNavigation { get; set; }
    }
}
