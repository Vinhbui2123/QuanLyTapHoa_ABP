using Abp.Authorization;
using Abp.Localization;
using Abp.MultiTenancy;

namespace InternProject.Authorization;

public class InternProjectAuthorizationProvider : AuthorizationProvider
{
    public override void SetPermissions(IPermissionDefinitionContext context)
    {
        context.CreatePermission(PermissionNames.Pages_Users, L("Users"));
        context.CreatePermission(PermissionNames.Pages_Users_Activation, L("UsersActivation"));
        context.CreatePermission(PermissionNames.Pages_Roles, L("Roles"));
        
        var customers = context.CreatePermission(PermissionNames.Pages_Customers, L("Customers"));
        customers.CreateChildPermission(PermissionNames.Pages_Customers_Create, L("CreateCustomer"));
        customers.CreateChildPermission(PermissionNames.Pages_Customers_Edit, L("EditCustomer"));
        customers.CreateChildPermission(PermissionNames.Pages_Customers_Delete, L("DeleteCustomer"));

        var suppliers = context.CreatePermission(PermissionNames.Pages_Suppliers, L("Suppliers"));
        suppliers.CreateChildPermission(PermissionNames.Pages_Suppliers_Create, L("CreateSupplier"));
        suppliers.CreateChildPermission(PermissionNames.Pages_Suppliers_Delete, L("DeleteSupplier"));
        suppliers.CreateChildPermission(PermissionNames.Pages_Suppliers_Edit, L("EditSupplier"));

        var products = context.CreatePermission(PermissionNames.Pages_Products, L("Products"));
        products.CreateChildPermission(PermissionNames.Pages_Products_Create, L("CreateProduct"));
        products.CreateChildPermission(PermissionNames.Pages_Products_Edit, L("EditProduct"));
        products.CreateChildPermission(PermissionNames.Pages_Products_Delete, L("DeleteProduct"));

        var categories = context.CreatePermission(PermissionNames.Pages_Categories, L("Categories"));
        categories.CreateChildPermission(PermissionNames.Pages_Categories_Create, L("CreateCategory"));
        categories.CreateChildPermission(PermissionNames.Pages_Categories_Edit, L("EditCategory"));
        categories.CreateChildPermission(PermissionNames.Pages_Categories_Delete, L("DeleteCategory"));

        var invoices = context.CreatePermission(PermissionNames.Pages_Invoices, L("Invoices"));
        invoices.CreateChildPermission(PermissionNames.Pages_Invoices_Create, L("CreateInvoice"));
        invoices.CreateChildPermission(PermissionNames.Pages_Invoices_Cancel, L("CancelInvoice"));

        var purchaseOrders = context.CreatePermission(PermissionNames.Pages_PurchaseOrders, L("PurchaseOrders"));
        purchaseOrders.CreateChildPermission(PermissionNames.Pages_PurchaseOrders_Create, L("CreatePurchaseOrder"));

        var stockBatches = context.CreatePermission(PermissionNames.Pages_StockBatches, L("StockBatches"));
        stockBatches.CreateChildPermission(PermissionNames.Pages_StockBatches_Dispose, L("DisposeStockBatch"));
        
        context.CreatePermission(PermissionNames.Pages_Tenants, L("Tenants"), multiTenancySides: MultiTenancySides.Host);
    }

    private static ILocalizableString L(string name)
    {
        return new LocalizableString(name, InternProjectConsts.LocalizationSourceName);
    }
}
