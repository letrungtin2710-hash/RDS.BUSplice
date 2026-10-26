# HANDOFF — RDS BU Splice

## Đang ở đâu
- Giai đoạn 1 (macro test) đã viết xong code: engine + layout + form + stub + deploy script.
- Compile OK với DLL Tekla 2020 (`D:\Tools\RDS.RakerBolt\lib`) bằng mcs; test toán thuần 31/31 PASS.
- **Chưa chạy live trong Tekla.**

## NEXT SESSION
1. Snit chạy `tools\Deploy.bat`, restart Tekla lần đầu, test trên model có PEB Member + S94.
2. Đọc `RDS_BUSplice.log` khi lỗi: hướng bolt, hướng PLT flange (1), fitting đúng đầu, cut web.
3. Sau khi hình học chốt: giai đoạn 2 = plugin `PluginBase` (pick-pick-chuột giữa, dialog Designer theo quy tắc rds-tekla-plugin, tab Picture có hình), dùng lại `Engine` + `Layout`.

## Chưa làm / để sau
- Stiff không fitting theo mặt endplate (sai lệch < 1 mm với dốc nhỏ).
- Không tạo weld (Snit tự key trong bản vẽ).
- Chưa có hình minh hoạ trong form macro.

_Cập nhật: 26-Oct-2026_
