using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace DailyTxReview.Models
{
    public class PatientAppointmentModel
    {
        public string AppointmentName { get; set; }
        public string StartDate { get; set; }
        public string Status { get; set; }
        public string AppointmentCategory { get; set; }
    }
}
