using FarmManagement.Core.Entities;
using FarmManagement.Core.Entities.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using FarmManagement.Core.Entities.Identity;

namespace FarmManagement.Infrastructure.Data
{
    // Inherit from IdentityDbContext using our custom ApplicationUser
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Name=ConnectionStrings:DefaultConnection");
            }
        }

        public DbSet<ContractType> ContractTypes { get; set; } = null!;
        public DbSet<Staff> Staff { get; set; } = null!;
        // renamed to avoid conflict with IdentityDbContext.Roles
        public DbSet<Role> AppRoles { get; set; } = null!;
        public DbSet<StaffRole> StaffRoles { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<AuthUser> AuthUsers { get; set; } = null!;
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        // Shift management
        public DbSet<FarmManagement.Core.Entities.ShiftType> ShiftTypes { get; set; } = null!;
        public DbSet<FarmManagement.Core.Entities.Shift> Shifts { get; set; } = null!;
        public DbSet<FarmManagement.Core.Entities.ShiftAssignment> ShiftAssignments { get; set; } = null!;
    // Time tracking
    public DbSet<FarmManagement.Core.Entities.TimeStation> TimeStations { get; set; } = null!;
    public DbSet<FarmManagement.Core.Entities.EntryType> EntryTypes { get; set; } = null!;
    public DbSet<FarmManagement.Core.Entities.TimeEntry> TimeEntries { get; set; } = null!;
    public DbSet<FarmManagement.Core.Entities.ExceptionType> ExceptionTypes { get; set; } = null!;
    public DbSet<FarmManagement.Core.Entities.ExceptionLog> ExceptionLogs { get; set; } = null!;
    public DbSet<FarmManagement.Core.Entities.AuditLog> AuditLogs { get; set; } = null!;

        // Payroll
        public DbSet<PayCalendar> PayCalendars { get; set; } = null!;
        public DbSet<PayrollRun> PayrollRuns { get; set; } = null!;
        public DbSet<PayrollLineItem> PayrollLineItems { get; set; } = null!;
        public DbSet<PayRate> PayRates { get; set; } = null!;
    public DbSet<FarmManagement.Core.Entities.Payroll.PayrollOptionEntity> PayrollOptions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Explicitly configure PKs for newly added time-tracking entities so design-time tools
            // can build a DbContext even when properties don't follow the default Id naming convention.
            modelBuilder.Entity<FarmManagement.Core.Entities.TimeStation>().HasKey(t => t.StationId);
            modelBuilder.Entity<FarmManagement.Core.Entities.EntryType>().HasKey(e => e.EntryTypeId);
            modelBuilder.Entity<FarmManagement.Core.Entities.TimeEntry>().HasKey(te => te.EntryId);
            modelBuilder.Entity<FarmManagement.Core.Entities.ExceptionType>().HasKey(et => et.TypeId);
            modelBuilder.Entity<FarmManagement.Core.Entities.ExceptionLog>().HasKey(el => el.ExceptionId);
            modelBuilder.Entity<FarmManagement.Core.Entities.AuditLog>().HasKey(al => al.AuditId);

            // Payroll entities
            modelBuilder.Entity<PayCalendar>().HasKey(pc => pc.PayCalendarId);
            modelBuilder.Entity<PayrollRun>().HasKey(pr => pr.PayrollRunId);
            modelBuilder.Entity<PayrollLineItem>().HasKey(pli => pli.PayrollLineItemId);
            modelBuilder.Entity<PayRate>().HasKey(pr => pr.PayRateId);
            modelBuilder.Entity<FarmManagement.Core.Entities.Payroll.PayrollOptionEntity>().HasKey(p => p.Id);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
