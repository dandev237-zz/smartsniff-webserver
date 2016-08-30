namespace smartsniff_api.Models
{
    public partial class AsocSessionDevice
    {
        public int IdSession { get; set; }
        public int IdDevice { get; set; }
        public int IdLocation { get; set; }

        public virtual Device device { get; set; }
        public virtual Location location { get; set; }
        public virtual Session session { get; set; }
    }
}