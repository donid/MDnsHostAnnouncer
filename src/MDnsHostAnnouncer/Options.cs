using CommandLine;
using CommandLine.Text;

namespace MDnsHostAnnouncer;

internal class Options
{
	[Value(0, MetaName = "AliasHostName",
		HelpText = "The alias name to announce (2-63 chars: a-z, A-Z, 0-9 or '-'). Required, unless --file is used.")]
	public string? Alias { get; set; }

	[Value(1, MetaName = "IPAddresses",
		HelpText = "IP addresses for the alias. If omitted, the IP addresses of the current host are used.")]
	public IEnumerable<string> IpAddresses { get; set; } = [];

	[Option('r', "repeat",
		HelpText = "Repeat the announcement every n seconds, until stopped with Ctrl+C.")]
	public int? RepeatSeconds { get; set; }

	[Option('f', "file",
		HelpText = "YAML file that maps IP addresses to comma separated alias names (instead of AliasHostName).")]
	public string? FilePath { get; set; }

	[Usage(ApplicationAlias = "MDnsHostAnnouncer")]
	public static IEnumerable<Example> Examples =>
	[
		new Example("Create an alias for the current IP address of the current host",
			new Options { Alias = "DatabaseSrv" }),
		new Example("Create an alias for the given IP addresses",
			new Options { Alias = "DatabaseSrv", IpAddresses = ["192.168.178.42", "192.168.178.43"] }),
		new Example("Repeat the announcement every hour, until stopped with Ctrl+C",
			new Options { Alias = "DatabaseSrv", RepeatSeconds = 3600 }),
		new Example("Create the aliases defined in a YAML file",
			new Options { FilePath = "aliases.yaml" }),
	];
}
