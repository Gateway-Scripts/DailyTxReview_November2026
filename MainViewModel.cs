using DailyTxReview.Models;
using DailyTxReview.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace DailyTxReview
{
    public class MainViewModel
    {
        public List<PatientModel> Patients { get; set; }
        private PatientModel _selectedPatient;

        public PatientModel SelectedPatient
        {
            get { return _selectedPatient; }
            set 
            { 
                _selectedPatient = value;
                LoadPatientAppointments();
            }
        }


        public ObservableCollection<PatientAppointmentModel> SelectedPatientAppointments { get; set; }
        public MainViewModel()
        {
            //initialize the lists. 
            Patients = new List<PatientModel>();
            SelectedPatientAppointments = new ObservableCollection<PatientAppointmentModel>();
            //TestFhirAccess();
            LoadAppointments();
        }
        public void LoadPatientAppointments()
        {
            try
            {
                SelectedPatientAppointments.Clear();
                if (SelectedPatient != null)
                {
                    var appointments = AriaFhir.GetAppointmentsByPatient(SelectedPatient.fhirID);
                    foreach (var appointment in appointments)
                    {
                        SelectedPatientAppointments.Add(appointment);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString(), "Failed to load appointments");
            }
        }
        private void LoadAppointments()
        {
            AriaFhir.InitializeFhirClient();
            var device = AriaFhir.GetDeviceIdFromName("GS_TrueBeam");
            var patients = AriaFhir.GetAppointmentsForDevice(device);
            foreach(var patient in patients)
            {
                Patients.Add(patient);
            }
        }

        private void TestFhirAccess()
        {
            AriaFhir.InitializeFhirClient();
            SelectedPatient = AriaFhir.GetPatientById("RapidPlan-01");
        }
    }
}
