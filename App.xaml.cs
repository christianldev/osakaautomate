using System.Windows;
using osakaautomate.Services;
using osakaautomate.ViewModels;

namespace osakaautomate;

public partial class App : Application
{
	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		var viewModel = new MainViewModel(
			new LectorFacturasCsvService(),
			new LectorPagosCsvService(),
			new LectorHistoricoExcelService(),
			new MotorCalculoService(),
			new ExportadorPlantillaExcelService());
		var mainWindow = new MainWindow
		{
			DataContext = viewModel
		};

		MainWindow = mainWindow;
		mainWindow.Show();
	}
}

