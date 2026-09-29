namespace Module.Inventory.Application.UseCases.Liquidation;

/// <summary>
/// Dataset 1 — DEMOGRAPHIC axis.
///
/// The brief says who the customer is ("the young people") and what they wear
/// ("loose stuff, oversized tees"). The model has to work out which brands and
/// products that maps to, using the brand names in the state.
///
/// The catalogue has no audience field, so this is a genuine inference.
/// </summary>
public static class LiquidationStateFixture
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
                // ---- Stagnant youth products -------------------------------------------------
                new ProductStateDto("p0", "ZAR-2", "Denim Overshirt", "ZARA", "Outerwear", "Unisex", 300, 22, 2, 96m, 1.0, 90.0,
                    [
                        new VariantStateDto("p0_v0", "ZAR-2-001", "azul", "M", 12, 1, 140m, 80m, 42.9, 300, 120.0),
                        new VariantStateDto("p0_v1", "ZAR-2-002", "azul", "L", 10, 1, 140m, 80m, 42.9, 300, 140.0)
                    ]),
                new ProductStateDto("p1", "HMI-4", "Graphic Cotton Tee", "H&M", "Tops", "Unisex", 160, 38, 5, 190m, 1.4, 45.0,
                    [
                        new VariantStateDto("p1_v0", "HMI-4-001", "blanco", "M", 20, 3, 50m, 35m, 30.0, 160, 48.0),
                        new VariantStateDto("p1_v1", "HMI-4-002", "negro", "M", 18, 2, 50m, 35m, 30.0, 160, 60.0)
                    ]),
                new ProductStateDto("p2", "SHI-1", "Printed Oversized Tee", "SHEIN", "Tops", "Unisex", 200, 30, 3, 120m, 0.9, 60.0,
                    [
                        new VariantStateDto("p2_v0", "SHI-1-001", "rosa", "S", 18, 2, 40m, 26m, 35.0, 200, 66.0),
                        new VariantStateDto("p2_v1", "SHI-1-002", "rosa", "M", 12, 1, 40m, 26m, 35.0, 200, 90.0)
                    ]),
                new ProductStateDto("p7", "SHI-6", "Ribbed Crop Top", "SHEIN", "Tops", "Female", 140, 26, 4, 104m, 2.2, 70.0,
                    [
                        new VariantStateDto("p7_v0", "SHI-6-001", "negro", "S", 15, 2, 40m, 26m, 35.0, 140, 60.0),
                        new VariantStateDto("p7_v1", "SHI-6-002", "blanco", "S", 11, 2, 40m, 26m, 35.0, 140, 70.0)
                    ]),
                new ProductStateDto("p8", "GUE-3", "Washed Denim Jacket", "GUESS", "Outerwear", "Female", 220, 19, 6, 190m, 1.1, 55.0,
                    [
                        new VariantStateDto("p8_v0", "GUE-3-001", "azul", "S", 10, 3, 100m, 60m, 40.0, 220, 60.0),
                        new VariantStateDto("p8_v1", "GUE-3-002", "azul", "M", 9, 3, 100m, 60m, 40.0, 220, 55.0)
                    ]),
                new ProductStateDto("p9", "DIE-4", "Slim Fit Chino", "DIESEL", "Pants", "Male", 280, 21, 3, 105m, 0.8, 100.0,
                    [
                        new VariantStateDto("p9_v0", "DIE-4-001", "negro", "32", 12, 2, 100m, 70m, 30.0, 280, 105.0),
                        new VariantStateDto("p9_v1", "DIE-4-002", "azul", "34", 9, 1, 100m, 70m, 30.0, 280, 130.0)
                    ]),
                new ProductStateDto("p10", "UNI-5", "Supima Crew Neck", "UNIQLO", "Tops", "Unisex", 170, 44, 8, 448m, 0.9, 40.0,
                    [
                        new VariantStateDto("p10_v0", "UNI-5-001", "gris", "M", 25, 4, 80m, 55m, 31.3, 170, 44.0),
                        new VariantStateDto("p10_v1", "UNI-5-002", "negro", "M", 19, 4, 80m, 55m, 31.3, 170, 42.0)
                    ]),

                // ---- Star products: the brief says do not touch --------------------------------
                new ProductStateDto("p3", "NIK-9", "Basic Cotton Tee", "NIKE", "Tops", "Unisex", 45, 40, 120, 2640m, 0.6, 3.0,
                    [
                        new VariantStateDto("p3_v0", "NIK-9-001", "blanco", "M", 24, 74, 60m, 38m, 36.7, 45, 0.7),
                        new VariantStateDto("p3_v1", "NIK-9-002", "negro", "M", 16, 46, 60m, 38m, 36.7, 45, 0.8)
                    ]),
                new ProductStateDto("p13", "TOM-7", "Icon Flag Tee", "TOMMY HILFIGER", "Tops", "Unisex", 50, 36, 95, 1995m, 0.7, 2.6,
                    [
                        new VariantStateDto("p13_v0", "TOM-7-001", "blanco", "M", 21, 55, 60m, 42m, 30.0, 50, 0.6),
                        new VariantStateDto("p13_v1", "TOM-7-002", "azul", "M", 15, 40, 60m, 42m, 30.0, 50, 0.7)
                    ]),

                // ---- Stagnant adult brands, worst numbers in the set ---------------------------
                new ProductStateDto("p4", "RAL-2", "Oxford Shirt", "RALPH LAUREN", "Shirts", "Male", 180, 24, 6, 288m, 1.2, 48.0,
                    [
                        new VariantStateDto("p4_v0", "RAL-2-001", "white", "M", 12, 3, 95m, 55m, 42.1, 180, 38.0),
                        new VariantStateDto("p4_v1", "RAL-2-002", "white", "L", 12, 3, 95m, 55m, 42.1, 180, 40.0)
                    ]),
                new ProductStateDto("p5", "RAL-5", "Chino Trouser", "RALPH LAUREN", "Pants", "Male", 320, 16, 1, 80m, 0, 130.0,
                    [
                        new VariantStateDto("p5_v0", "RAL-5-001", "beige", "30", 9, 1, 100m, 70m, 30.0, 320, 120.0),
                        new VariantStateDto("p5_v1", "RAL-5-002", "beige", "32", 7, 0, 100m, 70m, 30.0, 320, null)
                    ]),
                new ProductStateDto("p12", "LAC-3", "Cotton Pique Polo", "LACOSTE", "Tops", "Male", 340, 14, 1, 84m, 0, 150.0,
                    [
                        new VariantStateDto("p12_v0", "LAC-3-001", "verde", "M", 8, 1, 100m, 70m, 30.0, 340, 110.0),
                        new VariantStateDto("p12_v1", "LAC-3-002", "blanco", "L", 6, 0, 100m, 70m, 30.0, 340, null)
                    ]),

                // ---- Brands the model is genuinely unsure about --------------------------------
                new ProductStateDto("p6", "TOM-2", "Linen Shirt", "TOMMY HILFIGER", "Shirts", "Male", 240, 18, 2, 64m, 0, 120.0,
                    [
                        new VariantStateDto("p6_v0", "TOM-2-001", "beige", "M", 11, 1, 80m, 50m, 37.5, 240, 110.0),
                        new VariantStateDto("p6_v1", "TOM-2-002", "beige", "L", 7, 1, 80m, 50m, 37.5, 240, 140.0)
                    ]),
                new ProductStateDto("p11", "LEV-8", "511 Slim Jean", "LEVIS", "Pants", "Male", 260, 25, 3, 150m, 1.6, 85.0,
                    [
                        new VariantStateDto("p11_v0", "LEV-8-001", "azul", "32", 14, 2, 100m, 60m, 40.0, 260, 95.0),
                        new VariantStateDto("p11_v1", "LEV-8-002", "negro", "34", 11, 1, 100m, 60m, 40.0, 260, 130.0)
                    ]),

                // ---- Accessory: one size, so a size run cannot break ---------------------------
                new ProductStateDto("p14", "HMI-9", "Merino Wool Scarf", "H&M", "Accessories", "Unisex", 300, 33, 2, 132m, 1.0, 165.0,
                    [
                        new VariantStateDto("p14_v0", "HMI-9-001", "gris", "Unica", 33, 2, 60m, 40m, 33.3, 300, 165.0)
                    ])
            ]);
    }
}
