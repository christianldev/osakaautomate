using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using osakaautomate.Infrastructure;
using osakaautomate.Dtos;
using osakaautomate.Services;

namespace osakaautomate.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly ILectorFacturasCsvService _lectorFacturasCsvService;
    private readonly ILectorPagosCsvService _lectorPagosCsvService;
    private readonly IMotorCalculoService _motorCalculoService;
    private readonly IExportadorPlantillaExcelService _exportadorPlantillaExcelService;
    private readonly AsyncRelayCommand _cargarFacturasCommand;
    private readonly AsyncRelayCommand _cargarPagosCommand;
    private readonly RelayCommand _calcularCommand;
    private readonly AsyncRelayCommand _exportarExcelCommand;
    private List<FacturaCsvDto> _facturas = [];
    private List<PagoCsvDto> _pagos = [];
    private bool _isBusy;
    private string _estado = "Listo";
    private string _archivoFacturas = "Sin cargar";
    private string _archivoPagos = "Sin cargar";
    private decimal _totalCobroNeto;
    private decimal _totalComisiones;

    public MainViewModel(
        ILectorFacturasCsvService lectorFacturasCsvService,
        ILectorPagosCsvService lectorPagosCsvService,
        IMotorCalculoService motorCalculoService,
        IExportadorPlantillaExcelService exportadorPlantillaExcelService)
    {
        _lectorFacturasCsvService = lectorFacturasCsvService;
        _lectorPagosCsvService = lectorPagosCsvService;
        _motorCalculoService = motorCalculoService;
        _exportadorPlantillaExcelService = exportadorPlantillaExcelService;

        _cargarFacturasCommand = new AsyncRelayCommand(CargarFacturasAsync, () => !IsBusy);
        _cargarPagosCommand = new AsyncRelayCommand(CargarPagosAsync, () => !IsBusy);
        _calcularCommand = new RelayCommand(Calcular, PuedeCalcular);
        _exportarExcelCommand = new AsyncRelayCommand(ExportarExcelAsync, PuedeExportar);

        CargarFacturasCommand = _cargarFacturasCommand;
        CargarPagosCommand = _cargarPagosCommand;
        CalcularCommand = _calcularCommand;
        ExportarExcelCommand = _exportarExcelCommand;
    }

    public ObservableCollection<ComisionReporteDto> Comisiones { get; } = [];

    public string ArchivoFacturas
    {
        get => _archivoFacturas;
        private set => SetProperty(ref _archivoFacturas, value);
    }

    public string ArchivoPagos
    {
        get => _archivoPagos;
        private set => SetProperty(ref _archivoPagos, value);
    }

    public decimal TotalCobroNeto
    {
        get => _totalCobroNeto;
        private set => SetProperty(ref _totalCobroNeto, value);
    }

    public decimal TotalComisiones
    {
        get => _totalComisiones;
        private set => SetProperty(ref _totalComisiones, value);
    }

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

    public ICommand CargarFacturasCommand { get; }
    public ICommand CargarPagosCommand { get; }
    public ICommand CalcularCommand { get; }
    public ICommand ExportarExcelCommand { get; }

    private async Task CargarFacturasAsync()
    {
        var dialogo = new OpenFileDialog
        {
            Filter = "Archivo CSV|*.csv",
            Title = "Seleccionar exportación de facturas"
        };

        if (dialogo.ShowDialog() != true)
        {
            return;
        }

        IsBusy = true;
        try
        {
            _facturas = await Task.Run(() => _lectorFacturasCsvService.LeerFacturas(dialogo.FileName));
            ArchivoFacturas = Path.GetFileName(dialogo.FileName);
            LimpiarResultados();
            Estado = $"Facturas cargadas: {_facturas.Count}.";
        }
        catch (Exception exception)
        {
            Estado = $"Error al leer facturas: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CargarPagosAsync()
    {
        var dialogo = new OpenFileDialog
        {
            Filter = "Archivo CSV|*.csv",
            Title = "Seleccionar exportación de pagos de clientes"
        };

        if (dialogo.ShowDialog() != true)
        {
            return;
        }

        IsBusy = true;

        try
        {
            _pagos = await Task.Run(() => _lectorPagosCsvService.LeerPagos(dialogo.FileName));
            ArchivoPagos = Path.GetFileName(dialogo.FileName);
            LimpiarResultados();
            Estado = $"Movimientos de pago cargados: {_pagos.Count}.";
        }
        catch (Exception exception)
        {
            Estado = $"Error al leer pagos: {exception.Message}";
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
            var resultados = _motorCalculoService.CalcularComisiones(_facturas, _pagos).ToList();
            Comisiones.Clear();

            foreach (var factura in resultados)
            {
                Comisiones.Add(factura);
            }

            TotalCobroNeto = Comisiones.Sum(comision => comision.CobroNeto);
            TotalComisiones = Comisiones.Sum(comision => comision.TotalComision);
            Estado = $"Comisiones calculadas: {Comisiones.Count}.";
        }
        catch (Exception exception)
        {
            Estado = $"Error al calcular: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExportarExcelAsync()
    {
        var dialogo = new SaveFileDialog
        {
            Filter = "Libro de Excel|*.xlsx",
            Title = "Guardar reporte de comisiones",
            FileName = $"Comisiones_{DateTime.Today:yyyy_MM}.xlsx",
            DefaultExt = ".xlsx",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dialogo.ShowDialog() != true)
        {
            return;
        }

        IsBusy = true;

        try
        {
            var rutaSalida = await Task.Run(() => _exportadorPlantillaExcelService.ExportarComisiones(
                Comisiones.ToList(), dialogo.FileName));
            Estado = $"Archivo generado: {rutaSalida}";
        }
        catch (Exception exception)
        {
            Estado = $"Error al exportar: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool PuedeCalcular()
    {
        return _facturas.Count > 0 && _pagos.Count > 0 && !IsBusy;
    }

    private bool PuedeExportar()
    {
        return Comisiones.Count > 0 && !IsBusy;
    }

    private void LimpiarResultados()
    {
        Comisiones.Clear();
        TotalCobroNeto = 0m;
        TotalComisiones = 0m;
        ActualizarComandos();
    }

    private void ActualizarComandos()
    {
        _cargarFacturasCommand.NotifyCanExecuteChanged();
        _cargarPagosCommand.NotifyCanExecuteChanged();
        _calcularCommand.NotifyCanExecuteChanged();
        _exportarExcelCommand.NotifyCanExecuteChanged();
    }
}