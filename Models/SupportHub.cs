using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Trave.Models; // Đảm bảo Namespace trỏ đúng về Models của bạn

namespace Trave.Models
{
    [Authorize] // Bắt buộc phải đăng nhập mới được kết nối Chat
    public class SupportHub : Hub
    {
        // Từ điển lưu trữ danh sách User đang Online (An toàn đa luồng)
        // Key: Email/Username (User.Identity.Name)
        // Value: ConnectionId (Mã kết nối SignalR)
        private static readonly ConcurrentDictionary<string, string> _onlineUsers
            = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Chạy khi có người truy cập vào trang web (đã đăng nhập)
        /// </summary>
        public override Task OnConnected()
        {
            string userIdentity = Context.User.Identity.Name; // Tên đăng nhập hoặc Email
            string connectionId = Context.ConnectionId;

            // 1. Lưu hoặc Cập nhật ConnectionId mới nhất của User này
            _onlineUsers.AddOrUpdate(userIdentity, connectionId, (key, oldValue) => connectionId);

            // 2. Phân loại: Nếu là Admin/Employee thì cho vào Group "Admins" để nhận thông báo
            if (Context.User.IsInRole("Admin") || Context.User.IsInRole("Employee"))
            {
                Groups.Add(connectionId, "Admins");
            }

            return base.OnConnected();
        }

        /// <summary>
        /// Chạy khi người dùng tắt trình duyệt hoặc mất mạng
        /// </summary>
        public override Task OnDisconnected(bool stopCalled)
        {
            string userIdentity = Context.User.Identity.Name;
            string ignored;
            // Xóa khỏi danh sách Online
            _onlineUsers.TryRemove(userIdentity, out ignored);

            return base.OnDisconnected(stopCalled);
        }

        // ============================================================
        // CHỨC NĂNG 1: KHÁCH HÀNG GỬI TIN NHẮN CHO ADMIN
        // ============================================================
        public void SendToAdmin(string message)
        {
            string senderEmail = Context.User.Identity.Name;

            try
            {
                // 1. LƯU VÀO DATABASE
                using (var db = new DULICHEntities())
                {
                    // Tìm MaKH dựa trên Email/Tên đăng nhập
                    // (Lưu ý: Cần đảm bảo Logic Login lưu Email vào Identity.Name, nếu lưu Tên thì sửa code tìm theo TenKH)
                    var khach = db.KhachHangs.FirstOrDefault(k => k.Email == senderEmail || k.TenKH == senderEmail);

                    var chatMsg = new ChatMessage
                    {
                        SenderId = senderEmail,
                        ReceiverId = "Admin", // Đánh dấu gửi cho hệ thống
                        Message = message,
                        Timestamp = DateTime.Now,
                        IsRead = false,
                        MaKH = khach != null ? khach.MaKH : (int?)null // Liên kết khóa ngoại nếu tìm thấy khách
                    };

                    db.ChatMessages.Add(chatMsg);
                    db.SaveChanges();
                }

                // 2. GỬI REALTIME
                // Gửi cho toàn bộ Admin đang Online
                Clients.Group("Admins").addNewMessageToAdmin(senderEmail, message);

                // Hiện lại tin nhắn ở máy người gửi (để họ thấy tin đã đi)
                Clients.Caller.addNewMessageToCustomer("Tôi", message);
            }
            catch (Exception ex)
            {
                // Nếu lỗi database, vẫn báo lỗi về cho client biết
                Clients.Caller.showNotification("Lỗi gửi tin: " + ex.Message);
            }
        }

        // ============================================================
        // CHỨC NĂNG 2: ADMIN TRẢ LỜI KHÁCH HÀNG
        // ============================================================
        public void ReplyToCustomer(string customerIdentity, string message)
        {
            string adminName = Context.User.Identity.Name; // Tên Admin đang trả lời

            try
            {
                // 1. LƯU VÀO DATABASE
                using (var db = new DULICHEntities())
                {
                    var khach = db.KhachHangs.FirstOrDefault(k => k.Email == customerIdentity || k.TenKH == customerIdentity);

                    var chatMsg = new ChatMessage
                    {
                        SenderId = adminName,     // Người gửi là Admin
                        ReceiverId = customerIdentity, // Người nhận là Khách
                        Message = message,
                        Timestamp = DateTime.Now,
                        IsRead = true, // Admin gửi thì coi như đã đọc
                        MaKH = khach != null ? khach.MaKH : (int?)null
                    };

                    db.ChatMessages.Add(chatMsg);
                    db.SaveChanges();
                }

                // 2. GỬI REALTIME
                // Tìm ConnectionId của khách hàng trong từ điển
                if (_onlineUsers.TryGetValue(customerIdentity, out string customerConnectionId))
                {
                    // Gửi riêng cho khách hàng đó
                    Clients.Client(customerConnectionId).addNewMessageToCustomer("Hỗ trợ viên", message);

                    // Báo lại cho Admin là đã gửi thành công (Hiện lên màn hình chat admin)
                    Clients.Caller.addNewMessageToAdmin($"Tôi (trả lời {customerIdentity})", message);
                }
                else
                {
                    // Khách hàng đã thoát mạng -> Chỉ lưu DB, báo Admin biết
                    Clients.Caller.showNotification($"Đã lưu tin nhắn, nhưng khách hàng {customerIdentity} hiện không Online.");
                    // Vẫn hiện tin nhắn lên khung chat của Admin để lịch sử liền mạch
                    Clients.Caller.addNewMessageToAdmin($"Tôi (Offline-msg tới {customerIdentity})", message);
                }
            }
            catch (Exception ex)
            {
                Clients.Caller.showNotification("Lỗi gửi tin: " + ex.Message);
            }
        }
    }
}