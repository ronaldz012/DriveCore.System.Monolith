using Common.Domain;

namespace Module.Sales.Domain;

public class Sale : IMustHaveTenant, ICreatedAt, ICreatedBy, IUpdatedAt, IUpdatedBy, ISoftDelete
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid BranchId { get; set; }
    public Guid SoldById { get; set; }
    public string SoldByName { get; set; } = string.Empty;
    public Guid CashRegisterClosureId { get; set; } 
    public int Number { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? TransactionCode { get; set; }
    public decimal TotalAmount { get; set; }
    public int? InvoiceNumber { get; set; }
    public SaleType Type { get; set; }        
    public DocumentType DocumentType { get; set; }
    public Guid? OriginalSaleId { get; set; }

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public string? UpdatedByName { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public string? DeletedByName { get; set; }

    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    public CashRegisterClosure CashRegisterClosure { get; set; } = null!;

    public Sale? OriginalSale { get; set; }
    public ICollection<Sale> Returns { get; set; } = new List<Sale>();

    public static Sale CreateSaleWithTicket(
        Guid branchId,
        Guid soldById,
        string soldByName,
        Guid createdBy,
        string createdByName,
        Guid cashRegisterClosureId,
        PaymentMethod paymentMethod,
        string? transactionCode,
        string? notes,
        List<(Guid ProductVariantId, string ProductSku, string ProductDisplayName, decimal UnitPrice, int Quantity, decimal DiscountAmount, decimal UnitCost)> items)
    {
        return CreateBase(branchId, soldById, soldByName, createdBy, createdByName, cashRegisterClosureId, paymentMethod, 
            DocumentType.Ticket, transactionCode, notes, null, items);
    }

    public static Sale CreateSaleWithInvoice(
        Guid branchId,
        Guid soldById,
        string soldByName,
        Guid createdBy,
        string createdByName,
        Guid cashRegisterClosureId,
        PaymentMethod paymentMethod,
        DocumentType documentType, 
        int? invoiceNumber,
        string? transactionCode,
        string? notes,
        List<(Guid ProductVariantId, string ProductSku, string ProductDisplayName, decimal UnitPrice, int Quantity, decimal DiscountAmount, decimal UnitCost)> items)
    {
        if (documentType == DocumentType.Ticket)
            throw new InvalidOperationException("Cannot create an invoice with document type 'Ticket'.");

        if (documentType == DocumentType.Invoice && (!invoiceNumber.HasValue || invoiceNumber <= 0))
            throw new InvalidOperationException("Invoice number is required for completed invoices.");

        return CreateBase(branchId, soldById, soldByName, createdBy, createdByName, cashRegisterClosureId, paymentMethod, 
            documentType, transactionCode, notes, invoiceNumber, items);
    }

    // --- Private method to centralize common construction logic ---
    private static Sale CreateBase(
        Guid branchId,
        Guid soldById,
        string soldByName,
        Guid createdBy,
        string createdByName,
        Guid cashRegisterClosureId,
        PaymentMethod paymentMethod,
        DocumentType documentType,
        string? transactionCode,
        string? notes,
        int? invoiceNumber,
        List<(Guid ProductVariantId, string ProductSku, string ProductDisplayName, decimal UnitPrice, int Quantity, decimal DiscountAmount, decimal UnitCost)> items)
    {
        if (items == null || !items.Any())
            throw new InvalidOperationException("Cannot create a sale without products.");

        var now = DateTime.UtcNow;
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            BranchId = branchId,
            SoldById = soldById,
            SoldByName = soldByName,
            CashRegisterClosureId = cashRegisterClosureId,
            PaymentMethod = paymentMethod,
            DocumentType = documentType,
            Type = SaleType.Sale, 
            TransactionCode = transactionCode,
            Notes = notes,
            InvoiceNumber = invoiceNumber,
            CreatedAt = now,
            CreatedBy = createdBy,
            CreatedByName = createdByName
        };

        decimal total = 0;
        foreach (var item in items)
        {
            var subtotal = (item.UnitPrice - item.DiscountAmount) * item.Quantity;
            sale.SaleItems.Add(new SaleItem
            {
                ProductVariantId = item.ProductVariantId,
                ProductSku = item.ProductSku,
                ProductDisplayName = item.ProductDisplayName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                UnitCost = item.UnitCost,
                DiscountAmount = item.DiscountAmount,
                FinalPrice = subtotal,
                CreatedAt = now,
                CreatedBy = createdBy,
                CreatedByName = createdByName
            });
            total += subtotal;
        }

        sale.TotalAmount = total;
        return sale;
    }

    public static Sale CreateReturn(
        Guid branchId,
        Guid soldById,
        string soldByName,
        Guid createdBy,
        string createdByName,
        Guid cashRegisterClosureId,
        PaymentMethod paymentMethod,
        DocumentType documentType,
        string? transactionCode,
        string? notes,
        Guid originalSaleId,
        List<(Guid ProductVariantId, string ProductSku, string ProductDisplayName, decimal UnitPrice, int Quantity, decimal DiscountAmount, decimal UnitCost, Guid OriginalSaleItemId)> items)
    {
        if (items == null || !items.Any())
            throw new InvalidOperationException("Cannot create a return without products.");

        var now = DateTime.UtcNow;
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            BranchId = branchId,
            SoldById = soldById,
            SoldByName = soldByName,
            CashRegisterClosureId = cashRegisterClosureId,
            PaymentMethod = paymentMethod,
            DocumentType = documentType,
            Type = SaleType.Return,
            OriginalSaleId = originalSaleId,
            TransactionCode = transactionCode,
            Notes = notes,
            CreatedAt = now,
            CreatedBy = createdBy,
            CreatedByName = createdByName
        };

        decimal total = 0;
        foreach (var item in items)
        {
            var subtotal = (item.UnitPrice - item.DiscountAmount) * item.Quantity;
            sale.SaleItems.Add(new SaleItem
            {
                ProductVariantId = item.ProductVariantId,
                ProductSku = item.ProductSku,
                ProductDisplayName = item.ProductDisplayName,
                Quantity = -item.Quantity, // NEGATIVO para devolución
                UnitPrice = item.UnitPrice,
                UnitCost = item.UnitCost, // Costo de la venta ORIGINAL (snapshot)
                DiscountAmount = item.DiscountAmount,
                FinalPrice = -subtotal, // NEGATIVO para que netee en reportes
                CreatedAt = now,
                CreatedBy = createdBy,
                CreatedByName = createdByName,
                OriginalSaleItemId = item.OriginalSaleItemId
            });
            total += subtotal;
        }

        sale.TotalAmount = -total; // NEGATIVO para que netee en reportes
        return sale;
    }
}

public enum PaymentMethod
{
    Cash,
    QrCode,
}

public enum SaleType
{
    Sale,
    Return
}

public enum DocumentType
{
    Ticket,         // Solo boleta
    Invoice,        // Facturado
    PendingInvoice  // Con factura pendiente en caso de que salga mal
}