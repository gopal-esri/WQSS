using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WQSS.Models
{
    public class LabelForm
    { 
        public string SampleID { get; set; }
        public string CollectedDate { get; set; }
        public string CollectedTime { get; set; }
        public string Location { get; set; }
        public string Sample_Pt { get; set; }
        public string Matrix { get; set; }
        public string Reason { get; set; }

    }
}