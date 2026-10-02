using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

using Makaretu.Dns;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using YamlException = YamlDotNet.Core.YamlException;

namespace MDnsHostAnnouncer;

// creates the list of aliases to announce - from the command line arguments or from a YAML file
// validation errors are written to the console and result in null
internal class AliasListBuilder
{
	// alias -> IP addresses or host names (an empty list means: IP addresses of the current host)
	private readonly Dictionary<string, List<string>> _aliases = new(StringComparer.OrdinalIgnoreCase);
	// host name -> last resolved IPv4 addresses, to report only changes
	private readonly Dictionary<string, string> _resolvedHostNames = new(StringComparer.OrdinalIgnoreCase);

	private AliasListBuilder()
	{
	}

	public static AliasListBuilder? CreateFromArguments(string alias, IEnumerable<string> ipArguments)
	{
		if (!IsValidHostName(alias))
		{
			Console.WriteLine($"AliasHostName ({alias}) is not valid ( use 2-63 chars: a-z, A-Z, 0-9 or '-' )");
			return null;
		}

		List<string> targets = [];

		int index = 1;
		foreach (string currentArg in ipArguments)
		{
			IPAddress? iPAddress = ConvertToIpAddress(currentArg);
			if (iPAddress == null)
			{
				Console.WriteLine($"({index}) '{currentArg}' is not a valid IPAddress.");
				return null;
			}
			targets.Add(iPAddress.ToString());
			index++;
		}

		AliasListBuilder builder = new();
		builder._aliases.Add(alias, targets);
		builder.PrintAliases();
		return builder;
	}

	public static AliasListBuilder? LoadFromFile(string filePath)
	{
		AliasFile? aliasFile;
		try
		{
			IDeserializer deserializer = new DeserializerBuilder()
				.WithNamingConvention(CamelCaseNamingConvention.Instance)
				.WithDuplicateKeyChecking()
				.Build();
			using StreamReader reader = new(filePath);
			aliasFile = deserializer.Deserialize<AliasFile?>(reader);
		}
		catch (IOException ex)
		{
			Console.WriteLine($"File '{filePath}' could not be read: {ex.Message}");
			return null;
		}
		catch (UnauthorizedAccessException ex)
		{
			Console.WriteLine($"File '{filePath}' could not be read: {ex.Message}");
			return null;
		}
		catch (YamlException ex)
		{
			Console.WriteLine($"File '{filePath}' is not valid: {ex.Message}");
			return null;
		}

		if (aliasFile?.Aliases == null || aliasFile.Aliases.Count == 0)
		{
			Console.WriteLine($"File '{filePath}' contains no aliases.");
			return null;
		}

		AliasListBuilder builder = new();
		foreach ((string targetText, string? aliasList) in aliasFile.Aliases)
		{
			string target;
			IPAddress? ipAddress = ConvertToIpAddress(targetText);
			if (ipAddress != null)
			{
				target = ipAddress.ToString();
			}
			else if (IsValidTargetHostName(targetText))
			{
				target = targetText;
			}
			else
			{
				Console.WriteLine($"'{targetText}' in file '{filePath}' is neither a valid IPAddress nor a valid host name.");
				return null;
			}

			string[] aliasNames = (aliasList ?? string.Empty).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
			if (aliasNames.Length == 0)
			{
				Console.WriteLine($"'{targetText}' in file '{filePath}' has no alias.");
				return null;
			}

			foreach (string alias in aliasNames)
			{
				if (!IsValidHostName(alias))
				{
					Console.WriteLine($"AliasHostName ({alias}) in file '{filePath}' is not valid ( use 2-63 chars: a-z, A-Z, 0-9 or '-' )");
					return null;
				}

				// an alias may be listed for more than one IP address / host name
				if (!builder._aliases.TryGetValue(alias, out List<string>? targets))
				{
					targets = [];
					builder._aliases.Add(alias, targets);
				}
				// do not add target twice
				if (!targets.Contains(target, StringComparer.OrdinalIgnoreCase))
				{
					targets.Add(target);
				}
			}
		}

		builder.PrintAliases();
		return builder;
	}

	// creates one profile per alias, ready to be announced - host names are resolved to IPv4 addresses on each call
	public async Task<List<ServiceProfile>> ResolveServiceProfilesAsync(CancellationToken cancellationToken)
	{
		List<ServiceProfile> services = [];
		foreach ((string alias, List<string> targets) in _aliases)
		{
			if (targets.Count == 0)
			{
				// when no target is given: use the current host as target
				services.Add(new ServiceProfile("", alias, 0, null));
				continue;
			}

			HashSet<IPAddress> ipAddresses = [];
			foreach (string target in targets)
			{
				// the hostname of a target can resolve to multiple ipv4 addresses
				// add each one as a target-entry
				ipAddresses.UnionWith(await ResolveTargetAsync(target, cancellationToken));
			}

			// an empty list must not be passed on, it would announce the IP addresses of the current host
			if (ipAddresses.Count == 0)
			{
				Console.WriteLine($"Alias '{alias}' is skipped: no IP address available.");
				continue;
			}
			services.Add(new ServiceProfile("", alias, 0, ipAddresses));
		}
		return services;
	}

	private async Task<IPAddress[]> ResolveTargetAsync(string target, CancellationToken cancellationToken)
	{
		IPAddress? ipAddress = ConvertToIpAddress(target);
		if (ipAddress != null)
		{
			return [ipAddress];
		}

		IPAddress[] resolvedAddresses;
		try
		{
			resolvedAddresses = await Dns.GetHostAddressesAsync(target, AddressFamily.InterNetwork, cancellationToken);
		}
		catch (SocketException ex)
		{
			Console.WriteLine($"Host name '{target}' could not be resolved: {ex.Message}");
			return [];
		}

		if (resolvedAddresses.Length == 0)
		{
			Console.WriteLine($"Host name '{target}' has no IPv4 address.");
			return [];
		}

		string resolvedText = string.Join(", ", resolvedAddresses.Select(address => address.ToString()).Order());
		if (!_resolvedHostNames.TryGetValue(target, out string? previousText) || previousText != resolvedText)
		{
			Console.WriteLine($"Host name '{target}' resolved to {resolvedText}");
			_resolvedHostNames[target] = resolvedText;
		}
		return resolvedAddresses;
	}

	private void PrintAliases()
	{
		foreach ((string alias, List<string> targets) in _aliases)
		{
			Console.WriteLine($"Creating alias: '{alias}'");
			if (targets.Any())
			{
				Console.WriteLine($" for IP(s) / host name(s)");
				foreach (string target in targets)
				{
					Console.WriteLine("  " + target);
				}
			}
			else
			{
				Console.WriteLine($" for current host");
			}
		}
	}

	private static IPAddress? ConvertToIpAddress(string currentArg)
	{
		IPAddress.TryParse(currentArg, out IPAddress? result);
		return result;
	}

	// this is a simplified check - not all DNS implementations have the same requirements
	private static bool IsValidHostName(string alias)
	{
		if (Regex.Match(alias, "^[A-Z0-9\\-]*$", RegexOptions.IgnoreCase).Success == false)
		{
			return false;
		}
		if (alias.Length < 2 || alias.Length > 63)
		{
			return false;
		}
		return true;
	}

	// host name (optionally with domain, e.g. 'nas' or 'nas.fritz.box') that is resolved via DNS
	private static bool IsValidTargetHostName(string hostName)
	{
		if (hostName.Length > 253)
		{
			return false;
		}
		if (Regex.Match(hostName, "^[A-Z0-9]([A-Z0-9\\-]{0,61}[A-Z0-9])?(\\.[A-Z0-9]([A-Z0-9\\-]{0,61}[A-Z0-9])?)*$", RegexOptions.IgnoreCase).Success == false)
		{
			return false;
		}
		// a numeric last label is a mistyped IP address like '192.168.1.999', not a host name
		string lastLabel = hostName[(hostName.LastIndexOf('.') + 1)..];
		if (lastLabel.All(char.IsAsciiDigit))
		{
			return false;
		}
		return true;
	}
}
