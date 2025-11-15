using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

public class VnPayLibrary
{
    private SortedList<string, string> _requestData = new SortedList<string, string>(new VnPayCompare());
    private SortedList<string, string> _responseData = new SortedList<string, string>(new VnPayCompare());

    public void AddRequestData(string key, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _requestData.Add(key, value);
        }
    }

    public void AddResponseData(string key, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _responseData.Add(key, value);
        }
    }

    public string GetResponseData(string key)
    {
        return _responseData.TryGetValue(key, out var retValue) ? retValue : string.Empty;
    }

    public string CreateRequestUrl(string baseUrl, string vnp_HashSecret)
    {
        StringBuilder data = new StringBuilder();
        StringBuilder query = new StringBuilder();

        foreach (KeyValuePair<string, string> kv in _requestData)
        {
            if (!string.IsNullOrEmpty(kv.Value))
            {
                // Xây dựng chuỗi hash (dữ liệu thô hoặc encode tùy version, VNPay 2.1.0 thường dùng data đã encode)
                // Để an toàn nhất, chúng ta xây dựng query string chuẩn
                query.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");

                // Với Hash, VNPay yêu cầu nối liền không encode hoặc encode tùy trường hợp. 
                // Cách phổ biến nhất hoạt động trên Sandbox hiện tại:
                data.Append(kv.Key + "=" + WebUtility.UrlEncode(kv.Value) + "&");
            }
        }

        // Xóa dấu & cuối cùng
        string queryString = query.ToString();
        string rawData = data.ToString();
        if (queryString.Length > 0)
        {
            queryString = queryString.Remove(queryString.Length - 1, 1);
        }
        if (rawData.Length > 0)
        {
            rawData = rawData.Remove(rawData.Length - 1, 1);
        }

        // Tạo checksum
        string vnp_SecureHash = Utils.HmacSHA512(vnp_HashSecret, rawData);

        return baseUrl + "?" + queryString + "&vnp_SecureHash=" + vnp_SecureHash;
    }

    public bool ValidateSignature(string inputHash, string vnp_HashSecret)
    {
        StringBuilder data = new StringBuilder();
        foreach (KeyValuePair<string, string> kv in _responseData)
        {
            if (kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
            {
                data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
            }
        }
        string rawData = data.ToString();
        if (rawData.Length > 0)
            rawData = rawData.Remove(rawData.Length - 1, 1);

        string myChecksum = Utils.HmacSHA512(vnp_HashSecret, rawData);
        return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
    }
}

public class VnPayCompare : IComparer<string>
{
    public int Compare(string x, string y)
    {
        if (x == y) return 0;
        if (x == null) return -1;
        if (y == null) return 1;
        var vnpCompare = CompareInfo.GetCompareInfo("en-US");
        return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
    }
}

public class Utils
{
    public static string HmacSHA512(string key, string inputData)
    {
        var hash = new StringBuilder();
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
        using (var hmac = new HMACSHA512(keyBytes))
        {
            byte[] hashValue = hmac.ComputeHash(inputBytes);
            foreach (var theByte in hashValue)
            {
                hash.Append(theByte.ToString("x2"));
            }
        }
        return hash.ToString();
    }

    public static string GetIpAddress()
    {
        // Hardcode IP để tránh lỗi khi chạy localhost (IPv6 ::1 thường gây lỗi checksum trên sandbox)
        return "127.0.0.1";
    }
}