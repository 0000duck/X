using System;
using System.Linq;
using System.IO;
using System.IO.Compression;
using NewLife.Threading;
using NewLife.Web;
using NewLife.Log;
using System.Diagnostics;
using System.Net;

namespace NewLife.IP
{
    /// <summary>IP����</summary>
    public static class Ip
    {
        private static object lockHelper = new object();
        private static Zip zip;

        private static String _DbFile;
        /// <summary>�����ļ�</summary>
        public static String DbFile { get { return _DbFile; } set { _DbFile = value; zip = null; } }

        static Ip()
        {
            var ns = new String[] { "qqwry.dat", "qqwry.gz", "ip.gz", "ip.gz.config", "ipdata.config" };
            foreach (var item in ns)
            {
                var fs = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, item, SearchOption.AllDirectories);
                if (fs != null && fs.Length > 0)
                {
                    _DbFile = Path.GetFullPath(fs[0]);
                    break;
                }
            }

            // �������û��IP���ݿ⣬�����������
            if (_DbFile.IsNullOrWhiteSpace())
            {
                ThreadPoolX.QueueUserWorkItem(() =>
                {
                    var url = "http://www.newlifex.com/showtopic-51.aspx";
                    XTrace.WriteLine("û���ҵ�IP���ݿ⣬׼��������ȡ {0}", url);

                    var client = new WebClientX();
                    client.Log = XTrace.Log;
                    var dir = Runtime.IsWeb ? "App_Data" : "Data";
                    var file = client.DownloadLink(url, "ip.gz", dir.GetFullPath());
                });
            }
        }

        static Boolean? _inited;
        static Boolean Init()
        {
            if (_inited != null) return _inited.Value;
            lock (typeof(Ip))
            {
                if (_inited != null) return _inited.Value;

                var z = new Zip();

                if (!File.Exists(_DbFile))
                {
                    //throw new InvalidOperationException("�޷��ҵ�IP���ݿ�" + _DbFile + "��");
                    XTrace.WriteLine("�޷��ҵ�IP���ݿ�{0}", _DbFile);
                    return false;
                }
                using (var fs = File.OpenRead(_DbFile))
                {
                    try
                    {
                        z.SetStream(fs);
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteException(ex);
                        _inited = false;
                        return false;
                    }
                }
                zip = z;
            }

            if (zip.Stream == null) throw new InvalidOperationException("�޷���IP���ݿ�" + _DbFile + "��");
            _inited = true;
            return true;
        }

        /// <summary>��ȡIP��ַ</summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static String GetAddress(String ip)
        {
            if (String.IsNullOrEmpty(ip)) return "";

            if (!Init()) return "";

            var ip2 = IPToUInt32(ip.Trim());
            lock (lockHelper)
            {
                return zip.GetAddress(ip2) + "";
            }
        }

        /// <summary>��ȡIP��ַ</summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public static String GetAddress(IPAddress addr)
        {
            if (addr == null) return "";

            if (!Init()) return "";

            var ip2 = (UInt32)addr.GetAddressBytes().Reverse().ToInt();
            lock (lockHelper)
            {
                return zip.GetAddress(ip2) + "";
            }
        }

        static uint IPToUInt32(String IpValue)
        {
            var ss = IpValue.Split('.');
            var buf = new Byte[4];
            for (int i = 0; i < 4; i++)
            {
                var n = 0;
                if (i < ss.Length && Int32.TryParse(ss[i], out n))
                {
                    buf[3 - i] = (Byte)n;
                }
            }
            return BitConverter.ToUInt32(buf, 0);
        }
    }

    class MyIpProvider : NetHelper.IpProvider
    {
        public string GetAddress(IPAddress addr)
        {
            return Ip.GetAddress(addr);
        }
    }
}