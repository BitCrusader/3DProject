using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using Avalonia.Threading;

namespace Engine.Frontend
{
	public partial class App : Application
	{
		public static App Instance;

		public static void Main() => Run();

		public static void Run()
		{
			Win32PlatformOptions opts = new()
			{
				AllowEglInitialization = false,
				UseWgl = true,
			};

			AppBuilder.Configure<App>().UseWin32().With(opts).UseSkia().UseReactiveUI().StartWithClassicDesktopLifetime(new string[0]);
			Game.Cleanup();
		}

		public override void Initialize()
		{
			AvaloniaXamlLoader.Load(this);
			Instance = this;
		}
		
		public override void OnFrameworkInitializationCompleted()
		{
			var desktopLifetime = ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
			desktopLifetime.ShutdownMode = ShutdownMode.OnMainWindowClose;

			// Show splash screen.
			SplashScreen splash = new SplashScreen();
			desktopLifetime.MainWindow = splash;

			// Setup engine.
			Task startupTask = Task.Run(Game.Init);
			startupTask = startupTask.ContinueWith(t =>
			{
				// Make sure setup went okay.
				if (!t.IsFaulted)
				{
					Dispatcher.UIThread.Post(() =>
					{
						// Now that setup's complete, open the main window.
						desktopLifetime.MainWindow = new MainWindow();
						desktopLifetime.MainWindow.Show();

						// Close the splash screen, because we're done with that too.
						splash.Close();
					});
				}
				else
				{
					FrontendHelpers.InvokeHandled(() => t.Wait());
				}
			});
		}

		public void Shutdown()
		{
			(ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).TryShutdown();
		}
	}
}