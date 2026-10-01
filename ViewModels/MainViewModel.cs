using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using osakaautomate.Dtos;
using osakaautomate.Infrastructure;
using osakaautomate.Models;
using osakaautomate.Services;

namespace osakaautomate.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly ILectorPagosExcelService _lectorPagosExcelService;
    private readonly IOCRService _ocrService;
    private readonly IMotorCalculoService _motorCalculoService;
    private readonly IExportadorPlantillaExcelService _exportadorPlantillaExcelService;
    private readonly AsyncRelayCommand _cargarPagosCommand;
    private readonly AsyncRelayCommand _cargarPdfsCommand;
    private readonly RelayCommand _calcularCommand;
    private readonly AsyncRelayCommand _exportarExcelCommand;
    private List<string> _facturasPagadas = [];
    private List<FacturaDto> _facturasLeidas = [];
    private bool _isBusy;
    private string _estado = "Listo";

    public MainViewModel(
        ILectorPagosExcelService lectorPagosExcelService,
        IOCRService ocrService,
        IMotorCalculoService motorCalculoService,
        IExportadorPlantillaExcelService exportadorPlantillaExcelService)
    {
        _lectorPagosExcelService = lectorPagosExcelService;
        _ocrService = ocrService;
        _motorCalculoService = motorCalculoService;
        _exportadorPlantillaExcelService = exportadorPlantillaExcelService;

        _cargarPagosCommand = new AsyncRelayCommand(CargarPagosAsync);
        _cargarPdfsCommand = new AsyncRelayCommand(CargarPdfsAsync);
        _calcularCommand = new RelayCommand(Calcular, PuedeCalcular);
        _exportarExcelCommand = new AsyncRelayCommand(ExportarExcelAsync, PuedeExportar);

        CargarPagosCommand = _cargarPagosCommand;
        CargarPdfsCommand = _cargarPdfsCommand;
        CalcularCommand = _calcularCommand;
        ExportarExcelCommand = _exportarExcelCommand;
    }

    public ObservableCollection<FacturaProcesada> FacturasProcesadas { get; } = [];

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                ActualizarComandos();
            }
        }
    }

    public string Estado
    {
        get => _estado;
        private set => SetProperty(ref _estado, value);
    }

    public ICommand CargarPagosCommand { get; }
    public ICommand CargarPdfsCommand { get; }
    public ICommand CalcularCommand { get; }
    public ICommand ExportarExcelCommand { get; }

    private async Task CargarPagosAsync()
    {
        var dialogo = new OpenFileDialog
        {
            Filter = "Archivos de Excel|*.xlsx;*.xlsm",
            Title = "Seleccionar reporte de pagos"
        };

        if (dialogo.ShowDialog() != true)
        {
            return;
        }

        IsBusy = true;

        try
        {
            _facturasPagadas = await Task.Run(() => _lectorPagosExcelService.LeerFacturasPagadas(dialogo.FileName));
            Estado = $"Se cargaron {_facturasPagadas.Count} facturas pagadas.";
            ActualizarComandos();
        }
        catch (Exception exception)
        {
            Estado = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CargarPdfsAsync()
    {
        var dialogo = new OpenFolderDialog
        {
            Title = "Seleccionar carpeta de facturas PDF"
        };

        if (dialogo.ShowDialog() != true)
        {
            return;
        }

        IsBusy = true;

        try
        {
            var archivosPdf = Directory.EnumerateFiles(dialogo.FolderName, "*.pdf", SearchOption.TopDirectoryOnly).ToList();
            _facturasLeidas = (await Task.WhenAll(archivosPdf.Select(archivo => _ocrService.ExtraerFacturaAsync(archivo))))
                .Where(factura => !string.IsNullOrWhiteSpace(factura.NumeroFactura))
                .ToList();
            Estado = $"Se procesaron {_facturasLeidas.Count} facturas PDF.";
            ActualizarComandos();
        }
        catch (Exception exception)
        {
            Estado = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Calcular()
    {
        IsBusy = true;

        try
        {
            var resultados = _motorCalculoService.CalcularComisiones(_facturasPagadas, _facturasLeidas);
            FacturasProcesadas.Clear();

            foreach (var factura in resultados)
            {
                FacturasProcesadas.Add(factura);
            }

            Estado = $"Se calcularon {FacturasProcesadas.Count} comisiones.";
            ActualizarComandos();
        }
        catch (Exception exception)
        {
            Estado = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExportarExcelAsync()
    {
        var dialogo = new OpenFileDialog
        {
            Filter = "Archivos de Excel|*.xlsx;*.xlsm",
            Title = "Seleccionar plantilla de comisiones"
        };

        if (dialogo.ShowDialog() != true)
        {
            return;
        }

        IsBusy = true;

        try
        {
            var rutaSalida = await Task.Run(() => _exportadorPlantillaExcelService.ExportarComisiones(
                FacturasProcesadas.ToList(),
                dialogo.FileName));
            Estado = $"Archivo generado: {rutaSalida}";
        }
        catch (Exception exception)
        {
            Estado = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool PuedeCalcular()
    {
        return _facturasPagadas.Count > 0 && _facturasLeidas.Count > 0 && !IsBusy;
    }

    private bool PuedeExportar()
    {
        return FacturasProcesadas.Count > 0 && !IsBusy;
    }

    private void ActualizarComandos()
    {
        _calcularCommand.NotifyCanExecuteChanged();
        _exportarExcelCommand.NotifyCanExecuteChanged();
    }
}