using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using System.Security.Cryptography;
using System.Text;
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

        // POST: Auth/Login
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public JsonResult Login(string email, string password)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        //        {
        //            return Json(new { success = false, message = "Vui lòng nhập đầy đủ Email và Mật khẩu!" });
        //        }

        //        string hashedPassword = HashPassword(password);

        //        // Tìm user trong bảng Users
        //        var user = db.Users1.FirstOrDefault(u => u.Email == email && u.PasswordHash == hashedPassword);

        //        if (user != null)
        //        {
        //            // Đăng nhập thành công
        //            FormsAuthentication.SetAuthCookie(user.Email, false);

        //            // Lưu thông tin vào Session
        //            Session["UserID"] = user.UserID;
        //            Session["UserName"] = user.Name;
        //            Session["UserEmail"] = user.Email;
        //            Session["UserRole"] = user.Role;

        //            // Kiểm tra Role và chuyển hướng
        //            string redirectUrl = "";

        //            if (user.Role == "Admin")
        //            {
        //                redirectUrl = Url.Action("dashboard", "Admin"); // Trang quản lý Admin
        //            }
        //            else if (user.Role == "NhanVien" || user.Role == "Employee")
        //            {
        //                // Lấy thông tin nhân viên
        //                var nhanVien = db.NhanViens.FirstOrDefault(nv => nv.Email.Trim() == user.Email);
        //                if (nhanVien != null)
        //                {
        //                    Session["MaNV"] = nhanVien.MaNV;
        //                }
        //                redirectUrl = Url.Action("dashboard", "admin"); // Trang quản lý Nhân viên
        //            }
        //            else // Customer hoặc role khác
        //            {
        //                // Lấy thông tin khách hàng
        //                var khachHang = db.KhachHangs.FirstOrDefault(kh => kh.UserId == user.UserID);
        //                if (khachHang != null)
        //                {
        //                    Session["MaKH"] = khachHang.MaKH;
        //                }
        //                redirectUrl = Url.Action("Index", "Home"); // Trang chủ khách hàng
        //            }

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
                {
                    return Json(new { success = false, message = "Vui lòng nhập đầy đủ Email và Mật khẩu!" });
                }

                string hashedPassword = HashPassword(password);
                var user = db.Users1.FirstOrDefault(u => u.Email.ToLower() == email.ToLower() && u.PasswordHash == hashedPassword);

                if (user != null)
                {
                    // --- THIẾT LẬP XÁC THỰC VÀ SESSION ---
                    FormsAuthentication.SetAuthCookie(user.Name, false);
                    Session["UserID"] = user.UserID;
                    Session["UserRole"] = user.Role;
                    Session["UserEmail"] = user.Email;
                    Session["UserName"] = user.Name;

                    string redirectUrl = "";

                    // --- LOGIC XỬ LÝ RETURNURL CHÍNH XÁC ---

                    // 1. Kiểm tra xem ReturnUrl có tồn tại không
                    if (!string.IsNullOrEmpty(ReturnUrl))
                    {
                        string path = ReturnUrl;
                        string query = "";

                        // 2. Tách URL thành đường dẫn (path) và truy vấn (query)
                        int queryIndex = ReturnUrl.IndexOf('?');
                        if (queryIndex > -1)
                        {
                            path = ReturnUrl.Substring(0, queryIndex); // Lấy phần /Home/BookTour
                            query = ReturnUrl.Substring(queryIndex); // Lấy phần ?id=...&date=...
                        }

                        // 3. Chỉ kiểm tra bảo mật trên ĐƯỜNG DẪN (path)
                        // Điều này cho phép các URL như "/Home/BookTour" vượt qua kiểm tra
                        if (Url.IsLocalUrl(path))
                        {
                            // Nếu đường dẫn an toàn, ghép nó với truy vấn
                            redirectUrl = path + query;
                        }
                    }

                    // 4. Nếu redirectUrl vẫn rỗng (vì ReturnUrl không có hoặc không an toàn)
                    if (string.IsNullOrEmpty(redirectUrl))
                    {
                        // Dùng logic phân quyền mặc định
                        if (user.Role == "Admin")
                        {
                            redirectUrl = Url.Action("dashboard", "Admin");
                        }
                        else if (user.Role == "NhanVien" || user.Role == "Employee")
                        {
                            var nhanVien = db.NhanViens.FirstOrDefault(nv => nv.Email.ToLower() == user.Email.ToLower());
                            if (nhanVien != null)
                            {
                                Session["MaNV"] = nhanVien.MaNV;
                            }
                            redirectUrl = Url.Action("dashboard", "admin");
                        }
                        else // Customer hoặc role khác
                        {
                            var khachHang = db.KhachHangs.FirstOrDefault(kh => kh.UserId == user.UserID);
                            if (khachHang != null)
                            {
                                Session["MaKH"] = khachHang.MaKH;
                            }
                            redirectUrl = Url.Action("Index", "Home");
                        }
                    }
                    // --- KẾT THÚC LOGIC RETURNURL ---

                    // Trả về JSON để JavaScript xử lý chuyển hướng
                    return Json(new { success = true, message = "Đăng nhập thành công!", redirectUrl = redirectUrl });
                }
                else
                {
                    return Json(new { success = false, message = "Email hoặc mật khẩu không đúng!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
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
