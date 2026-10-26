// =============================================================================
// RDS BU Splice - form macro (giai doan 1). Control tao bang code (macro khong can bind Tekla).
// Quy tac macro (KNOWLEDGE §2): modeless Show() + pump loop, TopMost, KHONG set owner = Tekla.
// =============================================================================
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RDS.BUSplice
{
    public sealed class MainForm : Form
    {
        static readonly Color CBlue = Color.FromArgb(24, 95, 165), CNavy = Color.FromArgb(31, 56, 100),
            CBorder = Color.FromArgb(190, 196, 206), CRed = Color.FromArgb(185, 28, 28), CLogBg = Color.FromArgb(250, 250, 248),
            COk = Color.FromArgb(27, 122, 62), CWarn = Color.FromArgb(214, 130, 10), CErr = Color.FromArgb(163, 45, 45);

        readonly Dictionary<string, Control> _fields = new Dictionary<string, Control>();
        readonly RichTextBox _log;
        public event Action RunClicked, UndoClicked;
        public bool Busy;

        public MainForm()
        {
            Text = "RDS BU Splice (macro test)";
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9f);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            ClientSize = new Size(560, 724);

            // ---------------- bat / tat flange (1) + stiff (2): luon thay, ngoai tab ----------------
            var onFl = new CheckBox { Text = "Flange (1)", Location = new Point(12, 10), AutoSize = true, ForeColor = CNavy, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            var onSt = new CheckBox { Text = "Stiffener (2)", Location = new Point(140, 10), AutoSize = true, ForeColor = CNavy, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            Controls.Add(onFl); Controls.Add(onSt);
            _fields["FlangeOn"] = onFl; _fields["StiffOn"] = onSt;
            onFl.CheckedChanged += (s, e) => EnableGroup(onFl.Checked, "Flange", "PFlange.");
            onSt.CheckedChanged += (s, e) => EnableGroup(onSt.Checked, "Stiff", "PStiff.");

            var tabs = new TabControl { Location = new Point(8, 36), Size = new Size(544, 440) };
            Controls.Add(tabs);

            // ---------------- Tab Endplate / Bolt ----------------
            var t1 = NewTab(tabs, "Endplate / Bolt");
            int y = 10;
            y = Header(t1, "Endplate (mép tính từ bolt ngoài cùng)", y);
            y = Row(t1, y, "", new[] { "t", "Trên", "Dưới" }, null);
            y = Row(t1, y, "Endplate 1 (nhỏ)", null, new[] { "Ep1T", "Ep1Top", "Ep1Bot" });
            y = Row(t1, y, "Endplate 2 (lớn)", null, new[] { "Ep2T", "Ep2Top", "Ep2Bot" });
            y = Row(t1, y, "", new[] { "Trái", "Phải" }, null);
            y = Row(t1, y, "Cả 2 endplate", null, new[] { "EpLeft", "EpRight" });
            Note(t1, y, "Trái / phải: đứng ở rafter nhỏ nhìn sang rafter lớn."); y += 26;
            y = Header(t1, "Bolt", y);
            y = Field(t1, y, "Gốc bolt: 0 = Top, 1 = Middle", "BoltVertRef", 80);
            y = Field(t1, y, "Offset (Top: hàng trên → top nhỏ; Middle: tâm)", "BoltTopOffset", 80);
            y = Field(t1, y, "Khoảng cách hàng, tính xuống (vd 125 600 3*100)", "BoltDistDown", 200);
            y = Field(t1, y, "Khoảng cách cột (đối xứng tim web)", "BoltDistAcross", 200);
            y = Field(t1, y, "Xoá bolt số (hàng trên→dưới, trái→phải; vd 1 2)", "BoltDelete", 200);
            y = Field(t1, y, "Bolt size", "BoltSize", 80);
            y = Field(t1, y, "Bolt standard", "BoltStandard", 120);
            Field(t1, y, "Tolerance", "BoltTolerance", 80);

            // ---------------- Tab Flange / Stiff ----------------
            var t2 = NewTab(tabs, "Flange (1) / Stiff (2)");
            y = 10;
            y = Header(t2, "Flange (1) - cắt top flange / web rafter lớn, 2 đầu tấm cắt vuông", y);
            y = Field(t2, y, "Profile (PLTt*b)", "FlangeProfile", 120);
            y = Field(t2, y, "Chiều dài L từ mặt endplate (dọc top flange lớn)", "FlangeLength", 80);
            y += 10;
            y = Header(t2, "Stiffener (2) - 2 tấm, mỗi bên web", y);
            y = Field(t2, y, "t", "StiffT", 80);
            y = Field(t2, y, "b (0 = tới mép flange rafter lớn)", "StiffB", 80);
            y = Field(t2, y, "L từ mặt endplate", "StiffL", 80);
            y = Field(t2, y, "Chamfer X (góc web + endplate)", "StiffChamX", 80);
            Field(t2, y, "Chamfer Y", "StiffChamY", 80);

            // ---------------- Tab Parts ----------------
            var t3 = NewTab(tabs, "Parts");
            y = 10;
            y = Row(t3, y, "", new[] { "Name", "Prefix", "Start", "Material", "Class" }, null, 72);
            y = Row(t3, y, "Endplate", null, Keys("PEndplate"), 72);
            y = Row(t3, y, "Flange (1)", null, Keys("PFlange"), 72);
            y = Row(t3, y, "Stiff (2)", null, Keys("PStiff"), 72);
            Note(t3, y + 4, "Class trống = theo web rafter. Finish luôn theo web rafter.");

            // ---------------- nut ----------------
            var run = Btn("Pick 2 rafter → Tạo", true); run.Location = new Point(8, 484); run.Size = new Size(200, 32);
            run.Click += (s, e) => { if (!Busy) RunClicked?.Invoke(); };
            var undo = Btn("Xoá lần chạy trước", false); undo.ForeColor = CErr; undo.Location = new Point(216, 484); undo.Size = new Size(150, 32);
            undo.Click += (s, e) => { if (!Busy) UndoClicked?.Invoke(); };
            var def = Btn("Mặc định", false); def.Location = new Point(374, 484); def.Size = new Size(90, 32);
            def.Click += (s, e) => { if (!Busy) { SetSettings(new Settings()); Log("Đã nạp giá trị mặc định", CNavy); } };
            Controls.AddRange(new Control[] { run, undo, def });

            _log = new RichTextBox { Location = new Point(8, 524), Size = new Size(544, 172), ReadOnly = true, BackColor = CLogBg, Font = new Font("Consolas", 8.5f), BorderStyle = BorderStyle.FixedSingle };
            Controls.Add(_log);

            var cr = new Label
            {
                Text = "(c) 2026 Rockford Drafting Services (RDS). All rights reserved.   Create by Snit",
                ForeColor = CRed, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter, Location = new Point(0, 700), Size = new Size(560, 20)
            };
            Controls.Add(cr);
            Shown += (s, e) => Activate();
        }

        /// <summary>Mo / khoa cac o thuoc nhom (ten key bat dau bang prefix), tru chinh o bat tat.</summary>
        void EnableGroup(bool on, params string[] prefixes)
        {
            foreach (var kv in _fields)
            {
                if (kv.Key.EndsWith("On")) continue;
                foreach (string pf in prefixes)
                    if (kv.Key.StartsWith(pf)) kv.Value.Enabled = on;
            }
        }

        static string[] Keys(string p) => new[] { p + ".Name", p + ".Prefix", p + ".Start", p + ".Material", p + ".Class" };

        TabPage NewTab(TabControl tabs, string text)
        {
            var tp = new TabPage(text) { BackColor = Color.White };
            tabs.TabPages.Add(tp);
            tp.Size = new Size(tabs.Width - 8, tabs.Height - 28);   // KNOWLEDGE §2: dat size truoc khi them control
            return tp;
        }

        int Header(Control parent, string text, int y)
        {
            parent.Controls.Add(new Label { Text = text, ForeColor = CNavy, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Location = new Point(8, y), AutoSize = true });
            return y + 22;
        }

        void Note(Control parent, int y, string text) =>
            parent.Controls.Add(new Label { Text = text, ForeColor = Color.DimGray, Location = new Point(8, y), AutoSize = true });

        int Field(Control parent, int y, string label, string key, int w)
        {
            parent.Controls.Add(new Label { Text = label, Location = new Point(8, y + 3), Size = new Size(300, 18) });
            var tb = new TextBox { Location = new Point(312, y), Width = w };
            parent.Controls.Add(tb); _fields[key] = tb;
            return y + 26;
        }

        /// <summary>1 hang: nhan ben trai + (tieu de cot | o nhap).</summary>
        int Row(Control parent, int y, string label, string[] heads, string[] keys, int w = 56)
        {
            parent.Controls.Add(new Label { Text = label, Location = new Point(8, y + 3), Size = new Size(110, 18) });
            int n = heads != null ? heads.Length : keys.Length;
            for (int i = 0; i < n; i++)
            {
                int x = 120 + i * (w + 4);
                if (heads != null)
                    parent.Controls.Add(new Label { Text = heads[i], Location = new Point(x, y + 3), Size = new Size(w, 18), ForeColor = CNavy });
                else
                {
                    var tb = new TextBox { Location = new Point(x, y), Width = w };
                    parent.Controls.Add(tb); _fields[keys[i]] = tb;
                }
            }
            return y + 26;
        }

        Button Btn(string text, bool primary)
        {
            var b = new Button { Text = text, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            if (primary) { b.BackColor = CBlue; b.ForeColor = Color.White; b.Font = new Font("Segoe UI", 9f, FontStyle.Bold); b.FlatAppearance.BorderSize = 0; }
            else { b.BackColor = Color.White; b.ForeColor = CNavy; b.FlatAppearance.BorderColor = CBorder; }
            return b;
        }

        public void SetSettings(Settings s)
        {
            foreach (var kv in _fields)
            {
                string v = s.GetValue(kv.Key) ?? "";
                if (kv.Value is CheckBox cb) cb.Checked = v == "1";
                else kv.Value.Text = v;
            }
            EnableGroup(s.FlangeOn, "Flange", "PFlange.");
            EnableGroup(s.StiffOn, "Stiff", "PStiff.");
        }

        /// <summary>Doc form -> Settings. O sai dinh dang -> nem loi kem ten o.</summary>
        public Settings GetSettings()
        {
            var s = new Settings();
            foreach (var kv in _fields)
            {
                string v = kv.Value is CheckBox cb ? (cb.Checked ? "1" : "0") : kv.Value.Text;
                try { s.SetValue(kv.Key, v); }
                catch { throw new InvalidOperationException("Giá trị sai ở ô " + kv.Key + ": '" + v + "'"); }
            }
            return s;
        }

        public void Log(string msg, Color? c = null)
        {
            _log.SelectionStart = _log.TextLength;
            _log.SelectionColor = c ?? Color.Black;
            _log.AppendText(DateTime.Now.ToString("HH:mm:ss ") + msg + "\n");
            _log.ScrollToCaret();
        }

        public void LogOk(string m) => Log(m, COk);
        public void LogWarn(string m) => Log(m, CWarn);
        public void LogErr(string m) => Log(m, CErr);
    }
}
