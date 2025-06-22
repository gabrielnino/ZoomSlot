using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Models
{
    public class Education
    {
        public string Institution { get; set; }
        public string Location { get; set; }
        public string Degree { get; set; }

        [JsonPropertyName("Graduation Date")]
        public string GraduationDate { get; set; }
    }
}
