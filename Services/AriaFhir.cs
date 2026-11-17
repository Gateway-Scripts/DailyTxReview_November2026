using DailyTxReview.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace DailyTxReview.Services
{
    public static class AriaFhir
    {
        private static string tokenUrl = "https://master-ae.vic.com:44333/tokenservice/connect/token";
        private static string baseUrl = "https://master-ae:55370/fhir/r4";
        private static string clientId = "c8f6cdaf-b492-4958-a75a-9a3730c05e01";
        private static string clientSecret = "GatewayScripts_Varian!2026";
        private static string scopes = "system/ActivityDefinition.rs system/AllergyIntolerance.cruds system/AllergyIntolerance.rs system/Appointment.cruds system/Appointment.rs system/AuditEvent.c system/AuditEvent.cruds system/BodyStructure.rs system/CarePlan.rs system/CareTeam.cruds system/CareTeam.rs system/ChargeItem.cruds system/ChargeItem.rs system/Condition.cruds system/Condition.rs system/Device.rs system/DocumentReference.cruds system/DocumentReference.rs system/Group.rs system/HealthcareService.rs system/Location.rs system/Observation.rs system/Organization.rs system/Patient.cruds system/Patient.rs system/Practitioner.cruds system/Practitioner.rs system/Procedure.rs system/ServiceRequest.rs system/Task.cruds system/Task.rs system/ValueSet.rs user/ActivityDefinition.rs user/AllergyIntolerance.cruds user/AllergyIntolerance.rs user/Appointment.cruds user/Appointment.rs user/AuditEvent.c user/AuditEvent.cruds user/BodyStructure.rs user/CarePlan.rs user/CareTeam.cruds user/CareTeam.rs user/ChargeItem.cruds user/ChargeItem.rs user/Condition.cruds user/Condition.rs user/Device.rs user/DocumentReference.cruds user/DocumentReference.rs user/Group.rs user/HealthcareService.rs user/Location.rs user/Observation.rs user/Organization.rs user/Patient.cruds user/Patient.rs user/Practitioner.cruds user/Practitioner.rs user/Procedure.rs user/ServiceRequest.rs user/Task.cruds user/Task.rs user/ValueSet.rs";
        private static HttpClient _client;
        private static string _accessToken;

        public static void InitializeFhirClient()
        {
            //intializze the Http Client.
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            _client = new HttpClient(handler);
            //request the token.
            var tokenData = new Dictionary<string, string>
            {
                {"grant_type" , "client_credentials" },
                {"client_id", clientId },
                {"client_secret", clientSecret },
                {"scope", scopes }
            };
            var tokenRequest = _client.PostAsync(tokenUrl, new FormUrlEncodedContent(tokenData));
            var tokenResponse = tokenRequest.Result.Content.ReadAsStringAsync().Result;
            var tokenString = JObject.Parse(tokenResponse);
            _accessToken = tokenString["access_token"].ToString();
        }
        public static PatientModel GetPatientById(string patientId)
        {
            //remember the format of the FHIR request. 
            //url/Resource?searchParameter=value&_pretty=true;
            //authenticate with our new Access Token.
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
            _client.DefaultRequestHeaders.Add("Accept", "application/fhir+json");

            string patientUrl = $"{baseUrl}/Patient?_id={patientId}&_pretty=true";
            var patientRequest = _client.GetAsync(patientUrl);
            var patientResponse = patientRequest.Result.Content.ReadAsStringAsync().Result;
            var bundle = JObject.Parse(patientResponse);
            var entries = bundle["entry"];//all the entries. 
            //we only expect 1 entry.
            if (entries == null || !entries.HasValues) { return null; }
            //get the first entry.
            var patientEntry = entries.First["resource"];//patient resource
            PatientModel patient = new PatientModel();
            //checking for nulls the whole way down to the family name.
            string lastName = patientEntry["name"]?[0]?["family"]?.ToString();
            string firstName = patientEntry["name"]?[0]?["given"]?[0]?.ToString();
            patient.FullName = $"{lastName}, {firstName}";
            patient.fhirID = patientEntry["id"]?.ToString();
            foreach(var id in patientEntry["identifier"])
            {
                if (id["system"]?.ToString().Contains("ARIAID1") == true)
                {
                    patient.Mrn = id["value"]?.ToString();
                    patient.PatientId = id["value"]?.ToString();
                }
            }
            patient.DateOfBirth = patientEntry["birthDate"]?.ToString();
            patient.Gender = patientEntry["gender"]?.ToString();
            if (patientEntry["telecom"] != null)
            {
                foreach (var phone in patientEntry["telecom"])
                {
                    if (phone["use"]?.ToString() == "mobile")
                    {
                        patient.PrimaryPhone = phone["value"]?.ToString();
                    }
                }
            }

            return patient;
        }
        public static string GetDeviceIdFromName(string machineId)
        {
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
            _client.DefaultRequestHeaders.Add("Accept", "application/fhir+json");
            string deviceUrl = $"{baseUrl}/Device?identifier={machineId}&_pretty=true";
            var deviceRequest = _client.GetAsync(deviceUrl);
            var deviceResponse = deviceRequest.Result.Content.ReadAsStringAsync().Result;
            var bundle = JObject.Parse(deviceResponse);
            var entries = bundle["entry"];//all the entries. 
            //we only expect 1 entry.
            if (entries == null || !entries.HasValues) { return null; }
            //get the first entry.
            var deviceEntry = entries.First["resource"];//device resource
            return deviceEntry["id"]?.ToString();
        }
        public static  List<PatientModel> GetAppointmentsForDevice(string deviceId)
        {
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
            _client.DefaultRequestHeaders.Add("Accept", "application/fhir+json");
            string appointmentUrl = $"{baseUrl}/Appointment?date=ge{FormatDateTime(DateTime.Now.Date)}" +
                $"&date=le{FormatDateTime(DateTime.Now.AddDays(1).Date)}&actor=Device/{deviceId}&_pretty=true";
            var appointmentRequest = _client.GetAsync(appointmentUrl);
            var appointmentResponse = appointmentRequest.Result.Content.ReadAsStringAsync().Result;
            var bundle = JObject.Parse(appointmentResponse);
            var entries = bundle["entry"];//all the entries. 
            List<PatientModel> patients = new List<PatientModel>();
            if (entries == null || !entries.HasValues)
            {
                return patients;
            }
            foreach(var entry in entries)
            {
                var resource = entry["resource"];
                if (resource == null)
                {
                    continue;
                }
                //Get patient actor.
                var patientActor = resource["participant"]?
                    .FirstOrDefault(p => p["actor"]?["reference"]?.ToString().StartsWith("Patient/") == true)?["actor"]?["reference"]?.ToString();
                if (!String.IsNullOrWhiteSpace(patientActor))
                {
                    if(!patients.Any(a=>a.fhirID == patientActor.Split('/').Last()))
                    {
                        var patientId = patientActor.Split('/')[1];
                        var patient = GetPatientById(patientId);
                        if (patient != null)
                        {
                            patients.Add(patient);
                        }
                    }
                }
            }
            return patients;
        }
        private static string FormatDateTime(DateTime dateTime)
        {
            //format the date time to FHIR format. 
            return dateTime.ToString("yyyy-MM-ddTHH:mm:ss+00:00");
        }
        public static List<PatientAppointmentModel> GetAppointmentsByPatient(string fhirID)
        {
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
            _client.DefaultRequestHeaders.Add("Accept", "application/fhir+json");
            string appointmentUrl = $"{baseUrl}/Appointment?patient={fhirID}&_pretty=true";
            List<PatientAppointmentModel> appointments = new List<PatientAppointmentModel>();

            while (!string.IsNullOrEmpty(appointmentUrl))
            {
                var appointmentRequest = _client.GetAsync(appointmentUrl);
                var appointmentResponse = appointmentRequest.Result.Content.ReadAsStringAsync().Result;
                var bundle = JObject.Parse(appointmentResponse);
                var entries = bundle["entry"];//all the entries. 
                if (entries == null || !entries.HasValues)
                {
                    return appointments;
                }
                foreach (var entry in entries)
                {
                    var resource = entry["resource"];
                    if (resource == null)
                    {
                        continue;
                    }
                    //Get patient actor.
                    PatientAppointmentModel appointment = new PatientAppointmentModel();
                    appointment.AppointmentName = resource["serviceType"]?[0]?["coding"]?[0]?["display"]?.ToString();
                    appointment.StartDate = resource["start"]?.ToString();
                    appointment.AppointmentCategory = resource["serviceCategory"]?[0]?["coding"]?[0]?["display"]?.ToString();
                    appointment.Status = ConvertStatus(resource["status"]?.ToString());
                    appointments.Add(appointment);

                }
                appointmentUrl = null;

                var links = bundle["link"];

                if (links != null)

                {

                    foreach (var link in links)

                    {

                        if (link["relation"]?.ToString() == "next")

                        {

                            appointmentUrl = link["url"]?.ToString();

                            //pageNumber++;

                            break;

                        }

                    }

                }
            }
                return appointments;
        }

        private static string ConvertStatus(string? v)
        {
            if (!String.IsNullOrEmpty(v))
            {
                switch (v)
                {
                    case "booked":
                        return "Scheduled";
                    case "arrived":
                        return "Arrived";
                    case "fulfilled":
                        return "Completed";
                    case "cancelled":
                        return "Cancelled";
                    default:
                        return v;
                }
            }
            return v;
        }
    }
}
