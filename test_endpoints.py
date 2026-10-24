import subprocess
import time
import urllib.request
import json
import sys
from datetime import datetime

PORT = 5218
BASE_URL = f"http://127.0.0.1:{PORT}"

def wait_for_server():
    print(f"Waiting for server at {BASE_URL}...")
    for _ in range(30):
        try:
            req = urllib.request.Request(f"{BASE_URL}/swagger/v1/swagger.json")
            with urllib.request.urlopen(req) as resp:
                if resp.status == 200:
                    print("Server is up and Swagger is ready!")
                    return True
        except Exception:
            time.sleep(1)
    return False

def http_get(path):
    url = f"{BASE_URL}{path}"
    req = urllib.request.Request(url, headers={"Accept": "application/json"})
    with urllib.request.urlopen(req) as resp:
        return resp.status, json.loads(resp.read().decode())

def http_post(path, data):
    url = f"{BASE_URL}{path}"
    payload = json.dumps(data).encode("utf-8")
    req = urllib.request.Request(url, data=payload, headers={"Content-Type": "application/json", "Accept": "application/json"})
    try:
        with urllib.request.urlopen(req) as resp:
            return resp.status, json.loads(resp.read().decode())
    except urllib.error.HTTPError as err:
        body = err.read().decode()
        return err.code, json.loads(body) if body else {}

def run_tests():
    today = datetime.utcnow().strftime("%Y-%m-%d")
    print(f"\n==================================================")
    print(f"RUNNING HOSPITAL BILLING API ENDPOINT TESTS ({today})")
    print(f"==================================================")

    # 1. Admin Metrics
    status, data = http_get(f"/api/admin/metrics?date={today}")
    assert status == 200, f"Admin metrics failed: {status}"
    assert data["success"] is True
    metrics = data["data"]
    print(f"[PASS] GET /api/admin/metrics -> Total Patients: {metrics['totalRegisteredPatients']}, Total OPD Fees: {metrics['totalOpdFees']}, Total IPD Fees: {metrics['totalIpdFees']}, Grand Total: {metrics['grandTotalRevenue']}, Consultations: {metrics['totalConsultationCount']}")

    # 2. Doctor Daily Fees
    status, data = http_get(f"/api/admin/doctors/daily-fees?date={today}")
    assert status == 200, f"Doctor daily fees failed: {status}"
    assert data["success"] is True
    doc_fees = data["data"]
    assert len(doc_fees) >= 3, "Expected at least 3 doctors"
    print(f"[PASS] GET /api/admin/doctors/daily-fees -> Retrieved {len(doc_fees)} doctors:")
    for doc in doc_fees:
        print(f"       - {doc['doctorName']} ({doc['specialization']}): OPD Fee={doc['opdCharge']}, Visits={doc['consultationCount']}, Gross={doc['grossFeeCollected']}, Net Doctor Payout={doc['doctorNetPayout']}, Hospital Share={doc['hospitalShare']}")

    # 3. List Doctors
    status, data = http_get("/api/doctors")
    assert status == 200
    doctors = data["data"]
    print(f"[PASS] GET /api/doctors -> {len(doctors)} active doctors found.")

    # 4. Add New Doctor
    new_doc_req = {
        "fullName": "Dr. Sameer Joshi",
        "specialization": "Neurologist",
        "opdCharge": 1500.0,
        "payoutPercentage": 75.0,
        "isActive": True
    }
    status, data = http_post("/api/doctors", new_doc_req)
    assert status == 201, f"Failed adding doctor: {status} {data}"
    new_doc = data["data"]
    print(f"[PASS] POST /api/doctors -> Created Doctor ID {new_doc['id']}: {new_doc['fullName']} (Charge: {new_doc['opdCharge']}, Payout%: {new_doc['payoutPercentage']})")

    # 5. List Patients
    status, data = http_get("/api/patients?page=1&pageSize=10")
    assert status == 200
    patients_page = data["data"]
    print(f"[PASS] GET /api/patients -> Page {patients_page['page']}/{patients_page['totalPages']}, Total Patients: {patients_page['totalCount']}")

    # 6. Register New Patient
    new_pat_req = {
        "fullName": "Vikramaditya Roy",
        "age": 42,
        "gender": "Male",
        "contactNumber": "+91 9988776655"
    }
    status, data = http_post("/api/patients", new_pat_req)
    assert status == 201, f"Failed registering patient: {status} {data}"
    new_pat = data["data"]
    assert new_pat["uhid"].startswith("UHID-"), f"Invalid UHID format: {new_pat['uhid']}"
    print(f"[PASS] POST /api/patients -> Registered ID {new_pat['id']}: {new_pat['fullName']} (UHID: {new_pat['uhid']})")

    # 7. Search Patients
    status, data = http_get(f"/api/patients?search=Vikramaditya")
    assert status == 200
    search_results = data["data"]["items"]
    assert len(search_results) >= 1
    print(f"[PASS] GET /api/patients?search=Vikramaditya -> Found {len(search_results)} matching patient(s).")

    # 8. Get Single Patient Details with Billing History
    status, data = http_get(f"/api/patients/1")
    assert status == 200
    pat1_details = data["data"]
    print(f"[PASS] GET /api/patients/1 -> Full History for {pat1_details['fullName']}: {len(pat1_details['opdInvoices'])} OPD slip(s), {len(pat1_details['ipdAdmissions'])} IPD admission(s).")

    # 9. Generate New OPD Invoice
    opd_req = {
        "patientId": new_pat["id"],
        "doctorId": new_doc["id"],
        "paymentMode": "UPI"
    }
    status, data = http_post("/api/billing/opd", opd_req)
    assert status == 201, f"Failed generating OPD invoice: {status} {data}"
    opd_inv = data["data"]
    assert opd_inv["feeAmount"] == 1500.0, f"Expected fee snapshot 1500.0, got {opd_inv['feeAmount']}"
    assert opd_inv["invoiceNumber"].startswith("OPD-"), f"Invalid invoice number format: {opd_inv['invoiceNumber']}"
    print(f"[PASS] POST /api/billing/opd -> Generated Invoice #{opd_inv['invoiceNumber']}: Patient='{opd_inv['patientName']}', Doctor='{opd_inv['doctorName']}', Fee={opd_inv['feeAmount']}, Mode={opd_inv['paymentMode']}")

    # 10. List OPD Slips for Today
    status, data = http_get(f"/api/billing/opd?date={today}")
    assert status == 200
    opd_list = data["data"]
    print(f"[PASS] GET /api/billing/opd?date={today} -> Total {len(opd_list)} OPD invoices for today.")

    # 11. Admit Patient to IPD
    admit_req = {
        "patientId": new_pat["id"],
        "attendingDoctorId": new_doc["id"],
        "bedNumber": "GEN-205",
        "dailyRoomRate": 1800.0
    }
    status, data = http_post("/api/billing/ipd/admit", admit_req)
    assert status == 201, f"Failed admitting IPD patient: {status} {data}"
    admit_res = data["data"]
    assert admit_res["admissionNumber"].startswith("IPD-")
    admission_id = admit_res["id"]
    print(f"[PASS] POST /api/billing/ipd/admit -> Admitted Admission #{admit_res['admissionNumber']}: Patient='{admit_res['patientName']}', Bed='{admit_res['bedNumber']}', Rate={admit_res['dailyRoomRate']}")

    # 12. Check Active Admissions
    status, data = http_get("/api/billing/ipd/active")
    assert status == 200
    active_beds = data["data"]
    assert any(b["id"] == admission_id for b in active_beds)
    print(f"[PASS] GET /api/billing/ipd/active -> {len(active_beds)} active IPD admissions in hospital.")

    # 13. Settle and Discharge Patient
    discharge_req = {
        "doctorVisitCharges": 1200.0,
        "pharmacyCharges": 2500.0
    }
    status, data = http_post(f"/api/billing/ipd/discharge/{admission_id}", discharge_req)
    assert status == 200, f"Failed discharging patient: {status} {data}"
    discharge_res = data["data"]
    assert discharge_res["status"] == "Settled"
    # Days stayed = 1 min
    expected_room_charges = 1 * 1800.0
    expected_total = expected_room_charges + 1200.0 + 2500.0 # 5500.0
    assert discharge_res["totalBillAmount"] == expected_total, f"Expected total {expected_total}, got {discharge_res['totalBillAmount']}"
    print(f"[PASS] POST /api/billing/ipd/discharge/{admission_id} -> Settle successful!")
    print(f"       Breakdown: Days Stayed={discharge_res['daysStayed']}, Room Charges={discharge_res['roomCharges']}, Doctor Charges={discharge_res['doctorVisitCharges']}, Pharmacy Charges={discharge_res['pharmacyCharges']} => Total Settled Bill={discharge_res['totalBillAmount']}")

    # 14. Re-check Admin Metrics after new transactions
    status, data = http_get(f"/api/admin/metrics?date={today}")
    assert status == 200
    updated_metrics = data["data"]
    print(f"[PASS] GET /api/admin/metrics (Post-Transactions) -> Total Patients: {updated_metrics['totalRegisteredPatients']}, OPD: {updated_metrics['totalOpdFees']}, IPD: {updated_metrics['totalIpdFees']}, Grand Revenue: {updated_metrics['grandTotalRevenue']}")

    print("\n==================================================")
    print("ALL 14 API TEST CASES PASSED PERFECTLY!")
    print("==================================================\n")

if __name__ == "__main__":
    if not wait_for_server():
        print("Error: Server failed to start in time.")
        sys.exit(1)
    run_tests()
