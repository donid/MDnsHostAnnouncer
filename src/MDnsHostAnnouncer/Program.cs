using System.Net;
using System.Text.RegularExpressions;

using Makaretu.Dns;

namespace MDnsHostAnnouncer
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			if (args.Length < 1)
			{
				Console.WriteLine("Usage:");
				Console.WriteLine("> MDnsHostAnnouncer AliasHostName");
				Console.WriteLine("Creates an alias 'AliasHostName' for the current IPAddress of the current host.");
				Console.WriteLine("> MDnsHostAnnouncer AliasHostName 192.168.178.42 192.168.178.43");
				Console.WriteLine("Creates an alias 'AliasHostName' for the given IPAddresses.");
				return;
			}

			string alias = args[0];
			if (!IsValidHostName(alias))
			{
				Console.WriteLine($"AliasHostName ({alias}) is not valid ( use 2-63 chars: a-z, A-Z, 0-9 or '-' )");
				return;
			}

			List<IPAddress> ipAddresses = new();

			if (args.Length > 1)
			{
				for (int index = 1; index < args.Length; index++)
				{
					string currentArg = args[index];
					IPAddress? iPAddress = ConvertToIpAddress(currentArg);
					if (iPAddress == null)
					{
						Console.WriteLine($"({index}) '{currentArg}' is not a valid IPAddress.");
						return;
					}
					ipAddresses.Add(iPAddress);
				}
			}

			Console.WriteLine($"Creating alias: '{alias}'");
			if (ipAddresses.Any())
			{
				Console.WriteLine($" for IP(s)");
				foreach (IPAddress iPAddress in ipAddresses)
				{
					Console.WriteLine("  " + iPAddress.ToString());
				}
			}
			else
			{
				Console.WriteLine($" for current host");
			}

			List<IPAddress>? nullableIpAddresses = ipAddresses.Any() ? ipAddresses : null;
			var service = new ServiceProfile("", alias, 0, nullableIpAddresses);
			var sd = new ServiceDiscovery();
			sd.Announce(service);
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
	}
}