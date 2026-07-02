using Abp.AutoMapper;
using System.ComponentModel.DataAnnotations;

namespace InternProject.Grocery.Suppliers.Dto;

[AutoMapTo(typeof(Supplier))]
public class CreateUpdateSupplierDto
{
    // DTO này là dữ liệu form gửi từ màn hình tạo/cập nhật nhà cung cấp.
    // AutoMapTo giúp ABP/AutoMapper tự chuyển các field trùng tên sang entity Supplier.

    // Mã nhà cung cấp dùng để nhận diện nhanh, được cấu hình unique ở DbContext.
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    // Tên là thông tin bắt buộc vì được dùng trong danh sách, tìm kiếm và phiếu nhập hàng.
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    // Các thông tin liên hệ bên dưới không bắt buộc, nhưng giúp quản lý nhập hàng thực tế.
    [StringLength(32)]
    public string? Phone { get; set; }

    [StringLength(256)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(100)]
    public string? ContactPerson { get; set; }

    public bool IsActive { get; set; } = true;
}
