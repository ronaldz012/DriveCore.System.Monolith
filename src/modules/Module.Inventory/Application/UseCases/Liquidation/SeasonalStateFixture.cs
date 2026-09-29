namespace Module.Inventory.Application.UseCases.Liquidation;

/// <summary>
/// Dataset 2 — SEASON axis.
///
/// The brief says the winter is over. The catalogue has no season field at all, so
/// the model has to read the product name and the category to place each garment
/// in a season.
///
/// Three traps in here:
///   - Polar Jacket / Denim Jacket / Bomber Jacket all say "Jacket" and only one
///     is a winter garment. A keyword match takes all three.
///   - The summer items are stagnant too, but their season is arriving, so they
///     should be left alone. That is a cyclical inference, not a filter.
///   - Two star products as an internal control, to check the protection rule
///     still holds under a different objective.
/// </summary>
public static class SeasonalStateFixture
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
                // ---- Winter: the brief names this group outright -------------------------------
                new ProductStateDto("p0", "SEA-1", "Polar Jacket", "NIVEAU", "Outerwear", "Unisex", 280, 18, 2, 112m, 0.9, 100.0,
                    [
                        new VariantStateDto("p0_v0", "SEA-1-001", "negro", "M", 10, 1, 350m, 210m, 40.0, 280, 110.0),
                        new VariantStateDto("p0_v1", "SEA-1-002", "negro", "L", 8, 1, 350m, 210m, 40.0, 280, 120.0)
                    ]),
                new ProductStateDto("p1", "SEA-2", "Insulated Boot", "CATERPILLAR", "Boots", "Unisex", 330, 15, 1, 48m, 0, 140.0,
                    [
                        new VariantStateDto("p1_v0", "SEA-2-001", "marron", "40", 8, 1, 400m, 240m, 40.0, 330, 130.0),
                        new VariantStateDto("p1_v1", "SEA-2-002", "negro", "42", 7, 0, 400m, 240m, 40.0, 330, null)
                    ]),
                new ProductStateDto("p2", "SEA-3", "Merino Sweater", "UNIQLO", "Knitwear", "Unisex", 300, 24, 2, 132m, 1.0, 120.0,
                    [
                        new VariantStateDto("p2_v0", "SEA-3-001", "azul", "M", 13, 1, 220m, 130m, 40.9, 300, 115.0),
                        new VariantStateDto("p2_v1", "SEA-3-002", "gris", "M", 11, 1, 220m, 130m, 40.9, 300, 125.0)
                    ]),
                new ProductStateDto("p3", "SEA-4", "Thermal Leggings", "NIKE", "Base Layer", "Female", 260, 27, 3, 162m, 0.7, 90.0,
                    [
                        new VariantStateDto("p3_v0", "SEA-4-001", "negro", "S", 15, 2, 120m, 65m, 45.8, 260, 85.0),
                        new VariantStateDto("p3_v1", "SEA-4-002", "negro", "M", 12, 1, 120m, 65m, 45.8, 260, 95.0)
                    ]),
                new ProductStateDto("p4", "SEA-5", "Wool Scarf", "H&M", "Accessories", "Unisex", 320, 29, 2, 96m, 1.1, 150.0,
                    [
                        new VariantStateDto("p4_v0", "SEA-5-001", "rojo", "Unica", 16, 1, 80m, 40m, 50.0, 320, 140.0),
                        new VariantStateDto("p4_v1", "SEA-5-002", "gris", "Unica", 13, 1, 80m, 40m, 50.0, 320, 160.0)
                    ]),

                // ---- Year-round: traps, they all say "Jacket" or "Boot" but are not winter -----
                new ProductStateDto("p5", "SEA-6", "Denim Jacket", "LEVI'S", "Outerwear", "Unisex", 240, 21, 5, 147m, 1.2, 60.0,
                    [
                        new VariantStateDto("p5_v0", "SEA-6-001", "azul", "M", 11, 3, 280m, 170m, 39.3, 240, 58.0),
                        new VariantStateDto("p5_v1", "SEA-6-002", "azul", "L", 10, 2, 280m, 170m, 39.3, 240, 62.0)
                    ]),
                new ProductStateDto("p6", "SEA-7", "Bomber Jacket", "TOMMY HILFIGER", "Outerwear", "Unisex", 300, 17, 3, 128m, 0.9, 85.0,
                    [
                        new VariantStateDto("p6_v0", "SEA-7-001", "negro", "M", 9, 2, 320m, 195m, 39.1, 300, 82.0),
                        new VariantStateDto("p6_v1", "SEA-7-002", "verde", "L", 8, 1, 320m, 195m, 39.1, 300, 88.0)
                    ]),
                new ProductStateDto("p7", "SEA-8", "Chino Slim", "DIEGO BONETA", "Pants", "Male", 280, 26, 4, 130m, 1.0, 80.0,
                    [
                        new VariantStateDto("p7_v0", "SEA-8-001", "beige", "32", 14, 2, 180m, 110m, 38.9, 280, 78.0),
                        new VariantStateDto("p7_v1", "SEA-8-002", "azul", "34", 12, 2, 180m, 110m, 38.9, 280, 82.0)
                    ]),
                new ProductStateDto("p8", "SEA-9", "Cardigan", "ZARA", "Knitwear", "Unisex", 250, 20, 4, 120m, 1.3, 70.0,
                    [
                        new VariantStateDto("p8_v0", "SEA-9-001", "beige", "M", 11, 2, 160m, 90m, 43.8, 250, 68.0),
                        new VariantStateDto("p8_v1", "SEA-9-002", "gris", "M", 9, 2, 160m, 90m, 43.8, 250, 72.0)
                    ]),

                // ---- Summer: stagnant, but their season is arriving. Should be left alone -------
                new ProductStateDto("p9", "SEA-10", "Linen Shirt", "RALPH LAUREN", "Shirts", "Male", 290, 16, 2, 56m, 0.8, 90.0,
                    [
                        new VariantStateDto("p9_v0", "SEA-10-001", "blanco", "M", 9, 1, 140m, 80m, 42.9, 290, 95.0),
                        new VariantStateDto("p9_v1", "SEA-10-002", "celeste", "L", 7, 1, 140m, 80m, 42.9, 290, 110.0)
                    ]),
                new ProductStateDto("p10", "SEA-11", "Shorts", "NIKE", "Pants", "Unisex", 300, 28, 2, 96m, 0.6, 95.0,
                    [
                        new VariantStateDto("p10_v0", "SEA-11-001", "negro", "M", 16, 1, 120m, 65m, 45.8, 300, 100.0),
                        new VariantStateDto("p10_v1", "SEA-11-002", "gris", "M", 12, 1, 120m, 65m, 45.8, 300, 95.0)
                    ]),
                new ProductStateDto("p11", "SEA-12", "Sandals", "HAVA", "Shoes", "Unisex", 270, 23, 3, 101m, 0.5, 85.0,
                    [
                        new VariantStateDto("p11_v0", "SEA-12-001", "marron", "40", 13, 2, 110m, 60m, 45.5, 270, 88.0),
                        new VariantStateDto("p11_v1", "SEA-12-002", "negro", "42", 10, 1, 110m, 60m, 45.5, 270, 95.0)
                    ]),

                // ---- Stars: internal control, the protection rule must survive ------------------
                new ProductStateDto("p12", "SEA-13", "Basic Cotton Tee", "NIKE", "Tops", "Unisex", 40, 44, 130, 2860m, 0.5, 2.5,
                    [
                        new VariantStateDto("p12_v0", "SEA-13-001", "blanco", "M", 26, 78, 60m, 35m, 41.7, 40, 0.6),
                        new VariantStateDto("p12_v1", "SEA-13-002", "negro", "M", 18, 52, 60m, 35m, 41.7, 40, 0.7)
                    ]),
                new ProductStateDto("p13", "SEA-14", "Sports Sneaker", "PUMA", "Shoes", "Unisex", 45, 33, 88, 1386m, 0.8, 3.0,
                    [
                        new VariantStateDto("p13_v0", "SEA-14-001", "blanco", "40", 18, 48, 450m, 270m, 40.0, 45, 0.8),
                        new VariantStateDto("p13_v1", "SEA-14-002", "negro", "42", 15, 40, 450m, 270m, 40.0, 45, 0.9)
                    ])
            ]);
    }
}
