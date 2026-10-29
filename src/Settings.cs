// =============================================================================
// RDS BU Splice - thong so (giai doan macro luu file ini; plugin sau nay dung StructuresField cung ten).
// =============================================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace RDS.BUSplice
{
    /// <summary>Thong tin part. Class rong = lay theo web rafter. Finish LUON theo web rafter (part han vao rafter).</summary>
    public sealed class PartInfo
    {
        public string Name = "", Prefix = "f", Material = "250", Class = "";
        public int Start = 1;
    }

    public sealed class Settings
    {
        // ---------- Endplate 1 (rafter nho) ----------
        public double Ep1T = 12;
        public double Ep1Top = 45, Ep1Bot = 45;                  // mep tinh tu bolt ngoai cung
        // ---------- Endplate 2 (rafter lon) ----------
        public double Ep2T = 12;
        public double Ep2Top = 45, Ep2Bot = 45;
        // ---------- Mep trai / phai: DUNG CHUNG cho 2 endplate ----------
        public double EpLeft = 45, EpRight = 45;

        // ---------- Bolt (tham chieu Joining plates 14) ----------
        public int Flip = 0;                        // 1 = dao flange "top" (cot dung / muon lat phia flange (1), stiff, goc bolt)
        public int BoltVertRef = 0;                 // 0 = Top: BoltTopOffset = tam hang tren cung -> top rafter nho
                                                    // 1 = Middle: BoltTopOffset = tam nhom bolt -> giua chieu cao rafter nho
        public double BoltTopOffset = 56;           // (duong = phia tren)
        public string BoltDistDown = "125 600 170 125";   // khoang cach cac hang, tinh xuong
        public string BoltDistAcross = "140";        // khoang cach cac cot (doi xung quanh tim web)
        public string BoltDelete = "";               // so bolt bi xoa, vd "1 2" (hang tren -> duoi, trai -> phai)
        public double BoltSize = 20;
        public string BoltStandard = "8.8S";
        public double BoltTolerance = 2;

        // ---------- Flange (1) ----------
        public bool FlangeOn = true;
        public string FlangeProfile = "PLT20*230";
        public double FlangeLength = 600;            // tu mat ngoai endplate 2, doc top flange rafter lon

        // ---------- Stiffener (2) ----------
        public bool StiffOn = true;
        public double StiffT = 16;
        public double StiffB = 0;                    // 0 = tu tinh: tu mat web toi mep flange rafter lon
        public double StiffL = 300;
        public double StiffChamX = 15, StiffChamY = 15;

        // ---------- Part info ----------
        public PartInfo PEndplate = new PartInfo { Name = "END PLATE" };
        public PartInfo PFlange = new PartInfo { Name = "FLANGE PLATE" };
        public PartInfo PStiff = new PartInfo { Name = "STIFFENER" };

        // ================= luu / doc ini (key=value, InvariantCulture) =================
        public static string DefaultPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Tekla", "RDS-BUSplice.ini");

        public void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var sb = new StringBuilder();
            foreach (var kv in Flatten()) sb.Append(kv.Key).Append('=').Append(kv.Value).Append("\r\n");
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }

        public static Settings Load(string path)
        {
            var s = new Settings();
            if (!File.Exists(path)) return s;
            foreach (string line in File.ReadAllLines(path))
            {
                int k = line.IndexOf('=');
                if (k <= 0) continue;
                try { s.SetValue(line.Substring(0, k).Trim(), line.Substring(k + 1)); } catch { /* bo qua key hong */ }
            }
            return s;
        }

        /// <summary>Tat ca field -> "Ten" / "PEndplate.Name" ... = chuoi.</summary>
        public List<KeyValuePair<string, string>> Flatten()
        {
            var r = new List<KeyValuePair<string, string>>();
            foreach (FieldInfo f in typeof(Settings).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                object v = f.GetValue(this);
                if (v is PartInfo pi)
                {
                    foreach (FieldInfo g in typeof(PartInfo).GetFields(BindingFlags.Public | BindingFlags.Instance))
                        r.Add(new KeyValuePair<string, string>(f.Name + "." + g.Name, ToStr(g.GetValue(pi))));
                }
                else r.Add(new KeyValuePair<string, string>(f.Name, ToStr(v)));
            }
            return r;
        }

        public string GetValue(string key)
        {
            foreach (var kv in Flatten()) if (kv.Key == key) return kv.Value;
            return null;
        }

        public void SetValue(string key, string val)
        {
            object target = this; Type t = typeof(Settings); string name = key;
            int dot = key.IndexOf('.');
            if (dot > 0)
            {
                FieldInfo pf = t.GetField(key.Substring(0, dot));
                if (pf == null) return;
                target = pf.GetValue(this); t = typeof(PartInfo); name = key.Substring(dot + 1);
            }
            FieldInfo f = t.GetField(name);
            if (f == null) return;
            f.SetValue(target, FromStr(f.FieldType, val));
        }

        static string ToStr(object v)
        {
            if (v is double d) return d.ToString("0.###", CultureInfo.InvariantCulture);
            if (v is bool b) return b ? "1" : "0";
            return Convert.ToString(v, CultureInfo.InvariantCulture) ?? "";
        }

        static object FromStr(Type t, string s)
        {
            s = (s ?? "").Trim();
            if (t == typeof(double)) return double.Parse(s.Replace(',', '.'), CultureInfo.InvariantCulture);
            if (t == typeof(int)) return int.Parse(s, CultureInfo.InvariantCulture);
            if (t == typeof(bool)) return s == "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase);
            return s;
        }
    }
}
