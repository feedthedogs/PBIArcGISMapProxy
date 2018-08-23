# PBIArcGISMapProxy
Proxies PowerBI maps to an onprem or offline ArcGIS instance

Note: this only works on the normal version of PowerBI, the ReportServer version has maps disabled in hardcoded configuration


Required on the Server:


1. Install .NET Core 2.1

https://www.microsoft.com/net/download/dotnet-core/2.1

2. Build a trusted Certificate for arcgis.com etc and download static PowerBI ArcGIS files to be served to the clients:

Open the powershell file PBIStaticFiles\PrepareFilesAndCertificate.ps1

and run it manually so you understand what it is doing and adjust where appropiate for you

3. Edit the .config file pointing to your own GeoCodeServer and VectorTileServer

4. Compile and Run the Proxy Server


Required on the Client:

1. Start the PowerBI client online once so it can download the ArgGIS icon and resources

The files can then be copied offline from C:\Users\<user>\AppData\Local\Microsoft\Power BI Desktop\CEF

2. Add the domains to the hosts file (127.0.0.1 is local system or where the proxy is to be hosted):

C:\Windows\System32\drivers\etc\hosts (open as administrator)

	127.0.0.1       lacdn.arcgis.com
	
	127.0.0.1       www.arcgis.com
	
	127.0.0.1       basemaps.arcgis.com
	
	127.0.0.1       utility.arcgis.com
	
	127.0.0.1       static.arcgis.com
	
	127.0.0.1       visuals.azureedge.net
