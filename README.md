# Hospital Billing & Fee Management System REST API

A production-ready ASP.NET Core Web API (.NET 10 / 8+) built with Entity Framework Core (Code-First) and SQLite, featuring Swagger/OpenAPI documentation, automated database migrations, rich initial seed data, and a suite of endpoint tests.

---

## 🏥 Domain Architecture & Analogy

Designed with transactional tracking analogous to HRMS / Leave Management Systems:
- **Attendance Logs ➔ Patient OPD Visits & IPD Bed Occupancy:** Real-time logging of patient OPD encounters and inpatient bed occupancy with daily stay tracking.
- **Payroll & Deductions ➔ Doctor Revenue Shares & Net Payouts:** Daily settlement engine calculating doctor consultation counts, gross collections, net doctor payouts (e.g., 70% share), and hospital retainage shares (e.g., 30%).

---

## 🛠️ Tech Stack & Key Features

- **Framework:** ASP.NET Core Web API (.NET 10.0 / C# 13)
- **ORM:** Entity Framework Core 10.0 (Code-First with Migrations & Split Query Optimization)
- **Database:** SQLite (`hospital_billing.db`) — Zero-configuration local execution
- **API Documentation:** OpenAPI / Swagger UI enabled at both `/` and `/swagger`
- **Identifier Generators:** Thread-safe, database-tracked sequential formatters:
  - Patient UHID: `UHID-YYYYMMDD-XXXX` (e.g. `UHID-20261024-0001`)
  - OPD Invoice: `OPD-YYYYMMDD-XXXX` (e.g. `OPD-20261024-0001`)
  - IPD Admission: `IPD-YYYYMMDD-XXXX` (e.g. `IPD-20261024-0001`)
- **Financial Precision:** `decimal` with explicit EF precision `(18, 2)` across all fee, charge, and revenue fields. Negative value prevention.
- **Seeding:** Pre-seeded with 4 active specialist doctors, 3 patients, multiple OPD invoices, and active + settled IPD admissions.

---

## 📁 Project Structure

```
HospitalBillingApi/
├── Controllers/
│   ├── BaseApiController.cs         # Shared base controller attributes
│   ├── AdminController.cs            # Metrics dashboard & doctor daily payout engine
│   ├── PatientsController.cs         # Patient registration, pagination & history
│   ├── DoctorsController.cs          # Doctor registration & active listing
│   ├── OpdBillingController.cs       # OPD slip creation & daily invoice query
│   └── IpdBillingController.cs       # IPD admission, active beds & discharge calculation
├── Data/
│   ├── HospitalDbContext.cs          # EF Core DbContext with model configurations
│   └── DbInitializer.cs              # Database migration & realistic seed data
├── DTOs/
│   ├── Common/                       # ApiResponse<T> & PagedResult<T>
│   ├── Admin/                        # Admin metrics & doctor daily fee models
│   ├── Billing/                      # OPD and IPD request/response models
│   ├── Doctors/                      # Doctor request and response models
│   └── Patients/                     # Patient request, response & billing history models
├── Entities/
│   ├── Patient.cs                    # Patient model with unique UHID
│   ├── Doctor.cs                     # Doctor model with OPD rate & payout percentage
│   ├── OpdInvoice.cs                 # OPD consultation invoice with fee snapshot
│   └── IpdAdmission.cs               # IPD admission with stay & discharge calculation
├── Middleware/
│   └── GlobalExceptionMiddleware.cs  # Unified JSON error handling
├── Migrations/                       # EF Core Code-First migration files
├── Program.cs                        # WebApplication bootstrap, DI, and Swagger config
├── appsettings.json                  # SQLite connection string & logging
├── start_server.sh                   # Server launch script
├── test_endpoints.py                 # Automated verification test suite
└── run_and_test.sh                   # One-shot background test runner
```

---

## 🚀 Quickstart: Running Migrations and Starting Server

### 1. Build the Application
```bash
cd HospitalBillingApi
dotnet build
```

### 2. Apply Database Migrations (Optional - auto-applied on startup)
```bash
dotnet ef database update
```
*Note: The application automatically verifies and applies migrations via `context.Database.MigrateAsync()` on startup if not already run.*

### 3. Start the API Server
```bash
./start_server.sh 5218
```
Or directly using the dotnet CLI:
```bash
dotnet run --project HospitalBillingApi.csproj
```

### 4. Interactive Swagger UI
Open your browser to:
- **Root URL:** `http://localhost:5218/`
- **Swagger URL:** `http://localhost:5218/swagger`

### 5. Run the Automated Test Suite
With the server running (or using the automated test runner):
```bash
./run_and_test.sh
```

---

## 📡 Core API Reference & Examples

### 1. Admin Dashboard & Settlement Engine

#### `GET /api/admin/metrics?date=YYYY-MM-DD`
Retrieves total registered patients (lifetime), daily OPD fees, daily settled IPD fees, grand total revenue, and consultation counts.
- **Sample Request:**
  ```bash
  curl -X GET "http://localhost:5218/api/admin/metrics?date=2026-10-24"
  ```
- **Sample Response:**
  ```json
  {
    "success": true,
    "message": "Metrics retrieved for 2026-10-24.",
    "data": {
      "date": "2026-10-24",
      "totalRegisteredPatients": 4,
      "totalOpdFees": 4200.0,
      "totalIpdFees": 15200.0,
      "grandTotalRevenue": 19400.0,
      "totalConsultationCount": 5,
      "totalIpdSettledCount": 2
    }
  }
  ```

#### `GET /api/admin/doctors/daily-fees?date=YYYY-MM-DD`
Calculates consultation count, gross fee collection, doctor payout share, and hospital retainage for all active doctors on the given date.
- **Sample Request:**
  ```bash
  curl -X GET "http://localhost:5218/api/admin/doctors/daily-fees?date=2026-10-24"
  ```
- **Sample Response:**
  ```json
  {
    "success": true,
    "message": "Settlement calculation completed for 2026-10-24. Active doctors: 4.",
    "data": [
      {
        "doctorId": 1,
        "doctorName": "Dr. Rajesh Sharma",
        "specialization": "General Physician",
        "opdCharge": 500.0,
        "payoutPercentage": 70.0,
        "consultationCount": 2,
        "grossFeeCollected": 1000.0,
        "doctorNetPayout": 700.0,
        "hospitalShare": 300.0
      },
      {
        "doctorId": 2,
        "doctorName": "Dr. Priya Patel",
        "specialization": "Pediatrician",
        "opdCharge": 700.0,
        "payoutPercentage": 75.0,
        "consultationCount": 1,
        "grossFeeCollected": 700.0,
        "doctorNetPayout": 525.0,
        "hospitalShare": 175.0
      }
    ]
  }
  ```

---

### 2. Patient Management

#### `POST /api/patients`
Registers a new patient and assigns a unique sequential UHID (`UHID-YYYYMMDD-XXXX`).
- **Request Body:**
  ```json
  {
    "fullName": "Vikramaditya Roy",
    "age": 42,
    "gender": "Male",
    "contactNumber": "+91 9988776655"
  }
  ```
- **Response (201 Created):**
  ```json
  {
    "success": true,
    "message": "Patient registered successfully.",
    "data": {
      "id": 4,
      "uhid": "UHID-20261024-0001",
      "fullName": "Vikramaditya Roy",
      "age": 42,
      "gender": "Male",
      "contactNumber": "+91 9988776655",
      "createdAt": "2026-10-24T07:17:08.577242Z"
    }
  }
  ```

#### `GET /api/patients?search={nameOrUhid}&page=1&pageSize=10`
Lists patients with pagination and text search.

#### `GET /api/patients/{id}`
Retrieves patient details along with complete OPD invoices and IPD admissions history.

---

### 3. Doctor Management

#### `POST /api/doctors`
Registers a new medical doctor with specialization, OPD fee, and revenue share percentage.
- **Request Body:**
  ```json
  {
    "fullName": "Dr. Sameer Joshi",
    "specialization": "Neurologist",
    "opdCharge": 1500.0,
    "payoutPercentage": 75.0,
    "isActive": true
  }
  ```

#### `GET /api/doctors`
Retrieves list of active doctors.

---

### 4. OPD Billing

#### `POST /api/billing/opd`
Generates an OPD consultation slip. Automatically snapshots the doctor's current `OpdCharge`.
- **Request Body:**
  ```json
  {
    "patientId": 4,
    "doctorId": 5,
    "paymentMode": "UPI"
  }
  ```
- **Response (201 Created):**
  ```json
  {
    "success": true,
    "message": "OPD invoice generated successfully.",
    "data": {
      "id": 5,
      "invoiceNumber": "OPD-20261024-0005",
      "patientId": 4,
      "patientUhid": "UHID-20261024-0001",
      "patientName": "Vikramaditya Roy",
      "doctorId": 5,
      "doctorName": "Dr. Sameer Joshi",
      "specialization": "Neurologist",
      "billDate": "2026-10-24T07:17:08.766795Z",
      "feeAmount": 1500.0,
      "paymentMode": "UPI",
      "paymentStatus": "Paid"
    }
  }
  ```

#### `GET /api/billing/opd?date=YYYY-MM-DD`
Retrieves all OPD slips issued on a given date.

---

### 5. IPD Billing & Discharge

#### `POST /api/billing/ipd/admit`
Admits a patient to an IPD bed. Validates that neither the patient nor the bed is already occupied.
- **Request Body:**
  ```json
  {
    "patientId": 4,
    "attendingDoctorId": 5,
    "bedNumber": "GEN-205",
    "dailyRoomRate": 1800.0
  }
  ```
- **Response (201 Created):**
  ```json
  {
    "success": true,
    "message": "Patient admitted successfully.",
    "data": {
      "id": 3,
      "admissionNumber": "IPD-20261024-0003",
      "patientId": 4,
      "patientName": "Vikramaditya Roy",
      "attendingDoctorName": "Dr. Sameer Joshi",
      "bedNumber": "GEN-205",
      "dailyRoomRate": 1800.0,
      "status": "Admitted"
    }
  }
  ```

#### `GET /api/billing/ipd/active`
Lists all active inpatient admissions with calculated days stayed and live running room charges.

#### `POST /api/billing/ipd/discharge/{id}`
Settles and discharges an inpatient admission.
- **Formula:**
  $$\text{Days Stayed} = \max(1, \lceil \text{DischargeDate} - \text{AdmissionDate} \rceil)$$
  $$\text{Total Bill} = (\text{Days Stayed} \times \text{Daily Room Rate}) + \text{Doctor Visit Charges} + \text{Pharmacy Charges}$$
- **Request Body:**
  ```json
  {
    "doctorVisitCharges": 1200.0,
    "pharmacyCharges": 2500.0
  }
  ```
- **Response (200 OK):**
  ```json
  {
    "success": true,
    "message": "IPD patient discharged and bill settled successfully.",
    "data": {
      "admissionNumber": "IPD-20261024-0003",
      "bedNumber": "GEN-205",
      "daysStayed": 1,
      "dailyRoomRate": 1800.0,
      "roomCharges": 1800.0,
      "doctorVisitCharges": 1200.0,
      "pharmacyCharges": 2500.0,
      "totalBillAmount": 5500.0,
      "status": "Settled",
      "summaryMessage": "Patient discharged from Bed GEN-205. Stayed 1 day(s). Total settled bill: $5,500.00."
    }
  }
  ```
