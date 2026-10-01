# Spec-Driven Development (SDD): Sistema Automatizado de Comisiones (WPF)

## 1. Visión General del Proyecto
**Descripción:** Aplicación de escritorio orientada a automatizar la lectura de facturas de venta en formato PDF y el cálculo automático de comisiones para vendedores, utilizando reglas de negocio dinámicas.
**Plataforma:** Windows Desktop.
**Patrón Arquitectónico:** MVVM (Model-View-ViewModel).

## 2. Stack Tecnológico
*   **Lenguaje:** C# (.NET 8 o superior)
*   **Framework UI:** WPF (Windows Presentation Foundation)
*   **Diseño UI:** MaterialDesignThemes (NuGet)
*   **Arquitectura:** CommunityToolkit.Mvvm (para comandos y bindings eficientes)
*   **Base de Datos:** SQLite usando Entity Framework Core (`Microsoft.EntityFrameworkCore.Sqlite`)
*   **OCR / IA:** `Azure.AI.DocumentIntelligence` (Modelo preconstruido de facturas)
*   **Exportación:** `ClosedXML` (para generación de reportes en Excel)

## 3. Modelos de Dominio (Data Models)
El agente de IA deberá generar las entidades de Entity Framework Core basadas en la siguiente estructura:

*   **Vendedor (Seller):**
    *   `Id` (int, PK)
    *   `Nombre` (string)
    *   `Tipo` (string - ej: Junior, Senior)

*   **ReglaComision (CommissionRule):**
    *   `Id` (int, PK)
    *   `TipoVendedor` (string)
    *   `RangoMinimo` (decimal)
    *   `RangoMaximo` (decimal)
    *   `Porcentaje` (decimal - ej: 0.05 para 5%)

*   **FacturaProcesada (ProcessedInvoice):**
    *   `Id` (int, PK)
    *   `NumeroFactura` (string)
    *   `VendedorId` (int, FK)
    *   `Fecha` (DateTime)
    *   `Subtotal` (decimal)
    *   `ComisionCalculada` (decimal)
    *   `Estado` (string - "Éxito", "Requiere Revisión")

## 4. Interfaces de Servicios (Contracts)
El sistema debe estar desacoplado usando Inyección de Dependencias. Se requieren las siguientes interfaces:

*   `IOcrService`: Responsable de recibir la ruta de un PDF y devolver los datos extraídos.
    *   `Task<InvoiceExtractionResult> ExtractDataAsync(string pdfPath);`
*   `ICommissionEngine`: Responsable de aplicar las reglas de negocio sobre los datos extraídos.
    *   `Task<decimal> CalculateCommissionAsync(decimal subtotal, string tipoVendedor);`
*   `IExportService`: Responsable de generar el archivo de salida.
    *   `Task ExportToExcelAsync(IEnumerable<FacturaProcesada> records, string destinationPath);`

## 5. Flujo de Usuario y UI (Views & ViewModels)

### View 1: Dashboard (Procesamiento)
*   **UI:** Zona de "Drag and Drop" para soltar archivos PDF, botón "Procesar", y un `DataGrid` para ver resultados en tiempo real.
*   **ViewModel (`DashboardViewModel`):**
    *   `ObservableCollection<FacturaProcesada> Facturas`
    *   `RelayCommand ProcesarFacturasCommand`
    *   **Lógica:** Al activar el comando, iterar sobre los PDFs de forma asíncrona (`Task.WhenAll` para concurrencia limitada), llamar a `IOcrService`, luego a `ICommissionEngine`, y actualizar la UI mediante el hilo principal de WPF (Dispatcher).

### View 2: Configuración (Reglas de Negocio)
*   **UI:** `DataGrid` editable para administrar los porcentajes, rangos de venta y categorías de vendedores.
*   **ViewModel (`SettingsViewModel`):**
    *   CRUD completo conectado a Entity Framework Core (SQLite).

## 6. Casos de Uso Críticos y Manejo de Errores (Edge Cases)
1.  **Falla del OCR:** Si Azure AI no detecta el subtotal o el nombre, el estado de la factura en el Dashboard debe ser "Requiere Revisión" y el usuario debe poder editar el monto manualmente en la grilla.
2.  **Timeouts:** Implementar políticas de reintentos (Polly) al llamar a la API de Azure AI Document Intelligence.
3.  **UI Blocking:** Toda la lógica de procesamiento (OCR, BD, Cálculo) DEBE ejecutarse en hilos secundarios para no congelar la ventana de WPF.

## 7. Criterios de Aceptación para el Agente IA
1.  El código generado debe compilar sin errores en .NET 8.
2.  Se debe utilizar Inyección de Dependencias nativa de .NET (ServiceCollection) en `App.xaml.cs`.
3.  El acceso a datos debe ser asíncrono (`ToListAsync`, `SaveChangesAsync`).
4.  No se deben incluir las claves de la API de Azure hardcodeadas; deben leerse desde un archivo `appsettings.json`.