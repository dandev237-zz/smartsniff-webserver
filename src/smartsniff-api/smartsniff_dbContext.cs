using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using smartsniff_api.Models;

namespace smartsniff_api
{
    public partial class smartsniff_dbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            #warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
            optionsBuilder.UseNpgsql(@"Host=localhost;Database=smartsniff-db;Username=postgres;Password=root;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AsocSessionDevice>(entity =>
            {
                entity.HasKey(e => new { e.IdSession, e.IdLocation, e.IdDevice })
                    .HasName("PK_asocSessionDevice");

                entity.ToTable("asocSessionDevice", "schemadb");

                entity.HasIndex(e => e.IdDevice)
                    .HasName("fki_idDeviceForeign");

                entity.HasIndex(e => e.IdLocation)
                    .HasName("fki_idLocationForeign");

                entity.HasIndex(e => e.IdSession)
                    .HasName("fki_idSessionForeign");

                entity.Property(e => e.IdSession)
                    .HasColumnName("idSession")
                    .HasDefaultValueSql("nextval('schemadb.\"asocSessionDevice_idSession_seq\"'::regclass)");

                entity.Property(e => e.IdLocation)
                    .HasColumnName("idLocation")
                    .HasDefaultValueSql("nextval('schemadb.\"asocSessionDevice_idLocation_seq\"'::regclass)");

                entity.Property(e => e.IdDevice)
                    .HasColumnName("idDevice")
                    .HasDefaultValueSql("nextval('schemadb.\"asocSessionDevice_idDevice_seq\"'::regclass)");

                entity.HasOne(d => d.IdDeviceNavigation)
                    .WithMany(p => p.AsocSessionDevice)
                    .HasForeignKey(d => d.IdDevice)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("idDeviceForeign");

                entity.HasOne(d => d.IdLocationNavigation)
                    .WithMany(p => p.AsocSessionDevice)
                    .HasForeignKey(d => d.IdLocation)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("idLocationForeign");

                entity.HasOne(d => d.IdSessionNavigation)
                    .WithMany(p => p.AsocSessionDevice)
                    .HasForeignKey(d => d.IdSession)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("idSessionForeign");
            });

            modelBuilder.Entity<Device>(entity =>
            {
                entity.ToTable("device", "schemadb");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasDefaultValueSql("nextval('schemadb.device_id_seq'::regclass)");

                entity.Property(e => e.Bssid)
                    .IsRequired()
                    .HasColumnName("bssid");

                entity.Property(e => e.Characteristics).HasColumnName("characteristics");

                entity.Property(e => e.Manufacturer).HasColumnName("manufacturer");

                entity.Property(e => e.Ssid)
                    .IsRequired()
                    .HasColumnName("ssid");

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasColumnName("type");
            });

            modelBuilder.Entity<Location>(entity =>
            {
                entity.ToTable("location", "schemadb");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasDefaultValueSql("nextval('schemadb.location_id_seq'::regclass)");

                entity.Property(e => e.Coordinates).HasColumnName("coordinates");

                entity.Property(e => e.Date)
                    .HasColumnName("date")
                    .HasColumnType("timestamptz");
            });

            modelBuilder.Entity<Session>(entity =>
            {
                entity.ToTable("session", "schemadb");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasDefaultValueSql("nextval('schemadb.session_id_seq'::regclass)");

                entity.Property(e => e.EndDate)
                    .HasColumnName("endDate")
                    .HasColumnType("timestamptz");

                entity.Property(e => e.MacAddress)
                    .IsRequired()
                    .HasColumnName("macAddress");

                entity.Property(e => e.StartDate)
                    .HasColumnName("startDate")
                    .HasColumnType("timestamptz");
            });

            modelBuilder.HasSequence("asocSessionDevice_idDevice_seq", "schemadb");

            modelBuilder.HasSequence("asocSessionDevice_idLocation_seq", "schemadb");

            modelBuilder.HasSequence("asocSessionDevice_idSession_seq", "schemadb");

            modelBuilder.HasSequence("device_id_seq", "schemadb");

            modelBuilder.HasSequence("location_id_seq", "schemadb");

            modelBuilder.HasSequence("session_id_seq", "schemadb");
        }

        public virtual DbSet<AsocSessionDevice> AsocSessionDevice { get; set; }
        public virtual DbSet<Device> Device { get; set; }
        public virtual DbSet<Location> Location { get; set; }
        public virtual DbSet<Session> Session { get; set; }
    }
}