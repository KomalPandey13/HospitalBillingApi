using HospitalBillingApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace HospitalBillingApi.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(HospitalDbContext context)
    {
        // Apply code-first migrations / ensure database schema is up-to-date
        await context.Database.MigrateAsync();

        if (await context.Doctors.AnyAsync())
        {
            return; // DB has already been seeded
        }

        // 1. Seed Doctors
        var drRajesh = new Doctor
        {
            FullName = "Dr. Rajesh Sharma",
            Specialization = "General Physician",
            OpdCharge = 500.00m,
            PayoutPercentage = 70.00m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        var drPriya = new Doctor
        {
            FullName = "Dr. Priya Patel",
            Specialization = "Pediatrician",
            OpdCharge = 700.00m,
            PayoutPercentage = 75.00m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        var drVikram = new Doctor
        {
            FullName = "Dr. Vikram Malhotra",
            Specialization = "Orthopedic Surgeon",
            OpdCharge = 1000.00m,
            PayoutPercentage = 65.00m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        var drAnanya = new Doctor
        {
            FullName = "Dr. Ananya Iyer",
            Specialization = "Cardiologist",
            OpdCharge = 1200.00m,
            PayoutPercentage = 80.00m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-20)
        };

        context.Doctors.AddRange(drRajesh, drPriya, drVikram, drAnanya);
        await context.SaveChangesAsync();

        // 2. Seed Patients
        var patient1 = new Patient
        {
            UHID = "UHID-20261020-0001",
            FullName = "Amit Verma",
            Age = 34,
            Gender = "Male",
            ContactNumber = "+91 9876543210",
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        var patient2 = new Patient
        {
            UHID = "UHID-20261021-0002",
            FullName = "Sunita Gupta",
            Age = 29,
            Gender = "Female",
            ContactNumber = "+91 9812345678",
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        };

        var patient3 = new Patient
        {
            UHID = "UHID-20261022-0003",
            FullName = "Ramesh Patel",
            Age = 52,
            Gender = "Male",
            ContactNumber = "+91 9823456789",
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        context.Patients.AddRange(patient1, patient2, patient3);
        await context.SaveChangesAsync();

        // 3. Seed Sample OPD Invoices for today and yesterday
        var today = DateTime.UtcNow;
        var todayDatePart = today.ToString("yyyyMMdd");

        var opd1 = new OpdInvoice
        {
            InvoiceNumber = $"OPD-{todayDatePart}-0001",
            PatientId = patient1.Id,
            DoctorId = drRajesh.Id,
            BillDate = today.AddHours(-3),
            FeeAmount = drRajesh.OpdCharge, // 500
            PaymentMode = "UPI",
            PaymentStatus = "Paid",
            CreatedAt = today.AddHours(-3)
        };

        var opd2 = new OpdInvoice
        {
            InvoiceNumber = $"OPD-{todayDatePart}-0002",
            PatientId = patient2.Id,
            DoctorId = drRajesh.Id,
            BillDate = today.AddHours(-2),
            FeeAmount = drRajesh.OpdCharge, // 500
            PaymentMode = "Cash",
            PaymentStatus = "Paid",
            CreatedAt = today.AddHours(-2)
        };

        var opd3 = new OpdInvoice
        {
            InvoiceNumber = $"OPD-{todayDatePart}-0003",
            PatientId = patient2.Id,
            DoctorId = drPriya.Id,
            BillDate = today.AddHours(-1),
            FeeAmount = drPriya.OpdCharge, // 700
            PaymentMode = "Card",
            PaymentStatus = "Paid",
            CreatedAt = today.AddHours(-1)
        };

        var opd4 = new OpdInvoice
        {
            InvoiceNumber = $"OPD-{todayDatePart}-0004",
            PatientId = patient3.Id,
            DoctorId = drVikram.Id,
            BillDate = today.AddMinutes(-30),
            FeeAmount = drVikram.OpdCharge, // 1000
            PaymentMode = "UPI",
            PaymentStatus = "Paid",
            CreatedAt = today.AddMinutes(-30)
        };

        context.OpdInvoices.AddRange(opd1, opd2, opd3, opd4);
        await context.SaveChangesAsync();

        // 4. Seed IPD Admissions
        // 4a. One settled admission discharged today
        var admitDate1 = today.AddDays(-3);
        var dischargeDate1 = today.AddHours(-1);
        var daysStayed1 = 3;
        var dailyRate1 = 2000.00m;
        var roomCharges1 = daysStayed1 * dailyRate1; // 6000
        var doctorVisits1 = 1500.00m;
        var pharmacy1 = 2200.00m;
        var totalBill1 = roomCharges1 + doctorVisits1 + pharmacy1; // 9700

        var settledAdmission = new IpdAdmission
        {
            AdmissionNumber = $"IPD-{todayDatePart}-0001",
            PatientId = patient1.Id,
            AttendingDoctorId = drRajesh.Id,
            BedNumber = "PVT-102",
            AdmissionDate = admitDate1,
            DischargeDate = dischargeDate1,
            DailyRoomRate = dailyRate1,
            DoctorVisitCharges = doctorVisits1,
            PharmacyCharges = pharmacy1,
            TotalBillAmount = totalBill1,
            Status = "Settled",
            CreatedAt = admitDate1
        };

        // 4b. One active admission currently in hospital
        var admitDate2 = today.AddDays(-2);
        var activeAdmission = new IpdAdmission
        {
            AdmissionNumber = $"IPD-{todayDatePart}-0002",
            PatientId = patient3.Id,
            AttendingDoctorId = drVikram.Id,
            BedNumber = "ICU-04",
            AdmissionDate = admitDate2,
            DischargeDate = null,
            DailyRoomRate = 4500.00m,
            DoctorVisitCharges = 0m,
            PharmacyCharges = 0m,
            TotalBillAmount = 0m,
            Status = "Admitted",
            CreatedAt = admitDate2
        };

        context.IpdAdmissions.AddRange(settledAdmission, activeAdmission);
        await context.SaveChangesAsync();
    }
}
