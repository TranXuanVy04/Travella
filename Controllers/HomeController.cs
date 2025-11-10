using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Trave.Models;

namespace Trave.Controllers
{
    public class HomeController : Controller
    {
        private DULICHEntities db = new DULICHEntities();
        
        public ActionResult Index()
        {
            var model = new HomeViewModel
            {
                DiaDiems = db.DiaDiems.ToList(),
                Tours = db.Tours.ToList()
            };
            return View(model);
        }

        public ActionResult Tour()
        {

            var a = db.Tours.ToList();

            return View(a);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        
    }
}