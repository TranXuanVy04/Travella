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

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public JsonResult Login(string email, string password, string ReturnUrl)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        //        {
        //            return Json(new { success = false, message = "Vui lòng nhập đầy đủ Email và Mật khẩu!" });
        //        }

        //        string hashedPassword = HashPassword(password);
        //        var user = db.Users1.FirstOrDefault(u => u.Email.ToLower() == email.ToLower() && u.PasswordHash == hashedPassword);

        //        if (user != null)
        //        {
        //            // --- THIẾT LẬP XÁC THỰC VÀ SESSION ---
        //            FormsAuthentication.SetAuthCookie(user.Name, false);
        //            Session["UserID"] = user.UserID;
        //            Session["UserRole"] = user.Role;
        //            Session["UserEmail"] = user.Email;
        //            Session["UserName"] = user.Name;

        //            string redirectUrl = "";

        //            // --- LOGIC XỬ LÝ RETURNURL CHÍNH XÁC ---
        //            if (!string.IsNullOrEmpty(ReturnUrl))
        //            {
        //                // Giải mã URL (phòng trường hợp bị mã hóa ký tự %2f...)
        //                string decodedUrl = Server.UrlDecode(ReturnUrl);

        //                // Kiểm tra bảo mật: Chỉ chuyển hướng nếu là link nội bộ web mình
        //                if (Url.IsLocalUrl(decodedUrl))
        //                {
        //                    redirectUrl = decodedUrl;
        //                }
        //            }



        //            // 4. Nếu redirectUrl vẫn rỗng (vì ReturnUrl không có hoặc không an toàn)
        //            if (string.IsNullOrEmpty(redirectUrl))
        //            {
        //                // Dùng logic phân quyền mặc định
        //                if (user.Role == "Admin")
        //                {
        //                    redirectUrl = Url.Action("dashboard", "Admin");
        //                }
        //                else if (user.Role == "NhanVien" || user.Role == "Employee")
        //                {
        //                    var nhanVien = db.NhanViens.FirstOrDefault(nv => nv.Email.ToLower() == user.Email.ToLower());
        //                    if (nhanVien != null)
        //                    {
        //                        Session["MaNV"] = nhanVien.MaNV;
        //                    }
        //                    redirectUrl = Url.Action("dashboard", "admin");
        //                }
        //                else // Customer hoặc role khác
        //                {
        //                    var khachHang = db.KhachHangs.FirstOrDefault(kh => kh.UserId == user.UserID);
        //                    if (khachHang != null)
        //                    {
        //                        Session["MaKH"] = khachHang.MaKH;
        //                    }
        //                    redirectUrl = Url.Action("Index", "Home");
        //                }
        //            }
        //            // --- KẾT THÚC LOGIC RETURNURL ---

        //            // Trả về JSON để JavaScript xử lý chuyển hướng
        //            return Json(new { success = true, message = "Đăng nhập thành công!", redirectUrl = redirectUrl });
        //        }
        //        else
        //        {
        //            return Json(new { success = false, message = "Email hoặc mật khẩu không đúng!" });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
        //    }
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Login(string email, string password, string ReturnUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                    return Json(new { success = false, message = "Vui lòng nhập đầy đủ Email và Mật khẩu!" });

                string hashedPassword = HashPassword(password);
                var user = db.Users1.FirstOrDefault(u => u.Email.ToLower() == email.ToLower() && u.PasswordHash == hashedPassword);

                if (user == null)
                    return Json(new { success = false, message = "Email hoặc mật khẩu không đúng!" });

                // ================== THIẾT LẬP SESSION ==================
                FormsAuthentication.SetAuthCookie(user.Name, false);
                Session["UserID"] = user.UserID;
                Session["UserRole"] = user.Role;
                Session["UserEmail"] = user.Email;
                Session["UserName"] = user.Name;

                // ================== TỰ ĐỘNG TẠO KHÁCH HÀNG NẾU CHƯA CÓ (BUG CUỐI CÙNG) ==================
                var khachHang = db.KhachHangs.FirstOrDefault(kh => kh.UserId == user.UserID);

                if (khachHang == null)
                {
                    khachHang = new KhachHang
                    {
                        UserId = user.UserID,
                        TenKH = user.Name ?? "Khách hàng",
                        Email = user.Email,
                        SoDT = "",
                        //di = ""
                        // Các trường khác để default
                    };
                    db.KhachHangs.Add(khachHang);
                    db.SaveChanges();
                }

                // BẮT BUỘC GÁN MaKH SAU KHI ĐÃ CHẮC CHẮN CÓ KHÁCH HÀNG
                Session["MaKH"] = khachHang.MaKH;
                // =================================================================================

                // ================== XỬ LÝ RETURNURL HOÀN HẢO ==================
                string redirectUrl = Url.Action("Index", "Home"); // default

                if (!string.IsNullOrEmpty(ReturnUrl))
                {
                    string decoded = ReturnUrl;
                    for (int i = 0; i < 10; i++)
                    {
                        string temp = Server.UrlDecode(decoded);
                        if (temp == decoded) break;
                        decoded = temp;
                    }

                    if (decoded.StartsWith("/")
                        && !decoded.Contains("://")
                        && !decoded.ToLower().Contains("javascript:"))
                    {
                        redirectUrl = VirtualPathUtility.ToAbsolute(decoded);
                    }
                }

                // Override cho Admin/Nhân viên (không ảnh hưởng Customer)
                if (user.Role == "Admin")
                    redirectUrl = Url.Action("dashboard", "Admin");
                else if (user.Role == "NhanVien" || user.Role == "Employee")
                {
                    //var nv = db.NhanViens.FirstOrDefault(x => x.Email.ToLower() == user.Email.ToLower());
                    //if (nv != null) Session["MaNV"] = nv.MaNV;
                    //redirectUrl = Url.Action("dashboard", "admin");
                }
                // =================================================================

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
