namespace Inventory.UseCases.Products;

public record ProductUseCases(CreateProduct CreateProduct, GetProducts GetProducts, 
    ValidateProducts ValidateProducts, 
    ValidateProductVariants ValidateProductVariants,
    SearchProduct SearchProducts);