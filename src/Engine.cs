// =============================================================================
// RDS BU Splice - doc model + tao object Tekla tu SpliceLayout.
// Dung chung cho macro (giai doan 1) va plugin (giai doan 2).
// Moi toa do gui Tekla la GLOBAL (work plane = global trong suot qua trinh, restore o finally).
// =============================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Tekla.Structures;
using Tekla.Structures.Geometry3d;
using Tekla.Structures.Model;
using Tekla.Structures.Model.Operations;
using Tekla.Structures.Solid;

namespace RDS.BUSplice
{
    /// <summary>1 rafter BU: 3 part + dinh solid (global).</summary>
    public sealed class BuMember
    {
        public string Source;            // ten component cha / assembly
        public Part Web, TopFl, BotFl;
        public List<V3> WebPts, TopPts, BotPts;
        public IEnumerable<V3> AllPts => WebPts.Concat(TopPts).Concat(BotPts);
        public IEnumerable<Part> Parts => new[] { Web, TopFl, BotFl };
    }

    public sealed class Engine
    {
        readonly Model _model;
        readonly Action<string> _log;
        readonly List<ModelObject> _created = new List<ModelObject>();
        public List<string> Warnings = new List<string>();
        /// <summary>Macro: true (tu commit). Plugin: false (Tekla tu commit sau Run).</summary>
        public bool CommitAtEnd = true;

        public Engine(Model model, Action<string> log) { _model = model; _log = log ?? (s => { }); }

        public IList<ModelObject> Created => _created;

        // ============================ doc rafter ============================

        /// <summary>A1/A2: tu part pick -> component cha (hoac assembly) -> FLANGE x2 + web.</summary>
        public BuMember Resolve(Part picked)
        {
            var parts = new List<Part>();
            string src;
            BaseComponent father = picked.GetFatherComponent();
            if (father != null)
            {
                src = father.Name + " #" + father.Identifier.ID;
                ModelObjectEnumerator e = father.GetChildren();
                while (e.MoveNext()) { if (e.Current is Part pp) { pp.Select(); parts.Add(pp); } }
            }
            else
            {
                Assembly a = picked.GetAssembly();
                src = "assembly " + a.Name;
                if (a.GetMainPart() is Part mp) parts.Add(mp);
                foreach (object o in a.GetSecondaries()) if (o is Part sp) parts.Add(sp);
            }
            parts = parts.GroupBy(x => x.Identifier.ID).Select(g => g.First()).ToList();

            var flanges = parts.Where(x => (x.Name ?? "").ToUpperInvariant().Contains("FLANGE")).ToList();
            if (flanges.Count < 2) throw new InvalidOperationException("Khong thay 2 part ten FLANGE trong " + src);

            // truc rafter tu 2 flange dai nhat -> frame tam de phan top / bottom / web
            var fl = flanges.Select(f => new { P = f, V = Verts(f) }).OrderByDescending(f => Extent(f.V)).Take(2).ToList();
            List<V3> fpts = fl[0].V.Concat(fl[1].V).ToList();
            Frame fr = Frame.FromAxis(Geo.Centroid(fpts), Geo.Principal(fpts));
            double y0 = Geo.Centroid(fl[0].V.Select(fr.ToLocal).ToList()).Y;
            double y1 = Geo.Centroid(fl[1].V.Select(fr.ToLocal).ToList()).Y;
            var top = y0 > y1 ? fl[0] : fl[1];
            var bot = y0 > y1 ? fl[1] : fl[0];

            // web = part khong phai FLANGE, mong theo phap tuyen mat phang rafter (Z), cao nhat theo Y
            Part web = null; List<V3> webPts = null; double bestH = 0;
            foreach (Part c in parts)
            {
                if (flanges.Contains(c)) continue;
                List<V3> v = Verts(c);
                if (v.Count == 0) continue;
                var loc = v.Select(fr.ToLocal).ToList();
                double tz = loc.Max(q => q.Z) - loc.Min(q => q.Z);
                double h = loc.Max(q => q.Y) - loc.Min(q => q.Y);
                if (tz > 60 || h <= tz) continue;
                if (h > bestH) { bestH = h; web = c; webPts = v; }
            }
            if (web == null) throw new InvalidOperationException("Khong thay web trong " + src);

            var m = new BuMember { Source = src, Web = web, TopFl = top.P, BotFl = bot.P, WebPts = webPts, TopPts = top.V, BotPts = bot.V };
            _log(string.Format("Resolve {0}: web={1} '{2}' top={3} bot={4} ({5} part)", src, web.Identifier.ID, web.Name, top.P.Identifier.ID, bot.P.Identifier.ID, parts.Count));
            return m;
        }

        public static MemberGeo GeoOf(BuMember m, Frame f)
        {
            var w = m.WebPts.Select(f.ToLocal).ToList();
            double zmin = w.Min(q => q.Z), zmax = w.Max(q => q.Z);
            return new MemberGeo
            {
                Top = Geo.Flange(m.TopPts.Select(f.ToLocal).ToList()),
                Bot = Geo.Flange(m.BotPts.Select(f.ToLocal).ToList()),
                WebZc = (zmin + zmax) / 2,
                WebT = zmax - zmin,
                XMax = m.AllPts.Select(f.ToLocal).Max(q => q.X)
            };
        }

        /// <summary>Dung frame theo rafter "from", truc X huong ve rafter "to".</summary>
        public static Frame FrameOf(BuMember from, BuMember to)
        {
            List<V3> a = from.AllPts.ToList();
            V3 c = Geo.Centroid(a), ct = Geo.Centroid(to.AllPts.ToList());
            V3 ax = Geo.Principal(a);
            if (ax.Dot(ct - c) < 0) ax = ax * -1;
            return Frame.FromAxis(c, ax);
        }

        // ============================ tao splice ============================

        public void Build(Settings p, BuMember A, BuMember B)
        {
            // A3: xac dinh rafter nho / lon
            Frame f = FrameOf(A, B);
            MemberGeo ga = GeoOf(A, f), gb = GeoOf(B, f);
            BuMember S = A, L = B;
            if (!SpliceLayout.IsLarger(gb, ga, ga.XMax))
            {
                S = B; L = A;
                f = FrameOf(S, L);
            }
            MemberGeo gs = GeoOf(S, f), gl = GeoOf(L, f);
            _log(string.Format(CultureInfo.InvariantCulture, "Frame O={0} X={1} Y={2} Z={3}", f.O, f.X, f.Y, f.Z));
            _log(string.Format(CultureInfo.InvariantCulture, "Nho = {0}: h={1:0.#} web zc={2:0.#} tw={3:0.#} xmax={4:0.#}", S.Source, gs.Height(gs.XMax), gs.WebZc, gs.WebT, gs.XMax));
            _log(string.Format(CultureInfo.InvariantCulture, "Lon = {0}: h={1:0.#} web zc={2:0.#} tw={3:0.#}", L.Source, gl.Height(gs.XMax), gl.WebZc, gl.WebT));
            if (Math.Abs(gs.WebZc - gl.WebZc) > 2) Warnings.Add(string.Format(CultureInfo.InvariantCulture, "2 web lech tim {0:0.#} mm", gs.WebZc - gl.WebZc));

            SpliceLayout lay = SpliceLayout.Compute(p, gs, gl);
            Warnings.AddRange(lay.Warnings);
            _log("Bolt Y: " + string.Join(" ", lay.BoltY.Select(v => v.ToString("0.#", CultureInfo.InvariantCulture))) +
                 " | Z: " + string.Join(" ", lay.BoltZ.Select(v => v.ToString("0.#", CultureInfo.InvariantCulture))));

            // ---- fitting rafter ----
            foreach (Part pt in S.Parts) Fit(pt, f, lay.FitSmallX);
            Fit(L.Web, f, lay.FitLargeX);
            Fit(L.BotFl, f, lay.FitLargeX);
            if (lay.HasFlange) FitPlane(L.TopFl, f.ToGlobal(lay.FarPoint, 0), f.Dir(lay.FarNormal.Left), f.Z);
            else Fit(L.TopFl, f, lay.FitLargeX);

            // ---- endplate ----
            ContourPlate ep1 = RectToPlate(f, lay.Ep1, p.PEndplate, S.Web);
            ContourPlate ep2 = RectToPlate(f, lay.Ep2, p.PEndplate, L.Web);
            AddToAssembly(S.Web, ep1);
            var largeExtras = new List<Part> { ep2 };

            // ---- bolt ----
            MakeBolts(p, f, lay, ep1, ep2);

            // ---- flange (1) + cat rafter lon ----
            if (lay.HasFlange)
            {
                Beam fl = FlangePlate(p, f, lay, L.Web);
                if (fl != null) largeExtras.Add(fl);   // KHONG fitting: 2 dau tam cat vuong theo phuong tam
                CutWeb(f, lay.WebCut, gl.WebZc, gl.WebT + 20, L.Web);
            }

            // ---- stiffener (2) ----
            foreach (StiffPlate s in lay.Stiffs)
            {
                var pts = s.Pts.Select(q => f.ToGlobal(q.X, q.Y, q.Z)).ToList();
                ContourPlate cp = Plate(pts, "PL" + Num(s.T), p.PStiff, L.Web, new Chamfer(p.StiffChamX, p.StiffChamY, Chamfer.ChamferTypeEnum.CHAMFER_LINE));
                if (cp != null) largeExtras.Add(cp);
            }

            foreach (Part x in largeExtras) AddToAssembly(L.Web, x);
            if (CommitAtEnd) _model.CommitChanges();
        }

        // ============================ helper Tekla ============================

        void Fit(Part part, Frame f, double x) => FitPlane(part, f.ToGlobal(x, 0, 0), f.Y, f.Z);

        void FitPlane(Part part, V3 origin, V3 ax, V3 ay)
        {
            var fit = new Fitting
            {
                Father = part,
                Plane = new Plane { Origin = P(origin), AxisX = Vec(ax), AxisY = Vec(ay) }
            };
            bool ok = fit.Insert();
            if (ok) _created.Add(fit);
            _log(string.Format("Fitting part {0} '{1}' @ {2}: {3}", part.Identifier.ID, part.Name, origin, ok));
            if (!ok) Warnings.Add("Fitting loi part " + part.Identifier.ID);
        }

        ContourPlate RectToPlate(Frame f, RectPlate r, PartInfo info, Part refPart)
        {
            var pts = r.Corners.Select(q => f.ToGlobal(q.X, q.Y, q.Z)).ToList();
            return Plate(pts, "PL" + Num(r.T), info, refPart, null);
        }

        /// <summary>ContourPlate Depth MIDDLE: tam nam doi xung quanh mat phang contour.</summary>
        ContourPlate Plate(List<V3> pts, string profile, PartInfo info, Part refPart, Chamfer firstChamfer)
        {
            var cp = new ContourPlate();
            for (int i = 0; i < pts.Count; i++)
                cp.AddContourPoint(new ContourPoint(P(pts[i]), i == 0 && firstChamfer != null ? firstChamfer : null));
            cp.Profile.ProfileString = profile;
            ApplyInfo(cp, info, refPart);
            cp.Position.Depth = Position.DepthEnum.MIDDLE;
            bool ok = cp.Insert();
            _log(string.Format("Plate {0} '{1}': {2} id={3}", profile, cp.Name, ok, ok ? cp.Identifier.ID : 0));
            if (!ok) { Warnings.Add("Khong tao duoc " + cp.Name); return null; }
            _created.Add(cp);
            return cp;
        }

        void ApplyInfo(Part part, PartInfo info, Part refPart)
        {
            part.Name = info.Name;
            part.PartNumber.Prefix = info.Prefix;
            part.PartNumber.StartNumber = info.Start;
            part.Material.MaterialString = info.Material;
            part.Class = string.IsNullOrEmpty(info.Class) ? refPart.Class : info.Class;
            part.Finish = string.IsNullOrEmpty(info.Finish) ? refPart.Finish : info.Finish;
        }

        /// <summary>Tam (1) = Beam profile PLT, tu xoay de be rong b nam theo Z local.</summary>
        Beam FlangePlate(Settings p, Frame f, SpliceLayout lay, Part refPart)
        {
            var bm = new Beam
            {
                StartPoint = P(f.ToGlobal(lay.FlStart, lay.FlZ)),
                EndPoint = P(f.ToGlobal(lay.FlEnd, lay.FlZ))
            };
            bm.Profile.ProfileString = p.FlangeProfile.Trim().ToUpperInvariant();
            ApplyInfo(bm, p.PFlange, refPart);
            bm.Position.Plane = Position.PlaneEnum.MIDDLE;
            bm.Position.Depth = Position.DepthEnum.MIDDLE;
            bm.Position.Rotation = Position.RotationEnum.FRONT;
            if (!bm.Insert()) { Warnings.Add("Khong tao duoc flange (1) - kiem tra profile " + bm.Profile.ProfileString); return null; }
            _created.Add(bm);

            Position.RotationEnum best = bm.Position.Rotation; double bestErr = double.MaxValue;
            foreach (var rot in new[] { Position.RotationEnum.FRONT, Position.RotationEnum.TOP })
            {
                bm.Position.Rotation = rot; bm.Modify();
                List<V3> v = Verts(bm).Select(f.ToLocal).ToList();
                double ez = v.Count == 0 ? 0 : v.Max(q => q.Z) - v.Min(q => q.Z);
                double err = Math.Abs(ez - lay.FlB);
                _log(string.Format(CultureInfo.InvariantCulture, "  flange rot={0} extZ={1:0.#}", rot, ez));
                if (err < bestErr) { bestErr = err; best = rot; }
            }
            if (bm.Position.Rotation != best) { bm.Position.Rotation = best; bm.Modify(); }
            if (bestErr > 1) Warnings.Add("Flange (1) khong tu xoay duoc - kiem tra huong");
            return bm;
        }

        /// <summary>Cat web rafter lon bang BooleanPart (operative contour plate trong mat phang web).</summary>
        void CutWeb(Frame f, List<V2> poly, double zc, double thick, Part web)
        {
            var op = new ContourPlate();
            foreach (V2 q in poly) op.AddContourPoint(new ContourPoint(P(f.ToGlobal(q, zc)), null));
            op.Profile.ProfileString = "PL" + Num(Math.Ceiling(thick));
            op.Material.MaterialString = "ZERO_MATERIAL";
            op.Class = BooleanPart.BooleanOperativeClassName;
            op.Position.Depth = Position.DepthEnum.MIDDLE;
            if (!op.Insert()) { Warnings.Add("Khong tao duoc khoi cat web"); return; }
            var bp = new BooleanPart { Father = web };
            bp.SetOperativePart(op);
            bool ok = bp.Insert();
            op.Delete();
            if (ok) _created.Add(bp); else Warnings.Add("Cat web rafter lon loi");
            _log("Cut web " + web.Identifier.ID + ": " + ok);
        }

        void MakeBolts(Settings p, Frame f, SpliceLayout lay, Part ep1, Part ep2)
        {
            if (ep1 == null || ep2 == null) { Warnings.Add("Thieu endplate - bo qua bolt"); return; }
            string std; double size;
            ResolveBolt(p.BoltStandard, p.BoltSize, out std, out size);

            double zc = (lay.BoltZ[0] + lay.BoltZ[lay.BoltZ.Count - 1]) / 2;
            V3 a = f.ToGlobal(lay.X0, lay.BoltY[0], zc);
            V3 e = lay.BoltY.Count > 1 ? f.ToGlobal(lay.X0, lay.BoltY[lay.BoltY.Count - 1], zc) : f.ToGlobal(lay.X0, lay.BoltY[0] - 100, zc);

            if (lay.BoltKeep.Count == 0) { Warnings.Add("Khong con bolt nao - bo qua bolt"); return; }
            bool deleted = lay.BoltDeleted.Count > 0;
            // Vi tri bolt mong muon (global): kiem tra ca huong truc LAN phia trai/phai (XY list khong doi xung).
            var expect = lay.BoltKeep.Select(k => f.ToGlobal(lay.X0, k.X, k.Y)).ToList();

            // Khong xoa bolt -> BoltArray (sua tay trong Tekla de hon). Co xoa -> BoltXYList chi chua bolt con lai.
            // BoltXYList KHONG co Remove/Set dist (reflection dll 2020) -> doi dau Y = xoa group, tao lai.
            Func<int, Position.RotationEnum, BoltGroup> make = (sg, rot) =>
            {
                BoltGroup g = deleted ? (BoltGroup)XYList(lay, zc, sg) : ArrayOf(lay, p);
                g.PartToBoltTo = ep2;
                g.PartToBeBolted = ep1;
                g.FirstPosition = P(a);
                g.SecondPosition = P(e);
                g.BoltSize = size;
                g.BoltStandard = std;
                g.Tolerance = p.BoltTolerance;
                g.BoltType = BoltGroup.BoltTypeEnum.BOLT_TYPE_SITE;
                g.ThreadInMaterial = BoltGroup.BoltThreadInMaterialEnum.THREAD_IN_MATERIAL_YES;
                g.CutLength = 2 * (p.Ep1T + p.Ep2T) + 40;
                g.ExtraLength = 0;
                g.Bolt = true; g.Washer1 = false; g.Washer2 = false; g.Washer3 = true; g.Nut1 = true; g.Nut2 = false;
                g.Hole1 = true; g.Hole2 = true; g.Hole3 = true; g.Hole4 = true; g.Hole5 = true;
                g.Position.Plane = Position.PlaneEnum.MIDDLE;
                g.Position.Depth = Position.DepthEnum.MIDDLE;
                g.Position.Rotation = rot;
                return g.Insert() ? g : null;
            };

            BoltGroup b = make(1, Position.RotationEnum.FRONT);
            if (b == null) { Warnings.Add("Khong tao duoc bolt"); return; }

            // 1) chon rotation: truc bolt // X local (truc rafter nho), uu tien vi tri khop
            Position.RotationEnum best = b.Position.Rotation; double bestScore = double.MaxValue;
            foreach (var rot in new[] { Position.RotationEnum.FRONT, Position.RotationEnum.TOP, Position.RotationEnum.BACK, Position.RotationEnum.BELOW })
            {
                b.Position.Rotation = rot; b.Modify();
                CoordinateSystem cs = b.GetCoordinateSystem();
                Vector z = Vector.Cross(cs.AxisX, cs.AxisY); z.Normalize();
                double dot = Math.Abs(z.X * f.X.X + z.Y * f.X.Y + z.Z * f.X.Z);
                double err = PosError(b, expect);
                double score = (dot < 0.99 ? 1e6 : 0) + err;
                _log(string.Format(CultureInfo.InvariantCulture, "  bolt rot={0} |z.X|={1:0.###} posErr={2:0.#}", rot, dot, err));
                if (score < bestScore - 1e-6) { bestScore = score; best = rot; }
            }
            b.Position.Rotation = best; b.Modify();
            int sign = 1;

            // 2) XY list bi lat trai/phai -> tao lai voi dau Y nguoc, giu ban tot hon
            if (deleted && bestScore > 1 && bestScore < 1e6)
            {
                BoltGroup b2 = make(-1, best);
                double err2 = b2 == null ? 1e5 : PosError(b2, expect);
                _log(string.Format(CultureInfo.InvariantCulture, "  bolt sign=-1 rot={0} posErr={1:0.#}", best, err2));
                if (b2 != null && err2 < bestScore) { b.Delete(); b = b2; bestScore = err2; sign = -1; }
                else if (b2 != null) b2.Delete();
            }
            _created.Add(b);

            if (bestScore >= 1e6) Warnings.Add("Bolt khong xoay dung truc - kiem tra");
            else if (bestScore > 1) Warnings.Add("Vi tri bolt lech " + bestScore.ToString("0.#", CultureInfo.InvariantCulture) + " mm - kiem tra");
            _log(string.Format("Bolt {0} M{1} rows={2} cols={3} xoa=[{4}] con={5} rot={6} sign={7}", std, size, lay.BoltY.Count, lay.BoltZ.Count,
                string.Join(" ", lay.BoltDeleted), lay.BoltKeep.Count, best, sign));
        }

        static BoltArray ArrayOf(SpliceLayout lay, Settings p)
        {
            var ba = new BoltArray();
            if (lay.BoltY.Count == 1) ba.AddBoltDistX(0);
            for (int i = 1; i < lay.BoltY.Count; i++) ba.AddBoltDistX(Math.Round(lay.BoltY[i - 1] - lay.BoltY[i], 2));
            var across = Geo.ParseList(p.BoltDistAcross);
            if (across.Count == 0) ba.AddBoltDistY(0);
            foreach (double d in across) ba.AddBoltDistY(d);
            return ba;
        }

        /// <summary>BoltXYList: X = khoang cach tu hang tren cung xuong (doc First->Second), Y = lech ngang so voi tim (sign = dao chieu).</summary>
        static BoltXYList XYList(SpliceLayout lay, double zc, int sign)
        {
            var xy = new BoltXYList();
            foreach (V2 k in lay.BoltKeep)
            {
                xy.AddBoltDistX(Math.Round(lay.BoltY[0] - k.X, 2));
                xy.AddBoltDistY(Math.Round(sign * (k.Y - zc), 2));
            }
            return xy;
        }

        /// <summary>Sai lech lon nhat (mm) giua vi tri bolt Tekla tao va vi tri mong muon.</summary>
        static double PosError(BoltGroup b, List<V3> expect)
        {
            b.Select();
            ArrayList pos = b.BoltPositions;
            if (pos == null || pos.Count != expect.Count) return 1e5;
            double worst = 0;
            foreach (Point q in pos)
            {
                V3 v = new V3(q.X, q.Y, q.Z);
                worst = Math.Max(worst, expect.Min(x => (x - v).Len));
            }
            return worst;
        }

        void AddToAssembly(Part main, Part sec)
        {
            if (sec == null) return;
            try
            {
                Assembly a = main.GetAssembly();
                a.Add(sec);
                bool ok = a.Modify();
                _log("Assembly " + a.Identifier.ID + " += " + sec.Identifier.ID + ": " + ok);
                if (!ok) Warnings.Add("Khong add duoc " + sec.Name + " vao assembly");
            }
            catch (Exception ex) { Warnings.Add("Assembly loi: " + ex.Message); }
        }

        static Dictionary<string, List<double>> _boltCatalog;

        /// <summary>Kiem tra standard/size trong catalog (Insert()==true KHONG co nghia bolt hien ra).</summary>
        void ResolveBolt(string stdIn, double sizeIn, out string std, out double size)
        {
            std = stdIn; size = sizeIn;
            try
            {
                if (_boltCatalog == null)
                {
                    var cat = new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase);
                    var e = new Tekla.Structures.Catalogs.CatalogHandler().GetBoltItems();
                    while (e.MoveNext())
                    {
                        var it = e.Current;
                        if (it == null || string.IsNullOrEmpty(it.Standard)) continue;
                        if (!cat.TryGetValue(it.Standard, out var l)) cat[it.Standard] = l = new List<double>();
                        if (!l.Contains(it.Size)) l.Add(it.Size);
                    }
                    _boltCatalog = cat;
                }
                if (_boltCatalog.Count == 0) return;
                string key = _boltCatalog.Keys.FirstOrDefault(k => string.Equals(k, stdIn, StringComparison.OrdinalIgnoreCase));
                if (key == null)
                {
                    Warnings.Add("Bolt standard '" + stdIn + "' khong co trong catalog");
                    return;
                }
                std = key;
                var sizes = _boltCatalog[key];
                if (!sizes.Any(s => Math.Abs(s - sizeIn) < 0.01))
                {
                    size = sizes.OrderBy(s => Math.Abs(s - sizeIn)).First();
                    Warnings.Add(string.Format(CultureInfo.InvariantCulture, "Bolt {0} khong co M{1} -> dung M{2}", key, sizeIn, size));
                }
            }
            catch (Exception ex) { _log("ResolveBolt: " + ex.Message); }
        }

        // ============================ tien ich ============================

        public static List<V3> Verts(Part part)
        {
            var r = new List<V3>();
            Solid s = part.GetSolid();
            if (s == null) return r;
            FaceEnumerator fe = s.GetFaceEnumerator();
            while (fe.MoveNext())
            {
                LoopEnumerator le = fe.Current.GetLoopEnumerator();
                while (le.MoveNext())
                {
                    VertexEnumerator ve = le.Current.GetVertexEnumerator();
                    while (ve.MoveNext()) { Point q = ve.Current; r.Add(new V3(q.X, q.Y, q.Z)); }
                }
            }
            return r;
        }

        static double Extent(List<V3> v)
        {
            if (v.Count == 0) return 0;
            V3 a = Geo.Principal(v);
            return v.Max(q => q.Dot(a)) - v.Min(q => q.Dot(a));
        }

        static Point P(V3 v) => new Point(v.X, v.Y, v.Z);
        static Vector Vec(V3 v) => new Vector(v.X, v.Y, v.Z);
        static string Num(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
