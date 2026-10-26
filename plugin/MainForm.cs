using System;
using System.Globalization;
using System.Windows.Forms;
using Tekla.Structures.Dialog;

namespace RDS.BUSplice
{
    /// <summary>
    /// Dialog plugin RDS _ BU Splice - mo bang Visual Studio Designer (MainForm.Designer.cs + MainForm.resx).
    ///
    /// Quy tac khi tu sua dialog trong Designer (Tekla.Structures.Dialog 2020):
    ///  - Moi o nhap co AttributeName (= ten StructuresField trong PluginData.cs), AttributeTypeName
    ///    (Distance cho chieu dai -> Tekla hien so thap phan; Integer; String) va BindPropertyName
    ///    (Text cho TextBox, SelectedIndex cho ComboBox) - muc "on structuresExtender" trong Properties.
    ///  - Checkbox loc: cung AttributeName, AttributeTypeName de trong, BindPropertyName = Checked, IsFilter = True.
    ///  - O nhap can le trai (TextAlign = Left, mac dinh). Khong tao control bang code neu can bind.
    ///  - Hinh: chon picEndplate / picFlange -> Properties -> Image -> Import (ban goc o Images\).
    ///  - Khong goi InitializeForm(): Tekla tu goi.
    /// </summary>
    public partial class MainForm : PluginFormBase
    {
        public MainForm()
        {
            InitializeComponent();
            SetComboDefaults();
            WheelOnHover(this);
            cboFlangeOn.SelectedIndexChanged += (s, e) => EnableGroup(cboFlangeOn, "FlangeProfile", "FlangeLength", "FlName", "FlPrefix", "FlStart", "FlMaterial", "FlClass");
            cboStiffOn.SelectedIndexChanged += (s, e) => EnableGroup(cboStiffOn, "StiffT", "StiffB", "StiffL", "StiffChamX", "StiffChamY", "StName", "StPrefix", "StStart", "StMaterial", "StClass");
        }

        /// <summary>ComboBox trong Designer khong luu SelectedIndex - dat mac dinh o day (Tekla ghi de bang file attribute neu co).</summary>
        private void SetComboDefaults()
        {
            foreach (var kv in Defaults.Values)
            {
                Control[] found = Controls.Find("cbo" + kv.Key, true);
                if (found.Length == 0) continue;
                var cb = found[0] as ComboBox;
                if (cb == null || cb.Items.Count == 0) continue;
                int i = Convert.ToInt32(kv.Value, CultureInfo.InvariantCulture);
                if (i >= 0 && i < cb.Items.Count) cb.SelectedIndex = i;
            }
        }

        /// <summary>
        /// Lan chuot tren ComboBox (No/Yes, Top/Middle) doi gia tri ngay khi re chuot len, khong can click truoc:
        /// WinForms chi gui MouseWheel cho control dang focus -> focus combo khi chuot vao.
        /// </summary>
        private static void WheelOnHover(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                var cb = c as ComboBox;
                if (cb != null && cb.DropDownStyle == ComboBoxStyle.DropDownList)
                    cb.MouseEnter += (s, e) => { if (cb.Enabled && !cb.ContainsFocus) cb.Focus(); };
                if (c.HasChildren) WheelOnHover(c);
            }
        }

        /// <summary>Flange / Stiff = No -> khoa o nhap cua nhom (gia tri van luu, chi khong dung).</summary>
        private void EnableGroup(ComboBox onOff, params string[] attrs)
        {
            bool on = onOff.SelectedIndex != 0;
            foreach (string a in attrs)
                foreach (Control c in Controls.Find("txt" + a, true)) c.Enabled = on;
        }

        private void okApply_OkClicked(object sender, EventArgs e) { Apply(); Close(); }
        private void okApply_ApplyClicked(object sender, EventArgs e) { Apply(); }
        private void okApply_ModifyClicked(object sender, EventArgs e) { Modify(); }
        private void okApply_GetClicked(object sender, EventArgs e) { Get(); }
        private void okApply_OnOffClicked(object sender, EventArgs e) { ToggleSelection(); }
        private void okApply_CancelClicked(object sender, EventArgs e) { Close(); }

        // Handler rong do VS Designer sinh (Designer.cs dang tham chieu) - xoa thi phai go dong "+=" trong Designer truoc.
        private void lbl1_Click(object sender, EventArgs e) { }
        private void lbl6_Click(object sender, EventArgs e) { }
        private void lbl26_Click(object sender, EventArgs e) { }
        private void chkEpClass_CheckedChanged(object sender, EventArgs e) { }
        private void txtEpClass_TextChanged(object sender, EventArgs e) { }
    }
}
