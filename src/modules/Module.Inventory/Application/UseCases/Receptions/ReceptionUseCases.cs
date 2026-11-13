using Module.Inventory.Application.UseCases.Receptions.Confirm;
using Module.Inventory.Application.UseCases.Receptions.Create;
using Module.Inventory.Application.UseCases.Receptions.Edit;
using Module.Inventory.Application.UseCases.Receptions.Get;
using Module.Inventory.Application.UseCases.Receptions.GetById;
using Module.Inventory.Application.UseCases.Receptions.GetLabels;
using Module.Inventory.Application.UseCases.Receptions.Revert;

namespace Module.Inventory.Application.UseCases.Receptions;

public record ReceptionUseCases(
    CreateReceptionUc CreateReceptionUc,
    ConfirmStockReceptionUc ConfirmStockReceptionUc,
    AddItemsToDraftUc AddItemsToDraftUc,
    UpdateDraftUc UpdateDraftUc,
    DeleteDraftUc DeleteDraftUc,
    ListReceptions ListReceptions,
    GetReception GetReception,
    ReceptionLabels ReceptionLabels,
    RevertStockReception RevertStockReception);