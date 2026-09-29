namespace Module.Inventory.Application.UseCases.Liquidation;

/// <summary>
/// Dataset 3 — USE axis.
///
/// The brief says what the garment is built to do: survive daily punishment versus
/// pure fashion. The catalogue has no usage field, so the model has to read the
/// product name and the category to place it on that axis.
///
/// The trap in here is a pair that shares the noun and differs on the adjective:
///   - "Canvas Work Jacket" (durable) vs "Suede Jacket" (delicate)
///   - "Cargo Pant" (functional) vs "Chino" (fashion)
/// Getting those apart needs material knowledge, not a text filter.
///
/// The Denim Jacket and the Chino sit in the middle on purpose: they are the cases
/// where a shopkeeper genuinely would not know, and they should land in review.
/// </summary>
public static class WorkwearStateFixture
{
    public static LiquidationStateDto Sample(
        string branchName,
        string tenantLabel,
        int windowDays,
        DateTime? eventDate,
        ObjectiveType? objectiveType,
        MarginFloor? marginFloor,
        string? userNotes = null)
    {
        var context = LiquidationContextFactory.Create(
            branchName, tenantLabel, windowDays, eventDate, objectiveType, marginFloor, userNotes);

        return new LiquidationStateDto(context,
            [
                // ---- Workwear: built to be used, gathering dust ------------------------------
                new ProductStateDto("p0", "WRK-1", "Canvas Work Jacket", "SCOTCH BRITE", "Outerwear", "Unisex", 300, 22, 2, 132m, 1.0, 100.0,
                    [
                        new VariantStateDto("p0_v0", "WRK-1-001", "verde", "M", 12, 1, 260m, 150m, 42.3, 300, 105.0),
                        new VariantStateDto("p0_v1", "WRK-1-002", "verde", "L", 10, 1, 260m, 150m, 42.3, 300, 115.0)
                    ]),
                new ProductStateDto("p1", "WRK-2", "Cargo Pant", "DIEGO BONETA", "Pants", "Unisex", 280, 27, 3, 135m, 0.9, 90.0,
                    [
                        new VariantStateDto("p1_v0", "WRK-2-001", "negro", "32", 15, 2, 210m, 120m, 42.9, 280, 88.0),
                        new VariantStateDto("p1_v1", "WRK-2-002", "beige", "34", 12, 1, 210m, 120m, 42.9, 280, 95.0)
                    ]),
                new ProductStateDto("p2", "WRK-3", "Safety Boot", "CATERPILLAR", "Boots", "Unisex", 340, 16, 1, 48m, 0, 130.0,
                    [
                        new VariantStateDto("p2_v0", "WRK-3-001", "amarillo", "40", 9, 1, 380m, 220m, 42.1, 340, 125.0),
                        new VariantStateDto("p2_v1", "WRK-3-002", "negro", "42", 7, 0, 380m, 220m, 42.1, 340, null)
                    ]),
                new ProductStateDto("p3", "WRK-4", "Thermal Base Layer", "NIKE", "Base Layer", "Male", 260, 24, 3, 168m, 0.6, 85.0,
                    [
                        new VariantStateDto("p3_v0", "WRK-4-001", "negro", "M", 14, 2, 130m, 70m, 46.2, 260, 82.0),
                        new VariantStateDto("p3_v1", "WRK-4-002", "gris", "L", 10, 1, 130m, 70m, 46.2, 260, 90.0)
                    ]),
                new ProductStateDto("p4", "WRK-5", "Work Glove", "SCOTCH BRITE", "Accessories", "Unisex", 320, 42, 3, 231m, 0.4, 110.0,
                    [
                        new VariantStateDto("p4_v0", "WRK-5-001", "negro", "M", 22, 2, 55m, 25m, 54.5, 320, 105.0),
                        new VariantStateDto("p4_v1", "WRK-5-002", "negro", "L", 20, 1, 55m, 25m, 54.5, 320, 115.0)
                    ]),

                // ---- Pure fashion: will not take daily punishment -----------------------------
                new ProductStateDto("p5", "WRK-6", "Suede Jacket", "ZARA", "Outerwear", "Unisex", 310, 13, 2, 78m, 1.4, 95.0,
                    [
                        new VariantStateDto("p5_v0", "WRK-6-001", "miel", "S", 7, 1, 520m, 300m, 42.3, 310, 100.0),
                        new VariantStateDto("p5_v1", "WRK-6-002", "negro", "M", 6, 1, 520m, 300m, 42.3, 310, 110.0)
                    ]),
                new ProductStateDto("p6", "WRK-7", "Silk Shirt", "TOMMY HILFIGER", "Shirts", "Male", 290, 15, 2, 84m, 0.9, 80.0,
                    [
                        new VariantStateDto("p6_v0", "WRK-7-001", "azul", "M", 8, 1, 280m, 150m, 46.4, 290, 85.0),
                        new VariantStateDto("p6_v1", "WRK-7-002", "blanco", "L", 7, 1, 280m, 150m, 46.4, 290, 88.0)
                    ]),
                new ProductStateDto("p7", "WRK-8", "Graphic Tee", "SHEIN", "Tops", "Unisex", 240, 31, 5, 155m, 0.8, 60.0,
                    [
                        new VariantStateDto("p7_v0", "WRK-8-001", "blanco", "M", 17, 3, 90m, 50m, 44.4, 240, 58.0),
                        new VariantStateDto("p7_v1", "WRK-8-002", "negro", "M", 14, 2, 90m, 50m, 44.4, 240, 62.0)
                    ]),
                new ProductStateDto("p8", "WRK-9", "Crop Top", "SHEIN", "Tops", "Female", 250, 19, 4, 95m, 2.0, 70.0,
                    [
                        new VariantStateDto("p8_v0", "WRK-9-001", "blanco", "S", 10, 2, 75m, 38m, 49.3, 250, 70.0),
                        new VariantStateDto("p8_v1", "WRK-9-002", "rosa", "M", 9, 2, 75m, 38m, 49.3, 250, 72.0)
                    ]),

                // ---- Genuinely undecidable: these should land in review -------------------------
                new ProductStateDto("p9", "WRK-10", "Denim Jacket", "LEVI'S", "Outerwear", "Unisex", 260, 18, 4, 144m, 1.1, 75.0,
                    [
                        new VariantStateDto("p9_v0", "WRK-10-001", "azul", "M", 10, 2, 300m, 180m, 40.0, 260, 76.0),
                        new VariantStateDto("p9_v1", "WRK-10-002", "azul", "L", 8, 2, 300m, 180m, 40.0, 260, 80.0)
                    ]),
                new ProductStateDto("p10", "WRK-11", "Chino", "DIEGO BONETA", "Pants", "Male", 270, 24, 4, 130m, 1.0, 80.0,
                    [
                        new VariantStateDto("p10_v0", "WRK-11-001", "beige", "32", 13, 2, 190m, 110m, 42.1, 270, 78.0),
                        new VariantStateDto("p10_v1", "WRK-11-002", "negro", "34", 11, 2, 190m, 110m, 42.1, 270, 82.0)
                    ]),
                new ProductStateDto("p11", "WRK-12", "Leather Belt", "SCOTCH BRITE", "Accessories", "Unisex", 280, 19, 3, 84m, 0.7, 65.0,
                    [
                        new VariantStateDto("p11_v0", "WRK-12-001", "marron", "M", 11, 2, 140m, 80m, 42.9, 280, 64.0),
                        new VariantStateDto("p11_v1", "WRK-12-002", "negro", "M", 8, 1, 140m, 80m, 42.9, 280, 70.0)
                    ]),

                // ---- Stars: internal control ---------------------------------------------------
                new ProductStateDto("p12", "WRK-13", "Basic Cotton Tee", "NIKE", "Tops", "Unisex", 42, 41, 122, 2684m, 0.5, 3.0,
                    [
                        new VariantStateDto("p12_v0", "WRK-13-001", "blanco", "M", 25, 74, 65m, 38m, 41.5, 42, 0.6),
                        new VariantStateDto("p12_v1", "WRK-13-002", "negro", "M", 16, 48, 65m, 38m, 41.5, 42, 0.7)
                    ]),
                new ProductStateDto("p13", "WRK-14", "Sports Sneaker", "PUMA", "Shoes", "Unisex", 40, 35, 91, 1456m, 0.7, 2.8,
                    [
                        new VariantStateDto("p13_v0", "WRK-14-001", "blanco", "40", 19, 50, 480m, 290m, 39.6, 40, 0.7),
                        new VariantStateDto("p13_v1", "WRK-14-002", "negro", "42", 16, 41, 480m, 290m, 39.6, 40, 0.8)
                    ])
            ]);
    }
}
