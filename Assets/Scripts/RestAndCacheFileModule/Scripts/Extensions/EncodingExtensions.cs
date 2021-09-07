using System.Security.Cryptography;
using System.Text;

namespace CarScan.Caching
{
    public static class Extensions
    {
        private static MD5 _md5;

        public static byte[] GetBytes(this string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        public static string GetString(this byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }
    }
}