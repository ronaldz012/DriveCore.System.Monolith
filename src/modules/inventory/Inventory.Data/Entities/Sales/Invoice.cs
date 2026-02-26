using Inventory.Data.Entities.Shared.Base;

namespace Inventory.Data.Entities.Sales;

    public class Invoice:Params
    {
        public int Id { get; set; } // ID de la factura
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public int UserId { get; set; }
        public int BranchId { get; set; }
        public string NitRucNif { get; set; } = string.Empty;
        public int InvoiceNumber { get; set; } // Numero de factura en el sisteme PHP
    public string ControlCode { get; set; } = string.Empty; // Codigo de control de la factura

        public float Subtotal { get; set; }
        public float TotalTax { get; set; }
        public float Discount { get; set; }
        public float Total { get; set; }

        public float Cash { get; set; }
        public float MoneyBack { get; set; }

        public DateTime InvoiceDateTime { get; set; }
        public DateTime InvoiceLimitDate { get; set; }

        public string CurrencyCode { get; set; } = string.Empty;
        public string ExchangeRate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        // REFERIDO A LA SUCURSAL
        public int BranchCode { get; set; }           // codigoSucursal
        public int PointOfSaleCode { get; set; }      // puntoVenta
        public string EconomicActivityCode { get; set; } = string.Empty; // actividadEconomica

        //CODIGOS DEL SIAT  
        public int SectorDocumentCode { get; set; }  // codigoDocumentoSector
        public int IdentityDocumentType { get; set; } // tipoDocumentoIdentidad
        public int PaymentMethodCode { get; set; }  // codigoMetodoPago
        public int CurrencyCodeSiat { get; set; }    // codigoMoneda

        //CODIGOS DE FACTURACION SIAT
        public string Cufd { get; set; } = string.Empty;
        public string Cuf { get; set; } = string.Empty;
        public string Cafc { get; set; } = string.Empty;

        //Si se elige tipo de pago tarjeta 4 primero y ultimos 4 4797000000007896
        public string CardNumber { get; set; } = string.Empty;          // numeroTarjeta
        public string ExchangeRateValue { get; set; } = string.Empty;   // tipoCambio

        //En caso de contingencia
        public int EventId { get; set; }
        public int PackageId { get; set; }
        public string SiatId { get; set; } = string.Empty; // siatId

    //Revisa para que sirve
    public string EmissionType { get; set; } = string.Empty;        // tipoEmision
        public string InvoiceType { get; set; } = string.Empty;         // tipoFacturaDocumento
        public string IssuerTaxId { get; set; } = string.Empty;         // nitEmisor

        //Tipo de ambiente 1: Real ; 2 Piloto
        public int Environment { get; set; }            // ambiente

        //Leyenda de la factura segun el primer Item de la lista
        public string Legend { get; set; } = string.Empty;              // leyenda
        public string PrintUrl { get; set; } = string.Empty; // URL para imprimir la factura

    public ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
        public Payment Payment { get; set; } = new Payment();
}





