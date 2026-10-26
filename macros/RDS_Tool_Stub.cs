// ============================================================================
// RDS BU Splice - STUB. Day la file DUY NHAT duoc deploy lam entry catalog
// (<entry>.cs trong macros\modeling cua Tekla). No KHONG chua logic nao.
// Khuon lay tu RDS Auto Purlin V1.0 (10-Oct-2026), ban rut gon.
// Doi ten: thay "RDS.BUSplice.Impl.dll", "RDS.MacroImpl.BUSpliceImpl", "RDS-BUSplice", "RDS BU Splice".
// Impl phai co class RDS.MacroImpl.BUSpliceImpl voi ham: public static void Run(Tekla.Macros.Runtime.IMacroRuntime runtime)
//
// TAI SAO PHAI CO STUB (KNOWLEDGE.md 7.17, bang chung live 26-Jul-2026):
//   Tekla LUON tu compile lai file .cs cua entry moi lan chay (compile loi thi nuot lang).
//   Logic nam trong RDS.BUSplice.Impl.dll (build bang Roslyn, C# moi thoai mai, obfuscate duoc),
//   stub chi nap va goi.
//
// Rang buoc cua file nay (KHONG duoc quen):
//   - Compiler cua Tekla la CodeDom C# 5.0: khong "=>", khong "?.", khong $"...",
//     khong pattern matching. Giu C# 5 thuan.
//   - Ghi UTF-8 CO BOM khi deploy (Deploy-Macro.ps1 lo).
// ============================================================================
#pragma warning disable 1633
#pragma reference "Tekla.Macros.Runtime"
#pragma reference "System"
#pragma reference "System.Windows.Forms"
#pragma warning restore 1633

namespace UserMacros {
    public sealed class Macro {
        const string ImplDll = "RDS.BUSplice.Impl.dll";
        const string ImplType = "RDS.MacroImpl.BUSpliceImpl";
        const string TempFolder = "RDS-BUSplice";
        const string Title = "RDS BU Splice";

        // Native MessageBox voi MB_TOPMOST|MB_SETFOREGROUND: Tekla fullscreen khong che duoc dialog.
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        static extern int MessageBoxW(System.IntPtr hWnd, string text, string caption, uint type);

        static void Box(string msg) {
            try { MessageBoxW(System.IntPtr.Zero, msg, Title, 0x0 | 0x10 | 0x40000 | 0x10000); }
            catch { System.Windows.Forms.MessageBox.Show(msg, Title); }
        }

        static bool SameBytes(string a, string b) {
            if (!System.IO.File.Exists(b)) return false;
            byte[] x = System.IO.File.ReadAllBytes(a), y = System.IO.File.ReadAllBytes(b);
            if (x.Length != y.Length) return false;
            for (int i = 0; i < x.Length; i++) if (x[i] != y[i]) return false;
            return true;
        }

        // Copy impl vao %TEMP%\<TempFolder>\<ticks_size>[_n] va tra ve duong dan de LoadFrom.
        // Loi bat ky -> nap thang tu thu muc macro.
        static string ShadowCopy(string dll) {
            try {
                System.IO.FileInfo fi = new System.IO.FileInfo(dll);
                string key = fi.LastWriteTimeUtc.Ticks + "_" + fi.Length;
                string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), TempFolder);
                try { System.IO.DirectoryInfo rd = System.IO.Directory.CreateDirectory(root); rd.Attributes = rd.Attributes | System.IO.FileAttributes.Hidden; } catch { }
                for (int n = 0; n < 20; n++) {
                    string sh = System.IO.Path.Combine(root, n == 0 ? key : key + "_" + n);
                    string td = System.IO.Path.Combine(sh, ImplDll);
                    try {
                        if (!System.IO.Directory.Exists(sh)) System.IO.Directory.CreateDirectory(sh);
                        if (!SameBytes(dll, td)) System.IO.File.Copy(dll, td, true);
                        if (SameBytes(dll, td)) { CleanOld(root, sh); return td; }
                    } catch { }   // dang bi khoa (da nap ban khac) -> thu thu muc ke tiep
                }
            } catch { }
            return dll;
        }

        // Xoa cac ban copy cu (build truoc). Ban dang nap trong 1 phien Tekla khac bi khoa -> bo qua, lan sau xoa.
        static void CleanOld(string root, string keep) {
            try {
                foreach (string d in System.IO.Directory.GetDirectories(root))
                    if (!string.Equals(d, keep, System.StringComparison.OrdinalIgnoreCase))
                        try { System.IO.Directory.Delete(d, true); } catch { }
                foreach (string f in System.IO.Directory.GetFiles(root))
                    try { System.IO.File.Delete(f); } catch { }
            } catch { }
        }

        [Tekla.Macros.Runtime.MacroEntryPointAttribute()]
        public static void Run(Tekla.Macros.Runtime.IMacroRuntime runtime) {
            try {
                // Thu muc chua chinh assembly stub = macro dir cua Tekla (noi Deploy dat impl dll).
                string dir = System.IO.Path.GetDirectoryName(
                    new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
                string dll = System.IO.Path.Combine(dir, ImplDll);
                if (!System.IO.File.Exists(dll)) {
                    Box("Thieu " + ImplDll + " trong thu muc macro.\n\nCai lai tool.");
                    return;
                }
                // LoadFrom (khong phai Load(byte[])): context LoadFrom con do tim dependency ngay canh dll.
                // LoadFrom CACHE THEO DUONG DAN trong 1 process Tekla: deploy ban moi cung ten file van
                // nhan lai ban cu da nap (verified 1-Oct-2026). Nen nap tu ban copy trong
                // %TEMP%\<TempFolder>\<ticks ghi file> -> moi build 1 duong dan moi, khong can restart Tekla.
                // Neu impl co dll phu thuoc dat canh no: copy them vao cung thu muc shadow trong ShadowCopy.
                string load = ShadowCopy(dll);
                System.Reflection.Assembly asm = System.Reflection.Assembly.LoadFrom(load);
                System.Type t = asm.GetType(ImplType);
                if (t == null) { Box("File tool hong (khong thay " + ImplType + ")."); return; }
                System.Reflection.MethodInfo m = t.GetMethod("Run",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (m == null) { Box("File tool hong (khong thay entry Run)."); return; }
                m.Invoke(null, new object[] { runtime });
            } catch (System.Exception ex) {
                System.Exception e = ex;
                if (e is System.Reflection.TargetInvocationException && e.InnerException != null)
                    e = e.InnerException;
                Box("Loi khoi dong " + Title + ":\n\n" + e.Message);
            }
        }
    }
}
