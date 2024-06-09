using Microsoft.EntityFrameworkCore;
using JamfMaintainer.Entities;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Text.RegularExpressions;

namespace JamfMaintainer
{
    
    public class Context : Microsoft.EntityFrameworkCore.DbContext
    {
        private readonly SettingsManager _settingsManager = new SettingsManager();
        public DbSet<LCSUser> LCSUsers { get; set; }

        public DbSet<LCSLocation> LCSLocations { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(_settingsManager.LCSConnectionString, o => o.UseCompatibilityLevel(120));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LCSUser>().ToTable(_settingsManager.SkoleLCSTable);
            modelBuilder.Entity<LCSLocation>().ToTable(_settingsManager.SkoleSchoolTable);
        }
    }

    public class ADMLCSContext : Microsoft.EntityFrameworkCore.DbContext
    {
        private readonly SettingsManager _settingsManager = new SettingsManager();
        public DbSet<ADMLCSUser> ADMLCSUsers { get; set; }

        public DbSet<ADMLCSLocation> ADMLCSLocations { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(_settingsManager.ADMLCSConnectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ADMLCSUser>().ToTable(_settingsManager.ADMLCSTable);
            modelBuilder.Entity<ADMLCSLocation>().ToTable(_settingsManager.ADMSchoolTable);
        }
    }


    public class UserStorageContext : Microsoft.EntityFrameworkCore.DbContext
    {
        private readonly SettingsManager _settingsManager = new SettingsManager();
        public UserStorageContext(DbContextOptions<UserStorageContext> options) : base(options)
        {
        }

        public DbSet<VFSUser> VFSUsers { get; set; }

        public DbSet<UserInfoUser> UserInfoUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VFSUser>().ToTable("xxx");
            modelBuilder.Entity<UserInfoUser>().ToTable("xxx");
        }
    }

    public class ArchiveContext : Microsoft.EntityFrameworkCore.DbContext
    {
        private readonly SettingsManager _settingsManager = new SettingsManager();
        public DbSet<ArchiveUser> ArchiveUsers { get; set; }

        public DbSet<Entities.Group> Groups { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite(_settingsManager.ArchiveConnectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ArchiveUser>().ToTable("ArchiveItems");
            modelBuilder.Entity<Entities.Group>().ToTable("ArchiveGroups");
        }
    }
}
