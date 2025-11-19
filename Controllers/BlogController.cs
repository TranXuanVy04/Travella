using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Trave.Models;

namespace Trave.Controllers
{
    public class BlogController : Controller
    {
        private DULICHEntities db = new DULICHEntities();

        // GET: Blog
        public ActionResult Index(string search) // Thêm tham số search để hỗ trợ tìm kiếm
        {
            // Bắt đầu truy vấn
            var blogQuery = db.BaiViets.Include(b => b.DanhMucBlog).AsQueryable();

            // Áp dụng bộ lọc tìm kiếm (tái sử dụng logic đã sửa trước đó)
            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim().ToLower();
                blogQuery = blogQuery.Where(b =>
                    b.TieuDe.ToLower().Contains(search) ||
                    b.TomTat.ToLower().Contains(search) ||
                    b.Tacgia.ToLower().Contains(search)
                );
            }

            // Sắp xếp và thực thi truy vấn
            var model = blogQuery.OrderByDescending(b => b.NgayDang).ToList();
            return View(model);
        }

        // GET: Blog/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Tải Bài viết, bao gồm Danh mục. Sử dụng AsNoTracking() để ngăn EF theo dõi
            // đối tượng này ngay từ đầu, tránh các vấn đề tracking khi update.
            BaiViet baiViet = db.BaiViets
                                .Include(b => b.DanhMucBlog)
                                .AsNoTracking() // <--- TỐI ƯU HÓA 1: Không theo dõi
                                .SingleOrDefault(b => b.MaBaiViet == id);

            if (baiViet == null)
            {
                return HttpNotFound();
            }

            // -----------------------------------------------------------
            // TỐI ƯU HÓA 2: Chỉ cập nhật duy nhất trường LuotXem
            // -----------------------------------------------------------

            try
            {
                // 1. Tăng lượt xem
                int newLuotXem = (baiViet.LuotXem ?? 0) + 1;

                // 2. Tạo đối tượng proxy để update
                var counter = new BaiViet { MaBaiViet = baiViet.MaBaiViet };
                db.BaiViets.Attach(counter);

                // 3. Gán giá trị mới và đánh dấu chỉ LuotXem bị sửa đổi
                counter.LuotXem = newLuotXem;
                db.Entry(counter).Property(x => x.LuotXem).IsModified = true;

                db.SaveChanges();

                // Quan trọng: Cập nhật lại đối tượng đang được dùng cho View
                baiViet.LuotXem = newLuotXem;
            }
            catch (Exception ex)
            {
                // Xử lý lỗi (Ví dụ: Ghi log) nhưng vẫn trả về View để người dùng xem bài
                // Tránh lỗi 500 nếu cập nhật lượt xem thất bại
                Console.WriteLine("Lỗi khi cập nhật lượt xem: " + ex.Message);
            }

            // -----------------------------------------------------------

            return View(baiViet);
        }
    }
}
