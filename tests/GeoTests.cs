// Test toan thuan (khong can Tekla): mcs tests/GeoTests.cs src/Geo.cs src/Layout.cs src/Settings.cs && mono GeoTests.exe
using System;
using System.Collections.Generic;
using RDS.BUSplice;

static class GeoTests
{
    static int fail;
    static void Eq(string name, double a, double b, double tol = 0.01)
    {
        bool ok = Math.Abs(a - b) <= tol;
        if (!ok) fail++;
        Console.WriteLine((ok ? "PASS " : "FAIL ") + name + "  got=" + a.ToString("0.###") + " exp=" + b.ToString("0.###"));
    }
    static void True(string name, bool v) { if (!v) fail++; Console.WriteLine((v ? "PASS " : "FAIL ") + name); }

    /// <summary>Tam flange thang: mat tren y = y0 + k x, be day t, x tu x1 toi x2, rong b quanh z = 0.</summary>
    static List<V3> Box(double x1, double x2, double yTop0, double k, double t, double b)
    {
        var r = new List<V3>();
        foreach (double x in new[] { x1, x2 })
            foreach (double dy in new[] { 0.0, -t })
                foreach (double z in new[] { -b / 2, b / 2 })
                    r.Add(new V3(x, yTop0 + k * x + dy * Math.Sqrt(1 + k * k), z));
        return r;
    }

    static MemberGeo Member(double x1, double x2, double topY0, double k, double botY0, double kb, double tf, double bf, double tw)
    {
        return new MemberGeo
        {
            Top = Geo.Flange(Box(x1, x2, topY0, k, tf, bf)),
            Bot = Geo.Flange(Box(x1, x2, botY0 + tf, kb, tf, bf)),
            WebZc = 0, WebT = tw, XMax = x2
        };
    }

    static int Main()
    {
        // ---- list ----
        var l = Geo.ParseList("3*100 50");
        True("ParseList 3*100 50", l.Count == 4 && l[0] == 100 && l[3] == 50);
        var c = Geo.CenteredPositions(Geo.ParseList("140"));
        Eq("Centered 140 -> -70", c[0], -70); Eq("Centered 140 -> 70", c[1], 70);
        double t, b;
        True("ParsePlate PLT20*230", Geo.ParsePlate("plt20*230", out t, out b) && t == 20 && b == 230);

        // ---- frame ----
        Frame f = Frame.FromAxis(new V3(0, 0, 0), new V3(1, 0, 0.1));
        True("Frame Y huong len", f.Y.Z > 0.9);
        Eq("Frame truc giao", f.X.Dot(f.Y), 0, 1e-9);

        // ---- flange lines ----
        var fl = Geo.Flange(Box(0, 3000, 340, 0.05, 12, 230));
        Eq("Flange top YAt(0)", fl.Top.YAt(0), 340, 0.05);
        Eq("Flange thickness", fl.Thickness, 12, 0.05);
        Eq("Flange width", fl.Width, 230);

        // ---- layout: nho 600 ngang, lon cao hon + doc 5% ----
        var S = Member(-3000, 0, 300, 0, -300, 0, 12, 230, 10);
        var L = Member(0, 5000, 340, 0.05, -560, -0.02, 12, 230, 10);
        True("IsLarger(L,S)", SpliceLayout.IsLarger(L, S, 0));
        True("!IsLarger(S,L)", !SpliceLayout.IsLarger(S, L, 0));

        var p = new Settings();
        var lay = SpliceLayout.Compute(p, S, L);
        Eq("X0", lay.X0, 0); Eq("FitSmall", lay.FitSmallX, -12); Eq("FitLarge", lay.FitLargeX, 12);
        Eq("Bolt hang 1 = top nho + 56", lay.BoltY[0], 356);
        Eq("Bolt hang cuoi", lay.BoltY[lay.BoltY.Count - 1], 356 - 1020);
        Eq("Ep1 top", lay.Ep1.YTop, 401); Eq("Ep1 left", lay.Ep1.ZLeft, -115);

        // flange (1): top dau endplate = top rafter nho, bottom dau xa = mat duoi top flange lon
        V2 n = lay.FlDir.Left;
        Line2 topLine = Line2.Through(lay.FlStart + n * (lay.FlT / 2), lay.FlDir);
        Eq("Top (1) tai mat endplate = top nho (300)", topLine.YAt(lay.FitLargeX), 300, 0.05);
        Line2 botLine = topLine.Offset(-lay.FlT);
        Eq("Bottom (1) di qua FarPoint", botLine.N.Dot(lay.FarPoint) - botLine.C, 0, 0.05);
        Eq("FarPoint nam tren mat duoi top flange lon", L.Top.Bottom.N.Dot(lay.FarPoint) - L.Top.Bottom.C, 0, 0.05);
        Eq("L doc top flange lon", (lay.FarPoint - L.Top.Bottom.At(12)).Len, 600, 0.05);
        // 2 dau tam (1) cat VUONG theo phuong tam: goc dau = tim +- n*t/2
        V2 sTop = lay.FlStart + n * (lay.FlT / 2), sBot = lay.FlStart - n * (lay.FlT / 2);
        V2 eBot = lay.FlEnd - n * (lay.FlT / 2);
        Eq("Dau endplate: goc gan nhat cham mat endplate", Math.Min(sTop.X, sBot.X), lay.FitLargeX, 0.01);
        True("Dau endplate: khong goc nao lan vao endplate", sTop.X >= lay.FitLargeX - 1e-6 && sBot.X >= lay.FitLargeX - 1e-6);
        Eq("Dau xa: goc duoi = FarPoint", (eBot - lay.FarPoint).Len, 0, 0.01);
        Eq("Duong dau (start->end) // huong tam", Math.Abs((lay.FlEnd - lay.FlStart).Unit.Dot(lay.FlDir.Left)), 0, 1e-9);

        // tam doc xuong (rafter lon thap hon top nho) -> van khong lan vao endplate
        var Ld = Member(0, 5000, 290, 0.05, -560, -0.02, 12, 230, 10);
        var layD = SpliceLayout.Compute(new Settings { StiffOn = false }, S, Ld);
        V2 nd = layD.FlDir.Left;
        V2 dTop = layD.FlStart + nd * (layD.FlT / 2), dBot = layD.FlStart - nd * (layD.FlT / 2);
        Eq("Tam doc xuong: goc gan nhat cham endplate", Math.Min(dTop.X, dBot.X), layD.FitLargeX, 0.01);

        True("Polygon cat web 5 dinh", lay.WebCut.Count == 5);
        True("Polygon: dinh 2 = FarPoint", (lay.WebCut[1] - lay.FarPoint).Len < 1e-6);

        // stiff: bottom = bottom face bottom flange rafter nho (-300), mid = -292
        Eq("Stiff mid Y tai endplate", lay.Stiffs[0].Pts[0].Y, -300 + 8, 0.05);
        Eq("Stiff b auto", Math.Abs(lay.Stiffs[0].Pts[1].Z - lay.Stiffs[0].Pts[0].Z), 115 - 5);
        Eq("Stiff L", lay.Stiffs[0].Pts[3].X - lay.Stiffs[0].Pts[0].X, 300, 0.05);
        Eq("Stiff ben trai Z am", Math.Sign(lay.Stiffs[1].Pts[1].Z), -1);

        // bang chieu cao -> top cao hon la lon
        var S2 = Member(-3000, 0, 300, 0, -300, 0, 12, 230, 10);
        var L2 = Member(0, 3000, 350, 0, -250, 0, 12, 230, 10);
        True("Bang h: top cao hon = lon", SpliceLayout.IsLarger(L2, S2, 0));

        // khong flange / stiff
        p.FlangeOn = false; p.StiffOn = false;
        var lay2 = SpliceLayout.Compute(p, S, L);
        True("Tat flange/stiff", !lay2.HasFlange && lay2.Stiffs.Length == 0);

        // xoa bolt: luoi 5 hang x 2 cot (mac dinh), xoa 1 (tren-trai) va 10 (duoi-phai)
        var pd = new Settings { BoltDelete = "1 10 99" };
        var layB = SpliceLayout.Compute(pd, S, L);
        True("Xoa bolt: con 8", layB.BoltKeep.Count == 8);
        True("Xoa bolt: so 99 bao loi", layB.Warnings.Exists(w => w.Contains("99")));
        True("Bolt so 1 (tren-trai) bi xoa", !layB.BoltKeep.Exists(k => k.X == layB.BoltY[0] && k.Y == layB.BoltZ[0]));
        True("Bolt so 2 (tren-phai) con", layB.BoltKeep.Exists(k => k.X == layB.BoltY[0] && k.Y == layB.BoltZ[1]));
        Eq("Endplate van theo luoi day du", layB.Ep1.YTop, lay.Ep1.YTop);

        // Middle: tam nhom bolt = giua chieu cao rafter nho (S cao 600, tim y = 0), offset 0
        var pm = new Settings { BoltVertRef = 1, BoltTopOffset = 0 };
        var layM = SpliceLayout.Compute(pm, S, L);
        Eq("Middle: tam nhom bolt = giua rafter nho", 0.5 * (layM.BoltY[0] + layM.BoltY[layM.BoltY.Count - 1]), 0, 0.01);
        pm.BoltTopOffset = 20;
        layM = SpliceLayout.Compute(pm, S, L);
        Eq("Middle + offset 20 (len)", 0.5 * (layM.BoltY[0] + layM.BoltY[layM.BoltY.Count - 1]), 20, 0.01);

        // Frame theo TOP FLANGE (rafter tapered ngan): X // top flange, KHONG theo PCA ca tiet dien
        // top flange ngang dai 1500, bottom flange doc xuong 15% -> PCA ca tiet dien lech, top flange thi khong
        var topF = new List<V3>(); var all = new List<V3>();
        foreach (double x in new[] { 0.0, 1500.0 }) foreach (double z in new[] { -115.0, 115.0 }) foreach (double dy in new[] { 0.0, -12.0 })
        { topF.Add(new V3(x, 0, 600 + dy)); all.Add(new V3(x, 0, 600 + dy)); all.Add(new V3(x, 0, -0.15 * x + dy)); }
        Frame ft = Frame.FromMember(topF, all, new V3(5000, 0, 0));
        Eq("Frame X // top flange (tapered)", ft.X.X, 1, 1e-9);
        True("PCA ca tiet dien lech (ly do sua)", Math.Abs(Geo.Principal(all).Unit.Z) > 0.01);

        // settings ini round-trip
        string tmp = System.IO.Path.GetTempFileName();
        var s1 = new Settings { BoltDistDown = "3*100", Ep2Bot = 77 }; s1.PStiff.Class = "7";
        s1.Save(tmp);
        var s2 = Settings.Load(tmp);
        True("Settings round-trip", s2.BoltDistDown == "3*100" && s2.Ep2Bot == 77 && s2.PStiff.Class == "7" && s2.FlangeOn);

        Console.WriteLine(fail == 0 ? "ALL PASS" : fail + " FAIL");
        return fail;
    }
}
