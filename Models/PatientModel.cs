using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DailyTxReview.Models
{
    public class PatientModel
    {
        //prop + TAB + string + TAB+TAB + variablename + ENTER
        public string FullName { get; set; }
        public string DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string Mrn { get; set; }
        public string PrimaryPhone { get; set; }
        public string fhirID { get; set; }
        public string PatientId { get; set; }
    }
}
