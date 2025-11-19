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
using System.Globalization;
namespace Trave.Controllers

{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private DULICHEntities db = new DULICHEntities();

        // GET: Admin
        [OverrideAuthorization]
        [Authorize(Roles = "Admin, Employee")]
        public ActionResult Dashboard()
        {
            int totalTours = db.Tours.Count();
            int totalBookings = db.Bookings.Count();
            int totalUsers = db.KhachHangs.Count();

            decimal totalRevenueDecimal = db.Bookings
                .Where(b => b.TrangThai == "Đã thanh toán" )
                .Sum(b => (decimal?)b.TongGia) ?? 0m;

            ViewBag.TotalTours = totalTours;
            ViewBag.TotalBookings = totalBookings;
            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalRevenue = (double)totalRevenueDecimal;

            // --- 2. LẤY DỮ LIỆU BIỂU ĐỒ DOANH THU (Line Chart) ---
            // Chỉ lấy các đơn đã thanh toán trong 12 tháng qua
            DateTime oneYearAgo = DateTime.Now.AddYears(-1);
            var revenueData = db.Bookings
                .Where(b => (b.TrangThai == "Đã thanh toán" )
                            && b.NgayDat >= oneYearAgo)
               .GroupBy(b => new { Year = b.NgayDat.Value.Year, Month = b.NgayDat.Value.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Total = g.Sum(b => (decimal?)b.TongGia) ?? 0
                })
                .OrderBy(r => r.Year).ThenBy(r => r.Month)
                .ToList();

            // Gán vào ViewBag
            ViewBag.ChartLabels_Revenue = revenueData.Select(r => $"T{r.Month}/{r.Year}").ToList();
            ViewBag.ChartData_Revenue = revenueData.Select(r => r.Total).ToList();


            // --- 3. LẤY DỮ LIỆU BIỂU ĐỒ TỈ LỆ TOUR (Pie Chart) ---
            var tourPopularity = db.Bookings
                .Include(b => b.Tour) // Join với bảng Tour để lấy Tên Tour
                .GroupBy(b => b.Tour.TenTour)
                .Select(g => new
                {
                    TourName = g.Key,
                    Count = g.Count() // Đếm số lượng booking cho mỗi tour
                })
                .OrderByDescending(t => t.Count)
                .ToList();

            ViewBag.ChartLabels_Tour = tourPopularity.Select(t => t.TourName).ToList();
            ViewBag.ChartData_Tour = tourPopularity.Select(t => t.Count).ToList();
                

            // --- 4. LẤY MODEL CHO BẢNG (Mã cũ) ---
            var model = db.Bookings
                .Include(b => b.KhachHang)
                .Include(b => b.Tour)
                .OrderByDescending(b => b.NgayDat)
                .Take(10)
                .ToList();

            // 5. Trả về View
            return View(model);
        }

        // ==========DANH MỤC============
        // GET: DanhMucs
        [OverrideAuthorization]
        [Authorize(Roles = "Admin, Employee")]
        public ActionResult DMList()
        {
            return View(db.DanhMucs.ToList());
        }

        // GET: DanhMucs/Details/5
        [OverrideAuthorization]
        [Authorize(Roles = "Admin, Employee")]
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
        [OverrideAuthorization]
        [Authorize(Roles = "Admin, Employee")]
        public ActionResult Tourlist()
        {
            var tours = db.Tours.Include(t => t.DanhMuc).Include(t => t.DiaDiem);
            return View(tours.ToList());
        }

        // GET: Tours/Details/5
        [OverrideAuthorization]
        [Authorize(Roles = "Admin, Employee")]
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
            ViewBag.MaGuide = new SelectList(db.guides, "maguide", "tenguide");
            return View();
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TourCreate([Bind(Include = "MaTour,TenTour,Gia,ThoiGian,MaDD,MaDM,songuoi,mota,NgayKhoiHanh,MaGuide,PhuongTien")]
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
            ViewBag.MaGuide = new SelectList(db.guides, "maguide", "tenguide");

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
            ViewBag.MaGuide = new SelectList(db.guides, "maguide", "tenguide");

            return View(tour);
        }

        
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

        // ========== TÌNH TRẠNG TOUR ===========
        public ActionResult TTTourList()
        {
            var tinhTrangTours = db.TinhTrangTours.Include(t => t.Booking);
            return View(tinhTrangTours.ToList());
        }
        public ActionResult TTTourEdit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TinhTrangTour tinhTrangTour = db.TinhTrangTours.Find(id);
            if (tinhTrangTour == null)
            {
                return HttpNotFound();
            }
            ViewBag.MaBooking = new SelectList(db.Bookings, "MaBooking", "MaTour", tinhTrangTour.MaBooking);
            return View(tinhTrangTour);
        }

        // POST: TinhTrangTours/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TTTourEdit(int Matinhtrang, int TrangThai)
        {
            // 1. Tìm đối tượng gốc trong Database
            var tourInDb = db.TinhTrangTours.Find(Matinhtrang);

            if (tourInDb == null)
            {
                return HttpNotFound();
            }

            // 2. CHỈ CẬP NHẬT CỘT TRẠNG THÁI
            // Các cột khác (Tên, Mã Booking) giữ nguyên giá trị cũ trong DB -> An toàn tuyệt đối
            tourInDb.TrangThai = TrangThai;

            // 3. Lưu thay đổi
            // Lúc này Trigger SQL "trg_CapNhatTenHienThi" sẽ chạy và tự cập nhật Tên Tình Trạng chuẩn xác
            db.SaveChanges();

            return RedirectToAction("TTTourList");
        }
        // ========== BLOG ===========
        [OverrideAuthorization]
        [Authorize(Roles = "Admin, Employee")]
        public ActionResult BlogList()
        {
            // Tải Danh sách bài viết, bao gồm Danh mục đi kèm để hiển thị (Eager Loading)
            var model = db.BaiViets.Include(b => b.DanhMucBlog).ToList();
            return View(model);
        }

        // GET: Blog/Details/5
        // POST: Blog/Details/5
        [OverrideAuthorization]
        [Authorize(Roles = "Admin, Employee")]
        public ActionResult BlogDetails(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Tải Bài viết và Danh mục liên quan. 
            // SỬ DỤNG ASNOTRACKING để ngăn EF theo dõi đối tượng này ngay từ đầu
            BaiViet baiViet = db.BaiViets
                                .Include(b => b.DanhMucBlog)
                                .AsNoTracking() // Ngăn EF theo dõi đối tượng
                                .SingleOrDefault(b => b.MaBaiViet == id);

            if (baiViet == null)
            {
                return HttpNotFound();
            }

            // -----------------------------------------------------------
            // BƯỚC TỐI ƯU HÓA: Cập nhật chỉ trường LuotXem
            // -----------------------------------------------------------

            try
            {
                // 1. Tăng lượt xem (dựa trên giá trị hiện tại)
                int newLuotXem = (baiViet.LuotXem ?? 0) + 1;

                // 2. Tạo đối tượng proxy chỉ chứa ID
                var counter = new BaiViet { MaBaiViet = baiViet.MaBaiViet };

                // 3. Đính kèm đối tượng proxy (sẽ không gây lỗi vì baiViet đã là NoTracking)
                db.BaiViets.Attach(counter);

                // 4. Gán giá trị mới và đánh dấu CHỈ trường LuotXem bị sửa đổi
                counter.LuotXem = newLuotXem;
                db.Entry(counter).Property(x => x.LuotXem).IsModified = true;

                db.SaveChanges(); // Lưu chỉ một trường này

                // 5. Cập nhật lại đối tượng đang được hiển thị trong View 
                baiViet.LuotXem = newLuotXem;
            }
            catch (Exception ex)
            {
                // Ghi log lỗi (chỉ trong Console hoặc file log, không hiển thị cho người dùng)
                System.Diagnostics.Debug.WriteLine("Lỗi khi cập nhật lượt xem: " + ex.Message);
                // Bỏ qua lỗi và vẫn trả về View
            }

            return View(baiViet);
        }

        // GET: Blog/Create
        [OverrideAuthorization]
        [Authorize(Roles = "Admin, Employee")]
        public ActionResult BlogCreate()
        {
            // Chỉ cần tải Danh mục Blog (đã loại bỏ MaKH)
            ViewBag.MaDanhMucBlog = new SelectList(db.DanhMucBlogs, "MaDanhMucBlog", "TenDanhMuc");
            return View();
        }

        // POST: Blog/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [OverrideAuthorization]
        [Authorize(Roles = "Admin, Employee")]

        // Chỉ bind các trường cần nhập thủ công. NgayDang, LuotXem sẽ được gán trong code.
        public ActionResult BlogCreate([Bind(Include = "TieuDe,TomTat,NoiDung,HinhAnh,Tacgia,MaDanhMucBlog")] BaiViet baiViet,HttpPostedFileBase HinhAnhFile) // Tham số nhận file upload
        {
            // Kiểm tra tính hợp lệ của Model (trừ trường HinhAnh, vì ta sẽ gán nó sau)
            // Nếu bạn muốn yêu cầu phải có ảnh: if (HinhAnhFile == null) ModelState.AddModelError("HinhAnh", "Vui lòng chọn ảnh đại diện.");
            if (ModelState.IsValid)
            {
                // 1. Xử lý Upload Ảnh
                if (HinhAnhFile != null && HinhAnhFile.ContentLength > 0)
                {
                    try
                    {
                        // Lấy đường dẫn thư mục đích: ~/img/Blog/
                        string uploadPath = Server.MapPath("~/img/Blog/");

                        // Đảm bảo thư mục tồn tại
                        if (!Directory.Exists(uploadPath))
                        {
                            Directory.CreateDirectory(uploadPath);
                        }

                        // Tạo tên file mới duy nhất (sử dụng Guid)
                        string extension = Path.GetExtension(HinhAnhFile.FileName);
                        // Giới hạn độ dài extension để tránh lỗi
                        if (extension != null && extension.Length > 10) extension = extension.Substring(0, 10);

                        string newFileName = Guid.NewGuid().ToString() + extension;
                        string path = Path.Combine(uploadPath, newFileName);

                        // Lưu file vật lý vào thư mục trên server
                        HinhAnhFile.SaveAs(path);

                        // Gán tên file mới vào Model để lưu vào Database
                        baiViet.HinhAnh = newFileName;
                    }
                    catch (Exception ex)
                    {
                        // Nếu có lỗi upload, thêm lỗi vào ModelState
                        ModelState.AddModelError("", "Lỗi khi upload file: " + ex.Message);
                        goto RebindView;
                    }
                }
                else
                {
                    // Tùy chọn: Nếu không có ảnh, gán ảnh mặc định
                    baiViet.HinhAnh = "default_blog.jpg";
                }

                // 2. Gán các giá trị mặc định/hệ thống
                baiViet.NgayDang = DateTime.Now;
                baiViet.LuotXem = 0;

                // 3. Lưu vào Database
                db.BaiViets.Add(baiViet);
                db.SaveChanges();
                return RedirectToAction("BlogList");
            }

        RebindView:
            // Tải lại ViewBag nếu ModelState không hợp lệ hoặc có lỗi upload
            ViewBag.MaDanhMucBlog = new SelectList(db.DanhMucBlogs, "MaDanhMucBlog", "TenDanhMuc", baiViet.MaDanhMucBlog);
            return View(baiViet);
        }

        // GET: Blog/Edit/5
        // GET: Blog/Edit/5
        [OverrideAuthorization]
        [Authorize(Roles = "Admin, Employee")]
        public ActionResult BlogEdit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Tải Bài viết
            BaiViet baiViet = db.BaiViets.Find(id);

            if (baiViet == null)
            {
                return HttpNotFound();
            }

            // Chỉ cần MaDanhMucBlog
            ViewBag.MaDanhMucBlog = new SelectList(db.DanhMucBlogs, "MaDanhMucBlog", "TenDanhMuc", baiViet.MaDanhMucBlog);

            // KHÔNG cần MaKH (KhachHang) nữa

            return View(baiViet);
        }

        // POST: Blog/Edit/5
        // POST: Blog/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)] // Cho phép nội dung HTML
        [OverrideAuthorization]
        [Authorize(Roles = "Admin, Employee")]
        public ActionResult BlogEdit(
            [Bind(Include = "MaBaiViet,TieuDe,TomTat,NoiDung,HinhAnh,Tacgia,NgayDang,LuotXem,MaDanhMucBlog")] BaiViet baiViet,
            HttpPostedFileBase HinhAnhFile)
        {
            // Lấy thông tin bài viết cũ (trước khi chỉnh sửa)
            var baiVietDb = db.BaiViets.AsNoTracking().FirstOrDefault(b => b.MaBaiViet == baiViet.MaBaiViet);
            if (baiVietDb == null) return HttpNotFound();

            // 1. Gán lại các giá trị hệ thống không thay đổi từ form
            baiViet.NgayDang = baiVietDb.NgayDang;
            baiViet.LuotXem = baiVietDb.LuotXem;
            string oldFileName = baiVietDb.HinhAnh; // Lưu tên file cũ để xử lý sau

            if (ModelState.IsValid)
            {
                // 2. Xử lý Upload/Cập nhật Ảnh
                if (HinhAnhFile != null && HinhAnhFile.ContentLength > 0)
                {
                    try
                    {
                        string uploadPath = Server.MapPath("~/img/Blog/");
                        string extension = Path.GetExtension(HinhAnhFile.FileName);
                        string newFileName = Guid.NewGuid().ToString() + extension;
                        string path = Path.Combine(uploadPath, newFileName);

                        // Lưu file mới
                        HinhAnhFile.SaveAs(path);

                        // Xóa file cũ (tùy chọn, nên làm để tiết kiệm dung lượng)
                        if (!string.IsNullOrEmpty(oldFileName) && oldFileName != "default_blog.jpg")
                        {
                            string oldPath = Path.Combine(uploadPath, oldFileName);
                            if (System.IO.File.Exists(oldPath))
                            {
                                System.IO.File.Delete(oldPath);
                            }
                        }

                        // Gán tên file mới
                        baiViet.HinhAnh = newFileName;
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Lỗi khi upload file: " + ex.Message);
                        goto RebindView;
                    }
                }
                else
                {
                    // Nếu không upload file mới, giữ nguyên tên file cũ
                    baiViet.HinhAnh = oldFileName;
                }

                // 3. Lưu vào Database
                db.Entry(baiViet).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("BlogList");
            }

        RebindView:
            ViewBag.MaDanhMucBlog = new SelectList(db.DanhMucBlogs, "MaDanhMucBlog", "TenDanhMuc", baiViet.MaDanhMucBlog);
            return View(baiViet);
        }

        // GET: Blog/Delete/5
        [OverrideAuthorization]
        [Authorize(Roles = "Admin, Employee")]
        public ActionResult BlogDelete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            // Tải Bài viết, bao gồm Danh mục để hiển thị thông tin xác nhận
            BaiViet baiViet = db.BaiViets.Include(b => b.DanhMucBlog).SingleOrDefault(b => b.MaBaiViet == id);

            if (baiViet == null)
            {
                return HttpNotFound();
            }
            return View(baiViet);
        }

        // POST: Blog/Delete/5
        // POST: Blog/Delete/5
        [HttpPost, ActionName("BlogDelete")]
        [ValidateAntiForgeryToken]
        [OverrideAuthorization]
        [Authorize(Roles = "Admin, Employee")]
        public ActionResult BlogDeleteConfirmed(int id)
        {
            BaiViet baiViet = db.BaiViets.Find(id);

            if (baiViet != null)
            {
                // Xử lý Xóa file ảnh trên Server
                if (!string.IsNullOrEmpty(baiViet.HinhAnh) && baiViet.HinhAnh != "default_blog.jpg")
                {
                    string uploadPath = Server.MapPath("~/img/Blog/");
                    string path = Path.Combine(uploadPath, baiViet.HinhAnh);

                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }
                }

                // Xóa khỏi Database
                db.BaiViets.Remove(baiViet);
                db.SaveChanges();
            }

            return RedirectToAction("BlogList");
        }
        [Authorize(Roles = "Admin, Employee")]
        public ActionResult ChatCenter()
        {
            return View();
        }
        [HttpGet] // Chỉ cho phép method GET
        public JsonResult GetChatUsers()
        {
            try
            {
                // 1. Lấy TẤT CẢ tin nhắn liên quan đến Admin (Gửi đi hoặc Nhận về)
                var allMessages = db.ChatMessages
                    .Where(m => m.ReceiverId == "Admin" || m.SenderId == "Admin")
                    .OrderByDescending(m => m.Timestamp)
                    .ToList();

                // 2. Lọc ra danh sách Khách hàng duy nhất
                var users = allMessages
                    .Select(m => new
                    {
                        // Nếu người gửi là Admin -> Thì đối phương là Khách (Receiver)
                        // Ngược lại -> Đối phương là Khách (Sender)
                        CustomerEmail = (m.SenderId == "Admin" || m.SenderId == User.Identity.Name) ? m.ReceiverId : m.SenderId
                    })
                    .Where(x => x.CustomerEmail != "Admin" && x.CustomerEmail != User.Identity.Name) // Loại bỏ chính mình
                    .Distinct() // Lấy duy nhất
                    .ToList();

                // 3. Tổng hợp dữ liệu trả về (Kèm tin nhắn cuối cùng)
                var result = new List<object>();

                foreach (var user in users)
                {
                    // Tìm tin nhắn mới nhất giữa 2 người
                    var lastMsg = allMessages.FirstOrDefault(m =>
                        (m.SenderId == user.CustomerEmail && m.ReceiverId == "Admin") ||
                        (m.SenderId == "Admin" && m.ReceiverId == user.CustomerEmail));

                    if (lastMsg != null)
                    {
                        result.Add(new
                        {
                            Email = user.CustomerEmail,
                            LastMsg = (lastMsg.Message.Length > 30) ? lastMsg.Message.Substring(0, 30) + "..." : lastMsg.Message,
                            Time = lastMsg.Timestamp.HasValue ? lastMsg.Timestamp.Value.ToString("HH:mm dd/MM") : "",
                            // Thêm Timestamp thô để sắp xếp nếu cần
                            RawTime = lastMsg.Timestamp
                        });
                    }
                }

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu cần thiết
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // ============================================================
        // API CHAT: Lấy lịch sử chi tiết với 1 khách
        // ============================================================
        [HttpGet]
        public JsonResult GetChatHistory(string customerEmail)
        {
            try
            {
                var history = db.ChatMessages
                    .Where(m => (m.SenderId == customerEmail && m.ReceiverId == "Admin") ||
                                (m.SenderId == "Admin" && m.ReceiverId == customerEmail))
                    .OrderBy(m => m.Timestamp) // Sắp xếp tin nhắn cũ lên trước
                    .ToList()
                    .Select(m => new
                    {
                        type = (m.SenderId == customerEmail) ? "received" : "sent", // received = Khách nói, sent = Admin nói
                        msg = m.Message,
                        time = m.Timestamp.HasValue ? m.Timestamp.Value.ToString("HH:mm") : ""
                    });

                return Json(history, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
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