using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Trave.Models;

namespace Trave.Controllers
{
    public class AdminController : Controller
    {
        private DULICHEntities db = new DULICHEntities();

        // GET: Admin
        public ActionResult Dashboard()
        {
            return View();
        }

        // ==========DANH MỤC============
        // GET: DanhMucs
        public ActionResult DMList()
        {
            return View(db.DanhMucs.ToList());
        }

        // GET: DanhMucs/Details/5
        public ActionResult DMDetails(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DanhMuc danhMuc = db.DanhMucs.Find(id);
            if (danhMuc == null)
            {
                return HttpNotFound();
            }
            return View(danhMuc);
        }

        // GET: DanhMucs/Create
        public ActionResult DMCreate()
        {
            return View();
        }

        // POST: DanhMucs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DMCreate([Bind(Include = "MaDM,TenDM,soluongsanpham")] DanhMuc danhMuc)
        {
            if (ModelState.IsValid)
            {
                db.DanhMucs.Add(danhMuc);
                db.SaveChanges();
                return RedirectToAction("DMList");
            }

            return View(danhMuc);
        }

        // GET: DanhMucs/Edit/5
        public ActionResult DMEdit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DanhMuc danhMuc = db.DanhMucs.Find(id);
            if (danhMuc == null)
            {
                return HttpNotFound();
            }
            return View(danhMuc);
        }

        // POST: DanhMucs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DMEdit([Bind(Include = "MaDM,TenDM,soluongsanpham")] DanhMuc danhMuc)
        {
            if (ModelState.IsValid)
            {
                db.Entry(danhMuc).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("DMList");
            }
            return View(danhMuc);
        }

        // GET: DanhMucs/Delete/5
        public ActionResult DMDelete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DanhMuc danhMuc = db.DanhMucs.Find(id);
            if (danhMuc == null)
            {
                return HttpNotFound();
            }
            return View(danhMuc);
        }

        // POST: DanhMucs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DMDeleteConfirmed(string id)
        {
            DanhMuc danhMuc = db.DanhMucs.Find(id);
            db.DanhMucs.Remove(danhMuc);
            db.SaveChanges();
            return RedirectToAction("DMList");
        }

        // ========== TOUR ============

        public ActionResult Tourlist()
        {
            var tours = db.Tours.Include(t => t.DanhMuc).Include(t => t.DiaDiem);
            return View(tours.ToList());
        }

        // GET: Tours/Details/5
        public ActionResult TourDetails(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Tour tour = db.Tours.Find(id);
            if (tour == null)
            {
                return HttpNotFound();
            }
            return View(tour);
        }

        // GET: Tours/Create
        public ActionResult TourCreate()
        {
            ViewBag.MaDM = new SelectList(db.DanhMucs, "MaDM", "TenDM");
            ViewBag.MaDD = new SelectList(db.DiaDiems, "MaDD", "TenDD");
            return View();
        }

        // POST: Tours/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaTour,TenTour,Gia,trangthai,ThoiGian,NgayKhoiHanh,MaDD,MaDM,PhuongTien,anhmota,songuoi,mota")] Tour tour)
        {
            if (ModelState.IsValid)
            {
                db.Tours.Add(tour);
                db.SaveChanges();
                return RedirectToAction("Tourlist");
            }

            ViewBag.MaDM = new SelectList(db.DanhMucs, "MaDM", "TenDM", tour.MaDM);
            ViewBag.MaDD = new SelectList(db.DiaDiems, "MaDD", "TenDD", tour.MaDD);
            return View(tour);
        }

        // GET: Tours/Edit/5
        public ActionResult TourEdit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Tour tour = db.Tours.Find(id);
            if (tour == null)
            {
                return HttpNotFound();
            }
            ViewBag.MaDM = new SelectList(db.DanhMucs, "MaDM", "TenDM", tour.MaDM);
            ViewBag.MaDD = new SelectList(db.DiaDiems, "MaDD", "TenDD", tour.MaDD);
            return View(tour);
        }

        // POST: Tours/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaTour,TenTour,Gia,trangthai,ThoiGian,NgayKhoiHanh,MaDD,MaDM,PhuongTien,anhmota,songuoi,mota")] Tour tour)
        {
            if (ModelState.IsValid)
            {
                db.Entry(tour).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Tourlist");
            }
            ViewBag.MaDM = new SelectList(db.DanhMucs, "MaDM", "TenDM", tour.MaDM);
            ViewBag.MaDD = new SelectList(db.DiaDiems, "MaDD", "TenDD", tour.MaDD);
            return View(tour);
        }

        // GET: Tours/Delete/5
        public ActionResult TourDelete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Tour tour = db.Tours.Find(id);
            if (tour == null)
            {
                return HttpNotFound();
            }
            return View(tour);
        }

        // POST: Tours/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            Tour tour = db.Tours.Find(id);
            db.Tours.Remove(tour);
            db.SaveChanges();
            return RedirectToAction("Tourlist");
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