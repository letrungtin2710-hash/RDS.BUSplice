// =============================================================================
// RDS BU Splice - toan thuan (KHONG phu thuoc Tekla) -> test duoc bang mono.
// Quy uoc he toa do local cua splice (Frame):
//   X = truc rafter NHO, huong ve rafter LON
//   Y = len, nam trong mat phang web
//   Z = X x Y (= ben PHAI khi dung o rafter nho nhin sang rafter lon)
// Moi duong "2D" la duong trong mat phang (X,Y) local.
// =============================================================================
using System;
using System.Collections.Generic;
using System.Globalization;

namespace RDS.BUSplice
{
    /// <summary>Vector 3D toi gian (khong dung Tekla Point de test offline).</summary>
    public struct V3
    {
        public double X, Y, Z;
        public V3(double x, double y, double z) { X = x; Y = y; Z = z; }
        public static V3 operator +(V3 a, V3 b) => new V3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static V3 operator -(V3 a, V3 b) => new V3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static V3 operator *(V3 a, double k) => new V3(a.X * k, a.Y * k, a.Z * k);
        public double Dot(V3 b) => X * b.X + Y * b.Y + Z * b.Z;
        public V3 Cross(V3 b) => new V3(Y * b.Z - Z * b.Y, Z * b.X - X * b.Z, X * b.Y - Y * b.X);
        public double Len => Math.Sqrt(Dot(this));
        public V3 Unit { get { double l = Len; return l < 1e-12 ? this : this * (1.0 / l); } }
        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "({0:0.#},{1:0.#},{2:0.#})", X, Y, Z);
    }

    /// <summary>Diem / vector 2D trong mat phang (X,Y) local.</summary>
    public struct V2
    {
        public double X, Y;
        public V2(double x, double y) { X = x; Y = y; }
        public static V2 operator +(V2 a, V2 b) => new V2(a.X + b.X, a.Y + b.Y);
        public static V2 operator -(V2 a, V2 b) => new V2(a.X - b.X, a.Y - b.Y);
        public static V2 operator *(V2 a, double k) => new V2(a.X * k, a.Y * k);
        public double Dot(V2 b) => X * b.X + Y * b.Y;
        public double Len => Math.Sqrt(X * X + Y * Y);
        public V2 Unit { get { double l = Len; return l < 1e-12 ? this : this * (1.0 / l); } }
        /// <summary>Phap tuyen quay trai 90 do (huong "len" khi vector di ve +X).</summary>
        public V2 Left => new V2(-Y, X);
        public V2 Rotate(double a) { double c = Math.Cos(a), s = Math.Sin(a); return new V2(X * c - Y * s, X * s + Y * c); }
        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "({0:0.#},{1:0.#})", X, Y);
    }

    /// <summary>He toa do local cua splice.</summary>
    public sealed class Frame
    {
        public V3 O, X, Y, Z;

        /// <summary>
        /// Dung frame tu truc rafter (axis) + diem goc. Gia dinh rafter nam trong mat phang dung:
        /// Z = X x Zglobal (ngang), Y = Z x X (len).
        /// </summary>
        public static Frame FromAxis(V3 origin, V3 axis)
        {
            V3 x = axis.Unit;
            V3 n = x.Cross(new V3(0, 0, 1));
            if (n.Len < 1e-6) throw new InvalidOperationException("Rafter dung thang dung - khong ho tro");
            n = n.Unit;
            V3 y = n.Cross(x).Unit;           // len
            V3 z = x.Cross(y).Unit;           // = n
            return new Frame { O = origin, X = x, Y = y, Z = z };
        }

        public V3 ToLocal(V3 p) { V3 d = p - O; return new V3(d.Dot(X), d.Dot(Y), d.Dot(Z)); }
        public V3 ToGlobal(double x, double y, double z) => O + X * x + Y * y + Z * z;
        public V3 ToGlobal(V2 p, double z) => ToGlobal(p.X, p.Y, z);
        /// <summary>Vector 2D local -> vector global (z = 0).</summary>
        public V3 Dir(V2 d) => X * d.X + Y * d.Y;
    }

    /// <summary>Duong thang 2D dang n.p = c (n don vi, ny > 0 = phia "tren").</summary>
    public struct Line2
    {
        public V2 N;      // phap tuyen don vi, huong len
        public double C;
        public V2 Dir => new V2(N.Y, -N.X);   // huong +X

        public static Line2 Through(V2 p, V2 dir)
        {
            V2 d = dir.X < 0 ? dir * -1 : dir;
            V2 n = d.Unit.Left;
            return new Line2 { N = n, C = n.Dot(p) };
        }
        /// <summary>Cao do Y cua duong tai hoanh do x.</summary>
        public double YAt(double x)
        {
            if (Math.Abs(N.Y) < 1e-9) throw new InvalidOperationException("Duong thang dung");
            return (C - N.X * x) / N.Y;
        }
        public V2 At(double x) => new V2(x, YAt(x));
        /// <summary>Doi duong theo phap tuyen (duong = len).</summary>
        public Line2 Offset(double d) => new Line2 { N = N, C = C + d };
        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "n={0} c={1:0.#}", N, C);
    }

    /// <summary>Mat tren / mat duoi cua 1 tam flange trong mat phang (X,Y) local.</summary>
    public sealed class FlangeLines
    {
        public Line2 Top, Bottom;
        public double ZMin, ZMax;
        public double Thickness => Top.C - Bottom.C;
        public double Width => ZMax - ZMin;
        public double YMid(double x) => 0.5 * (Top.YAt(x) + Bottom.YAt(x));
    }

    public static class Geo
    {
        /// <summary>Truc chinh (PCA) cua dam may diem 3D - power iteration tren ma tran hiep phuong sai.</summary>
        public static V3 Principal(IList<V3> pts)
        {
            if (pts.Count < 2) throw new ArgumentException("Can >= 2 diem");
            V3 c = Centroid(pts);
            double xx = 0, xy = 0, xz = 0, yy = 0, yz = 0, zz = 0;
            foreach (V3 p in pts)
            {
                V3 d = p - c;
                xx += d.X * d.X; xy += d.X * d.Y; xz += d.X * d.Z;
                yy += d.Y * d.Y; yz += d.Y * d.Z; zz += d.Z * d.Z;
            }
            // vector khoi dau = diem xa tam nhat (tranh truong hop truc giao voi truc chinh)
            V3 v = new V3(1, 0, 0); double best = -1;
            foreach (V3 p in pts) { double l = (p - c).Len; if (l > best) { best = l; v = p - c; } }
            v = v.Unit;
            for (int i = 0; i < 100; i++)
            {
                V3 w = new V3(xx * v.X + xy * v.Y + xz * v.Z, xy * v.X + yy * v.Y + yz * v.Z, xz * v.X + yz * v.Y + zz * v.Z);
                if (w.Len < 1e-12) break;
                v = w.Unit;
            }
            return v;
        }

        public static V3 Centroid(IList<V3> pts)
        {
            V3 s = new V3();
            foreach (V3 p in pts) s = s + p;
            return s * (1.0 / pts.Count);
        }

        /// <summary>Truc chinh 2D (PCA) cua cac diem trong mat phang (X,Y), huong +X.</summary>
        public static V2 Principal2(IList<V2> pts)
        {
            double mx = 0, my = 0;
            foreach (V2 p in pts) { mx += p.X; my += p.Y; }
            mx /= pts.Count; my /= pts.Count;
            double sxx = 0, sxy = 0, syy = 0;
            foreach (V2 p in pts) { double dx = p.X - mx, dy = p.Y - my; sxx += dx * dx; sxy += dx * dy; syy += dy * dy; }
            double a = 0.5 * Math.Atan2(2 * sxy, sxx - syy);
            V2 d = new V2(Math.Cos(a), Math.Sin(a));
            return d.X < 0 ? d * -1 : d;
        }

        /// <summary>Tu dinh solid (da doi sang local) cua 1 tam flange -> mat tren/duoi + be rong theo Z.</summary>
        public static FlangeLines Flange(IList<V3> localPts)
        {
            var p2 = new List<V2>(localPts.Count);
            double zmin = double.MaxValue, zmax = double.MinValue;
            foreach (V3 p in localPts) { p2.Add(new V2(p.X, p.Y)); zmin = Math.Min(zmin, p.Z); zmax = Math.Max(zmax, p.Z); }
            V2 d = Principal2(p2);
            V2 n = d.Left;
            double cmin = double.MaxValue, cmax = double.MinValue;
            foreach (V2 p in p2) { double c = n.Dot(p); cmin = Math.Min(cmin, c); cmax = Math.Max(cmax, c); }
            return new FlangeLines
            {
                Top = new Line2 { N = n, C = cmax },
                Bottom = new Line2 { N = n, C = cmin },
                ZMin = zmin, ZMax = zmax
            };
        }

        /// <summary>
        /// Duong mat TREN tam flange nghieng: di qua A (mat tren, dau endplate), mat DUOI di qua B (dau xa),
        /// be day t. Tra ve huong d (+X) cua tam. B phai nam duoi duong.
        /// </summary>
        public static V2 PlateDirThrough(V2 a, V2 bBottom, double t)
        {
            V2 v = bBottom - a;
            double r = v.Len;
            if (r <= t + 1e-6) throw new InvalidOperationException("Tam flange qua ngan so voi be day");
            double phi = Math.Asin(t / r);
            return v.Unit.Rotate(phi);   // quay nguoc kim dong ho -> B nam duoi duong qua A
        }

        /// <summary>
        /// Doc list khoang cach kieu Tekla: "125 600 170", "3*100 50", phan cach space / ; / ,.
        /// </summary>
        public static List<double> ParseList(string s)
        {
            var r = new List<double>();
            if (string.IsNullOrWhiteSpace(s)) return r;
            foreach (string tok in s.Split(new[] { ' ', ';', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int k = tok.IndexOf('*');
                if (k > 0)
                {
                    int n = int.Parse(tok.Substring(0, k), CultureInfo.InvariantCulture);
                    double v = double.Parse(tok.Substring(k + 1), CultureInfo.InvariantCulture);
                    for (int i = 0; i < n; i++) r.Add(v);
                }
                else r.Add(double.Parse(tok, CultureInfo.InvariantCulture));
            }
            return r;
        }

        /// <summary>Vi tri cac cot bolt theo Z, doi xung quanh 0 (giong dist Y cua Tekla).</summary>
        public static List<double> CenteredPositions(IList<double> spacings)
        {
            var r = new List<double> { 0 };
            foreach (double s in spacings) r.Add(r[r.Count - 1] + s);
            double half = r[r.Count - 1] / 2;
            for (int i = 0; i < r.Count; i++) r[i] -= half;
            return r;
        }

        /// <summary>Doc profile PLTt*b / PLt*b -> t, b.</summary>
        public static bool ParsePlate(string prof, out double t, out double b)
        {
            t = b = 0;
            if (string.IsNullOrWhiteSpace(prof)) return false;
            string s = prof.Trim().ToUpperInvariant();
            int i = 0;
            while (i < s.Length && char.IsLetter(s[i])) i++;
            string[] parts = s.Substring(i).Split('*', 'X');
            if (parts.Length != 2) return false;
            return double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out t)
                && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out b)
                && t > 0 && b > 0;
        }
    }
}
