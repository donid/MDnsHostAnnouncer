namespace MDnsHostAnnouncer;

// content of the YAML file given with --file, e.g.:
// aliases:
//   "192.168.1.1": "dbsrv, wikisrv"
//   "192.168.1.50": "mailsrv"
internal class AliasFile
{
	// IP address -> comma separated list of alias names
	public Dictionary<string, string?>? Aliases { get; set; }
}
