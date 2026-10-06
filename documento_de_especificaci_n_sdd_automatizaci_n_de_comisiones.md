# Especificación de Diseño de Software (SDD)
## Proyecto: Sistema de Automatización de Comisiones
## Plataforma: .NET WPF (C#)
## Arquitectura: MVVM (Model-View-ViewModel)

---

### 1. Descripción General del Sistema
Aplicación de escritorio local desarrollada en WPF y C# para automatizar la recuperación de cartera y el cálculo de comisiones. El sistema cruza dos exportaciones CSV del sistema: facturas y pagos de clientes. Concilia cobros, retenciones y notas de crédito, y genera un libro Excel mensual para el supervisor.

### 2. Stack Tecnológico (Paquetes NuGet Requeridos)
*   **UI y Presentación:** `MaterialDesignThemes` (UI Moderna).
*   **Acceso a Datos:** `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Tools` (SQLite local para entidades y configuración).
*   **Lectura CSV:** `Microsoft.VisualBasic.FileIO.TextFieldParser` para exportaciones delimitadas por punto y coma con metadatos antes de los encabezados.
*   **Manipulación de Excel:** `ClosedXML` para generar el reporte final `.xlsx`.

---

### 3. Modelos de Datos (Entity Framework Core)

**Vendedor.cs**
*   `Id` (int, PK)
*   `Nombre` (string, max 100)
*   `Nivel` (string, max 50) - Ej: Junior, Senior

**ReglaComision.cs**
*   `Id` (int, PK)
*   `NivelVendedor` (string, match con Vendedor.Nivel)
*   `TipoProducto` (string)
*   `RangoVentaMinimo` (decimal)
*   `RangoVentaMaximo` (decimal)
*   `PorcentajeComision` (decimal)

**FacturaProcesada.cs** (Transaccional)
*   `Id` (int, PK)
*   `NumeroFactura` (string)
*   `VendedorId` (int, FK)
*   `Subtotal` (decimal)
*   `MontoComision` (decimal)
*   `Estado` (string) - Ej: "Pagada", "Pendiente"

---

### 4. Flujo de Ejecución y Reglas de Negocio

1.  **Ingesta de Facturas (CSV):** El usuario carga la exportación de facturas. Se leen `Código`, `Cliente (Nombre)`, `Dirección de entrega`, `Usuario`, `Usuario (Nombre)`, `Fecha de creación`, `Subtotal`, `Descuento`, `Impuestos`, `Total` y `Estatus`. Se excluyen las anuladas; `Usuario (Nombre)` identifica al vendedor.
2.  **Ingesta de Pagos (CSV):** El usuario carga la exportación de pagos de clientes. Se procesan movimientos con `Estatus = Recibido`; transferencias, depósitos y retenciones se extraen de `Comentario`. Las referencias abreviadas, como `F/001-011-000000842/895`, se expanden a sus dos facturas. Las notas de crédito se asocian por cliente a los saldos pendientes de facturas con cobros enlazados.
3.  **Cruce de Datos:** Los números se normalizan entre formatos como `FA001-011-000000848`, `F/001-011-000000848` y secuenciales abreviados. Si un movimiento cita varias facturas, el importe se distribuye proporcionalmente al total de cada factura.
4.  **Cálculo:** `Venta Bruta = Subtotal + Descuento`. `Total Cobrado` muestra transferencias y depósitos. Retenciones y notas de crédito aplicadas participan en la conciliación. `Cobro Neto` es el cobro total conciliado multiplicado por la proporción `Subtotal / Total`, limitado al subtotal. Los días se cuentan entre la fecha de factura y la fecha de registro del pago. La comisión es provisionalmente el 3% de `Cobro Neto`.
5.  **Exportación:** Se genera un nuevo libro `Comisiones_aaaa_mm.xlsx`, con hojas agrupadas por vendedor y mes, campos del reporte supervisor y totales; no se requiere una plantilla previa.

---

### 5. Definición de Servicios (Interfaces)

*   `ILectorFacturasCsvService`: Lee el CSV de facturas y devuelve cliente, vendedor, fechas, importes y estado.
*   `ILectorPagosCsvService`: Lee el CSV de pagos y devuelve movimientos recibidos con sus facturas normalizadas, fecha, tipo e importe.
*   `IMotorCalculoService`: Cruza y distribuye movimientos por factura, aplica retenciones y notas de crédito, y crea las filas del reporte con comisión provisional del 3%.
*   `IExportadorPlantillaExcelService`: Recibe las filas del reporte y una ruta de salida. Usa ClosedXML para generar el libro mensual por vendedor.

---

### 6. Fases de Desarrollo (Prompts para el Agente IA)

**Fase 1: Base de Datos y Entidades**
> "Implementa la capa de datos. Crea las clases `Vendedor`, `ReglaComision` y `FacturaProcesada` con DataAnnotations. Configura `AppDbContext` heredando de DbContext para usar SQLite. Incluye un método `OnModelCreating` que inyecte datos semilla (Seed Data) de prueba para vendedores y reglas."

**Fase 2: Lectura de Exportaciones CSV**
> "Implementa lectores CSV para facturas y pagos de clientes. Detecta la fila de encabezados después de los metadatos, usa `;` como delimitador y conserva importes y fechas con formato ecuatoriano. Del CSV de facturas obtiene cliente, vendedor y valores; del CSV de pagos extrae referencias completas, abreviadas y múltiples desde `Comentario`."

**Fase 3: Cruce de Datos y Cálculo**
> "Implementa `IMotorCalculoService` para cruzar facturas y pagos recibidos por número normalizado. Excluye facturas anuladas, distribuye pagos multifactura proporcionalmente, aplica retenciones y notas de crédito a saldos enlazados y calcula los campos del reporte con comisión provisional del 3% sobre Cobro Neto."

**Fase 4: Inyección en Plantilla Excel**
> "Implementa `IExportadorPlantillaExcelService` con ClosedXML para generar un libro nuevo del reporte supervisor, agrupado en hojas por vendedor y mes. Incluye Cliente, Sucursal, Factura, fechas, Venta Bruta, Descuento, Total Cobrado, IVA, Cobro Neto, Días, porcentaje y total de comisión, con formatos monetarios y totales."

**Fase 5: Capa MVVM**
> "Crea `BaseViewModel` con `INotifyPropertyChanged`. Crea `MainViewModel` con una colección observable de filas del reporte, estado `IsBusy` y comandos para cargar el CSV de facturas, cargar el CSV de pagos, calcular y exportar. Inyecta los lectores CSV, el motor y el exportador."

**Fase 6: Interfaz de Usuario (UI)**
> "Diseña `MainWindow.xaml` utilizando MaterialDesignInXAML. Divide la pantalla en una barra lateral con acciones para cargar ambos CSV, calcular y exportar, y un área principal con resumen y un `DataGrid` de solo lectura con las columnas del reporte supervisor."