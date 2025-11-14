using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net;
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
            var model = db.Tours.Include(t => t.DiaDiem).Include(t => t.DanhMuc).ToList();

            // 1. Lấy danh sách Địa điểm (duy nhất)
            var allDestinations = model
                .Select(t => t.DiaDiem)
                .GroupBy(d => d.MaDD)
                .Select(g => g.First())
                .ToList();

            // 2. Lấy danh sách Quốc gia (duy nhất)
            var allCountries = allDestinations
                .Select(d => d.QuocGia)
                .Distinct()
                .ToList();

            // 3. Gửi sang View
            ViewBag.AllDestinations = allDestinations; // Dùng cho Card
            ViewBag.AllCountries = allCountries;     // Dùng cho Thanh lọc

            return View(model); // Model chính vẫn là Tour (cho các section khác)
        }

        // Trong file HomeController.cs
        public ActionResult Tour(string locationId, string categoryId, string countryFilter) // Thêm countryFilter
        {
            // 1. Chuẩn bị dữ liệu cho Dropdown tìm kiếm
            ViewBag.DiaDiemList = db.DiaDiems.ToList();
            ViewBag.DanhMucList = db.DanhMucs.ToList();

            // 2. Bắt đầu truy vấn Tour
            var tours = db.Tours
                .Include(t => t.DiaDiem)
                .Include(t => t.DanhMuc)
                .AsQueryable();

            // 3. Lọc theo Địa điểm (từ thanh tìm kiếm)
            if (!string.IsNullOrEmpty(locationId))
            {
                tours = tours.Where(t => t.MaDD == locationId);
            }

            // 4. Lọc theo Danh mục (từ thanh tìm kiếm)
            if (!string.IsNullOrEmpty(categoryId))
            {
                tours = tours.Where(t => t.MaDM == categoryId);
            }

            // 5. LỌC THEO QUỐC GIA (MỚI - từ Destination Section)
            if (!string.IsNullOrEmpty(countryFilter))
            {
                tours = tours.Where(t => t.DiaDiem.QuocGia == countryFilter);
            }

            // 6. Trả về danh sách đã lọc sang View
            return View(tours.ToList());
        }

        public ActionResult About()
        {
            var model = db.guides.ToList();
            return View(model);
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Contact([Bind(Include = "ContactID,YourName,YourEmail,Subject,Message,SubmissionDate")] Contact contact)
        {
            if (ModelState.IsValid)
            {
                contact.SubmissionDate = DateTime.Now;
                db.Contacts.Add(contact);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            

            return View(contact);
        }


        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Tải tour duy nhất, bao gồm cả dữ liệu liên quan
            Tour tour = db.Tours
                .Include(t => t.DiaDiem)
                .Include(t => t.DanhMuc)
                .FirstOrDefault(t => t.MaTour.Trim() == id); // Dùng Trim() để so khớp

            if (tour == null)
            {
                return HttpNotFound();
            }

            // (Tùy chọn) Lấy danh sách tour liên quan
            ViewBag.RelatedTours = db.Tours
                .Include(t => t.DiaDiem)
                .Include(t => t.DanhMuc)
                .Where(t => t.MaDM == tour.MaDM && t.MaTour != id)
                .Take(3)
                .ToList();

            return View(tour); // Trả về 1 đối tượng Tour (Model)
        }
        public ActionResult BookTour(string id, int persons = 1, DateTime? date = null)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Tour tour = db.Tours
                .Include(t => t.DiaDiem)
                .Include(t => t.DanhMuc)
                .FirstOrDefault(t => t.MaTour.Trim() == id);

            if (tour == null)
            {
                return HttpNotFound();
            }

            // Gửi thông tin đã chọn sang View
            ViewBag.NumberOfPersons = persons;
            ViewBag.BookingDate = date;

            // Tính toán tổng tiền
            double subtotal = tour.Gia * persons;
            double fees = 0; // Bạn có thể thêm phí dịch vụ nếu muốn
            ViewBag.Total = subtotal + fees;

            return View(tour); // Trả Model là tour đang đặt
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitBooking(FormCollection form)
        {
            string maTour = form["MaTour"]; // Lấy MaTour ngay lập tức để chuyển hướng nếu lỗi

            try
            {
                // === BƯỚC 1: LẤY DỮ LIỆU TỪ FORM ===
                var newBooking = new Booking
                {
                    MaTour = maTour,
                    HoTen = form["HoTen"],
                    Email = form["Email"],
                    SoDienThoai = form["SoDienThoai"],
                    GhiChu = form["GhiChu"],
                    PaymentMethod = form["PaymentMethod"],
                    SoNguoi = int.Parse(form["SoNguoi"]),
                    TongTien = decimal.Parse(form["TongTien"]),
                    // Chuyển đổi Ngày Di từ định dạng của form
                    NgayDi = DateTime.ParseExact(form["BookingDate"], "MM/dd/yyyy hh:mm tt", CultureInfo.InvariantCulture),
                    NgayDat = DateTime.Now,
                    PhuongTien = form["PhuongTien"]
                };

                // === BƯỚC 2: XỬ LÝ THANH TOÁN (Mô phỏng) ===
                // Trong ứng dụng thật: Gọi API Stripe/PayPal/VNPay ở đây
                // Ở đây chúng ta gọi hàm giả lập:
                bool paymentSuccess = SimulatePayment(newBooking.TongTien);

                // === BƯỚC 3: KIỂM TRA THANH TOÁN ===
                if (paymentSuccess)
                {
                    // Thanh toán thành công -> MỚI LƯU VÀO DATABASE
                    db.Bookings.Add(newBooking);
                    db.SaveChanges();

                    // Tạo thông báo thành công
                    TempData["BookingSuccessMessage"] = "Thanh toán thành công! Bạn đã đặt tour. Chúng tôi sẽ liên hệ với bạn sớm.";

                    // Chuyển về trang chủ
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    // Thanh toán thất bại (ví dụ: thẻ bị từ chối)
                    TempData["BookingErrorMessage"] = "Thanh toán thất bại. Vui lòng kiểm tra lại thông tin.";

                    // Trả người dùng về lại trang Details của tour đó
                    return RedirectToAction("Details", "Home", new { id = maTour });
                }
            }
            catch (Exception ex)
            {
                // Bắt các lỗi khác (như lỗi CSDL, lỗi định dạng ngày...)
                TempData["BookingErrorMessage"] = "Đã xảy ra lỗi hệ thống khi đặt tour. Vui lòng thử lại. Lỗi: " + ex.Message;
                return RedirectToAction("Details", "Home", new { id = maTour });
            }
        }

        /// <summary>
        /// Hàm này MÔ PHỎNG việc gọi API thanh toán.
        /// Trong thực tế, hàm này sẽ gọi đến Stripe, VNPay, MoMo...
        /// </summary>
        /// <returns>True (Thành công) hoặc False (Thất bại)</returns>
        private bool SimulatePayment(decimal amount)
        {
            // (Đây là logic giả)
            // Trong thực tế: Gửi 'amount' đến API cổng thanh toán và chờ kết quả
            // Ở đây chúng ta luôn giả định là thành công.
            return true;

            // Để thử nghiệm lỗi, bạn có thể đổi thành:
            // return false; 
        }
        public ActionResult Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return HttpNotFound();
            }
            var tour = db.Tours
                         .Where(t => t.MaTour.Trim() == id)
                         .FirstOrDefault();
        
            if (tour == null)
            {
                return HttpNotFound();
            }
            return View(tour);
        }
    }
}
