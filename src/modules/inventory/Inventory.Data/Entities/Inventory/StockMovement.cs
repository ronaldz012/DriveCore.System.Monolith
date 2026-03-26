using Inventory.Data.Entities.Products;
using Inventory.Data.Entities.Shared.Base;

namespace Inventory.Data.Entities.Inventory;

public class StockMovement : Params
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int ProductVariantId { get; set; }
    public int UserId { get; set; }
    public decimal Quantity { get; set; }
    public string Notes { get; set; } = string.Empty;
    public MovementType MovementType { get; set; }
    public int? TransferToBranchId { get; set; }
    public int? RelatedMovementId { get; set; }

    public ProductVariant ProductVariant { get; set; } = null!;
    public StockMovement? RelatedMovement { get; set; }

    // Ingreso por recepción
    public static StockMovement CreateReception(int branchId, int productVariantId, int userId, decimal quantity, string? notes = null)
    {
        return new StockMovement
        {
            BranchId = branchId,
            ProductVariantId = productVariantId,
            UserId = userId,
            Quantity = quantity,
            MovementType = MovementType.Reception,
            Notes = notes ?? string.Empty
        };
    }
    public static StockMovement CreateReceptionForNewVariant(int branchId, ProductVariant productVariant, int userId, decimal quantity, string? notes = null)
    {
        return new StockMovement
        {
            BranchId = branchId,
            ProductVariant = productVariant, // EF resuelve el Id
            UserId = userId,
            Quantity = quantity,
            MovementType = MovementType.Reception,
            Notes = notes ?? string.Empty
        };
    }

    // Egreso por venta
    public static StockMovement CreateSale(int branchId, int productVariantId, int userId, decimal quantity, string? notes = null)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Quantity must be greater than zero");

        return new StockMovement
        {
            BranchId = branchId,
            ProductVariantId = productVariantId,
            UserId = userId,
            Quantity = -quantity, // negativo representa egreso
            MovementType = MovementType.Sale,
            Notes = notes ?? string.Empty
        };
    }

    // Ajuste manual (puede ser positivo o negativo)
    public static StockMovement CreateAdjustment(int branchId, int productVariantId, int userId, decimal quantity, string notes)
    {
        if (string.IsNullOrEmpty(notes))
            throw new InvalidOperationException("Adjustment requires a note explaining the reason");

        return new StockMovement
        {
            BranchId = branchId,
            ProductVariantId = productVariantId,
            UserId = userId,
            Quantity = quantity,
            MovementType = MovementType.Adjustment,
            Notes = notes
        };
    }

    // Traspaso — devuelve los dos movimientos linkeados
    public static (StockMovement Out, StockMovement In) CreateTransfer(
        int fromBranchId, int toBranchId, int productVariantId, int userId, decimal quantity, string? notes = null)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Quantity must be greater than zero");

        var transferOut = new StockMovement
        {
            BranchId = fromBranchId,
            ProductVariantId = productVariantId,
            UserId = userId,
            Quantity = -quantity,
            MovementType = MovementType.TransferOut,
            TransferToBranchId = toBranchId,
            Notes = notes ?? string.Empty
        };

        var transferIn = new StockMovement
        {
            BranchId = toBranchId,
            ProductVariantId = productVariantId,
            UserId = userId,
            Quantity = quantity,
            MovementType = MovementType.TransferIn,
            RelatedMovement = transferOut, // EF resuelve el Id
            Notes = notes ?? string.Empty
        };

        transferOut.RelatedMovement = transferIn;

        return (transferOut, transferIn);
    }
}

public enum MovementType
{
    Reception,   // ingreso por recepción de mercadería
    Sale,        // egreso por venta
    Adjustment,  // ajuste manual
    TransferOut, // egreso por traspaso
    TransferIn   // ingreso por traspaso
}