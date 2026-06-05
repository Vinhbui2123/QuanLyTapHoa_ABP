using Abp.Zero.EntityFrameworkCore;
using InternProject.Authorization.Roles;
using InternProject.Authorization.Users;
using InternProject.Grocery;
using InternProject.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace InternProject.EntityFrameworkCore;

public class InternProjectDbContext : AbpZeroDbContext<Tenant, Role, User, InternProjectDbContext>
{
    /* Define a DbSet for each entity of the application */
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceItem> InvoiceItems { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
    public DbSet<InventoryLog> InventoryLogs { get; set; }
    public DbSet<StockBatch> StockBatches { get; set; }
    public DbSet<InvoiceItemBatch> InvoiceItemBatches { get; set; }

    public InternProjectDbContext(DbContextOptions<InternProjectDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Customer>(b =>
        {
            b.ToTable("Customers");
            b.Property(x => x.Code).HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
            b.Property(x => x.Phone).HasMaxLength(32);
            b.Property(x => x.Address).HasMaxLength(512);

            b.HasIndex(x => x.Code).IsUnique().HasFilter("[Code] IS NOT NULL");
        });

        modelBuilder.Entity<Category>(b =>
        {
            b.ToTable("Categories");
            b.Property(x => x.Name).IsRequired().HasMaxLength(100);
            b.Property(x => x.Description).HasMaxLength(500);

            b.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Supplier>(b =>
        {
            b.ToTable("Suppliers");
            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(100);
            b.Property(x => x.Phone).HasMaxLength(32);
            b.Property(x => x.Address).HasMaxLength(256);
            b.Property(x => x.Email).HasMaxLength(100);
            b.Property(x => x.ContactPerson).HasMaxLength(100);

            b.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Product>(b =>
        {
            b.ToTable("Products");
            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.Sku).HasMaxLength(50);
            b.Property(x => x.Unit).HasMaxLength(20);
            b.Property(x => x.ImageUrl).HasMaxLength(500);

            b.Property(x => x.CostPrice).HasPrecision(18, 2);
            b.Property(x => x.SalePrice).HasPrecision(18, 2);

            b.HasIndex(x => x.Sku).IsUnique().HasFilter("[Sku] IS NOT NULL");

            b.HasOne(x => x.Category)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Invoice>(b =>
        {
            b.ToTable("Invoices");
            b.Property(x => x.InvoiceNumber).IsRequired().HasMaxLength(50);
            b.Property(x => x.Note).HasMaxLength(500);

            b.Property(x => x.TotalAmount).HasPrecision(18, 2);
            b.Property(x => x.AmountPaid).HasPrecision(18, 2);
            b.Property(x => x.ChangeAmount).HasPrecision(18, 2);

            b.HasIndex(x => x.InvoiceNumber).IsUnique();

            b.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.CashierUser)
                .WithMany()
                .HasForeignKey(x => x.CashierUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceItem>(b =>
        {
            b.ToTable("InvoiceItems");
            b.Property(x => x.UnitPrice).HasPrecision(18, 2);
            b.Property(x => x.Subtotal).HasPrecision(18, 2);

            b.HasOne(x => x.Invoice)
                .WithMany(x => x.InvoiceItems)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseOrder>(b =>
        {
            b.ToTable("PurchaseOrders");
            b.Property(x => x.OrderNumber).IsRequired().HasMaxLength(50);
            b.Property(x => x.Note).HasMaxLength(500);

            b.Property(x => x.TotalAmount).HasPrecision(18, 2);

            b.HasIndex(x => x.OrderNumber).IsUnique();

            b.HasOne(x => x.Supplier)
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseOrderItem>(b =>
        {
            b.ToTable("PurchaseOrderItems");
            b.Property(x => x.BatchId).HasMaxLength(50);

            b.Property(x => x.UnitPrice).HasPrecision(18, 2);
            b.Property(x => x.Subtotal).HasPrecision(18, 2);

            b.HasOne(x => x.PurchaseOrder)
                .WithMany(x => x.PurchaseOrderItems)
                .HasForeignKey(x => x.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryLog>(b =>
        {
            b.ToTable("InventoryLogs");
            b.Property(x => x.BatchId).HasMaxLength(50);
            b.Property(x => x.ReferenceType).HasMaxLength(100);
            b.Property(x => x.Note).HasMaxLength(500);

            b.Property(x => x.UnitCostAtTime).HasPrecision(18, 2);

            b.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Supplier)
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.StockBatch)
                .WithMany()
                .HasForeignKey(x => x.StockBatchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockBatch>(b =>
        {
            b.ToTable("StockBatches");
            b.Property(x => x.BatchCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.ImportPrice).HasPrecision(18, 2);

            b.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.Supplier)
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.PurchaseOrderItem)
                .WithMany()
                .HasForeignKey(x => x.PurchaseOrderItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceItemBatch>(b =>
        {
            b.ToTable("InvoiceItemBatches");
            b.Property(x => x.CostPrice).HasPrecision(18, 2);

            b.HasOne(x => x.InvoiceItem)
                .WithMany(x => x.InvoiceItemBatches)
                .HasForeignKey(x => x.InvoiceItemId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.StockBatch)
                .WithMany()
                .HasForeignKey(x => x.StockBatchId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
