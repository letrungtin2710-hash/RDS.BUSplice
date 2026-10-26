# HANDOFF — RDS BU Splice

## Đang ở đâu
- Code đủ 2 dạng, dùng chung engine `src\` (Geo / Layout / Settings / Engine):
  - **Macro test** `RDS-BUSplice` (`tools\Deploy.bat`, không cần tắt Tekla khi deploy lại).
  - **Plugin** `RDS _ BU Splice` (`plugin\RDS.BUSplice.csproj`, dialog Designer 3 tab, 39 ô bind + 39 checkbox lọc).
- Vòng 4 (26-Oct): font dialog = Microsoft Sans Serif 8.25; bỏ Finish (theo web rafter); mép trái/phải endplate dùng chung (`EpLeft` / `EpRight`).
- Vòng 5 (26-Oct, test live đầu tiên ở nhà): endplate nghiêng ~4.7° so với rafter → sửa trục frame = phương top flange rafter nhỏ; thêm gốc bolt Top / Middle (`BoltVertRef`); lăn chuột trên combo.
- Compile OK bằng mcs với DLL Tekla 2020 (`D:\Tools\RDS.RakerBolt\lib`); test toán thuần **45/45 PASS**.
- Plugin đã chạy live lần đầu (máy nhà): dialog lên, tạo được endplate + bolt; lỗi endplate nghiêng (đã sửa, chờ test lại).
- Dialog = bản Snit tự chỉnh bằng Designer trên máy nhà (t1/t2, flange profile, stiff t/b dời sang tab Parts) + combo `cboBoltVertRef` Top/Middle (đặt trên ô Ep1Top, Snit chỉnh lại vị trí nếu cần). 40 ô bind + 40 checkbox lọc.

## Máy mới (nhà) — chuẩn bị 1 lần
1. `git clone https://github.com/letrungtin2710-hash/RDS.BUSplice.git` (đặt ở đâu cũng được, vd `D:\Tools\RDS.BUSplice`).
2. Cần: Tekla Structures 2020 cài ở `C:\Program Files\Tekla Structures\2020.0` (hoặc sửa `TeklaBin` trong csproj), Visual Studio có .NET Framework 4.8 targeting pack.
3. (Tuỳ chọn) copy DLL Tekla vào `plugin\lib\` — không có thì csproj tự lấy trong thư mục cài Tekla.
4. Macro test: cần .NET SDK (Roslyn) cho `tools\Deploy.bat`; Tekla cài ở `C:\TeklaStructures\2020.0` cũng được (script dò cả 2).

## NEXT SESSION
0. Test lại endplate vuông góc top flange rafter nhỏ (offset Top 80 phải đo đúng 80.00), Middle, lăn chuột trên combo.
1. Plugin: build Release → tắt Tekla → `plugin\CopyToTekla.bat` → test pick-pick-chuột giữa, Modify, checkbox lọc, font dialog.
2. Kiểm tra `Assembly.Add` trong plugin khi component chạy lại (rủi ro chưa verify).
3. Đọc `RDS_BUSplice.log` khi lỗi: hướng bolt (`posErr` ≈ 0, nhất là khi xoá bolt không đối xứng), hướng PLT flange (1), 2 đầu tấm (1) cắt vuông, fitting đúng đầu, cut web.
4. Chỉnh vị trí ô trên hình trong VS Designer nếu lệch đường kích thước.

## Chưa làm / để sau
- Stiff không fitting theo mặt endplate (sai lệch < 1 mm với dốc nhỏ).
- Không tạo weld (Snit tự key trong bản vẽ).
- Chưa có ProfileCatalog / MaterialCatalog trong dialog plugin.

_Last commit: xem `git log -1` · Cập nhật: 26-Oct-2026_
