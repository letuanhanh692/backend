using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace BEPrj3.Libaries
{
    public class VnPayLibrary
    {
        private readonly SortedList<string, string> _requestData = new SortedList<string, string>(); // Dữ liệu gửi đi
        private readonly SortedList<string, string> _responseData = new SortedList<string, string>(); // Dữ liệu phản hồi

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData[key] = value;
            }
        }

        public string CreateRequestUrl(string baseUrl, string hashSecret)
        {
            StringBuilder data = new StringBuilder();
            foreach (KeyValuePair<string, string> kv in _requestData)
            {
                if (data.Length > 0) data.Append('&');
                data.Append(HttpUtility.UrlEncode(kv.Key) + "=" + HttpUtility.UrlEncode(kv.Value));
            }

            string rawData = data.ToString();
            string secureHash = ComputeHmacSHA512(hashSecret, rawData);
            return $"{baseUrl}?{rawData}&vnp_SecureHash={secureHash}";
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _responseData[key] = value;
            }
        }

        public bool ValidateSignature(string hashSecret)
        {
            if (!_responseData.ContainsKey("vnp_SecureHash")) return false;

            string receivedHash = _responseData["vnp_SecureHash"];
            _responseData.Remove("vnp_SecureHash");

            StringBuilder data = new StringBuilder();
            foreach (KeyValuePair<string, string> kv in _responseData)
            {
                if (data.Length > 0) data.Append('&');
                data.Append(HttpUtility.UrlEncode(kv.Key) + "=" + HttpUtility.UrlEncode(kv.Value));
            }

            string computedHash = ComputeHmacSHA512(hashSecret, data.ToString());
            return receivedHash.Equals(computedHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private static string ComputeHmacSHA512(string key, string data)
        {
            using (HMACSHA512 hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
    }
}
