namespace InternProject.Grocery
{
    public enum PaymentMethod
    {
        Cash = 1,
        Transfer = 2,
        Momo = 3,
        ZaloPay = 4
    }

    public enum InvoiceStatus
    {
        Pending = 1,
        Completed = 2,
        Cancelled = 3
    }

    public enum PurchaseOrderStatus
    {
        Pending = 1,
        Completed = 2,
        Cancelled = 3
    }

    public enum InventoryLogType
    {
        Import = 1,
        Export = 2,
        Dispose = 3,
        Adjust = 4
    }

    public enum StockStatus
    {
        InStock = 0,
        LowStock = 1,
        OutOfStock = 2
    }
}
