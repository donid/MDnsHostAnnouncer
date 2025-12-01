[![.NET](https://github.com/donid/MDnsHostAnnouncer/actions/workflows/dotnet.yml/badge.svg)](https://github.com/donid/MDnsHostAnnouncer/actions/workflows/dotnet.yml)

# MDnsHostAnnouncer

MDnsHostAnnouncer allows you to create alias names for hosts in a local network

This tool is currently only tested with a FritzBox (Firmware 7.29) as DNS server.

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
its host list after about 6 hours.

If you want to see what is happening in your network with regards to mDNS you can use this project (part of the library that MDnsHostAnnouncer uses):

<https://github.com/richardschneider/net-mdns/tree/master/Browser>
