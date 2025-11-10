using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Trave.Models;

namespace Trave.Controllers
{
    public class DiaDiemsController : Controller
    {
        private DULICHEntities1 db = new DULICHEntities1();

        // GET: DiaDiems
        public ActionResult Index()
        {
            return View(db.DiaDiems.ToList());
        }

        // GET: DiaDiems/Details/5
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DiaDiem diaDiem = db.DiaDiems.Find(id);
            if (diaDiem == null)
            {
                return HttpNotFound();
            }
            return View(diaDiem);
        }

        // GET: DiaDiems/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: DiaDiems/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaDD,TenDD,ThanhPho,QuocGia,MoTa,HinhAnh")] DiaDiem diaDiem)
        {
            if (ModelState.IsValid)
            {
                db.DiaDiems.Add(diaDiem);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(diaDiem);
        }

        // GET: DiaDiems/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DiaDiem diaDiem = db.DiaDiems.Find(id);
            if (diaDiem == null)
            {
                return HttpNotFound();
            }
            return View(diaDiem);
        }

        // POST: DiaDiems/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaDD,TenDD,ThanhPho,QuocGia,MoTa,HinhAnh")] DiaDiem diaDiem)
        {
            if (ModelState.IsValid)
            {
                db.Entry(diaDiem).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(diaDiem);
        }

        // GET: DiaDiems/Delete/5
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DiaDiem diaDiem = db.DiaDiems.Find(id);
            if (diaDiem == null)
            {
                return HttpNotFound();
            }
            return View(diaDiem);
        }

        // POST: DiaDiems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            DiaDiem diaDiem = db.DiaDiems.Find(id);
            db.DiaDiems.Remove(diaDiem);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
