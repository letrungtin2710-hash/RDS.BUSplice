// =============================================================================
// RDS BU Splice - entry cua impl dll (stub RDS-BUSplice.cs goi RDS.MacroImpl.BUSpliceImpl.Run qua reflection).
// Tham so runtime de kieu object -> impl khong can ref Tekla.Macros.Runtime.
// =============================================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Tekla.Structures;
using Tekla.Structures.Model;
using Tekla.Structures.Model.Operations;
using Tekla.Structures.Model.UI;
using RDS.BUSplice;

namespace RDS.MacroImpl
{
    public static class BUSpliceImpl
    {
        static string LastPath => Path.Combine(Path.GetDirectoryName(Settings.DefaultPath), "RDS-BUSplice.last");

        public static void Run(object runtime)
        {
            var model = new Model();
            if (!model.GetConnectionStatus()) { MessageBox.Show("Khong ket noi duoc Tekla model", "RDS BU Splice"); return; }

            var ui = new MainForm();
            ui.SetSettings(Settings.Load(Settings.DefaultPath));
            ui.Log("Version " + typeof(BUSpliceImpl).Assembly.GetName().Version);
            ui.RunClicked += () => Guard(ui, () => DoSplice(model, ui));
            ui.UndoClicked += () => Guard(ui, () => DoUndo(model, ui));
            ui.Show();
            while (ui.Visible) { Application.DoEvents(); Thread.Sleep(30); }   // KNOWLEDGE §2: pump loop
            ui.Dispose();
        }

        static void Guard(MainForm ui, Action a)
        {
            ui.Busy = true;
            try { a(); }
            catch (Exception ex) { ui.LogErr("LOI: " + ex.Message); FileLog(new Model(), "EXCEPTION " + ex); }
            finally { ui.Busy = false; ui.WindowState = FormWindowState.Normal; ui.TopMost = true; ui.Activate(); }
        }

        static void DoSplice(Model model, MainForm ui)
        {
            Settings p = ui.GetSettings();
            p.Save(Settings.DefaultPath);

            // thu nho form de pick trong model
            ui.TopMost = false; ui.WindowState = FormWindowState.Minimized; Application.DoEvents();
            var picker = new Picker();
            Part a, b;
            try
            {
                a = picker.PickObject(Picker.PickObjectEnum.PICK_ONE_PART, "Pick BU 1 (web hoac flange)") as Part;
                b = picker.PickObject(Picker.PickObjectEnum.PICK_ONE_PART, "Pick BU 2 (web hoac flange)") as Part;
            }
            catch (Exception) { ui.LogWarn("Huỷ pick"); return; }
            if (a == null || b == null) { ui.LogWarn("Chưa pick đủ 2 part"); return; }

            WorkPlaneHandler wph = model.GetWorkPlaneHandler();
            TransformationPlane saved = wph.GetCurrentTransformationPlane();
            var lines = new List<string>();
            Action<string> log = s => lines.Add(s);
            var eng = new Engine(model, log);
            try
            {
                wph.SetCurrentTransformationPlane(new TransformationPlane());   // moi toa do = global
                BuMember ma, mb;
                eng.ResolvePair(a, b, p.Flip == 1, out ma, out mb);
                if (ma.Web.Identifier.ID == mb.Web.Identifier.ID) throw new InvalidOperationException("2 lan pick cung 1 rafter");
                eng.Build(p, ma, mb);
            }
            finally
            {
                wph.SetCurrentTransformationPlane(saved);
                SaveCreated(eng.Created);
                foreach (string w in eng.Warnings) log("WARN " + w);
                FileLog(model, "=== RUN " + DateTime.Now + "\r\n" + string.Join("\r\n", lines));
            }

            foreach (string w in eng.Warnings) ui.LogWarn(w);
            ui.LogOk(string.Format("Xong: {0} object. Log: RDS_BUSplice.log trong thư mục model.", eng.Created.Count));
            Operation.DisplayPrompt(eng.Warnings.Count == 0 ? "RDS BU Splice: xong" : "RDS BU Splice: " + string.Join(" | ", eng.Warnings));
        }

        /// <summary>Xoa object cua lan chay truoc (luu GUID trong file .last) - tien test lai.</summary>
        static void DoUndo(Model model, MainForm ui)
        {
            if (!File.Exists(LastPath)) { ui.LogWarn("Không có lần chạy trước"); return; }
            var guids = File.ReadAllLines(LastPath).Where(l => l.Trim().Length > 0).Reverse().ToList();
            int ok = 0, miss = 0;
            foreach (string g in guids)
            {
                ModelObject mo = null;
                try { mo = model.SelectModelObject(new Identifier(new Guid(g.Trim()))); } catch { }
                if (mo != null && mo.Delete()) ok++; else miss++;
            }
            model.CommitChanges();
            File.Delete(LastPath);
            ui.LogOk(string.Format("Đã xoá {0} object ({1} không còn trong model).", ok, miss));
        }

        static void SaveCreated(IList<ModelObject> created)
        {
            try
            {
                var lines = created.Select(o => o.Identifier.GUID.ToString()).ToList();
                if (lines.Count == 0) return;
                Directory.CreateDirectory(Path.GetDirectoryName(LastPath));
                File.WriteAllLines(LastPath, lines);
            }
            catch { }
        }

        static void FileLog(Model model, string text)
        {
            try { File.AppendAllText(Path.Combine(model.GetInfo().ModelPath, "RDS_BUSplice.log"), text + "\r\n"); } catch { }
        }
    }
}
