using System;
using System.Web;
using System.Linq;
using Trave.Models;
using Newtonsoft.Json;

namespace Trave
{
    public class TourData : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);

            try
            {
                using (var db = new DULICHEntities())
                {
                    var tours = db.Tours.Select(t => new
                    {
                        MaTour = t.MaTour,
                        TenTour = t.TenTour ?? "Chưa đặt tên",
                        Gia = t.Gia  ,
                        PhuongTien = t.PhuongTien ?? "Tự túc",
                        MoTa = t.mota ?? "",
                        guide = t.guide == null ? "" : t.guide.tenguide ?? "",
                        thoigian = t.ThoiGian ?? "",

                    }).ToList();

                    string json = JsonConvert.SerializeObject(tours);
                    context.Response.Write(json);
                }
            }
            catch
            {
                context.Response.Write("[]");
            }
        }

        public bool IsReusable => false;
    }
}
