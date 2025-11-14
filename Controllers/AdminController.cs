using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using Trave.Models;
using BCrypt.Net;
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
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Create([Bind(Include = "MaTour,TenTour,Gia,trangthai,ThoiGian,NgayKhoiHanh,MaDD,MaDM,PhuongTien,anhmota,songuoi,mota")] Tour tour)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        db.Tours.Add(tour);
        //        db.SaveChanges();
        //        return RedirectToAction("Tourlist");
        //    }

        //    ViewBag.MaDM = new SelectList(db.DanhMucs, "MaDM", "TenDM", tour.MaDM);
        //    ViewBag.MaDD = new SelectList(db.DiaDiems, "MaDD", "TenDD", tour.MaDD);
        //    return View(tour);
        //}
        // POST: Admin/Create
        // POST: Admin/TourCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TourCreate(
     [Bind(Include = "MaTour,TenTour,Gia,ThoiGian,MaDD,MaDM,songuoi,mota,NgayKhoiHanh")]
    Tour tour,
     HttpPostedFileBase ImageFile)
        {
            // LƯU Ý: 'trangthai' và 'anhmota' vẫn được xử lý ở ngoài [Bind]

            // 1. Xử lý file ảnh (không ảnh hưởng đến ModelState)
            if (ImageFile != null && ImageFile.ContentLength > 0)
            {
                try
                {
                    // Yêu cầu: using System.IO;
                    string fileName = Path.GetFileName(ImageFile.FileName);
                    string path = Path.Combine(Server.MapPath("~/img/destination/"), fileName);

                    ImageFile.SaveAs(path);
                    tour.anhmota = fileName;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi tải ảnh lên: " + ex.Message);
                }
            }

            // 2. Set các giá trị mặc định 
            tour.trangthai = "Available";

            // 3. Kiểm tra ModelState và thực hiện Lưu (chỉ một lần)
            if (ModelState.IsValid)
            {
                try
                {
                    db.Tours.Add(tour);
                    db.SaveChanges(); // <-- Đã được bảo vệ
                    return RedirectToAction("Tourlist");
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException ex)
                {
                    // Bắt lỗi chi tiết và đưa vào ModelState
                    var errorMessages = ex.EntityValidationErrors
                        .SelectMany(v => v.ValidationErrors)
                        .Select(v => v.PropertyName + ": " + v.ErrorMessage);

                    var fullErrorMessage = string.Join("; ", errorMessages);
                    ModelState.AddModelError("", "Lỗi Xác thực EF: " + fullErrorMessage);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi hệ thống không xác định: " + ex.Message);
                }
            }

            // 4. Nếu lỗi (ModelState không hợp lệ HOẶC lỗi Validation EF), load lại View
            ViewBag.MaDD = new SelectList(db.DiaDiems, "MaDD", "TenDD", tour.MaDD);
            ViewBag.MaDM = new SelectList(db.DanhMucs, "MaDM", "TenDM", tour.MaDM);
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
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Edit([Bind(Include = "MaTour,TenTour,Gia,trangthai,ThoiGian,NgayKhoiHanh,MaDD,MaDM,PhuongTien,anhmota,songuoi,mota")] Tour tour)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        db.Entry(tour).State = EntityState.Modified;
        //        db.SaveChanges();
        //        return RedirectToAction("Tourlist");
        //    }
        //    ViewBag.MaDM = new SelectList(db.DanhMucs, "MaDM", "TenDM", tour.MaDM);
        //    ViewBag.MaDD = new SelectList(db.DiaDiems, "MaDD", "TenDD", tour.MaDD);
        //    return View(tour);
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Phải thêm tham số 'HttpPostedFileBase ImageFile'
        public ActionResult TourEdit(Tour tour, HttpPostedFileBase ImageFile)
        {
            // Giả sử 'db' là DbContext của bạn

            // 1. Xử lý file ảnh mới
            if (ImageFile != null && ImageFile.ContentLength > 0)
            {
                try
                {
                    // Lấy tên file và lưu vào thư mục
                    string fileName = Path.GetFileName(ImageFile.FileName);
                    string path = Path.Combine(Server.MapPath("~/Content/images/tours"), fileName);
                    ImageFile.SaveAs(path);

                    // Cập nhật tên file vào model
                    tour.anhmota = fileName;
                }
                catch (Exception ex)
                {
                    // Xử lý lỗi tải ảnh
                    ModelState.AddModelError("", "Lỗi tải ảnh lên: " + ex.Message);
                }
            }
            else
            {
                // 2. Nếu không có file mới, ta phải lấy lại tên ảnh cũ từ CSDL
                //    (để tránh việc HiddenFor(anhmota) bị thay đổi giá trị)
                // LƯU Ý: Nếu bạn dùng HiddenFor(model => model.anhmota) trong View, 
                //        bạn có thể bỏ qua bước này. Nếu không, bạn phải query lại DB.
            }


            // 3. Kiểm tra và Lưu vào CSDL
            if (ModelState.IsValid)
            {
                // EntityState.Modified sẽ cập nhật tất cả các trường, bao gồm cả anhmota mới (nếu có)
                db.Entry(tour).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Tourlist"); // Hoặc "ManageTours"
            }

            // Nếu ModelState không hợp lệ, load lại DropDownList
            ViewBag.MaDD = new SelectList(db.DiaDiems, "MaDD", "TenDD", tour.MaDD);
            ViewBag.MaDM = new SelectList(db.DanhMucs, "MaDM", "TenDM", tour.MaDM);
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
        [HttpPost, ActionName("TourDelete")]
        [ValidateAntiForgeryToken]
        public ActionResult TourDeleteConfirmed(string id)
        {
            Tour tour = db.Tours.Find(id);
            db.Tours.Remove(tour);
            db.SaveChanges();
            return RedirectToAction("Tourlist");
        }

        // ========== Account ===========
        public ActionResult Account()
        {
            return View(db.Users1.ToList());
        }

        // GET: Users/Details/5
        public ActionResult AccountDetails(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Users users = db.Users1.Find(id);
            if (users == null)
            {
                return HttpNotFound();
            }
            return View(users);
        }

        // GET: Users/Create
        public ActionResult AccountCreate()
        {
            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AccountCreate([Bind(Include = "Name,Email,Role,PasswordHash")] Users user)
        {
            if (ModelState.IsValid)
            {
                // 1. Mã hóa mật khẩu (KHÔNG lưu mật khẩu dạng TEXT!)
                // Giả sử bạn dùng BCrypt
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);

                // 2. Gán ngày tạo
                user.CreatedAt = DateTime.Now;

                db.Users1.Add(user);
                db.SaveChanges();
                return RedirectToAction("account");
            }

            // Nếu lỗi, phải load lại danh sách Roles để tránh lỗi
            List<string> RolesList = new List<string> { "Admin", "Customer", "Employee" };
            ViewBag.Roles = new SelectList(RolesList, user.Role);
            return View(user);
        }

        // GET: Users/Edit/5
        public ActionResult AccountEdit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Users users = db.Users1.Find(id);
            if (users == null)
            {
                return HttpNotFound();
            }
            return View(users);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Thêm tham số 'string NewPassword' vào đây
        public ActionResult AccountEdit(Users user, string NewPassword)
        {
            // Giả sử 'db' là DbContext của bạn

            if (ModelState.IsValid)
            {
                // 1. Kiểm tra Mật khẩu Mới
                if (!string.IsNullOrEmpty(NewPassword))
                {
                    // Hash mật khẩu mới và gán vào model
                    // Yêu cầu: using BCrypt.Net;
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
                }
                else
                {
                    // 2. Nếu không có mật khẩu mới, chúng ta phải giữ lại Hash cũ.
                    //    Vì PasswordHash được gửi đi qua HiddenFor, nên user.PasswordHash
                    //    sẽ vẫn chứa hash cũ. Chúng ta chỉ cần đảm bảo nó không bị ghi đè.
                }

                db.Entry(user).State = EntityState.Modified;

                // Cần đảm bảo rằng CreatedAt (HiddenFor) vẫn được theo dõi đúng cách
                // Tùy thuộc vào cấu hình Entity Framework của bạn, bạn có thể cần thêm:
                // db.Entry(user).Property(u => u.CreatedAt).IsModified = false;

                db.SaveChanges();
                return RedirectToAction("account");
            }

            // Tải lại các ViewBag nếu cần (ví dụ: View(user))
            List<string>
            RolesList = new List<string>
                { "Admin", "Customer", "Employee" };
            ViewBag.Roles = new SelectList(RolesList, user.Role);
            return View(user);
        }

        // GET: Users/Delete/5
        public ActionResult AccountDelete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Users users = db.Users1.Find(id);
            if (users == null)
            {
                return HttpNotFound();
            }
            return View(users);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("AccountDelete")]
        [ValidateAntiForgeryToken]
        public ActionResult AccountDeleteConfirmed(int id)
        {
            Users users = db.Users1.Find(id);
            db.Users1.Remove(users);
            db.SaveChanges();
            return RedirectToAction("Account");
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