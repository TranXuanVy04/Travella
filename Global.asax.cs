using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
// --- THÊM 2 DÒNG NÀY ĐỂ XỬ LÝ BẢO MẬT ---
using System.Web.Security;
using System.Security.Principal;

namespace Trave
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        // =====================================================================
        // HÀM QUAN TRỌNG NHẤT: CHẠY MỖI KHI NGƯỜI DÙNG TẢI TRANG
        // Nhiệm vụ: Đọc Cookie -> Lấy Role -> Gán vào User hiện tại
        // =====================================================================
        protected void Application_PostAuthenticateRequest(Object sender, EventArgs e)
        {
            // 1. Lấy Cookie đăng nhập của người dùng gửi lên
            var authCookie = HttpContext.Current.Request.Cookies[FormsAuthentication.FormsCookieName];

            if (authCookie != null)
            {
                // 2. Giải mã Cookie lấy ra cái Ticket (Vé)
                FormsAuthenticationTicket authTicket = FormsAuthentication.Decrypt(authCookie.Value);

                if (authTicket != null && !authTicket.Expired)
                {
                    // 3. Lấy danh sách Role từ phần UserData của Ticket (lúc Login mình đã nhét vào đây)
                    // Nếu có nhiều role cách nhau bằng dấu phẩy thì Split ra
                    var roles = authTicket.UserData.Split(',');

                    // 4. Tạo định danh người dùng mới có chứa thông tin Role
                    HttpContext.Current.User = new System.Security.Principal.GenericPrincipal(new FormsIdentity(authTicket), roles);
                }
            }
        }
    }
}