[![.NET](https://github.com/donid/MDnsHostAnnouncer/actions/workflows/dotnet.yml/badge.svg)](https://github.com/donid/MDnsHostAnnouncer/actions/workflows/dotnet.yml)

# MDnsHostAnnouncer

MDnsHostAnnouncer allows you to create alias names for hosts in a local network

This tool is currently only tested with a FritzBox (Firmware 7.29 or 7.62) as DNS server.

I haven't found a way to create an alias (or CNAME) for a network device (PC, notebook etc.)
in the FritzBox web-ui for DNS. But I saw a comment in an Internet forum, where someone
claimed that the FritzBox listens to mDNS (MultiCastDNS) announcements and "integrates" them into its own list of hosts.

Example usage:
If you have a physical machine named 'AlwaysOnSrv' an run the following command on it:

*MDnsHostAnnouncer DatabaseSrv*

You can now use the name 'DatabaseSrv' instead of 'AlwaysOnSrv' in ping commands or
http requests, or anywhere else.

I couldn't find detailed documentation for this behavior, only a statement from AVM that
"the Fritz!Box supports mDNS, but not Bonjour".

According to my experiments the FritzBox will "forget" the "mDNS entries" in
its host list after about 6 hours - newer tests showed that entries can be "lost" after a few minutes!

To keep the alias alive, the announcement can be repeated periodically. The program then
runs until it is stopped with Ctrl+C. The interval is given in seconds (`-r` for short):

*MDnsHostAnnouncer DatabaseSrv --repeat 3600*

Multiple aliases can be defined in a YAML file (`-f` for short), which maps IP addresses
or host names to comma separated alias names:

*MDnsHostAnnouncer --file aliases.yaml --repeat 600*

```yaml
aliases:
  "192.168.1.1": "dbsrv, wikisrv"
  "192.168.1.50": "mailsrv"
  "nas.fritz.box": "backupsrv"
```

A host name is resolved to its IPv4 address(es) via DNS before the alias is announced.
With `--repeat` it is resolved again for each announcement, so the alias follows the host
when its IP address changes (e.g. a DHCP client). If a host name can't be resolved, its
alias is skipped for that announcement.

Note: an alias can be listed for more than one IP address (or host name) in the YAML file,
and for more than one IP address on the command line. This is meant for a single machine
with several addresses, e.g. a notebook connected via Ethernet and Wi-Fi. Don't use it for
different machines, because clients will just pick one of the addresses:

```yaml
aliases:
  "192.168.1.20": "notebook"   # Ethernet
  "192.168.1.21": "notebook"   # Wi-Fi
```

If you want to see what is happening in your network with regards to mDNS you can use this project (part of the library that MDnsHostAnnouncer uses):

<https://github.com/richardschneider/net-mdns/tree/master/Browser>
