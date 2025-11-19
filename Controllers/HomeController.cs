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
        // SỬA LỖI QUAN TRỌNG: Đổi 'int persons' thành 'int? persons' để chấp nhận giá trị null
        public ActionResult BookTour(string id, string date, int? persons)
        {
            // 1. Kiểm tra đăng nhập
            if (Session["MaKH"] == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để đặt tour.";

                // --- QUAN TRỌNG: Gửi kèm ReturnUrl để đăng nhập xong quay lại đây ---
                // Tạo đường dẫn hiện tại: /Home/BookTour?id=...&date=...&persons=...
                string returnUrl = Url.Action("BookTour", "Home", new { id = id, date = date, persons = persons });

                // Chuyển hướng sang Login kèm ReturnUrl
                return RedirectToAction("Login", "Auth", new { ReturnUrl = returnUrl });
            }

            // 2. Xử lý tham số đầu vào an toàn
            // Nếu persons là null (không truyền), mặc định là 1 người
            int soLuongNguoi = persons ?? 1;

            // Kiểm tra ID Tour
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Index"); // Không có ID thì về trang chủ
            }

            // 3. Lấy dữ liệu từ Database
            int currentMaKH = (int)Session["MaKH"]; // Đã check null ở bước 1 nên cast an toàn
            var khachHang = db.KhachHangs.Find(currentMaKH);

            //var tour = db.Tours.Find(id); // ID là string theo thiết kế của bạn
            var tour = db.Tours.FirstOrDefault(t => t.MaTour.Trim() == id.Trim());

            // Kiểm tra dữ liệu tồn tại
            if (tour == null || khachHang == null)
            {
                TempData["Debug"] = "Tour null: " + (tour == null) + " | KH null: " + (khachHang == null) + " | MaKH: " + Session["MaKH"];
                return HttpNotFound();
                //return HttpNotFound("Không tìm thấy thông tin Tour hoặc Khách hàng.");
            }

            // 4. Xử lý ngày tháng (Date)
            DateTime ngayDiParsed;
            // Thử parse ngày, nếu lỗi (hoặc null) thì mặc định là ngày mai
            if (!DateTime.TryParse(date, out ngayDiParsed))
            {
                ngayDiParsed = DateTime.Now.AddDays(1);
            }

            // 5. Tính toán giá tiền
            // Ép kiểu cẩn thận, phòng trường hợp giá trong DB là null (nếu có)
            decimal giaMoiNguoi = Convert.ToDecimal(tour.Gia);
            decimal tongGia = giaMoiNguoi * soLuongNguoi;

            // 6. Tạo Model Booking (để hiển thị, chưa lưu DB)
            var bookingModel = new Booking
            {
                // Thông tin cơ bản
                MaKH = currentMaKH,
                MaTour = id,
                NgayDi = ngayDiParsed,
                SoNguoi = soLuongNguoi,
                TongGia = tongGia,

                // Trạng thái mặc định
                TrangThai = "0", // 0: Chưa thanh toán / Mới tạo

                // Gán Navigation Properties (Để View hiển thị tên Tour, tên Khách)
                Tour = tour,
                KhachHang = khachHang
            };

            // 7. Trả về View
            return View("BookTour", bookingModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProcessPayment(Booking bookingModel, string PhuongThucThanhToan)
        {
            // 1. Cấu hình VNPay
            string vnp_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            string vnp_TmnCode = "QE91CB08";
            // QUAN TRỌNG: Dùng .Trim() để cắt bỏ khoảng trắng thừa nếu có
            string vnp_HashSecret = "R08TH1P0J1N4T0L63X0Q3KDTFWBYS8YT".Trim();

            try
            {
                // Kiểm tra dữ liệu đầu vào quan trọng
                if (bookingModel.TongGia == null || bookingModel.TongGia <= 0)
                {
                    TempData["ErrorMessage"] = "Lỗi: Tổng tiền không hợp lệ.";
                    return RedirectToAction("BookTour", new { id = bookingModel.MaTour });
                }

                bookingModel.NgayDat = DateTime.Now; // Gán ngày đặt


                // 2. Lưu đơn hàng vào DB (Trạng thái: Chưa thanh toán)
                // Trong Action ProcessPayment
                if (PhuongThucThanhToan == "VNPay")
                {
                    // SỬA: Đổi "0" thành "Chưa thanh toán"
                    bookingModel.TrangThai = "Chưa thanh toán";
                }
                else
                {
                    // Thanh toán tiền mặt/Chuyển khoản sau cũng để là Chưa thanh toán (hoặc Đã xác nhận tùy bạn)
                    bookingModel.TrangThai = "Thanh toán trực tiếp";
                }

                db.Bookings.Add(bookingModel);
                db.SaveChanges();

                // 3. Xử lý chuyển hướng VNPay
                if (PhuongThucThanhToan == "VNPay")
                {
                    VnPayLibrary vnpay = new VnPayLibrary();

                    // Lấy giá trị tiền tệ (nhân 100)
                    long amount = (long)(bookingModel.TongGia.Value * 100* 26379);

                    vnpay.AddRequestData("vnp_Version", "2.1.0");
                    vnpay.AddRequestData("vnp_Command", "pay");
                    vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
                    vnpay.AddRequestData("vnp_Amount", amount.ToString());
                    vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
                    vnpay.AddRequestData("vnp_CurrCode", "VND");
                    vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress()); // Đã sửa trong Library thành 127.0.0.1
                    vnpay.AddRequestData("vnp_Locale", "vn");

                    // Nội dung thanh toán: Dùng tiếng Việt KHÔNG DẤU để tránh lỗi encoding sai chữ ký
                    vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang " + bookingModel.MaBooking);
                    vnpay.AddRequestData("vnp_OrderType", "other");

                    // Url Trả về
                    vnpay.AddRequestData("vnp_ReturnUrl", Url.Action("PaymentCallback", "Home", null, Request.Url.Scheme));
                    vnpay.AddRequestData("vnp_TxnRef", bookingModel.MaBooking.ToString());

                    string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

                    // Ghi log URL ra Output để debug nếu cần
                    System.Diagnostics.Debug.WriteLine("VNPay URL: " + paymentUrl);

                    return Redirect(paymentUrl);
                }

                // Thanh toán tiền mặt
                return RedirectToAction("BookingConfirmation", new { id = bookingModel.MaBooking });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi hệ thống: " + ex.Message;
                return RedirectToAction("Index", "Home"); // Hoặc trang báo lỗi
            }
        }


        public ActionResult PaymentCallback()
        {
            string vnp_HashSecret = "R08TH1P0J1N4T0L63X0Q3KDTFWBYS8YT"; // Chuỗi bí mật
            var vnpayData = Request.QueryString;
            VnPayLibrary vnpay = new VnPayLibrary();

            // 1. Lấy toàn bộ dữ liệu trả về để check checksum
            foreach (string s in vnpayData)
            {
                if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(s, vnpayData[s]);
                }
            }

            // 2. Lấy các tham số quan trọng từ VNPay
            long orderId = Convert.ToInt64(vnpay.GetResponseData("vnp_TxnRef")); // Đây chính là BookingId
            long vnpayTranId = Convert.ToInt64(vnpay.GetResponseData("vnp_TransactionNo"));
            string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            string vnp_SecureHash = Request.QueryString["vnp_SecureHash"];

            // 3. Kiểm tra chữ ký bảo mật
            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

            if (checkSignature)
            {
                // Tìm đơn hàng trong Database
                var booking = db.Bookings.Find(orderId);

                if (booking != null)
                {
                    // --- TRƯỜNG HỢP 1: THÀNH CÔNG (Mã 00) ---
                    if (vnp_ResponseCode == "00")
                    {
                        booking.TrangThai = "Đã thanh toán"; // Cập nhật trạng thái thành công

                        // (Tùy chọn) Lưu mã giao dịch VNPay để đối soát sau này
                        // booking.MaGiaoDich = vnpayTranId; 

                        db.SaveChanges();

                        TempData["Message"] = "Thanh toán thành công đơn hàng #" + orderId;
                        return RedirectToAction("BookingConfirmation", new { id = orderId });
                    }

                    // --- TRƯỜNG HỢP 2: THẤT BẠI / HỦY (Các mã khác) ---
                    else
                    {
                        // Cập nhật trạng thái "Hủy giao dịch" vào Database (Không xóa đơn)
                        booking.TrangThai = "Hủy giao dịch";
                        db.SaveChanges();

                        // Tạo thông báo lỗi thân thiện
                        string lyDo = "";
                        if (vnp_ResponseCode == "24") lyDo = "Bạn đã hủy giao dịch thanh toán.";
                        else lyDo = "Lỗi thanh toán từ ngân hàng (Mã: " + vnp_ResponseCode + ")";

                        TempData["PaymentError"] = lyDo;

                        // [QUAN TRỌNG] Truyền bookingId sang trang PaymentFail để lấy thông tin nút "Thử lại"
                        return RedirectToAction("PaymentFail", new { bookingId = orderId });
                    }
                }
                else
                {
                    // Trường hợp hiếm: Có ID trả về nhưng không tìm thấy trong DB
                    TempData["PaymentError"] = "Không tìm thấy đơn hàng tương ứng.";
                    return RedirectToAction("PaymentFail");
                }
            }
            else
            {
                // --- TRƯỜNG HỢP 3: SAI CHỮ KÝ (Checksum Fail) ---
                TempData["PaymentError"] = "Cảnh báo: Phát hiện sai lệch chữ ký bảo mật.";
                return RedirectToAction("PaymentFail");
            }
        }


        // GET: PaymentFail
        public ActionResult PaymentFail(long? bookingId)
        {
            // 1. Lấy thông báo lỗi từ TempData (truyền từ PaymentCallback sang)
            ViewBag.ErrorMessage = TempData["PaymentError"] ?? "Giao dịch thất bại hoặc bị hủy.";

            // 2. Xử lý Logic: Lấy thông tin cũ để "Thử lại" và Xóa đơn rác
            if (bookingId.HasValue)
            {
                try
                {
                    var booking = db.Bookings.Find(bookingId);
                    if (booking != null)
                    {
                        // --- QUAN TRỌNG: Lưu lại thông tin để tạo link "Thử lại" ---
                        ViewBag.LastTourId = booking.MaTour;
                        ViewBag.LastPersons = booking.SoNguoi;
                        // Chuyển ngày sang chuỗi yyyy-MM-dd để truyền lên URL an toàn
                        ViewBag.LastDate = booking.NgayDi.ToString("yyyy-MM-dd");

                        //// Xóa đơn hàng lỗi khỏi Database (Dọn dẹp rác)
                        //db.Bookings.Remove(booking);
                        //db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    // Ghi log lỗi nếu cần thiết, nhưng không để chết trang web
                    System.Diagnostics.Debug.WriteLine("Lỗi xóa booking rác: " + ex.Message);
                }
            }

            return View();
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
