using System;
using System.Collections.Generic;
using System.IO;
using Tekla.Structures;
using Tekla.Structures.Model;
using Tekla.Structures.Model.Operations;
using Tekla.Structures.Model.UI;
using Tekla.Structures.Plugins;

namespace RDS.BUSplice
{
    /// <summary>
    /// Plugin RDS _ BU Splice: pick 2 rafter BU (web / flange bat ky) -> chuot giua.
    /// Hinh hoc nam o src/Layout.cs + src/Engine.cs (dung chung voi macro test).
    /// </summary>
    [Plugin(BUSplicePlugin.PluginName)]
    [PluginUserInterface("RDS.BUSplice.MainForm")]
    public class BUSplicePlugin : PluginBase
    {
        public const string PluginName = "RDS _ BU Splice";

        private readonly PluginData _data;
        private readonly Model _model;

        public BUSplicePlugin(PluginData data)
        {
            _data = data;
            _model = new Model();
        }

        // ------------------------------------------------------------------ input
        public override List<InputDefinition> DefineInput()
        {
            var inputs = new List<InputDefinition>();
            var picker = new Picker();
            ModelObjectEnumerator e = picker.PickObjects(Picker.PickObjectsEnum.PICK_N_PARTS,
                "Pick BU 1 va BU 2 (web hoac flange), chuot giua de ket thuc");
            var ids = new List<Identifier>();
            while (e.MoveNext())
            {
                var p = e.Current as Part;
                if (p == null || ids.Exists(x => x.ID == p.Identifier.ID)) continue;
                ids.Add(p.Identifier);
                if (ids.Count == 2) break;
            }
            if (ids.Count < 2) { Operation.DisplayPrompt("RDS BU Splice: can pick 2 rafter"); return inputs; }
            inputs.Add(new InputDefinition(ids[0]));
            inputs.Add(new InputDefinition(ids[1]));
            return inputs;
        }

        // ------------------------------------------------------------------ run
        public override bool Run(List<InputDefinition> Input)
        {
            var lines = new List<string> { "=== RUN " + DateTime.Now };
            WorkPlaneHandler wph = _model.GetWorkPlaneHandler();
            TransformationPlane saved = wph.GetCurrentTransformationPlane();
            Engine eng = null;
            try
            {
                if (Input == null || Input.Count < 2) { lines.Add("Input thieu"); return false; }
                var a = _model.SelectModelObject((Identifier)Input[0].GetInput()) as Part;
                var b = _model.SelectModelObject((Identifier)Input[1].GetInput()) as Part;
                if (a == null || b == null) { lines.Add("Khong doc duoc part input"); return false; }

                Settings p = ToSettings();
                foreach (var kv in p.Flatten()) lines.Add("  " + kv.Key + "=" + kv.Value);

                wph.SetCurrentTransformationPlane(new TransformationPlane());   // toan bo toa do = global
                eng = new Engine(_model, s => lines.Add(s)) { CommitAtEnd = false };
                BuMember ma = eng.Resolve(a), mb = eng.Resolve(b);
                if (ma.Web.Identifier.ID == mb.Web.Identifier.ID) throw new InvalidOperationException("2 lan pick cung 1 rafter");
                eng.Build(p, ma, mb);
                return true;
            }
            catch (Exception ex)
            {
                lines.Add("EXCEPTION " + ex);
                Operation.DisplayPrompt("RDS BU Splice loi: " + ex.Message);
                return false;
            }
            finally
            {
                wph.SetCurrentTransformationPlane(saved);
                if (eng != null)
                {
                    foreach (string w in eng.Warnings) lines.Add("WARN " + w);
                    if (eng.Warnings.Count > 0) Operation.DisplayPrompt("RDS BU Splice: " + string.Join(" | ", eng.Warnings));
                }
                Log(string.Join(Environment.NewLine, lines));
            }
        }

        // ------------------------------------------------------------------ dialog -> Settings
        /// <summary>O de mac dinh (IsDefaultValue) giu gia tri mac dinh trong Settings.</summary>
        private Settings ToSettings()
        {
            var d = _data; var s = new Settings();
            s.Ep1T = D(d.Ep1T, s.Ep1T); s.Ep1Top = D(d.Ep1Top, s.Ep1Top); s.Ep1Bot = D(d.Ep1Bot, s.Ep1Bot);
            s.Ep2T = D(d.Ep2T, s.Ep2T); s.Ep2Top = D(d.Ep2Top, s.Ep2Top); s.Ep2Bot = D(d.Ep2Bot, s.Ep2Bot);
            s.EpLeft = D(d.EpLeft, s.EpLeft); s.EpRight = D(d.EpRight, s.EpRight);

            s.BoltTopOffset = D(d.BoltTopOffset, s.BoltTopOffset);
            s.BoltDistDown = S(d.BoltDistDown, s.BoltDistDown);
            s.BoltDistAcross = S(d.BoltDistAcross, s.BoltDistAcross);
            s.BoltDelete = S(d.BoltDelete, "", true);
            s.BoltSize = D(d.BoltSize, s.BoltSize);
            s.BoltStandard = S(d.BoltStandard, s.BoltStandard);
            s.BoltTolerance = D(d.BoltTolerance, s.BoltTolerance);

            s.FlangeOn = I(d.FlangeOn, 1) == 1;
            s.FlangeProfile = S(d.FlangeProfile, s.FlangeProfile);
            s.FlangeLength = D(d.FlangeLength, s.FlangeLength);

            s.StiffOn = I(d.StiffOn, 1) == 1;
            s.StiffT = D(d.StiffT, s.StiffT); s.StiffB = D(d.StiffB, s.StiffB); s.StiffL = D(d.StiffL, s.StiffL);
            s.StiffChamX = D(d.StiffChamX, s.StiffChamX); s.StiffChamY = D(d.StiffChamY, s.StiffChamY);

            Part(s.PEndplate, d.EpName, d.EpPrefix, d.EpStart, d.EpMaterial, d.EpClass);
            Part(s.PFlange, d.FlName, d.FlPrefix, d.FlStart, d.FlMaterial, d.FlClass);
            Part(s.PStiff, d.StName, d.StPrefix, d.StStart, d.StMaterial, d.StClass);
            return s;
        }

        private void Part(PartInfo pi, string name, string pre, int start, string mat, string cls)
        {
            pi.Name = S(name, pi.Name); pi.Prefix = S(pre, pi.Prefix); pi.Start = I(start, pi.Start);
            pi.Material = S(mat, pi.Material); pi.Class = S(cls, "", true);
        }

        private double D(double v, double def) { return IsDefaultValue(v) ? def : v; }
        private int I(int v, int def) { return IsDefaultValue(v) ? def : v; }
        /// <summary>allowEmpty = o de trong la co nghia (Class / xoa bolt).</summary>
        private string S(string v, string def, bool allowEmpty = false)
        {
            if (v == null || IsDefaultValue(v)) return def;
            v = v.Trim();
            return v.Length == 0 && !allowEmpty ? def : v;
        }

        private void Log(string text)
        {
            try
            {
                string dir = _model.GetInfo().ModelPath;
                if (string.IsNullOrEmpty(dir)) dir = Path.GetTempPath();
                File.AppendAllText(Path.Combine(dir, "RDS_BUSplice.log"), text + Environment.NewLine);
            }
            catch { }
        }
    }
}
