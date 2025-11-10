using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Trave.Models
{
    public class HomeViewModel
    {
        public List<DiaDiem> DiaDiems { get; set; }
        public List<Tour> Tours { get; set; }
    }
}