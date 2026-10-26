# HANDOFF — RDS BU Splice

## Đang ở đâu
- Giai đoạn 1 (macro test) đã viết xong code: engine + layout + form + stub + deploy script.
- Compile OK với DLL Tekla 2020 (`D:\Tools\RDS.RakerBolt\lib`) bằng mcs; test toán thuần 42/42 PASS.
- Vòng 2 (26-Oct): tấm flange (1) cắt vuông theo phương tấm (bỏ fitting xiên), checkbox flange/stiff ngoài tab, ô xoá bolt (BoltXYList).
- Vòng 3 (26-Oct): bản **plugin** `plugin\RDS.BUSplice.csproj` (PluginBase + dialog Designer 3 tab, 44 ô bind + 44 checkbox lọc). Compile OK bằng mcs với DLL Tekla 2020; reflection xác nhận `[Plugin("RDS _ BU Splice")]`, resx 2 hình nhúng đúng.
- **Chưa chạy live trong Tekla** (cả macro lẫn plugin).

## NEXT SESSION
1. Snit chạy `tools\Deploy.bat`, restart Tekla lần đầu, test trên model có PEB Member + S94.
2. Test xoá bolt: log in `posErr` từng rotation, phải ≈ 0. Đọc `RDS_BUSplice.log` khi lỗi: hướng bolt, hướng PLT flange (1), fitting đúng đầu, cut web.
3. Plugin: build Release → tắt Tekla → `plugin\CopyToTekla.bat` → test pick-pick-chuột giữa, Modify, filter checkbox. Kiểm tra `Assembly.Add` trong plugin khi component chạy lại (rủi ro chưa verify).

## Chưa làm / để sau
- Stiff không fitting theo mặt endplate (sai lệch < 1 mm với dốc nhỏ).
- Không tạo weld (Snit tự key trong bản vẽ).
- Form macro không có hình (bản plugin có).
- Chưa có ProfileCatalog / MaterialCatalog trong dialog plugin.

_Cập nhật: 26-Oct-2026_
