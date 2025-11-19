using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Trave.Models;
namespace Trave.Controllers
{
    public class AuthController : Controller
    {
        private DULICHEntities db = new DULICHEntities(); // Tên DbContext do EF tạo

        // Hàm Hash mật khẩu bằng SHA256
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // GET: Auth/Login
        [HttpGet]
        public ActionResult Login()
        {
            // Nếu đã đăng nhập, chuyển về trang chủ
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        

        [HttpPost]
        [ValidateAntiForgeryToken]
        //public JsonResult Login(string email, string password, string ReturnUrl)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        //            return Json(new { success = false, message = "Vui lòng nhập đầy đủ Email và Mật khẩu!" });

        //        string hashedPassword = HashPassword(password);
        //        var user = db.Users1.FirstOrDefault(u => u.Email.ToLower() == email.ToLower() && u.PasswordHash == hashedPassword);

        //        if (user == null)
        //            return Json(new { success = false, message = "Email hoặc mật khẩu không đúng!" });

        //        // ================== THIẾT LẬP SESSION ==================
        //        FormsAuthentication.SetAuthCookie(user.Name, false);
        //        Session["UserID"] = user.UserID;
        //        Session["UserRole"] = user.Role;
        //        Session["UserEmail"] = user.Email;
        //        Session["UserName"] = user.Name;

        //        // ================== TỰ ĐỘNG TẠO KHÁCH HÀNG NẾU CHƯA CÓ (BUG CUỐI CÙNG) ==================
        //        var khachHang = db.KhachHangs.FirstOrDefault(kh => kh.UserId == user.UserID);

        //        if (khachHang == null)
        //        {
        //            khachHang = new KhachHang
        //            {
        //                UserId = user.UserID,
        //                TenKH = user.Name ?? "Khách hàng",
        //                Email = user.Email,
        //                SoDT = "",
        //                //di = ""
        //                // Các trường khác để default
        //            };
        //            db.KhachHangs.Add(khachHang);
        //            db.SaveChanges();
        //        }

        //        // BẮT BUỘC GÁN MaKH SAU KHI ĐÃ CHẮC CHẮN CÓ KHÁCH HÀNG
        //        Session["MaKH"] = khachHang.MaKH;
        //        // =================================================================================

        //        // ================== XỬ LÝ RETURNURL HOÀN HẢO ==================
        //        string redirectUrl = Url.Action("Index", "Home"); // default

        //        if (!string.IsNullOrEmpty(ReturnUrl))
        //        {
        //            string decoded = ReturnUrl;
        //            for (int i = 0; i < 10; i++)
        //            {
        //                string temp = Server.UrlDecode(decoded);
        //                if (temp == decoded) break;
        //                decoded = temp;
        //            }

        //            if (decoded.StartsWith("/")
        //                && !decoded.Contains("://")
        //                && !decoded.ToLower().Contains("javascript:"))
        //            {
        //                redirectUrl = VirtualPathUtility.ToAbsolute(decoded);
        //            }
        //        }

        //        // Override cho Admin/Nhân viên (không ảnh hưởng Customer)
        //        if (user.Role == "Admin")
        //            redirectUrl = Url.Action("dashboard", "Admin");
        //        else if (user.Role == "NhanVien" || user.Role == "Employee")
        //        {
        //            //var nv = db.Users1.FirstOrDefault(x => x.Email.ToLower() == user.Email.ToLower());
        //            //if (nv != null) Session["MaNV"] = nv.Email;
        //            redirectUrl = Url.Action("dashboard", "admin");
        //        }
        //        // =================================================================

        //        return Json(new
        //        {
        //            success = true,
        //            message = "Đăng nhập thành công!",
        //            redirectUrl = redirectUrl
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
        //    }
        //}

        public JsonResult Login(string email, string password, string ReturnUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                    return Json(new { success = false, message = "Vui lòng nhập đầy đủ Email và Mật khẩu!" });

                // BƯỚC 1: Tìm user theo Email trước (để hỗ trợ cả BCrypt hoặc MD5)
                var user = db.Users1.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());

                // BƯỚC 2: Kiểm tra mật khẩu
                // Lưu ý: Nếu bạn dùng BCrypt, phải dùng BCrypt.Verify. Nếu dùng MD5 thường thì dùng so sánh chuỗi.
                // Ở đây mình giả định bạn dùng hàm HashPassword tự viết:
                if (user == null || user.PasswordHash != HashPassword(password))
                {
                    return Json(new { success = false, message = "Email hoặc mật khẩu không đúng!" });
                }

                // ================== QUAN TRỌNG: TẠO TICKET CHỨA ROLE ==================
                // Thay thế dòng FormsAuthentication.SetAuthCookie cũ bằng đoạn này:

                string userRole = user.Role ?? ""; // Lấy role, nếu null thì để rỗng

                var authTicket = new FormsAuthenticationTicket(
                    1,
                    user.Name, // Tên đăng nhập (lưu vào User.Identity.Name)
                    DateTime.Now,
                    DateTime.Now.AddMinutes(60),
                    false,
                    userRole // <--- QUAN TRỌNG: Lưu Role vào đây để [Authorize] đọc được
                );

                string encryptedTicket = FormsAuthentication.Encrypt(authTicket);
                var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
                Response.Cookies.Add(authCookie);

                // ================== THIẾT LẬP SESSION (Cho việc hiển thị) ==================
                Session["UserID"] = user.UserID;
                Session["UserRole"] = user.Role;
                Session["UserEmail"] = user.Email;
                Session["UserName"] = user.Name;

                // ================== TỰ ĐỘNG TẠO KHÁCH HÀNG ==================
                var khachHang = db.KhachHangs.FirstOrDefault(kh => kh.UserId == user.UserID);

                if (khachHang == null)
                {
                    khachHang = new KhachHang
                    {
                        UserId = user.UserID,
                        TenKH = user.Name ?? "Khách hàng",
                        Email = user.Email,
                        SoDT = ""
                    };
                    db.KhachHangs.Add(khachHang);
                    db.SaveChanges();
                }

                // Gán MaKH vào Session
                Session["MaKH"] = khachHang.MaKH;


                // ================== XỬ LÝ RETURNURL & ĐIỀU HƯỚNG ==================
                string redirectUrl = Url.Action("Index", "Home"); // Mặc định về trang chủ

                // 1. Ưu tiên xử lý ReturnUrl nếu có (và an toàn)
                if (!string.IsNullOrEmpty(ReturnUrl))
                {
                    string decoded = ReturnUrl;
                    // Decode nhiều lần để tránh lỗi encode kép
                    for (int i = 0; i < 5; i++) { decoded = Server.UrlDecode(decoded); }

                    if (decoded.StartsWith("/") && !decoded.Contains("://") && !decoded.ToLower().Contains("javascript:"))
                    {
                        redirectUrl = VirtualPathUtility.ToAbsolute(decoded);
                    }
                }

                // 2. Logic điều hướng theo Role (Ghi đè nếu là Admin/NV)
                // Nếu User là Admin/NV -> Luôn ưu tiên vào Dashboard (trừ khi họ đang có việc cụ thể cần returnUrl)
                // (Nếu bạn muốn ReturnUrl được ưu tiên cao nhất thì để đoạn này LÊN TRÊN đoạn xử lý ReturnUrl)

                if (user.Role == "Admin" || user.Role == "Employee" || user.Role == "NhanVien")
                {
                    // Nếu không có ReturnUrl cụ thể, hoặc ReturnUrl là trang Login/Home -> Đưa vào Dashboard
                    if (string.IsNullOrEmpty(ReturnUrl) || redirectUrl == "/" || redirectUrl.ToLower().Contains("home"))
                    {
                        redirectUrl = Url.Action("Dashboard", "Admin");
                    }
                }

                return Json(new
                {
                    success = true,
                    message = "Đăng nhập thành công!",
                    redirectUrl = redirectUrl
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }


        // GET: Auth/Register
        [HttpGet]
        public ActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("login", "auth");
            }
            return View();
        }

        // POST: Auth/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Register(string name, string email, string username, string password, 
                                   string phone, string birthDate, string gender)
        {
            try
            {
                // Validate dữ liệu
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || 
                    string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) ||
                    string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(gender))
                {
                    return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin bắt buộc!" });
                }

                // Kiểm tra email đã tồn tại
                if (db.Users1.Any(u => u.Email == email))
                {
                    return Json(new { success = false, message = "Email này đã được đăng ký!" });
                }

                // Kiểm tra số điện thoại đã tồn tại
                if (db.KhachHangs.Any(kh => kh.SoDT == phone))
                {
                    return Json(new { success = false, message = "Số điện thoại này đã được sử dụng!" });
                }

                // Tạo User mới
                var newUser = new Users
                {
                    Name = username,
                    Email = email,
                    Role = "Customer",
                    PasswordHash = HashPassword(password),
                    CreatedAt = DateTime.Now
                };
                db.Users1.Add(newUser);
                db.SaveChanges();

                // Parse ngày sinh
                DateTime? ngaySinh = null;
                if (!string.IsNullOrEmpty(birthDate))
                {
                    DateTime tempDate;
                    if (DateTime.TryParse(birthDate, out tempDate))
                    {
                        ngaySinh = tempDate;
                    }
                }

                // Tạo KhachHang mới
                var newKhachHang = new KhachHang
                {
                    TenKH = name,
                    Email = email,
                    SoDT = phone,
                    NgaySinh = ngaySinh,
                    GioiTinhKH = gender,
                    UserId = newUser.UserID
                };
                db.KhachHangs.Add(newKhachHang);
                db.SaveChanges();

                return Json(new { success = true, message = "Đăng ký thành công! Vui lòng đăng nhập." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // Đăng xuất
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();

            // 2. Xóa tất cả các Session đã lưu (Tuỳ chọn, nhưng nên làm)
            Session.Clear();
            Session.Abandon();

            // 3. Chuyển hướng về trang Index của Home Controller
            return RedirectToAction("Index", "Home");
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
