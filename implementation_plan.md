# Kế Hoạch Thiết Kế - Giao Diện Sáng Nhận Diện Thương Hiệu & Trang Chủ Admin Cao Cấp

Kế hoạch này chi tiết hóa việc nâng cấp toàn bộ giao diện vai trò **Admin** sang tông màu **Sáng Nhận Diện PLO** sang trọng, mang bản sắc riêng của dự án (sử dụng sắc Tím Violet, Hồng Rose và Vàng Amber trên nền kem ấm) kết hợp kính mờ (glassmorphism) và xây dựng lại trang chủ Admin với lời chào dành riêng cho Xếp: *"Chào Mừng Xếp Đã Quay Lại. Hôm Nay tôi có thể giúp gì được cho Xếp."*

---

## 1. Phối Màu Giao Diện Sáng Nhận Diện PLO (PLO Signature Light Theme)

Loại bỏ màu xám lạnh, chuyển sang hệ màu pastel ấm áp và rực rỡ đồng bộ với giao diện chính của PLO:

| Thành phần giao diện | Cách phối màu & CSS | Ý nghĩa thiết kế |
| :--- | :--- | :--- |
| **Nền chính (Body Background)** | `linear-gradient(135deg, #fff7ed 0%, #faf5ff 50%, #fff1f2 100%)` | Sự kết hợp cực kỳ tinh tế giữa màu cam kem, tím nhạt và hồng phấn. Không bị chói mắt và tràn ngập năng lượng giáo dục. |
| **Thanh Sidebar** | `rgba(255, 255, 255, 0.8)` kết hợp `backdrop-filter: blur(16px)` và viền hồng nhạt `rgba(231, 84, 133, 0.15)` | Kính mờ màu sáng với viền nhuốm ánh hồng thương hiệu PLO thay vì viền xám. |
| **Thẻ thông tin & Khung nội dung** | `rgba(255, 255, 255, 0.85)` và viền tím nhạt `1px solid rgba(124, 58, 237, 0.08)` | Các thẻ hiển thị dữ liệu màu trắng sứ nổi bật trên nền pastel, viền ánh tím nhẹ. |
| **Màu chữ chính (Primary Text)** | Tiêu đề: `#2e1065` (Tím đậm Hoàng Gia). Thân bài: `#4c1d95` (Tím thẫm). | Thay thế chữ đen/xám bằng tông tím thẫm sang trọng, có độ tương phản cực kỳ cao. |
| **Màu chữ phụ (Muted Text)** | `#7c3aed` (Tím Violet) với độ mờ nhạt hoặc `#db2777` (Hồng Rose) | Nhấn mạnh các ngày tháng hoặc thông tin phụ bằng sắc tím/hồng nhẹ nhàng. |
| **Điểm nhấn chính (Accents)** | `linear-gradient(135deg, #7c3aed 0%, #e75485 100%)` (Tím Violet sang Hồng Rose) | Dành cho các nút bấm hành động chính, nút đang được chọn (active sidebar). |
| **Đổ bóng (Shadows)** | `box-shadow: 0 10px 30px rgba(124, 58, 237, 0.04)` | Đổ bóng nhạt mang ánh tím nhạt tạo hiệu ứng chiều sâu thời thượng. |

---

## 2. Thiết Kế Thích Ứng Mọi Màn Hình (Responsive Layout)

Căn chỉnh khoảng cách biên (margin/padding) và cỡ chữ tự động co giãn theo từng kích thước màn hình để không bị tràn:

*   **Màn hình PC lớn (>= 1200px)**:
    *   Sidebar rộng cố định `280px` ở bên trái.
    *   Nội dung chính thụt lề trái `280px`, khoảng đệm (padding) rộng `40px` để không gian thoáng đãng.
    *   Hiển thị lưới 4 cột (dành cho thẻ thống kê) và 2 cột (dành cho biểu đồ/bảng dữ liệu).
*   **Laptop & PC nhỏ (992px - 1199px)**:
    *   Sidebar co lại còn `260px` để nhường chỗ cho nội dung.
    *   Khoảng đệm nội dung giảm còn `30px`.
    *   Lưới thống kê tự co giãn còn 3 cột, biểu đồ chuyển sang dạng 1 cột dọc.
*   **Máy tính bảng (768px - 991px)**:
    *   Sidebar tự động ẩn đi (`transform: translateX(-100%)`).
    *   Nội dung chính tràn ra toàn màn hình (margin-left: 0), padding là `24px`.
    *   Xuất hiện nút Menu (Hamburger) trên Header. Khi Admin bấm nút này, Sidebar sẽ trượt từ trái ra ngoài đè lên nội dung để chọn tác vụ.
*   **Điện thoại di động (<= 767px)**:
    *   Sidebar ẩn, hoạt động dạng ngăn kéo trượt (drawer) when bấm nút menu.
    *   Padding nội dung thu nhỏ tối đa còn `16px` để tối ưu không gian hiển thị.
    *   Các bảng biểu dữ liệu được bao bọc bởi lớp cuộn ngang để không làm vỡ giao diện.
    *   Font chữ tiêu đề tự động giảm đi 20% để tránh xuống dòng đột ngột. Lưới dữ liệu chuyển hoàn toàn thành 1 cột dọc.

---

## 3. Các Tệp Sẽ Thay Đổi

### Giao diện và Phong cách

#### [MODIFY] [_AdminLayout.cshtml](file:///d:/EXE202/EXE_OnlineLearning/Views/Shared/_AdminLayout.cshtml)
- Thiết kế lại toàn bộ thẻ `<style>` để đổi sang tông màu sáng nhận diện PLO (nền kem sữa chuyển sắc, viền tím/hồng nhạt, chữ tím đậm, các nút bấm tím hồng chuyển màu).
- Thêm các quy tắc truy vấn phương tiện (`@@media`) để điều chỉnh tự động khoảng cách padding, sidebar, kích thước chữ cho các breakpoint: `1200px`, `992px`, `768px`, `576px`.

#### [MODIFY] [Index.cshtml](file:///d:/EXE202/EXE_OnlineLearning/Views/Home/Index.cshtml)
- Thêm điều kiện kiểm tra ở đầu trang: Nếu người dùng có role là `Admin`, thay vì hiển thị trang giới thiệu dịch vụ (landing page) công cộng, hệ thống sẽ kết xuất ra giao diện **Admin Dashboard**.
- **Bố cục Dashboard Admin**:
  - **Khung Chào Mừng Lớn**: Đặt ở đầu trang, thiết kế dạng thẻ kính mờ sáng rực rỡ với dòng chữ:
    - *"Chào Mừng Xếp Đã Quay Lại. Hôm Nay tôi có thể giúp gì được cho Xếp."* (Dùng màu chữ chuyển sắc gradient Tím-Hồng nổi bật).
    - Hiển thị ngày giờ hiện tại và trạng thái máy chủ.
  - **Hệ thống 4 Thẻ Thống Kê Nhanh**:
    - Số học viên hoạt động (Liên kết sang `/User`).
    - Tổng doanh thu hệ thống (Liên kết sang `/Revenue`).
    - Số gói học đang mở bán (Liên kết sang `/Package`).
    - Số giao dịch đang chờ duyệt (Liên kết sang `/Transaction`).
  - **Lối tắt tác vụ nhanh**: Các nút hành động nhanh giúp Xếp thêm nhanh Môn học, duyệt nhanh giao dịch hoặc xem báo cáo ngay tức thì.

---

## 4. Kế Hoạch Nghiệm Thu (Verification)

### Kiểm tra biên dịch
- Chạy lệnh `dotnet build` để đảm bảo code Razor không bị lỗi cú pháp.

### Kiểm tra thủ công bằng Trình duyệt
- Đăng nhập bằng tài khoản Admin (`admin` / `Admin@123`).
- Xác nhận trang chủ hiển thị đúng lời chào và bố cục Dashboard của Admin.
- Thay đổi kích thước trình duyệt (hoặc dùng chế độ Responsive F12 trong Chrome) để kiểm tra giao diện trên màn hình Desktop lớn, Laptop nhỏ, iPad (Tablet) và các dòng iPhone/Android (Mobile).
