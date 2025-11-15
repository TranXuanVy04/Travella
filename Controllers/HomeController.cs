using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Trave.Models;

namespace Trave.Controllers
{ 
    //[AllowAnonymous] /// Cho phép truy cập không cần đăng nhập
    //[Authorize(Roles = "Customer")]
    public class HomeController : Controller
    {
        private DULICHEntities db = new DULICHEntities();
        public ActionResult Index()
        {   
            var model = db.Tours
                .Include(t => t.DiaDiem)
                .Include(t => t.DanhMuc)
                .Include(t => t.DanhGias)
                .Include(t => t.Bookings)
                .Include(t => t.DanhGias.Select(dg => dg.KhachHang))
                .ToList();

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

        // Đặt trong HomeController của bạn

        [Authorize]
        [HttpGet]
        // 1. Sửa 'id' thành 'string' để khớp với MaTour (varchar(10))
        public ActionResult BookTour(string id, string date, int persons)
        {
            if (Session["MaKH"] == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng. Vui lòng đăng nhập lại.";

                FormsAuthentication.SignOut();
                return RedirectToAction("Login", "Auth");
            }

            // 2. Bây giờ mới lấy dữ liệu (đã an toàn)
            int currentMaKH = (int)Session["MaKH"];
            var khachHang = db.KhachHangs.Find(currentMaKH);

            // 3. Lấy thông tin Tour (dùng string id)
            var tour = db.Tours.Find(id);

            if (tour == null || khachHang == null)
            {
                return HttpNotFound();
            }

            // 4. Xử lý dữ liệu
            DateTime ngayDiParsed;
            DateTime.TryParse(date, out ngayDiParsed); // Cần xử lý lỗi nếu date sai

            // 5. Dùng cột 'Gia' (kiểu float/double) và ép kiểu sang decimal
            decimal giaMoiNguoi = (decimal)tour.Gia;
            decimal tongGia = giaMoiNguoi * persons;

            // 6. TẠO MỘT ĐỐI TƯỢNG BOOKING MỚI (CHƯA LƯU DB)
            var bookingModel = new Booking
            {
                // Gán các giá trị tính toán
                MaKH = currentMaKH,
                MaTour = id, // id là string (varchar(10))
                NgayDi = ngayDiParsed,
                SoNguoi = persons,
                TongGia = tongGia,

                // 7. GÁN THUỘC TÍNH ĐIỀU HƯỚNG (QUAN TRỌNG)
                // Điều này cho phép View truy cập Model.Tour và Model.KhachHang
                Tour = tour,
                KhachHang = khachHang
            };

            // 8. Trả về View "Payment" với model là đối tượng Booking
            return View("booktour", bookingModel);
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult ProcessPayment(Booking bookingModel, string PhuongThucThanhToan)
        //{
        //    // Chúng ta nhận 'bookingModel' đã được bind (MaTour, MaKH, NgayDi, SoNguoi, TongGia, ghichu)
        //    // và 'PhuongThucThanhToan' (từ input radio)

        //    try
        //    {
        //        // 1. Hoàn thiện đối tượng Booking trước khi lưu
        //        bookingModel.NgayDat = DateTime.Now;

        //        if (PhuongThucThanhToan == "VnPay")
        //        {
        //            bookingModel.TrangThai = "Chờ thanh toán VNPay";
        //        }
        //        else // BankTransfer
        //        {
        //            bookingModel.TrangThai = "Chờ xác nhận (Chuyển khoản)";
        //        }

        //        // 2. Thêm vào DB và Lưu
        //        db.Bookings.Add(bookingModel);
        //        db.SaveChanges();

        //        // Sau khi SaveChanges(), 'bookingModel.MaBooking' sẽ tự động
        //        // cập nhật ID của đơn hàng vừa được tạo (ví dụ: 105)

        //        // 3. Xử lý logic thanh toán
        //        if (PhuongThucThanhToan == "VnPay")
        //        {
        //            // ==========================================================
        //            // TODO: TÍCH HỢP VNPAY TẠI ĐÂY
        //            // ==========================================================
        //            // 1. Gọi thư viện VNPay, tạo URL thanh toán
        //            // 2. Gán 'bookingModel.MaBooking' làm mã tham chiếu (vnp_TxnRef)
        //            // 3. string paymentUrl = vnpay.CreateRequestUrl(...);
        //            // 4. return Redirect(paymentUrl);
        //            // ==========================================================

        //            // (Mã tạm thời cho đến khi bạn tích hợp VNPay)
        //            TempData["SuccessMessage"] = "Đang chuyển hướng đến cổng thanh toán VNPay...";
        //            // Chúng ta chuyển tạm đến trang xác nhận
        //            return RedirectToAction("BookingConfirmation", new { id = bookingModel.MaBooking });
        //        }
        //        else // BankTransfer
        //        {
        //            // Chuyển thẳng đến trang xác nhận, nơi sẽ hiển thị thông tin STK
        //            return RedirectToAction("BookingConfirmation", new { id = bookingModel.MaBooking });
        //        }
        //    }
        //    catch (DbEntityValidationException ex)
        //    {
        //        // Xử lý lỗi validation (nếu có)
        //        var errorMessages = ex.EntityValidationErrors
        //                .SelectMany(x => x.ValidationErrors)
        //                .Select(x => x.ErrorMessage);
        //        TempData["ErrorMessage"] = "Lỗi dữ liệu: " + string.Join("; ", errorMessages);

        //        // Quay lại trang thanh toán (cần tải lại Nav Properties)
        //        bookingModel.Tour = db.Tours.Find(bookingModel.MaTour);
        //        bookingModel.KhachHang = db.KhachHangs.Find(bookingModel.MaKH);
        //        return View("BookTour", bookingModel);
        //    }
        //    catch (Exception ex)
        //    {
        //        // Xử lý lỗi chung
        //        TempData["ErrorMessage"] = "Đã xảy ra lỗi không mong muốn: " + ex.Message;

        //        // Quay lại trang thanh toán (cần tải lại Nav Properties)
        //        bookingModel.Tour = db.Tours.Find(bookingModel.MaTour);
        //        bookingModel.KhachHang = db.KhachHangs.Find(bookingModel.MaKH);
        //        return View("BookTour", bookingModel);
        //    }
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProcessPayment(Booking bookingModel, string PhuongThucThanhToan)
        {
            // Chúng ta nhận 'bookingModel' đã được bind và 'PhuongThucThanhToan'

            try
            {
                // 1. Hoàn thiện đối tượng Booking
                bookingModel.NgayDat = DateTime.Now; // Gán ngày đặt

                // --- SỬA ĐỔI TẠI ĐÂY ---
                // Mặc định mọi đơn hàng đều thành công (đã thanh toán)
                bookingModel.TrangThai = "Đã thanh toán";
                // (Hoặc "Đã xác nhận", "Đã hoàn thành" tùy theo quy trình của bạn)
                // Bỏ qua logic kiểm tra (if PhuongThucThanhToan == "VnPay"...)

                // 2. Thêm vào DB và Lưu
                db.Bookings.Add(bookingModel);
                db.SaveChanges();

                // 3. Chuyển thẳng đến trang xác nhận (Cảm ơn)
                // Vì đã mặc định thành công, chúng ta không cần chuyển đến VNPay
                return RedirectToAction("BookingConfirmation", new { id = bookingModel.MaBooking });
            }
            catch (Exception ex)
            {
                // Xử lý lỗi
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;

                // Quay lại trang thanh toán (cần tải lại Nav Properties)
                bookingModel.Tour = db.Tours.Find(bookingModel.MaTour);
                bookingModel.KhachHang = db.KhachHangs.Find(bookingModel.MaKH);

                // Trả về View "BookTour" từ HomeController
                return View("~/Views/Home/BookTour.cshtml", bookingModel);
            }
        }




        [Authorize]
        public ActionResult BookingConfirmation(int id) // id ở đây là MaBooking
        {
            // Kiểm tra bảo mật (đơn này phải là của KH đang đăng nhập)
            if (Session["MaKH"] == null)
            {
                return RedirectToAction("Login", "Auth");
            }
            int currentMaKH = (int)Session["MaKH"];

            var booking = db.Bookings.Include(b => b.Tour)
                                     .Include(b => b.KhachHang)
                                     .FirstOrDefault(b => b.MaBooking == id && b.MaKH == currentMaKH);

            if (booking == null)
            {
                // Không tìm thấy đơn hàng hoặc đơn hàng không thuộc về người này
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng hợp lệ.";
                return RedirectToAction("Index", "Home");
            }

            // Trả về View "BookingConfirmation.cshtml" (bạn sẽ cần tạo View này)
            return View(booking);
        }


        [Authorize]
        public ActionResult Profile()
        {
            // 1. Lấy khách hàng hiện tại
            if (Session["MaKH"] == null)
            {
                return RedirectToAction("Login", "Auth");
            }
            int currentMaKH = (int)Session["MaKH"];

            // 2. TẢI TRƯỚC DỮ LIỆU (Eager Loading)
            // Tải KhachHang CÙNG VỚI:
            //    -> Tất cả Bookings CỦA HỌ
            //    ->    -> Thông tin Tour CỦA Booking
            //    ->    ->    -> Thông tin Guide CỦA Tour
            //    ->    -> Thông tin DanhGia CỦA Booking
            var khachHang = db.KhachHangs
                .Include(k => k.Bookings.Select(b => b.Tour.guide))
                .Include(k => k.Bookings.Select(b => b.Tour.DanhGias))
                .FirstOrDefault(k => k.MaKH == currentMaKH);

            if (khachHang == null)
            {
                FormsAuthentication.SignOut();
                return RedirectToAction("Login", "Auth");
            }

            // 3. Gửi đối tượng KhachHang (đã chứa tất cả dữ liệu) đến View
            return View(khachHang);
        }
    }
}
