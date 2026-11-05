    using Common.Contracts.authentication;
    using Common.Contracts.inventory;
    using Common.Utilities;
    using Microsoft.EntityFrameworkCore;
    using Module.Sales.Application.Abstraction;
    using Module.Sales.Application.UseCases.Registers.Close;
    using Module.Sales.Application.UseCases.Registers.GetById;
    using Module.Sales.Domain;
    using Moq;

    namespace Test.Sales;

    public class ClosureDetailTests
    {
        private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        private static readonly Guid BranchId = Guid.NewGuid();
        private static readonly Guid UserId = Guid.NewGuid();

        [Fact]
        public async Task ExecuteCurrent_ShouldExcludeSoftDeletedMovements_AndNotAffectTotals()
        {
            var dbName = $"ClosureTest_{Guid.NewGuid()}";
            var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);

            // 1. Open closure
            using (var db = TestSalesDbContextFactory.Create(tenantCtx, dbName))
            {
                var closure = CashRegisterClosure.Open(BranchId, 1000m, UserId, "Test User");
                db.CashRegisterClosures.Add(closure);
                await db.SaveChangesAsync();

                var saleCash = CreateSale(closure.Id, PaymentMethod.Cash, 500m);
                var saleQr = CreateSale(closure.Id, PaymentMethod.QrCode, 300m);
                db.Sales.AddRange(saleCash, saleQr);
                await db.SaveChangesAsync();

                var outflowKeep = CashRegisterMovement.Create(closure.Id, 100m, "Gasto valido", CashRegisterMovementType.Outflow, UserId, "Test User");
                var outflowDeleted = CashRegisterMovement.Create(closure.Id, 999m, "Gasto eliminado", CashRegisterMovementType.Outflow, UserId, "Test User");
                var inflow = CashRegisterMovement.Create(closure.Id, 50m, "Ingreso", CashRegisterMovementType.Inflow, UserId, "Test User");
                db.CashRegisterMovements.AddRange(outflowKeep, outflowDeleted, inflow);
                await db.SaveChangesAsync();

                outflowDeleted.DeletedAt = DateTime.UtcNow;
                outflowDeleted.DeletedBy = UserId;
                await db.SaveChangesAsync();
            }

            // 2. Query current details with fresh context (simulates new request scope, query filters apply)
            using (var db = TestSalesDbContextFactory.Create(tenantCtx, dbName))
            {
                var inventoryMock = new Mock<IInventoryIntegrationService>();
                inventoryMock.Setup(s => s.GetVariantsWithStock(It.IsAny<List<Guid>>(), It.IsAny<Guid>()))
                    .ReturnsAsync(new List<ProductVariantStockDto>());

                var sut = new GetClosureDetail(db, inventoryMock.Object);
                var result = await sut.ExecuteCurrent(CreateActorContext());

                Assert.True(result.IsSuccess, result.Error?.Message);
                var dto = result.Value;

                Assert.Equal(2, dto.Movements.Count);
                Assert.DoesNotContain(dto.Movements, m => m.Description == "Gasto eliminado");
                Assert.Equal(100m, dto.TotalExpenses);
                Assert.Equal(800m, dto.TotalSales);
                Assert.Equal(500m, dto.CashSales);
                Assert.Equal(2, dto.Sales.Count);
            }
        }

        [Fact]
        public async Task Close_ShouldExcludeSoftDeletedMovements_FromSystemSalesAmount()
        {
            var dbName = $"ClosureTest_{Guid.NewGuid()}";
            var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
            Guid closureId;

            using (var db = TestSalesDbContextFactory.Create(tenantCtx, dbName))
            {
                var closure = CashRegisterClosure.Open(BranchId, 1000m, UserId, "Test User");
                closureId = closure.Id;
                db.CashRegisterClosures.Add(closure);
                await db.SaveChangesAsync();

                var saleCash = CreateSale(closure.Id, PaymentMethod.Cash, 400m);
                db.Sales.Add(saleCash);
                await db.SaveChangesAsync();

                var outflowKeep = CashRegisterMovement.Create(closure.Id, 80m, "Gasto keep", CashRegisterMovementType.Outflow, UserId, "Test User");
                var outflowDeleted = CashRegisterMovement.Create(closure.Id, 500m, "Gasto deleted", CashRegisterMovementType.Outflow, UserId, "Test User");
                db.CashRegisterMovements.AddRange(outflowKeep, outflowDeleted);
                await db.SaveChangesAsync();

                outflowDeleted.DeletedAt = DateTime.UtcNow;
                outflowDeleted.DeletedBy = UserId;
                await db.SaveChangesAsync();
            }

            // Close with fresh context
            using (var db = TestSalesDbContextFactory.Create(tenantCtx, dbName))
            {
                var directCount = await db.CashRegisterMovements.CountAsync();
                Assert.Equal(1, directCount);

                var closeSut = new CloseCashRegister(db);
                var closeResult = await closeSut.Execute(CreateActorContext(), new CloseCashRegisterDto { RealCountedAmount = 1320m });

                Assert.True(closeResult.IsSuccess, closeResult.Error?.Message);
                Assert.Equal(1320m, closeResult.Value.ExpectedCash);
                Assert.Equal(80m, closeResult.Value.OutflowsTotal);
                Assert.Equal(0m, closeResult.Value.Difference);
            }

            // Verify via GetClosureDetail after close also excludes deleted
            using (var db = TestSalesDbContextFactory.Create(tenantCtx, dbName))
            {
                var inventoryMock = new Mock<IInventoryIntegrationService>();
                inventoryMock.Setup(s => s.GetVariantsWithStock(It.IsAny<List<Guid>>(), It.IsAny<Guid>()))
                    .ReturnsAsync(new List<ProductVariantStockDto>());
                var detailSut = new GetClosureDetail(db, inventoryMock.Object);
                var detail = await detailSut.Execute(CreateActorContext(), closureId);
                Assert.True(detail.IsSuccess);
                Assert.Equal(80m, detail.Value.TotalExpenses);
                Assert.Single(detail.Value.Movements);
            }
        }

        [Fact]
        public async Task Execute_ShouldReturnHasReturnFalse_AndCanReturnTrue_WhenSaleWithoutReturn()
        {
            var dbName = $"ClosureTest_{Guid.NewGuid()}";
            var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
            Guid closureId;
            Guid saleId;
            using (var db = TestSalesDbContextFactory.Create(tenantCtx, dbName))
            {
                var closure = CashRegisterClosure.Open(BranchId, 500m, UserId, "Test User");
                closureId = closure.Id;
                db.CashRegisterClosures.Add(closure);
                await db.SaveChangesAsync();
                var sale = CreateSale(closure.Id, PaymentMethod.Cash, 100m);
                saleId = sale.Id;
                db.Sales.Add(sale);
                await db.SaveChangesAsync();
            }
            using (var db = TestSalesDbContextFactory.Create(tenantCtx, dbName))
            {
                var inventoryMock = new Mock<IInventoryIntegrationService>();
                inventoryMock.Setup(s => s.GetVariantsWithStock(It.IsAny<List<Guid>>(), It.IsAny<Guid>()))
                    .ReturnsAsync(new List<ProductVariantStockDto>());
                var sut = new GetClosureDetail(db, inventoryMock.Object);
                var result = await sut.Execute(CreateActorContext(), closureId);
                Assert.True(result.IsSuccess);
                var saleDto = Assert.Single(result.Value.Sales.Where(s => s.Id == saleId));
                Assert.False(saleDto.HasReturn);
                Assert.True(saleDto.CanReturn);
                Assert.Null(saleDto.CantReturnReason);
                Assert.Equal(SaleType.Sale, saleDto.Type);
            }
        }

        [Fact]
        public async Task Execute_ShouldReturnHasReturnTrue_AndCanReturnFalse_WhenSaleAlreadyRefunded()
        {
            var dbName = $"ClosureTest_{Guid.NewGuid()}";
            var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
            Guid closureId;
            Guid saleId;
            using (var db = TestSalesDbContextFactory.Create(tenantCtx, dbName))
            {
                var closure = CashRegisterClosure.Open(BranchId, 500m, UserId, "Test User");
                closureId = closure.Id;
                db.CashRegisterClosures.Add(closure);
                await db.SaveChangesAsync();
                var sale = CreateSale(closure.Id, PaymentMethod.Cash, 100m);
                saleId = sale.Id;
                db.Sales.Add(sale);
                await db.SaveChangesAsync();
                var ret = new Sale
                {
                    Id = Guid.NewGuid(),
                    BranchId = BranchId,
                    SoldById = UserId,
                    SoldByName = "Test User",
                    CreatedBy = UserId,
                    CreatedByName = "Test User",
                    CashRegisterClosureId = closure.Id,
                    PaymentMethod = PaymentMethod.Cash,
                    DocumentType = DocumentType.Ticket,
                    Type = SaleType.Return,
                    OriginalSaleId = saleId,
                    TotalAmount = -100m,
                    TenantId = TenantId,
                    CreatedAt = DateTime.UtcNow
                };
                db.Sales.Add(ret);
                await db.SaveChangesAsync();
            }
            using (var db = TestSalesDbContextFactory.Create(tenantCtx, dbName))
            {
                var inventoryMock = new Mock<IInventoryIntegrationService>();
                inventoryMock.Setup(s => s.GetVariantsWithStock(It.IsAny<List<Guid>>(), It.IsAny<Guid>()))
                    .ReturnsAsync(new List<ProductVariantStockDto>());
                var sut = new GetClosureDetail(db, inventoryMock.Object);
                var result = await sut.Execute(CreateActorContext(), closureId);
                Assert.True(result.IsSuccess);
                var saleDto = result.Value.Sales.First(s => s.Id == saleId);
                Assert.True(saleDto.HasReturn);
                Assert.False(saleDto.CanReturn);
                Assert.Equal("ALREADY_REFUNDED", saleDto.CantReturnReason);
            }
        }

        [Fact]
        public async Task Execute_ShouldReturnCanReturnFalse_WhenSaleIsReturnItself()
        {
            var dbName = $"ClosureTest_{Guid.NewGuid()}";
            var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
            Guid closureId;
            Guid saleId = Guid.NewGuid();
            Guid returnId = Guid.NewGuid();
            using (var db = TestSalesDbContextFactory.Create(tenantCtx, dbName))
            {
                var closure = CashRegisterClosure.Open(BranchId, 500m, UserId, "Test User");
                closureId = closure.Id;
                db.CashRegisterClosures.Add(closure);
                await db.SaveChangesAsync();
                var sale = CreateSale(closure.Id, PaymentMethod.Cash, 100m);
                sale.Id = saleId;
                sale.Type = SaleType.Sale;
                db.Sales.Add(sale);
                await db.SaveChangesAsync();
                var ret = new Sale
                {
                    Id = returnId,
                    BranchId = BranchId,
                    SoldById = UserId,
                    SoldByName = "Test User",
                    CreatedBy = UserId,
                    CreatedByName = "Test User",
                    CashRegisterClosureId = closure.Id,
                    PaymentMethod = PaymentMethod.Cash,
                    DocumentType = DocumentType.Ticket,
                    Type = SaleType.Return,
                    OriginalSaleId = saleId,
                    TotalAmount = -100m,
                    TenantId = TenantId,
                    CreatedAt = DateTime.UtcNow
                };
                db.Sales.Add(ret);
                await db.SaveChangesAsync();
            }
            using (var db = TestSalesDbContextFactory.Create(tenantCtx, dbName))
            {
                var inventoryMock = new Mock<IInventoryIntegrationService>();
                inventoryMock.Setup(s => s.GetVariantsWithStock(It.IsAny<List<Guid>>(), It.IsAny<Guid>()))
                    .ReturnsAsync(new List<ProductVariantStockDto>());
                var sut = new GetClosureDetail(db, inventoryMock.Object);
                var result = await sut.Execute(CreateActorContext(), closureId);
                Assert.True(result.IsSuccess);
                var returnDto = result.Value.Sales.First(s => s.Id == returnId);
                Assert.False(returnDto.HasReturn);
                Assert.False(returnDto.CanReturn);
                Assert.Equal("IS_RETURN", returnDto.CantReturnReason);
                Assert.Equal(SaleType.Return, returnDto.Type);
            }
        }

        [Fact]
        public async Task Execute_ShouldSetReturnStatus_ForEachSale()
        {
            var dbName = $"ClosureTest_{Guid.NewGuid()}";
            var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
            Guid closureId;
            Guid untouchedId;
            Guid partialId;
            Guid fullId;

            using (var db = TestSalesDbContextFactory.Create(tenantCtx, dbName))
            {
                var closure = CashRegisterClosure.Open(BranchId, 500m, UserId, "Test User");
                closureId = closure.Id;
                db.CashRegisterClosures.Add(closure);
                await db.SaveChangesAsync();

                var untouched = CreateSale(closure.Id, PaymentMethod.Cash, 50m, quantity: 1, number: 10);
                var partial = CreateSale(closure.Id, PaymentMethod.Cash, 100m, quantity: 2, number: 11);
                var full = CreateSale(closure.Id, PaymentMethod.Cash, 100m, quantity: 2, number: 12);
                untouchedId = untouched.Id;
                partialId = partial.Id;
                fullId = full.Id;

                db.Sales.AddRange(untouched, partial, full);
                await db.SaveChangesAsync();

                db.Sales.Add(CreateReturn(closure.Id, partial, returnedQuantity: 1, number: 20));
                db.Sales.Add(CreateReturn(closure.Id, full, returnedQuantity: 2, number: 21));
                await db.SaveChangesAsync();
            }

            using (var db = TestSalesDbContextFactory.Create(tenantCtx, dbName))
            {
                var inventoryMock = new Mock<IInventoryIntegrationService>();
                inventoryMock.Setup(s => s.GetVariantsWithStock(It.IsAny<List<Guid>>(), It.IsAny<Guid>()))
                    .ReturnsAsync(new List<ProductVariantStockDto>());

                var sut = new GetClosureDetail(db, inventoryMock.Object);
                var result = await sut.Execute(CreateActorContext(), closureId);

                Assert.True(result.IsSuccess, result.Error?.Message);
                var byId = result.Value!.Sales.ToDictionary(s => s.Id);

                Assert.Equal(SaleReturnStatus.None, byId[untouchedId].ReturnStatus);
                Assert.Equal(SaleReturnStatus.Partial, byId[partialId].ReturnStatus);
                Assert.Equal(SaleReturnStatus.Full, byId[fullId].ReturnStatus);

                // HasReturn stays consistent with the new status.
                Assert.False(byId[untouchedId].HasReturn);
                Assert.True(byId[partialId].HasReturn);
                Assert.True(byId[fullId].HasReturn);
            }
        }

        [Fact]
        public async Task Execute_ShouldPopulateOriginalSaleIdentity_OnReturnRows()
        {
            var dbName = $"ClosureTest_{Guid.NewGuid()}";
            var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
            Guid closureId;
            Guid saleId;
            Guid returnId;

            using (var db = TestSalesDbContextFactory.Create(tenantCtx, dbName))
            {
                var closure = CashRegisterClosure.Open(BranchId, 500m, UserId, "Test User");
                closureId = closure.Id;
                db.CashRegisterClosures.Add(closure);
                await db.SaveChangesAsync();

                var sale = CreateSale(closure.Id, PaymentMethod.Cash, 100m, quantity: 1, number: 42);
                saleId = sale.Id;
                db.Sales.Add(sale);
                await db.SaveChangesAsync();

                var ret = CreateReturn(closure.Id, sale, returnedQuantity: 1, number: 43);
                returnId = ret.Id;
                db.Sales.Add(ret);
                await db.SaveChangesAsync();
            }

            using (var db = TestSalesDbContextFactory.Create(tenantCtx, dbName))
            {
                var inventoryMock = new Mock<IInventoryIntegrationService>();
                inventoryMock.Setup(s => s.GetVariantsWithStock(It.IsAny<List<Guid>>(), It.IsAny<Guid>()))
                    .ReturnsAsync(new List<ProductVariantStockDto>());

                var sut = new GetClosureDetail(db, inventoryMock.Object);
                var result = await sut.Execute(CreateActorContext(), closureId);

                Assert.True(result.IsSuccess, result.Error?.Message);
                var byId = result.Value!.Sales.ToDictionary(s => s.Id);

                Assert.Equal(saleId, byId[returnId].OriginalSaleId);
                Assert.Equal(42, byId[returnId].OriginalSaleNumber);
                Assert.Equal(SaleReturnStatus.None, byId[returnId].ReturnStatus);

                // A plain sale has no original sale reference.
                Assert.Null(byId[saleId].OriginalSaleId);
                Assert.Null(byId[saleId].OriginalSaleNumber);
                Assert.Equal(SaleReturnStatus.Full, byId[saleId].ReturnStatus);
            }
        }

        private static ActorContext CreateActorContext()
            => new(TenantId, UserId, "Test User", BranchId, [BranchId]);

        private static Sale CreateSale(Guid closureId, PaymentMethod payment, decimal total, int quantity = 1, int number = 0)
        {
            var sale = new Sale
            {
                Id = Guid.NewGuid(),
                Number = number,
                BranchId = BranchId,
                SoldById = UserId,
                SoldByName = "Test User",
                CreatedBy = UserId,
                CreatedByName = "Test User",
                CashRegisterClosureId = closureId,
                PaymentMethod = payment,
                DocumentType = DocumentType.Ticket,
                Type = SaleType.Sale,
                TotalAmount = total,
                TenantId = TenantId,
                CreatedAt = DateTime.UtcNow,
                SaleItems =
                [
                    new SaleItem
                    {
                        Id = Guid.NewGuid(),
                        ProductVariantId = Guid.NewGuid(),
                        ProductSku = "SKU-TEST",
                        ProductDisplayName = "Test Product",
                        Quantity = quantity,
                        UnitPrice = total,
                        UnitCost = 10m,
                        DiscountAmount = 0,
                        FinalPrice = total * quantity,
                        TenantId = TenantId
                    }
                ]
            };
            return sale;
        }

        private static Sale CreateReturn(Guid closureId, Sale original, int returnedQuantity, int number)
        {
            var originalItem = original.SaleItems.First();
            var returnId = Guid.NewGuid();

            return new Sale
            {
                Id = returnId,
                Number = number,
                BranchId = BranchId,
                SoldById = UserId,
                SoldByName = "Test User",
                CreatedBy = UserId,
                CreatedByName = "Test User",
                CashRegisterClosureId = closureId,
                PaymentMethod = PaymentMethod.Cash,
                DocumentType = DocumentType.Ticket,
                Type = SaleType.Return,
                OriginalSaleId = original.Id,
                TotalAmount = -originalItem.UnitPrice * returnedQuantity,
                TenantId = TenantId,
                CreatedAt = DateTime.UtcNow,
                SaleItems =
                [
                    new SaleItem
                    {
                        Id = Guid.NewGuid(),
                        SaleId = returnId,
                        OriginalSaleItemId = originalItem.Id,
                        ProductVariantId = originalItem.ProductVariantId,
                        ProductSku = originalItem.ProductSku,
                        ProductDisplayName = originalItem.ProductDisplayName,
                        Quantity = -returnedQuantity,
                        UnitPrice = originalItem.UnitPrice,
                        UnitCost = originalItem.UnitCost,
                        DiscountAmount = 0,
                        FinalPrice = -originalItem.UnitPrice * returnedQuantity,
                        TenantId = TenantId
                    }
                ]
            };
        }
    }
