# Phân tích & Thiết kế hệ thống

# Phân tích & Thiết kế hệ thống — InternProject

> Tài liệu cho phần **Phân tích & Thiết kế** trong báo cáo. Đã cập nhật theo yêu cầu: màn POS hiển thị ảnh sản phẩm.
> 

<aside>
🎯

**Quyết định thiết kế cho ảnh sản phẩm**: Chỉ thêm trường `ImageUrl` (string, nullable, max 500) vào entity `Product`. KHÔNG tách bảng `ProductImage` riêng. **Ảnh lưu trên disk** (`wwwroot/uploads/products/`), DB chỉ lưu URL/path.

**Lý do**:
• Mỗi SP chỉ cần 1 ảnh đại diện cho POS — không cần gallery.
• Query nhanh: load grid POS chỉ 1 SELECT, không JOIN.
• Tránh bloat DB (ảnh kiểu BLOB làm bảng to, backup chậm).
• Nếu sau cần nhiều ảnh → mở rộng bằng `ProductImage` table, không phá vỡ schema.

</aside>

---

## I. PHÂN TÍCH YÊU CẦU

### 1.1 Yêu cầu chức năng

| Mã | Yêu cầu | Actor |
| --- | --- | --- |
| FR-01 | Đăng nhập / Đăng xuất | Tất cả |
| FR-02 | Quản lý danh mục (CRUD) | Admin |
| FR-03 | Quản lý sản phẩm + **upload ảnh** | Admin |
| FR-04 | Quản lý khách hàng | Admin, Cashier (tạo nhanh) |
| FR-05 | Quản lý nhà cung cấp | Admin |
| FR-06 | Nhập hàng theo lô + HSD | Admin |
| FR-07 | **Bán hàng POS — hiển thị ảnh + tên + giá** | Admin, Cashier |
| FR-08 | Xuất kho thủ công / Hủy hàng | Admin |
| FR-09 | Kiểm kê kho | Admin |
| FR-10 | Cảnh báo SP sắp hết hàng / hết hạn | Admin |
| FR-11 | Báo cáo doanh thu / lợi nhuận / tồn kho | Admin |
| FR-12 | Hủy hóa đơn + hoàn kho | Admin |
| FR-13 | In hóa đơn | Admin, Cashier |
| FR-14 | Quản lý user + phân quyền | Admin |

### 1.2 Yêu cầu phi chức năng

- **Hiệu năng**: màn POS load < 2s, tạo hóa đơn < 1s.
- **Bảo mật**: RBAC, JWT cho API, Cookie cho UI, Antiforgery cho mọi POST.
- **Ảnh SP**: validate mime + size <= 2MB, resize 500x500 px khi upload (giảm băng thông).
- **Đồ tin cậy**: Transaction cho nghiệp vụ tiền + kho. Soft delete để khôi phục.

---

## II. SƠ ĐỒ USE CASE

```mermaid
graph LR
	Admin((Admin))
	Cashier((Cashier))

	Admin --- UC1[Quản lý danh mục]
	Admin --- UC2[Quản lý SP + ảnh]
	Admin --- UC3[Quản lý NCC]
	Admin --- UC4[Nhập hàng theo lô]
	Admin --- UC5[Quản lý kho]
	Admin --- UC6[Xem báo cáo]
	Admin --- UC7[Quản lý user]
	Admin --- UC8[Hủy hóa đơn]

	Cashier --- UC9[Bán hàng POS]
	Cashier --- UC10[Tạo nhanh khách hàng]
	Cashier --- UC11[In hóa đơn]
	Cashier --- UC12[Xem tồn kho]

	Admin --- UC9
	Admin --- UC11
	Admin --- UC12
```

### Use Case chi tiết — UC9: Bán hàng POS

| Mục | Nội dung |
| --- | --- |
| **Actor** | Cashier (hoặc Admin) |
| **Mục tiêu** | Lập hóa đơn, thu tiền, in, trừ kho FIFO |
| **Tiền điều kiện** | Đã login, có permission `Sales.CreateInvoice` |
| **Hậu điều kiện** | Invoice = Completed, tồn kho giảm, log Export được ghi |
| **Luồng chính** | 1. Mở /Sales.
2. Hệ thống load **grid SP có ảnh + tên + giá + tồn**.
3. Click ảnh SP / quét barcode → add vào giỏ.
4. Điều chỉnh số lượng / xóa item.
5. (Tùy chọn) chọn khách.
6. Chọn payment method.
7. Nếu Cash: nhập số tiền khách đưa.
8. Bấm "Thanh toán".
9. Hệ thống validate, tạo invoice, trừ kho FIFO, hiển thị tiền thừa.
10. Bấm "In" → render trang in. |
| **Luồng phụ A** | SP hết hàng → báo lỗi, không tạo invoice. |
| **Luồng phụ B** | Tiền khách < tổng → báo lỗi, yêu cầu nhập lại. |
| **Luồng phụ C** | Concurrency: 2 cashier cùng bán SP cuối → 1 bên rollback, hiển "Hết hàng, làm mới". |

---

## III. SƠ ĐỒ ERD (ĐÃ CẬP NHẬT ẢNH SP)

```mermaid
erDiagram
	AbpUsers ||--o{ Invoices : creates
	AbpUsers ||--o{ InventoryLogs : creates
	AbpUsers ||--o{ PurchaseOrders : creates
	Categories ||--o{ Products : contains
	Products ||--o{ InvoiceItems : sold_in
	Products ||--o{ InventoryLogs : tracked
	Products ||--o{ PurchaseOrderItems : ordered_in
	Customers ||--o{ Invoices : buys
	Suppliers ||--o{ PurchaseOrders : supplies
	Suppliers ||--o{ InventoryLogs : supplies
	Invoices ||--o{ InvoiceItems : contains
	PurchaseOrders ||--o{ PurchaseOrderItems : contains

	Products {
		Guid Id PK
		string Name
		string Barcode UK
		Guid CategoryId FK
		decimal CostPrice
		decimal SalePrice
		int StockQuantity
		int MinStock
		string Unit
		string ImageUrl "MỚI"
		bool IsActive
		bool IsDeleted
	}

	Categories {
		Guid Id PK
		string Name UK
		string Description
		bool IsActive
		bool IsDeleted
	}

	Customers {
		Guid Id PK
		string Name
		string Phone
		string Address
		bool IsActive
		bool IsDeleted
	}

	Suppliers {
		Guid Id PK
		string Name
		string Phone
		string Address
		string Email
		string ContactPerson
		bool IsActive
		bool IsDeleted
	}

	Invoices {
		Guid Id PK
		string InvoiceNumber UK
		Guid CustomerId FK
		long CashierUserId FK
		decimal TotalAmount
		decimal AmountPaid
		decimal ChangeAmount
		string PaymentMethod
		string Status
		string Note
		datetime CreationTime
		bool IsDeleted
	}

	InvoiceItems {
		Guid Id PK
		Guid InvoiceId FK
		Guid ProductId FK
		int Quantity
		decimal UnitPrice
		decimal Subtotal
	}

	InventoryLogs {
		Guid Id PK
		Guid ProductId FK
		long UserId FK
		string Type
		int Quantity
		int RemainingQuantity
		string BatchId
		datetime ExpiryDate
		Guid SupplierId FK
		string Note
		Guid ReferenceId
		string ReferenceType
		datetime CreationTime
	}

	PurchaseOrders {
		Guid Id PK
		string OrderNumber UK
		Guid SupplierId FK
		long UserId FK
		decimal TotalAmount
		string Status
		string Note
		bool IsDeleted
	}

	PurchaseOrderItems {
		Guid Id PK
		Guid PurchaseOrderId FK
		Guid ProductId FK
		int Quantity
		decimal UnitPrice
		decimal Subtotal
		string BatchId
		datetime ExpiryDate
	}

	AbpUsers {
		long Id PK
		string UserName UK
		string Name
		string Surname
		string EmailAddress UK
		string Password
		bool IsActive
		bool IsDeleted
	}
```

### Điểm mới so với bản cũ

| Bảng | Trường mới | Kiểu | Mô tả |
| --- | --- | --- | --- |
| Products | `ImageUrl` | nvarchar(500) NULL | Đường dẫn tương đối, ví dụ `/uploads/products/{guid}.jpg`. NULL = chưa có ảnh, UI hiển placeholder. |

---

## IV. CLASS DIAGRAM (DOMAIN MODEL)

```mermaid
classDiagram
	class FullAuditedAggregateRoot~Guid~ {
		+Guid Id
		+DateTime CreationTime
		+long? CreatorUserId
		+DateTime? LastModificationTime
		+long? LastModifierUserId
		+bool IsDeleted
		+long? DeleterUserId
		+DateTime? DeletionTime
	}

	class Category {
		+string Name
		+string Description
		+bool IsActive
		+ICollection~Product~ Products
	}

	class Product {
		+string Name
		+string Barcode
		+Guid CategoryId
		+decimal CostPrice
		+decimal SalePrice
		+int StockQuantity
		+int MinStock
		+string Unit
		+string ImageUrl
		+bool IsActive
		+Category Category
	}

	class Customer {
		+string Name
		+string Phone
		+string Address
		+bool IsActive
	}

	class Supplier {
		+string Name
		+string Phone
		+string Address
		+string Email
		+string ContactPerson
		+bool IsActive
	}

	class Invoice {
		+string InvoiceNumber
		+Guid? CustomerId
		+long? CashierUserId
		+decimal TotalAmount
		+decimal AmountPaid
		+decimal ChangeAmount
		+PaymentMethod PaymentMethod
		+InvoiceStatus Status
		+string Note
		+Customer Customer
		+ICollection~InvoiceItem~ Items
	}

	class InvoiceItem {
		+Guid InvoiceId
		+Guid ProductId
		+int Quantity
		+decimal UnitPrice
		+decimal Subtotal
		+Invoice Invoice
		+Product Product
	}

	class InventoryLog {
		+Guid ProductId
		+long? UserId
		+InventoryLogType Type
		+int Quantity
		+int? RemainingQuantity
		+string BatchId
		+DateTime? ExpiryDate
		+Guid? SupplierId
		+string Note
		+Guid? ReferenceId
		+string ReferenceType
		+Product Product
		+Supplier Supplier
	}

	class PurchaseOrder {
		+string OrderNumber
		+Guid SupplierId
		+long? UserId
		+decimal TotalAmount
		+PurchaseOrderStatus Status
		+string Note
		+Supplier Supplier
		+ICollection~PurchaseOrderItem~ Items
	}

	class PurchaseOrderItem {
		+Guid PurchaseOrderId
		+Guid ProductId
		+int Quantity
		+decimal UnitPrice
		+decimal Subtotal
		+string BatchId
		+DateTime? ExpiryDate
		+PurchaseOrder PurchaseOrder
		+Product Product
	}

	class PaymentMethod {
		<<enum>>
		Cash
		Transfer
		Momo
		ZaloPay
	}

	class InvoiceStatus {
		<<enum>>
		Pending
		Completed
		Cancelled
	}

	class InventoryLogType {
		<<enum>>
		Import
		Export
		Dispose
		Adjust
	}

	class AbpUserBase {
		+long Id
		+string UserName
		+string Name
		+string Surname
		+string EmailAddress
		+bool IsActive
		+bool IsDeleted
	}

	class User {
		+const string DefaultPassword
		+CreateRandomPassword() string
		+CreateTenantAdminUser() User
	}

	FullAuditedAggregateRoot <|-- Category
	FullAuditedAggregateRoot <|-- Product
	FullAuditedAggregateRoot <|-- Customer
	FullAuditedAggregateRoot <|-- Supplier
	FullAuditedAggregateRoot <|-- Invoice
	FullAuditedAggregateRoot <|-- PurchaseOrder
	AbpUserBase <|-- User
	Category "1" --o "*" Product
	Invoice "1" --o "*" InvoiceItem
	Product "1" --o "*" InvoiceItem
	Customer "1" --o "*" Invoice
	Product "1" --o "*" InventoryLog
	Supplier "1" --o "*" InventoryLog
	Supplier "1" --o "*" PurchaseOrder
	PurchaseOrder "1" --o "*" PurchaseOrderItem
	Product "1" --o "*" PurchaseOrderItem
	User "1" --o "*" Invoice
	User "1" --o "*" InventoryLog
	User "1" --o "*" PurchaseOrder
```

---

## V. SƠ ĐỒ TUẦN TỰ (SEQUENCE)

### 5.1 Sequence: Bán hàng POS (luồng chính)

```mermaid
sequenceDiagram
	actor Cashier
	participant View as Sales View (Razor+jQuery)
	participant Ctrl as SalesController
	participant App as InvoiceAppService
	participant Mgr as InvoiceManager
	participant Inv as InventoryManager
	participant DB as DbContext

	Cashier->>View: Mở /Sales
	View->>Ctrl: GET /Sales
	Ctrl->>App: GetActiveProductsForPosAsync()
	App->>DB: SELECT Products (IsActive=1, IsDeleted=0)<br>columns: Id, Name, Barcode, SalePrice, StockQuantity, ImageUrl
	DB-->>App: List ProductPosDto
	App-->>Ctrl: List ProductPosDto
	Ctrl-->>View: Render grid (ảnh + tên + giá)

	Cashier->>View: Click ảnh / quét barcode
	View->>View: Add vào giỏ (client-side)
	Cashier->>View: Bấm "Thanh toán"
	View->>Ctrl: POST /api/services/app/Invoice/Create

	Ctrl->>App: CreateAsync(input)
	App->>App: [UnitOfWork] BEGIN
	App->>Mgr: ValidateInput(input)
	App->>Inv: CheckStockAsync(items)
	Inv->>DB: SUM RemainingQuantity per product
	DB-->>Inv: stock map
	Inv-->>App: OK / throw OutOfStockException

	App->>Mgr: GenerateInvoiceNumber()
	Mgr-->>App: HD-20260528-001
	App->>DB: INSERT Invoice + InvoiceItems
	App->>Inv: DeductStockFifoAsync(items, invoiceId)
	loop Mỗi sản phẩm
		Inv->>DB: SELECT InventoryLog (Type=Import, Remaining>0)<br>ORDER BY ExpiryDate, CreationTime
		DB-->>Inv: lots[]
		Inv->>DB: UPDATE RemainingQuantity per lot
		Inv->>DB: INSERT InventoryLog (Type=Export)
		Inv->>DB: UPDATE Product.StockQuantity
	end
	App->>App: [UnitOfWork] COMMIT
	App-->>Ctrl: InvoiceCreatedDto
	Ctrl-->>View: 200 OK + JSON
	View-->>Cashier: Hiển tiền thừa + nút "In"
```

### 5.2 Sequence: Upload ảnh sản phẩm

```mermaid
sequenceDiagram
	actor Admin
	participant View as Product Edit View
	participant Ctrl as ProductController
	participant File as FileUploadService
	participant App as ProductAppService
	participant Disk as wwwroot/uploads/products/
	participant DB as DbContext

	Admin->>View: Chọn file ảnh
	View->>Ctrl: POST /Products/Edit (multipart/form-data)
	Ctrl->>File: SaveImageAsync(file)
	File->>File: Validate mime + size <= 2MB
	File->>File: Resize 500x500 (ImageSharp)
	File->>Disk: Lưu {guid}.jpg
	File-->>Ctrl: imageUrl = /uploads/products/{guid}.jpg
	Ctrl->>App: UpdateAsync(dto)
	App->>DB: UPDATE Products SET ImageUrl=...
	DB-->>App: OK
	App-->>Ctrl: ProductDto
	Ctrl-->>View: Redirect + toast
```

### 5.3 Sequence: Đăng nhập (Cookie cho MVC)

```mermaid
sequenceDiagram
	actor User
	participant View as Login View
	participant Acc as AccountController
	participant Sign as SignInManager
	participant DB as DbContext

	User->>View: Nhập username + password
	View->>Acc: POST /Account/Login
	Acc->>Sign: PasswordSignInAsync(user, pass)
	Sign->>DB: SELECT AbpUsers WHERE UserName=...
	DB-->>Sign: User entity
	Sign->>Sign: VerifyHashedPassword
	alt Hợp lệ
		Sign->>Acc: SignInResult.Success<br>Set-Cookie .AspNetCore.Identity
		Acc-->>View: 302 Redirect /Home
	else Sai
		Sign-->>Acc: Failed
		Acc-->>View: Render lỗi "Sai tài khoản"
	end
```

### 5.4 Sequence: Nhập hàng theo lô

```mermaid
sequenceDiagram
	actor Admin
	participant View as PurchaseOrder Form
	participant Ctrl as PurchaseOrderController
	participant App as PurchaseOrderAppService
	participant Mgr as PurchaseOrderManager
	participant DB as DbContext

	Admin->>View: Chọn NCC + thêm items (SP, qty, giá, BatchId, HSD)
	View->>Ctrl: POST /PurchaseOrders/Create
	Ctrl->>App: CreateAsync(input)
	App->>App: [UnitOfWork] BEGIN
	App->>Mgr: GenerateOrderNumber()
	Mgr-->>App: PN-20260528-001
	App->>DB: INSERT PurchaseOrder + Items
	loop Mỗi item
		App->>DB: INSERT InventoryLog (Type=Import,<br>RemainingQty=Qty, BatchId, ExpiryDate)
		App->>DB: UPDATE Product.StockQuantity += Qty<br>(tùy chọn) UPDATE CostPrice
	end
	App->>App: [UnitOfWork] COMMIT
	App-->>Ctrl: PurchaseOrderDto
	Ctrl-->>View: Redirect + toast
```

---

## VI. STATE DIAGRAM — VÒNG ĐỜI HÓA ĐƠN

```mermaid
stateDiagram-v2
	[*] --> Pending: Tạo náp (nếu chưa thanh toán)
	Pending --> Completed: Thanh toán thành công<br>(trừ kho FIFO)
	Pending --> Cancelled: Hủy trước khi thanh toán
	Completed --> Cancelled: Admin hủy<br>(hoàn kho qua log Adjust)
	Cancelled --> [*]
	Completed --> [*]
```

> Trong implementation hiện tại, hóa đơn POS tạo thẳng `Completed` (không qua `Pending`) vì thu ngân thanh toán ngay. `Pending` giữ lại để mở rộng cho "đặt hàng trước, trả tiền sau" trong tương lai.
> 

---

## VII. THAY ĐỔI CỤ THỂ TRONG CODE

### 7.1 Entity `Product.cs`

```csharp
public class Product : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; set; }
    public string? Barcode { get; set; }
    public Guid? CategoryId { get; set; }
    public decimal CostPrice { get; set; } = 0;
    public decimal SalePrice { get; set; }
    public int StockQuantity { get; set; } = 0;
    public int MinStock { get; set; } = 10;
    public string Unit { get; set; } = "cái";
    public string? ImageUrl { get; set; }   // MỚI: đường dẫn ảnh đại diện
    public bool IsActive { get; set; } = true;
    public virtual Category Category { get; set; }
}
```

### 7.2 Fluent API trong `InternProjectDbContext`

```csharp
builder.Entity<Product>(b =>
{
    b.Property(x => x.ImageUrl).HasMaxLength(500);
    // ... các cấu hình khác giữ nguyên
});
```

### 7.3 Migration

```
dotnet ef migrations add Add_Product_ImageUrl -p src/InternProject.EntityFrameworkCore -s src/InternProject.Web.Mvc
dotnet ef database update -p src/InternProject.EntityFrameworkCore -s src/InternProject.Web.Mvc
```

### 7.4 DTO cho POS

```csharp
public class ProductPosDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Barcode { get; set; }
    public decimal SalePrice { get; set; }
    public int StockQuantity { get; set; }
    public string Unit { get; set; }
    public string? ImageUrl { get; set; }  // hiển thị trên grid POS
}
```

### 7.5 FileUploadService (Web.Mvc)

```csharp
public class FileUploadService
{
    private readonly IWebHostEnvironment _env;
    private const long MaxSize = 2 * 1024 * 1024;  // 2MB
    private static readonly string[] AllowedMimes = { "image/jpeg", "image/png", "image/webp" };

    public async Task<string> SaveProductImageAsync(IFormFile file)
    {
        if (file.Length > MaxSize) throw new UserFriendlyException("Ảnh quá 2MB");
        if (!AllowedMimes.Contains(file.ContentType)) throw new UserFriendlyException("Định dạng ảnh không hợp lệ");

        var folder = Path.Combine(_env.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid()}.jpg";
        var fullPath = Path.Combine(folder, fileName);

        // Resize + lưu JPEG bằng SixLabors.ImageSharp
        using var image = await Image.LoadAsync(file.OpenReadStream());
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(500, 500)
        }));
        await image.SaveAsJpegAsync(fullPath);

        return $"/uploads/products/{fileName}";
    }
}
```

### 7.6 POS View (snippet hiển grid ảnh)

```html
<div class="row" id="pos-products">
    @foreach (var p in Model.Products)
    {
        <div class="col-md-2 product-tile" data-id="@p.Id" data-price="@p.SalePrice">
            <img src="@(string.IsNullOrEmpty(p.ImageUrl) ? "/images/no-image.png" : p.ImageUrl)"
                 alt="@p.Name" class="img-fluid" />
            <div class="product-name">@p.Name</div>
            <div class="product-price">@p.SalePrice.ToString("N0") đ</div>
            <div class="product-stock">Tồn: @p.StockQuantity @p.Unit</div>
        </div>
    }
</div>
```

---

## VIII. CHECKLIST CHO BÁO CÁO

- [ ]  Vẽ lại Use Case Diagram (mở [draw.io](http://draw.io) / Visual Paradigm theo sơ đồ mermaid ở mục II).
- [ ]  Vẽ lại ERD trong PowerDesigner / MySQL Workbench / [draw.io](http://draw.io) theo mục III.
- [ ]  Vẽ lại Class Diagram theo mục IV.
- [ ]  Vẽ lại 4 Sequence theo mục V (POS, upload ảnh, đăng nhập, nhập hàng).
- [ ]  Vẽ State Diagram hóa đơn theo mục VI.
- [ ]  Viết bảng FR/NFR vào chương "Phân tích yêu cầu".
- [ ]  Thêm screenshot màn POS thực tế có hiển ảnh SP.
- [ ]  Giải thích ngắn tại sao chọn `ImageUrl` thay vì `ProductImage` table (mục đầu trang).

---

<aside>
💡

**Phát biểu nhanh khi thầy hỏi**: *"Em chỉ thêm 1 cột `ImageUrl` vào bảng Products vì mỗi sản phẩm chỉ cần 1 ảnh đại diện cho màn POS. Ảnh được lưu trên disk để tránh phình DB và dễ backup CDN sau này. Khi load màn POS, chỉ một SELECT đã lấy đủ ảnh, không cần JOIN. Nếu sau cần nhiều ảnh / gallery, em sẽ thêm bảng ProductImage 1-N mà không phá vỡ schema hiện tại."*

</aside>