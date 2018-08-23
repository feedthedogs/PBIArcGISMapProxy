###### Create a new Certificate to impersonate ArcGIS' domains

# This is for testing purposes, you should use a CA to sign a certificate and keep it in the Certificate Manager
# Create a selfsigned certficiate with the list of domains
$cert = New-SelfSignedCertificate -FriendlyName "PBIProxy" -KeyLength 2048 -certstorelocation Cert:\LocalMachine\My -DnsName "lacdn.arcgis.com", "www.arcgis.com","basemaps.arcgis.com","utility.arcgis.com","static.arcgis.com","visuals.azureedge.net"
# Move it to be trusted as a Root CA
Move-Item (Join-Path Cert:\LocalMachine\My $cert.Thumbprint) -Destination Cert:\LocalMachine\Root

# Export the certificate so that the webserver can use it
$mypwd = ConvertTo-SecureString -String "Password01$" -Force -AsPlainText
Get-ChildItem -Path (Join-Path Cert:\LocalMachine\Root $cert.Thumbprint) | Export-PfxCertificate -FilePath ..\PBICert.pfx -Password $mypwd


###### Download Static PowerBI ArcGIS files

# Have the requests look similar to PowerBI
$userAgent = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/64.0.3282.122 Safari/537.36'
$headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]"
$headers.Add('Accept','*/*')
$headers.Add('Referer','https://app.powerbi.com/')
$headers.Add('Origin','ms-pbi://pbi.microsoft.com')

# Get the list of Static files that are not provided by ArcGis offline
$root = (Get-Item -Path ".\").FullName
$urls = Get-Content "$root\Urls.txt"
foreach($url in $urls) {
    # Build the directories to match the paths that PowerBI requests
    $path = $url.Split('/')
    $buildpath = '\'
    for ($i = 3; $i -lt $path.Count - 1; $i++) {
        # Write-Output "$root$($buildpath)$($path[$i])"
        md -Path "$root$($buildpath)$($path[$i])" -ErrorAction SilentlyContinue
        $buildpath += "$($path[$i])\"
    }
    # Querystrings are appended to the end of the filename with an underscore
    $filename = $path[$path.Count - 1].Replace('?','_')
    
    # Get the file and write it to a folder to serve from
    Write-Output "Downloading: $root$($buildpath)$filename"
    Invoke-WebRequest $url -OutFile "$root$($buildpath)$filename" -UserAgent $userAgent -Headers $headers
}