using HospitalBillingApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace HospitalBillingApi.Data;

public class HospitalDbContext : DbContext
{
    public HospitalDbContext(DbContextOptions<HospitalDbContext> options) : base(options)
    {
    }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<OpdInvoice> OpdInvoices => Set<OpdInvoice>();
    public DbSet<IpdAdmission> IpdAdmissions => Set<IpdAdmission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Patient Configuration
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UHID).IsUnique();
            entity.Property(e => e.UHID).IsRequired().HasMaxLength(30);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Gender).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ContactNumber).HasMaxLength(25);
        });

        // 2. Doctor Configuration
        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Specialization).IsRequired().HasMaxLength(100);
            entity.Property(e => e.OpdCharge).HasPrecision(18, 2);
            entity.Property(e => e.PayoutPercentage).HasPrecision(5, 2);
        });

        // 3. OpdInvoice Configuration
        modelBuilder.Entity<OpdInvoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(30);
            entity.Property(e => e.FeeAmount).HasPrecision(18, 2);
            entity.Property(e => e.PaymentMode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.PaymentStatus).IsRequired().HasMaxLength(20);

            entity.HasOne(e => e.Patient)
                  .WithMany(p => p.OpdInvoices)
                  .HasForeignKey(e => e.PatientId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Doctor)
                  .WithMany(d => d.OpdInvoices)
                  .HasForeignKey(e => e.DoctorId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 4. IpdAdmission Configuration
        modelBuilder.Entity<IpdAdmission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AdmissionNumber).IsUnique();
            entity.Property(e => e.AdmissionNumber).IsRequired().HasMaxLength(30);
            entity.Property(e => e.BedNumber).IsRequired().HasMaxLength(30);
            entity.Property(e => e.DailyRoomRate).HasPrecision(18, 2);
            entity.Property(e => e.DoctorVisitCharges).HasPrecision(18, 2);
            entity.Property(e => e.PharmacyCharges).HasPrecision(18, 2);
            entity.Property(e => e.TotalBillAmount).HasPrecision(18, 2);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);

            entity.HasOne(e => e.Patient)
                  .WithMany(p => p.IpdAdmissions)
                  .HasForeignKey(e => e.PatientId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AttendingDoctor)
                  .WithMany(d => d.IpdAdmissions)
                  .HasForeignKey(e => e.AttendingDoctorId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
