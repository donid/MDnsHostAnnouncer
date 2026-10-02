using CommandLine;
using CommandLine.Text;

using Makaretu.Dns;
using System.Reflection;

namespace MDnsHostAnnouncer;

internal class Program
{
	private const int cMaxRepeatSeconds = 360_000;

	private static async Task Main(string[] args)
	{
		Console.Error.WriteLine($"[MDnsHostAnnouncer] version {AssemblyVersion()}");

		ParserResult<Options> parserResult = Parser.Default.ParseArguments<Options>(args);
		await parserResult.WithParsedAsync(options => RunAsync(options, parserResult));
	}

	/// <summary>
	/// The <c>Version</c> from the project file, without the build metadata that the
	/// SDK appends after a '+'.
	/// </summary>
	private static string AssemblyVersion()
	{
		string? version = typeof(Program).Assembly
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
			?.InformationalVersion;

		if (string.IsNullOrEmpty(version))
		{
			return "unknown";
		}

		int plus = version.IndexOf('+');
		return plus < 0 ? version : version[..plus];
	}

	private static async Task RunAsync(Options options, ParserResult<Options> parserResult)
	{
		int? repeatSeconds = options.RepeatSeconds;
		if (repeatSeconds < 5 || repeatSeconds > cMaxRepeatSeconds)
		{
			Console.WriteLine($"Repeat interval ({repeatSeconds}) is not valid ( use 5 .. {cMaxRepeatSeconds} seconds )");
			return;
		}

		AliasListBuilder? aliasList;
		if (options.FilePath != null)
		{
			if (options.Alias != null)
			{
				Console.WriteLine("Use either AliasHostName or --file, not both.");
				return;
			}
			aliasList = AliasListBuilder.LoadFromFile(options.FilePath);
		}
		else if (options.Alias != null)
		{
			aliasList = AliasListBuilder.CreateFromArguments(options.Alias, options.IpAddresses);
		}
		else
		{
			Console.Error.WriteLine(HelpText.AutoBuild(parserResult, helpText => helpText, example => example));
			return;
		}

		if (aliasList == null)
		{
			return;
		}

		if (repeatSeconds == null)
		{
			List<ServiceProfile> services = await aliasList.ResolveServiceProfilesAsync(CancellationToken.None);
			ServiceDiscovery sd = new();
			foreach (ServiceProfile service in services)
			{
				sd.Announce(service);
			}
			return;
		}

		await AnnounceRepeatedlyAsync(aliasList, TimeSpan.FromSeconds(repeatSeconds.Value));
	}

	private static async Task AnnounceRepeatedlyAsync(AliasListBuilder aliasList, TimeSpan interval)
	{
		using CancellationTokenSource cts = new();
		ConsoleCancelEventHandler cancelHandler = (sender, e) =>
		{
			// keep the process alive, so the loop can end and release its resources
			e.Cancel = true;
			cts.Cancel();
		};
		Console.CancelKeyPress += cancelHandler;

		try
		{
			using ServiceDiscovery sd = new();
			using PeriodicTimer timer = new(interval);
			Console.WriteLine($"Repeating the announcement every {interval.TotalSeconds} seconds - press Ctrl+C to stop.");
			do
			{
				// resolved on each repetition, because the IP address of a host name may change
				List<ServiceProfile> services = await aliasList.ResolveServiceProfilesAsync(cts.Token);
				foreach (ServiceProfile service in services)
				{
					sd.Announce(service);
				}
				Console.WriteLine($"{DateTime.Now:G} announced");
			}
			while (await timer.WaitForNextTickAsync(cts.Token));
		}
		catch (OperationCanceledException)
		{
			Console.WriteLine("Stopped.");
		}
		finally
		{
			Console.CancelKeyPress -= cancelHandler;
		}
	}
}
