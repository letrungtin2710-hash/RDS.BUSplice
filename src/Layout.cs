// =============================================================================
// RDS BU Splice - tinh toan bo hinh hoc trong he local (KHONG goi Tekla) -> test offline.
// Engine.cs chi doi ket qua o day sang object Tekla.
// =============================================================================
using System;
using System.Collections.Generic;
using System.Globalization;

namespace RDS.BUSplice
{
    /// <summary>Hinh hoc 1 rafter BU trong he local cua splice.</summary>
    public sealed class MemberGeo
    {
        public FlangeLines Top, Bot;     // top flange, bottom flange
        public double WebZc, WebT;       // tim web theo Z, be day web
        public double XMax;              // diem xa nhat theo +X (dau rafter phia +X)

        /// <summary>Top cua top flange tai x.</summary>
        public double TopY(double x) => Top.Top.YAt(x);
        /// <summary>Bottom cua bottom flange tai x.</summary>
        public double BotY(double x) => Bot.Bottom.YAt(x);
        public double Height(double x) => TopY(x) - BotY(x);
        public double FlangeHalfWidth => Math.Max(Top.Width, Bot.Width) / 2;
    }

    /// <summary>Tam chu nhat nam trong mat phang x = const (endplate).</summary>
    public sealed class RectPlate
    {
        public double X, T, YTop, YBot, ZLeft, ZRight;
        public V3[] Corners => new[]
        {
            new V3(X, YTop, ZLeft), new V3(X, YTop, ZRight), new V3(X, YBot, ZRight), new V3(X, YBot, ZLeft)
        };
    }

    public sealed class StiffPlate
    {
        public V3[] Pts;   // Pts[0] = goc giap web + endplate (co chamfer)
        public double T;
    }

    public sealed class SpliceLayout
    {
        public double X0, FitSmallX, FitLargeX;        // mat splice, mat fitting rafter nho / lon
        public List<double> BoltY = new List<double>(), BoltZ = new List<double>();
        public List<int> BoltDeleted = new List<int>();     // so thu tu bolt bi xoa (1-based)
        public List<V2> BoltKeep = new List<V2>();          // bolt con lai: X = Y local (hang), Y = Z local (cot)
        public RectPlate Ep1, Ep2;

        // Flange (1)
        public bool HasFlange;
        public double FlT, FlB;
        public V2 FlStart, FlEnd;          // duong tim (giua be day) cua tam; 2 dau cat VUONG theo phuong tam
        public double FlZ;                 // tim tam theo Z (= tim web rafter lon)
        public V2 FarPoint, FarNormal;     // mat F: qua FarPoint, phap tuyen FarNormal (doc top flange rafter lon)
        public V2 FlDir;
        public List<V2> WebCut = new List<V2>();   // polygon cat web rafter lon (trong mat phang web)

        // Stiffener (2)
        public bool HasStiff;
        public StiffPlate[] Stiffs = new StiffPlate[0];

        public List<string> Warnings = new List<string>();

        /// <summary>Rafter B co phai "lon" hon A tai x (A3: cao hon; bang nhau +-2 thi top cao hon).</summary>
        public static bool IsLarger(MemberGeo b, MemberGeo a, double x)
        {
            double hb = b.Height(x), ha = a.Height(x);
            if (Math.Abs(hb - ha) > 2.0) return hb > ha;
            return b.TopY(x) > a.TopY(x);
        }

        public static SpliceLayout Compute(Settings p, MemberGeo S, MemberGeo L)
        {
            var r = new SpliceLayout();
            r.X0 = S.XMax;
            r.FitSmallX = r.X0 - p.Ep1T;
            r.FitLargeX = r.X0 + p.Ep2T;
            double x0 = r.X0, s2 = r.FitLargeX;

            // ---------------- Bolt ----------------
            // Top: tinh tu top rafter nho. Middle: tam nhom bolt = giua chieu cao rafter nho (+ offset), giong Joining plates (14).
            var down = Geo.ParseList(p.BoltDistDown);
            double y;
            if (p.BoltVertRef == 1)
            {
                double h = 0; foreach (double dd in down) h += dd;
                y = 0.5 * (S.TopY(x0) + S.BotY(x0)) + p.BoltTopOffset + h / 2;
            }
            else y = S.TopY(x0) + p.BoltTopOffset;
            r.BoltY.Add(y);
            foreach (double d in down) { y -= d; r.BoltY.Add(y); }
            foreach (double z in Geo.CenteredPositions(Geo.ParseList(p.BoltDistAcross))) r.BoltZ.Add(S.WebZc + z);

            // Bolt bi xoa (kieu Joining plates 14): danh so theo hang tu TREN xuong, trong hang tu TRAI sang PHAI
            // (trai = phia -Z, dung o rafter nho nhin sang rafter lon). Kich thuoc endplate van tinh theo luoi day du.
            int nc = r.BoltZ.Count, nTotal = r.BoltY.Count * nc;
            foreach (double v in Geo.ParseList(p.BoltDelete))
            {
                int k = (int)Math.Round(v);
                if (k < 1 || k > nTotal) { r.Warnings.Add("Bolt xoa so " + k + " ngoai luoi 1.." + nTotal); continue; }
                if (!r.BoltDeleted.Contains(k)) r.BoltDeleted.Add(k);
            }
            for (int i = 0; i < r.BoltY.Count; i++)
                for (int j = 0; j < nc; j++)
                    if (!r.BoltDeleted.Contains(i * nc + j + 1)) r.BoltKeep.Add(new V2(r.BoltY[i], r.BoltZ[j]));
            if (r.BoltKeep.Count == 0) r.Warnings.Add("Da xoa het bolt");

            double bTop = r.BoltY[0], bBot = r.BoltY[r.BoltY.Count - 1];
            double bL = r.BoltZ[0], bR = r.BoltZ[r.BoltZ.Count - 1];

            // ---------------- Endplate (mep tinh tu bolt ngoai cung, KHONG vat) ----------------
            r.Ep1 = new RectPlate { X = x0 - p.Ep1T / 2, T = p.Ep1T, YTop = bTop + p.Ep1Top, YBot = bBot - p.Ep1Bot, ZLeft = bL - p.EpLeft, ZRight = bR + p.EpRight };
            r.Ep2 = new RectPlate { X = x0 + p.Ep2T / 2, T = p.Ep2T, YTop = bTop + p.Ep2Top, YBot = bBot - p.Ep2Bot, ZLeft = bL - p.EpLeft, ZRight = bR + p.EpRight };

            if (r.Ep1.YTop < S.TopY(x0) - 0.5 || r.Ep1.YBot > S.BotY(x0) + 0.5)
                r.Warnings.Add("Endplate 1 khong phu het chieu cao rafter nho");
            double lTopAtJoint = p.FlangeOn ? S.TopY(x0) : L.TopY(x0);
            if (r.Ep2.YTop < lTopAtJoint - 0.5 || r.Ep2.YBot > L.BotY(x0) + 0.5)
                r.Warnings.Add("Endplate 2 khong phu het chieu cao rafter lon");

            // ---------------- Flange (1) ----------------
            if (p.FlangeOn)
            {
                double t, b;
                if (!Geo.ParsePlate(p.FlangeProfile, out t, out b))
                    throw new InvalidOperationException("Profile flange sai: " + p.FlangeProfile + " (vd PLT20*230)");
                if (p.FlangeLength <= t) throw new InvalidOperationException("Chieu dai flange qua ngan");
                r.HasFlange = true; r.FlT = t; r.FlB = b; r.FlZ = L.WebZc;

                V2 dLT = L.Top.Top.Dir;                       // doc top flange rafter lon (+X)
                V2 q = L.Top.Bottom.At(s2);                   // mat duoi top flange lon tai mat endplate 2
                r.FarPoint = q + dLT * p.FlangeLength;        // = bottom tam (1) tai dau xa
                r.FarNormal = dLT;
                V2 a = S.Top.Top.At(s2);                      // top tam (1) tai mat endplate = top rafter nho
                V2 d = Geo.PlateDirThrough(a, r.FarPoint, t);
                V2 n = d.Left;
                r.FlDir = d;
                // Tam (1) CAT VUONG theo phuong cua no (Snit 26-Oct): KHONG fitting theo mat endplate / mat F.
                // Dau endplate: dich dau vuong toi khi ca 2 goc nam ngoai mat endplate (x >= s2), goc gan nhat cham mat.
                // Dau xa: goc duoi dau vuong = FarPoint (bottom tam = bottom top flange lon).
                double u0 = d.X > 1e-9 ? Math.Max(0, -d.Y * t / d.X) : 0;
                double u1 = (r.FarPoint - a).Dot(d);
                if (u1 - u0 <= t) throw new InvalidOperationException("Chieu dai flange (1) qua ngan");
                r.FlStart = a + d * u0 - n * (t / 2);
                r.FlEnd = a + d * u1 - n * (t / 2);

                // polygon cat web rafter lon: phan nam TREN mat duoi tam (1), ben trai mat F
                Line2 under = Line2.Through(r.FarPoint, d);
                V2 m = dLT.Left;                               // huong doc mat F, len
                double tf = L.Top.Thickness;
                V2 p3 = r.FarPoint + m * (tf / Math.Max(0.2, m.Dot(L.Top.Top.N)) + 5);   // qua mat tren top flange lon
                double yHigh = Math.Max(Math.Max(p3.Y, a.Y), L.TopY(s2)) + 500;
                double xL = s2 - 100;
                r.WebCut.Add(under.At(xL));
                r.WebCut.Add(r.FarPoint);
                r.WebCut.Add(p3);
                r.WebCut.Add(new V2(p3.X, yHigh));
                r.WebCut.Add(new V2(xL, yHigh));
                if (p3.X <= xL + 1) r.Warnings.Add("Mat F qua gan endplate - kiem tra cat web");
                if (Math.Abs(b - 2 * L.FlangeHalfWidth) > 1)
                    r.Warnings.Add(string.Format(CultureInfo.InvariantCulture, "Flange (1) b={0:0} khac be rong flange rafter lon {1:0}", b, 2 * L.FlangeHalfWidth));
            }

            // ---------------- Stiffener (2) ----------------
            if (p.StiffOn)
            {
                r.HasStiff = true;
                Line2 sb = S.Bot.Bottom;                       // bottom stiff = bottom face bottom flange rafter nho
                Line2 mid = sb.Offset(p.StiffT / 2);
                V2 d = sb.Dir;
                V2 m0 = mid.At(s2);
                V2 m1 = m0 + d * p.StiffL;
                double zf = L.WebT / 2;
                double b = p.StiffB > 0 ? p.StiffB : L.FlangeHalfWidth - zf;
                if (b <= 0) throw new InvalidOperationException("Be rong stiff <= 0");
                var list = new List<StiffPlate>();
                foreach (int sg in new[] { 1, -1 })
                {
                    double zi = L.WebZc + sg * zf, zo = L.WebZc + sg * (zf + b);
                    list.Add(new StiffPlate
                    {
                        T = p.StiffT,
                        Pts = new[] { new V3(m0.X, m0.Y, zi), new V3(m0.X, m0.Y, zo), new V3(m1.X, m1.Y, zo), new V3(m1.X, m1.Y, zi) }
                    });
                }
                r.Stiffs = list.ToArray();
                if (sb.YAt(s2) < L.Bot.Top.YAt(s2) - 0.5)
                    r.Warnings.Add("Stiff nam duoi mat tren bottom flange rafter lon");
            }
            return r;
        }
    }
}
