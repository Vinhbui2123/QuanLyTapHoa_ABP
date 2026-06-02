# ĐẶC TẢ HỆ THỐNG QUẢN LÝ TẠP HÓA (RÚT GỌN & CỐT LÕI)

> Tài liệu được rút gọn tối giản cho cửa hàng tạp hóa nhỏ. Triển khai **Code-first** trên **ASP.NET Boilerplate (ABP) 10.2.0** + **.NET 9.0** + **EF Core 9.0.5** + **SQL Server** + **Razor MVC**.

---

## I. TỔNG QUAN HỆ THỐNG & PHÂN QUYỀN

### 1.1 Khái quát vai trò (RBAC)
Hệ thống chỉ thiết lập **2 Role** chính:
- **`admin` (Quản trị viên / Chủ quán)**: Toàn quyền quản lý danh mục, sản phẩm, nhà cung cấp, kiểm kê/nhập/xuất kho, hủy hàng, cấu hình tài khoản và xem các báo cáo doanh thu/lợi nhuận.
- **`cashier` (Thu ngân)**: Thực hiện bán hàng tại quầy (POS), tạo nhanh thông tin khách hàng từ màn hình POS, xem thông tin sản phẩm và xem tồn kho (chế độ Read-only). *Khách hàng không có tài khoản hệ thống.*

### 1.2 Quy chuẩn thiết kế AppService (Dependency Injection)
Để phục vụ mục đích kiểm soát luồng hoạt động, dễ viết kiểm thử và thuận tiện cho việc tiếp cận học tập:
- Dự án **KHÔNG** sử dụng generic `AsyncCrudAppService` và `IAsyncCrudAppService` của ABP cho các thực thể nghiệp vụ mới.
- Quy chuẩn triển khai:
  1. **Interface**: Kế thừa `IApplicationService` và khai báo tường minh các phương thức cần thiết (ví dụ: `GetAsync`, `GetListAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`).
  2. **Class**: Kế thừa `InternProjectAppServiceBase`, thực hiện Constructor Dependency Injection cho các Repository hoặc Manager cần thiết và viết code xử lý dữ liệu thủ công.

---

## II. ĐẶC TẢ DATABASE (CODE-FIRST ENTITIES)

### 2.1 Bảng `Categories` (Danh mục)
- `Name`: Tên danh mục (Bắt buộc, Unique, nvarchar(100)).
- `Description`: Mô tả danh mục (nvarchar(500), Nullable).
- `IsActive`: Trạng thái hoạt động (bit, Default = true).
- *Ràng buộc*: Không cho phép xóa danh mục nếu đang có sản phẩm thuộc danh mục đó.

### 2.2 Bảng `Products` (Sản phẩm)
- `Name`: Tên sản phẩm (Bắt buộc, nvarchar(200)).
- `Sku`: Mã định danh Sku (Unique, nvarchar(50), Nullable).
- `CategoryId`: Khóa ngoại liên kết tới bảng danh mục (Guid?, Nullable, có thể gán sau).
- `CostPrice`: Giá vốn / giá nhập hàng (decimal(18,2), >= 0).
- `SalePrice`: Giá bán ra niêm yết (decimal(18,2), >= 0).
- `StockQuantity`: Tổng số lượng tồn kho (int, tính toán tự động qua InventoryLogs).
- `MinStock`: Ngưỡng cảnh báo sắp hết hàng (int, Default = 10, >= 0).
- `Unit`: Đơn vị tính (nvarchar(20), ví dụ: "cái", "kg").
- `ImageUrl`: Đường dẫn ảnh sản phẩm (nvarchar(500), Nullable).
- `IsActive`: Trạng thái kinh doanh (bit, Default = true).

### 2.3 Bảng `Customers` (Khách hàng)
- `Code`: Mã khách hàng (nvarchar(50), Unique, Nullable).
- `Name`: Tên khách hàng (Bắt buộc, nvarchar(128)).
- `Phone`: Số điện thoại liên lạc (nvarchar(32), Nullable).
- `Address`: Địa chỉ khách hàng (nvarchar(512), Nullable).
- `IsActive`: Trạng thái hoạt động (bit, Default = true).

### 2.4 Bảng `Suppliers` (Nhà cung cấp)
- `Name`: Tên nhà cung cấp (Bắt buộc, nvarchar(100)).
- `Phone`: Số điện thoại liên lạc (nvarchar(32), Nullable).
- `Address`: Địa chỉ văn phòng/kho (nvarchar(256), Nullable).
- `Email`: Thư điện tử (nvarchar(100), Nullable).
- `ContactPerson`: Người đại diện liên hệ (nvarchar(100), Nullable).
- `IsActive`: Trạng thái hoạt động (bit, Default = true).

### 2.5 Bảng `Invoices` & `InvoiceItems` (Hóa đơn bán lẻ)
- **`Invoices` (Hóa đơn)**:
  - `InvoiceNumber`: Mã hóa đơn (Unique, tự động sinh dạng `HD-YYYYMMDD-XXX`).
  - `CustomerId`: Khóa ngoại liên kết khách hàng (Nullable, nếu Null = Khách lẻ).
  - `CashierUserId`: Khóa ngoại người thực hiện thanh toán (User tạo hóa đơn).
  - `TotalAmount`: Tổng tiền hóa đơn (decimal(18,2)).
  - `AmountPaid`: Số tiền khách đưa (decimal(18,2)).
  - `ChangeAmount`: Số tiền thối lại cho khách (decimal(18,2)).
  - `PaymentMethod`: Hình thức thanh toán (`Cash` / `Transfer` / `Momo` / `ZaloPay`).
  - `Status`: Trạng thái (`Pending` / `Completed` / `Cancelled`).
  - `Note`: Ghi chú hóa đơn (nvarchar(500), Nullable).
- **`InvoiceItems` (Chi tiết hóa đơn)**:
  - `InvoiceId`: Liên kết tới hóa đơn chính.
  - `ProductId`: Liên kết tới sản phẩm được mua.
  - `Quantity`: Số lượng mua (int, > 0).
  - `UnitPrice`: Giá bán tại thời điểm lập hóa đơn (decimal(18,2), >= 0).
  - `Subtotal`: Thành tiền từng sản phẩm (= Quantity * UnitPrice).

### 2.6 Bảng `PurchaseOrders` & `PurchaseOrderItems` (Nhập kho từ nhà cung cấp)
- **`PurchaseOrders` (Phiếu nhập hàng)**:
  - `OrderNumber`: Mã phiếu nhập (Unique, tự động sinh dạng `PN-YYYYMMDD-XXX`).
  - `SupplierId`: Khóa ngoại liên kết tới nhà cung cấp (Bắt buộc).
  - `UserId`: Khóa ngoại người lập phiếu nhập.
  - `TotalAmount`: Tổng giá trị nhập hàng (decimal(18,2)).
  - `Status`: Trạng thái phiếu nhập (`Pending` / `Completed` / `Cancelled`).
  - `Note`: Ghi chú phiếu nhập (nvarchar(500), Nullable).
  - *Lưu ý*: Phiếu nhập hàng trong hệ thống mặc định coi như thanh toán ngay cho NCC (không phát sinh công nợ).
- **`PurchaseOrderItems` (Chi tiết phiếu nhập)**:
  - `PurchaseOrderId`: Liên kết tới phiếu nhập chính.
  - `ProductId`: Liên kết tới sản phẩm nhập kho.
  - `Quantity`: Số lượng nhập kho (int, > 0).
  - `UnitPrice`: Giá nhập hàng (decimal(18,2), >= 0).
  - `Subtotal`: Thành tiền từng loại sản phẩm (= Quantity * UnitPrice).
  - `BatchId`: Mã lô hàng để quản lý (nvarchar(50), Nullable).
  - `ExpiryDate`: Hạn sử dụng của sản phẩm trong lô này (datetime, Nullable).

### 2.7 Bảng `InventoryLogs` (Nhật ký kho - Trọng tâm quản lý lô & HSD)
- `ProductId`: Liên kết sản phẩm chịu tác động biến động kho.
- `UserId`: Người tạo giao dịch biến động kho (long?, Nullable, null nếu hệ thống tự động sinh log).
- `Type`: Loại biến động kho (`Import` - Nhập / `Export` - Xuất / `Dispose` - Hủy hàng / `Adjust` - Kiểm kê điều chỉnh).
- `Quantity`: Số lượng biến động (int, > 0).
- `RemainingQuantity`: Số lượng còn lại trong lô (int?, Nullable). *Chỉ sử dụng đối với dòng giao dịch `Type = Import` phục vụ thuật toán xuất kho FIFO.*
- `UnitCostAtTime`: Giá vốn tại thời điểm phát sinh log (decimal(18,2), Nullable). Với Type = Import: bằng giá nhập của lô. Với Type = Export/Dispose: bằng giá nhập của lô đang bị trừ theo FIFO. Phục vụ tính lợi nhuận chính xác.
- `BatchId`: Mã lô hàng liên kết (nvarchar(50), Nullable).
- `ExpiryDate`: Hạn sử dụng của sản phẩm (datetime, Nullable).
- `SupplierId`: Nhà cung cấp của lô hàng (chỉ với `Type = Import`).
- `ReferenceId`: Id của đối tượng tham chiếu gốc (InvoiceId hoặc PurchaseOrderId, Nullable).
- `ReferenceType`: Tên bảng tham chiếu gốc (ví dụ: "Invoice", "PurchaseOrder", Nullable).
- `Note`: Ghi chú chi tiết nguyên nhân biến động (nvarchar(500), Nullable).

---

## III. THUẬT TOÁN & NGHIỆP VỤ KHO CỐT LÕI (FIFO & HSD)

### 3.1 Quy trình Nhập hàng (Import)
1. Thực hiện tạo bản ghi `PurchaseOrder` + các `PurchaseOrderItems`.
2. Với mỗi mặt hàng nhập kho:
   - Tạo mới một bản ghi giao dịch `InventoryLog` có kiểu `Type = Import`.
   - Gán `Quantity = Số lượng nhập`, đồng thời thiết lập `RemainingQuantity = Số lượng nhập`.
   - Lưu thông tin `BatchId`, `ExpiryDate` và `SupplierId` vào bản ghi.
3. Cộng dồn số lượng vừa nhập vào thuộc tính tổng của thực thể sản phẩm: `Product.StockQuantity = Product.StockQuantity + Quantity`.

### 3.2 Quy trình Xuất kho Bán hàng / Hủy hàng (FIFO)
Khi xuất kho sản phẩm (do hóa đơn bán hàng `Export`, xuất hỏng nội bộ `Export`, hoặc hủy hàng hết hạn `Dispose`), hệ thống tự động khấu trừ tồn kho theo nguyên lý **FIFO (Lô có hạn sử dụng gần nhất hoặc nhập trước sẽ được xuất trước)**:
1. Truy vấn các dòng giao dịch `InventoryLog` của sản phẩm đó có `Type = Import` và `RemainingQuantity > 0`.
2. Sắp xếp danh sách lô hàng theo thứ tự ưu tiên: `ExpiryDate ASC` (HSD gần nhất xếp trước), tiếp theo là `CreationTime ASC` (Lô nhập trước xếp trước).
3. Duyệt và trừ dần số lượng cần xuất vào thuộc tính `RemainingQuantity` của từng lô cho tới khi khấu trừ đủ số lượng yêu cầu xuất.
4. Ghi nhận giao dịch xuất kho bằng cách tạo mới bản ghi `InventoryLog` với `Type` tương ứng (`Export` hoặc `Dispose`).
5. Cập nhật thuộc tính tồn kho của sản phẩm: `Product.StockQuantity = Product.StockQuantity - Số lượng xuất`.

### 3.3 Quy trình Kiểm kê & Điều chỉnh (Adjust)
1. Thực hiện so sánh tồn kho thực tế đếm được tại quầy so với thuộc tính tồn kho hệ thống `Product.StockQuantity`.
2. Nếu xảy ra chênh lệch:
   - Tạo mới bản ghi giao dịch `InventoryLog` có kiểu `Type = Adjust`, gán số lượng chênh lệch vào `Quantity` kèm ghi chú giải trình.
   - Cập nhật trực tiếp `Product.StockQuantity` về bằng giá trị tồn kho thực tế kiểm đếm.

---

## IV. PHÂN QUYỀN HỆ THỐNG CHI TIẾT (ROLE TO PERMISSION MAPPING)

| Module / Chức năng | Mã Permission | Admin | Cashier |
| --- | --- | :---: | :---: |
| **Categories** (Danh mục) | `Pages.Categories.*` | ✅ | ❌ |
| **Products** (Sản phẩm) | `Pages.Products` (Xem)<br>`Pages.Products.Create/Edit/Delete` | ✅<br>✅ | ✅<br>❌ |
| **Customers** (Khách hàng) | `Pages.Customers` (Xem)<br>`Pages.Customers.Create`<br>`Pages.Customers.Edit/Delete` | ✅<br>✅<br>✅ | ✅<br>✅<br>❌ |
| **Suppliers** (Nhà cung cấp) | `Pages.Suppliers.*` | ✅ | ❌ |
| **Invoices** (Hóa đơn) | `Pages.Sales.CreateInvoice` (Lập POS)<br>`Pages.Sales.CancelInvoice` (Hủy HĐ) | ✅<br>✅ | ✅<br>❌ |
| **Inventory** (Kho hàng) | `Pages.Inventory` (Xem tồn)<br>`Pages.Inventory.Export / Dispose / Adjust` | ✅<br>✅ | ✅<br>❌ |
| **PurchaseOrders** (Phiếu nhập) | `Pages.PurchaseOrders.*` | ✅ | ❌ |
| **Reports** (Thống kê/Doanh thu) | `Pages.Reports.*` | ✅ | ❌ |