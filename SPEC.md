# RDS BU Splice — SPEC

Component splice 2 rafter thép tổ hợp (BU: fl + w + fl, dựng bằng PEB Member hoặc S94):
2 endplate + bolt, tuỳ chọn tấm flange (1) và stiffener (2).
Giai đoạn 1 = **macro** (test không cần tắt Tekla), giai đoạn 2 = plugin `PluginBase` dùng chung engine.

## Quyết định đã chốt (26-Oct-2026, Snit)

| # | Chủ đề | Quyết định |
|---|---|---|
| A1 | Input | Pick 1 part bất kỳ của BU 1, rồi BU 2. Tool leo lên component cha (`GetFatherComponent`) lấy đủ part con; không có component cha thì lấy assembly (main + secondaries). |
| A2 | Nhận diện | Part tên chứa `FLANGE` = flange (luôn như vậy). Web = part còn lại, tên theo main part (RAFTER, COLUMN…), chọn bằng hình học: plate mỏng theo pháp tuyến mặt phẳng rafter, cao nhất. Top/bottom flange theo cao độ trong hệ toạ độ rafter. |
| A3 | Nhỏ / lớn | So chiều cao (top flange top → bottom flange bottom) tại mặt splice. Bằng nhau (±2 mm) thì rafter có top cao hơn (Z) = "lớn" (nơi đặt flange + stiff). Flange (1) và stiff (2) đều tuỳ chọn → dùng cho splice thường chỉ có endplate + bolt. |
| B1 | Mặt splice | Mặt phẳng ⊥ trục rafter nhỏ, qua đầu rafter nhỏ (điểm xa nhất của rafter nhỏ theo trục, hướng về rafter lớn). Mỗi rafter fitting lùi đúng 1 bề dày endplate của nó. **Endplate không vát**: tấm chữ nhật, nằm trong mặt phẳng splice; rafter lớn được fitting theo mặt phẳng đó (đầu rafter lớn có thể xiên). |
| B2 | Kích thước endplate | Mỗi tấm nhập riêng: t, mép trên / dưới / trái / phải tính từ **bolt ngoài cùng**. Trái/phải nhìn từ rafter nhỏ sang rafter lớn. |
| B3 | Bolt | Tham chiếu Joining plates (14). Layout: tâm hàng trên cùng → top rafter nhỏ (dương = phía trên), list khoảng cách dọc xuống (hỗ trợ `3*100`), list khoảng cách ngang (Tekla dist Y, đối xứng quanh tim web). |
| C1 | Flange (1) | Profile `PLTt*b` nhập tay. Đầu sát endplate: top tấm = top flange rafter nhỏ. Đầu xa (L đo theo trục rafter lớn từ mặt ngoài endplate 2): bottom tấm = bottom top flange rafter lớn. |
| C2 | Cắt rafter lớn | Khi có flange (1): cắt ngắn top flange rafter lớn tới mặt F; cắt web rafter lớn phần trên mặt dưới tấm (1). |
| C3 | Đầu tấm (1) | **Cắt vuông theo phương của tấm, không fitting xiên** (Snit 26-Oct, sửa quyết định cũ). Đầu endplate: góc gần nhất chạm mặt endplate, không góc nào lấn vào endplate. Đầu xa: góc dưới = mặt dưới top flange lớn tại L. Top flange rafter lớn vẫn cắt tại F ⊥ chính nó. |
| D1 | Stiff (2) | 2 tấm, mỗi bên web rafter lớn 1 tấm, song song bottom flange rafter nhỏ, **bottom stiff = bottom face bottom flange rafter nhỏ**. Ô nhập t, b (0 = tự = mép flange rafter lớn), L (từ mặt ngoài endplate 2). |
| D2 | Chamfer stiff | Góc giáp web + endplate, mặc định 15×15. |
| E1 | Assembly | Endplate 1 → assembly rafter nhỏ; endplate 2, flange (1), stiff (2) → assembly rafter lớn (`Assembly.Add`). Bolt Site. |
| B4 | Xoá bolt | Ô "xoá bolt số" kiểu Joining plates (14): đánh số hàng trên→dưới, trong hàng trái→phải (trái = −Z). Có xoá thì dùng BoltXYList, không xoá dùng BoltArray. Kích thước endplate vẫn theo lưới đầy đủ. |
| F1 | Bật/tắt | Checkbox Flange (1) / Stiffener (2) nằm ngoài tab (luôn thấy), tắt thì khoá các ô của nhóm. |
| E2 | Weld | Không tạo. Snit tự key trong bản vẽ. |
| G1 | Plugin (26-Oct) | `RDS _ BU Splice` (PluginBase, thư mục `plugin\`), dùng chung `src\Geo/Layout/Settings/Engine.cs`. Input: pick 2 part → chuột giữa. Dialog 3 tab dùng 2 hình Snit gửi (Joining plates + splice), MainForm sửa được bằng VS Designer. Mọi ô có checkbox lọc; ô chiều dài kiểu Distance (hiện thập phân); ô căn lề trái. Bật/tắt flange / stiff = ComboBox No/Yes. |

## Rủi ro đã biết
- PEB Member chạy lại thì tạo lại part (KNOWLEDGE §7.31): fitting / cut / assembly do tool gắn lên part PEB có thể mất → chạy lại tool sau khi modify PEB.
- Flange nhận diện bằng PCA đỉnh solid trong mặt phẳng rafter: flange phải thẳng (không gãy khúc).
- Rafter giả định nằm trong mặt phẳng đứng (web ⊥ phương ngang), 2 rafter cùng mặt phẳng.
