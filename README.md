# RDS BU Splice — macro test (giai đoạn 1)

Splice 2 rafter BU (PEB Member / S94): 2 endplate + bolt, tuỳ chọn flange (1) và stiffener (2). Quyết định thiết kế: `SPEC.md`.

## Deploy (không cần tắt Tekla khi deploy lại)
1. Chạy `tools\Deploy.bat`. Script build `libs\RDS.BUSplice.Impl.dll` bằng Roslyn (cần .NET SDK), bump AssemblyVersion, ghi stub `RDS-BUSplice.cs` (UTF-8 BOM + CRLF) và dll vào `macros\modeling` (ưu tiên australasia, rồi common).
2. **Lần đầu**: restart Tekla để catalog thấy entry `RDS-BUSplice`. Các lần sau chỉ cần chạy lại `Deploy.bat` rồi chạy lại macro.

## Dùng
1. Applications & components → `RDS-BUSplice` → form mở (nổi trên Tekla).
2. Nhập thông số → **Pick 2 rafter → Tạo** → pick 1 part bất kỳ (web / flange) của BU 1, rồi BU 2. Thứ tự pick không quan trọng: tool tự xác định rafter nhỏ / lớn.
3. **Xoá lần chạy trước**: xoá toàn bộ object (plate, bolt, fitting, cut) của lần chạy gần nhất → sửa thông số → chạy lại.
4. Log: `RDS_BUSplice.log` trong thư mục model. Thông số lưu ở `Desktop\Tekla\RDS-BUSplice.ini`.

## Cấu trúc
| File | Vai trò |
|---|---|
| `src/Geo.cs` | Toán thuần: frame, PCA, đường thẳng 2D, parse list |
| `src/Layout.cs` | Toàn bộ hình học splice trong hệ local (không gọi Tekla) |
| `src/Engine.cs` | Đọc BU từ model, tạo fitting / plate / bolt / cut — dùng lại cho plugin |
| `src/MainForm.cs`, `src/Entry.cs` | Form + entry macro |
| `macros/RDS_Tool_Stub.cs` | Stub C# 5 Tekla compile mỗi lần chạy |
| `tests/GeoTests.cs` | Test offline: `mcs tests/GeoTests.cs src/Geo.cs src/Layout.cs src/Settings.cs && mono GeoTests.exe` |

Create by Snit
