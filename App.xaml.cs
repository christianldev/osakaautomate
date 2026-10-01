using System.Windows;
using Microsoft.EntityFrameworkCore;
using osakaautomate.Data;
using osakaautomate.Services;
using osakaautomate.ViewModels;

namespace osakaautomate;

public partial class App : Application
{
	private AppDbContext? _dbContext;

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseSqlite("Data Source=comisiones.db")
			.Options;
		_dbContext = new AppDbContext(options);
		_dbContext.Database.EnsureCreated();

		var endpoint = Environment.GetEnvironmentVariable("AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT");
		var apiKey = Environment.GetEnvironmentVariable("AZURE_DOCUMENT_INTELLIGENCE_KEY");
		IOCRService ocrService = !string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(apiKey)
			? new AzureDocumentIntelligenceOcrService(endpoint, apiKey)
			: new OcrNoConfiguradoService();

		var viewModel = new MainViewModel(
			new LectorPagosExcelService(),
			ocrService,
			new MotorCalculoService(_dbContext),
			new ExportadorPlantillaExcelService());
		var mainWindow = new MainWindow
		{
			DataContext = viewModel
		};

		MainWindow = mainWindow;
		mainWindow.Show();
	}

	protected override void OnExit(ExitEventArgs e)
	{
		_dbContext?.Dispose();
		base.OnExit(e);
	}
}

