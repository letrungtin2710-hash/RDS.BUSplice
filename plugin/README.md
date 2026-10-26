# RDS _ BU Splice — plugin Tekla Structures 2020

Splice 2 rafter BU (PEB Member / S94): 2 endplate + bolt, tuỳ chọn flange (1) và stiffener (2). Hình học dùng chung với macro test (`..\src\`), quyết định thiết kế ở `..\SPEC.md`.

## Build & cài đặt
1. (Tuỳ chọn) copy `D:\Tools\RDS.RakerBolt\lib\*.dll` vào `plugin\lib\`. Không có `lib\` thì csproj lấy DLL từ `C:\Program Files\Tekla Structures\2020.0\nt\bin\plugins`.
2. Mở `RDS.BUSplice.csproj` bằng Visual Studio → Build **Release**.
3. **Tắt Tekla** → chạy `CopyToTekla.bat` (tự xin Admin, copy DLL vào `nt\bin\plugins` + thumbnail `et_element_RDS _ BU Splice.bmp` vào `nt\bitmaps`).
4. Mở Tekla → Applications & components → **RDS _ BU Splice**.

## Dùng
Pick 1 part bất kỳ (web / flange) của BU 1 → BU 2 → **chuột giữa**. Thứ tự pick không quan trọng: tool tự xác định rafter nhỏ / lớn. Modify: chọn biểu tượng component → sửa ô (đang tick) → Modify.
Log: `RDS_BUSplice.log` trong thư mục model. Cảnh báo hiện ở thanh prompt dưới đáy Tekla.

## Dialog
| Tab | Nội dung |
|---|---|
| Endplate / Bolts | Hình Joining plates: mép trên / dưới từng endplate, mép trái / phải chung, gốc bolt **Top / Middle** + offset, khoảng cách cột. Cột phải: khoảng cách hàng, xoá bolt số, bolt size / standard / tolerance |
| Flange / Stiffener | Hình splice: L flange (1), L stiff (2). Cột phải: bật/tắt (No/Yes, lăn chuột được), chamfer stiff |
| Parts | t1 / t2 endplate, profile flange (1), t / b stiff; Name / Prefix / Start / Material / Class (Class trống = theo web rafter; Finish luôn theo web rafter) |

Sửa dialog: mở `MainForm.cs` bằng **View Designer** (Shift+F7). Quy tắc bind ghi ở đầu `MainForm.cs`.
- Ô chiều dài: AttributeTypeName = **Distance** → Tekla hiện dạng thập phân (45.00). Ô căn lề trái (TextAlign mặc định).
- Mỗi ô có checkbox lọc bên trái (IsFilter = True).
- Thay hình: chọn `picEndplate` / `picFlange` → Properties → Image → Import (bản gốc ở `Images\`).
- Thêm ô mới: thêm field vào `PluginData.cs` (+ map trong `BUSplicePlugin.ToSettings`), kéo TextBox + CheckBox vào form, đặt 3 thuộc tính structuresExtender.

Create by Snit
