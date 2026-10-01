# Especificación de Diseño de Software (SDD)
## Proyecto: Sistema de Automatización de Comisiones
## Plataforma: .NET WPF (C#)
## Arquitectura: MVVM (Model-View-ViewModel)

---

### 1. Descripción General del Sistema
Aplicación de escritorio local desarrollada en WPF y C# para automatizar el cálculo de comisiones por ventas. El sistema reemplaza un proceso manual cruzando datos de dos fuentes: un reporte de pagos en Excel y las facturas originales en PDF. Finalmente, calcula las comisiones según reglas de negocio predefinidas y exporta los resultados inyectándolos en una plantilla de Excel existente, preservando sus fórmulas.

### 2. Stack Tecnológico (Paquetes NuGet Requeridos)
*   **UI y Presentación:** `MaterialDesignThemes` (UI Moderna).
*   **Acceso a Datos:** `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Tools` (SQLite local).
*   **Lectura y Extracción OCR:** `Azure.AI.DocumentIntelligence` (Capa gratuita F0) o `Tesseract` + `UglyToad.PdfPig` (Alternativa local).
*   **Manipulación de Excel:** `ClosedXML` (Para leer el reporte de pagos y escribir en la plantilla final).

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

1.  **Ingesta de Pagos (Excel):** El usuario carga el archivo Excel de pagos. El sistema lee una columna específica y utiliza Expresiones Regulares (RegEx) para extraer el número de factura oculto dentro de cadenas de texto (ej. "Pago de FAC-1234").
2.  **Lectura OCR (PDFs):** El usuario selecciona la carpeta con las facturas en PDF. El sistema extrae el número de factura, nombre del vendedor y subtotal usando Azure Document Intelligence.
3.  **Cruce de Datos (Matching):** El sistema realiza un `Join` en memoria. Solo se calculan las comisiones de las facturas en PDF cuyo número extraído coincida con los números extraídos del Excel de pagos.
4.  **Cálculo de Comisión:** Para los matches exitosos, el motor cruza el nivel del vendedor y el subtotal contra la tabla `ReglasComision` en la base de datos para obtener el porcentaje aplicable y calcular el monto final.
5.  **Exportación a Plantilla (Excel):** El sistema abre un archivo `.xlsx` preexistente (plantilla del jefe), inscribe los registros a partir de una fila específica (ej. fila 5) sin alterar las fórmulas adyacentes, y guarda el resultado como un archivo nuevo (ej. `Comisiones_Generadas.xlsx`).

---

### 5. Definición de Servicios (Interfaces)

*   `ILectorPagosExcelService`: Recibe un `string rutaArchivo`. Utiliza `ClosedXML` y RegEx para devolver un `List<string>` con los números de factura pagados.
*   `IOCRService`: Recibe un `string rutaPdf`. Retorna un objeto temporal `FacturaDto` con Subtotal, Vendedor y NumeroFactura.
*   `IMotorCalculoService`: Recibe un `List<string> facturasPagadas` y un `List<FacturaDto> facturasLeidas`. Interactúa con EF Core para evaluar rangos y devuelve un `IEnumerable<FacturaProcesada>`.
*   `IExportadorPlantillaExcelService`: Recibe el `IEnumerable<FacturaProcesada>` y un `string rutaPlantilla`. Usa `ClosedXML` (`new XLWorkbook(rutaPlantilla)`) para insertar los datos y guarda un nuevo archivo.

---

### 6. Fases de Desarrollo (Prompts para el Agente IA)

**Fase 1: Base de Datos y Entidades**
> "Implementa la capa de datos. Crea las clases `Vendedor`, `ReglaComision` y `FacturaProcesada` con DataAnnotations. Configura `AppDbContext` heredando de DbContext para usar SQLite. Incluye un método `OnModelCreating` que inyecte datos semilla (Seed Data) de prueba para vendedores y reglas."

**Fase 2: Servicios de Ingesta (Excel y OCR)**
> "Implementa `ILectorPagosExcelService` usando ClosedXML para leer la columna 'Descripción' de un Excel y extraer números de factura usando RegEx. Luego, implementa `IOCRService` usando Azure.AI.DocumentIntelligence para extraer el Número de Factura, Subtotal y Nombre del Vendedor de un PDF."

**Fase 3: Cruce de Datos y Cálculo**
> "Implementa `IMotorCalculoService`. Escribe la lógica LINQ que filtre las facturas leídas por el OCR verificando si existen en la lista de facturas pagadas (obtenida del Excel). Para las coincidencias, busca la regla correspondiente en `AppDbContext` basándose en el nivel del vendedor y el monto, calcula la comisión, y devuelve la lista de resultados."

**Fase 4: Inyección en Plantilla Excel**
> "Implementa `IExportadorPlantillaExcelService`. Usa ClosedXML para abrir un archivo Excel existente a modo de plantilla (`rutaPlantilla`). Inserta la lista de resultados de comisiones iterando a partir de la fila 5, rellenando las columnas 'Factura', 'Vendedor' y 'Comisión'. Guarda el resultado como un archivo nuevo usando `SaveAs`."

**Fase 5: Capa MVVM**
> "Crea `BaseViewModel` con `INotifyPropertyChanged`. Crea `MainViewModel` con propiedades `ObservableCollection<FacturaProcesada>`, un estado `IsBusy`, y comandos `ICommand` para 'CargarPagos', 'CargarPDFs', 'Calcular' y 'ExportarExcel'. Inyecta las dependencias de los servicios creados en las fases anteriores."

**Fase 6: Interfaz de Usuario (UI)**
> "Diseña `MainWindow.xaml` utilizando MaterialDesignInXAML. Divide la pantalla en dos secciones: una barra lateral con los botones de acción atados a los comandos del ViewModel, y un área principal con un `DataGrid` de solo lectura atado al `ObservableCollection` para visualizar las comisiones calculadas en tiempo real."