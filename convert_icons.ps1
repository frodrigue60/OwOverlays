Add-Type -AssemblyName System.Drawing

# Convert app icon
$bmpApp = [System.Drawing.Bitmap]::new("c:\tmp\OwOverlays\app_icon.png")
$iconApp = [System.Drawing.Icon]::FromHandle($bmpApp.GetHicon())
$fsApp = [System.IO.FileStream]::new("c:\tmp\OwOverlays\app_icon.ico", [System.IO.FileMode]::Create)
$iconApp.Save($fsApp)
$fsApp.Close()
$bmpApp.Dispose()
Write-Host "Created app_icon.ico"

# Convert tray icon
$bmpTray = [System.Drawing.Bitmap]::new("c:\tmp\OwOverlays\tray_icon.png")
$iconTray = [System.Drawing.Icon]::FromHandle($bmpTray.GetHicon())
$fsTray = [System.IO.FileStream]::new("c:\tmp\OwOverlays\tray_icon.ico", [System.IO.FileMode]::Create)
$iconTray.Save($fsTray)
$fsTray.Close()
$bmpTray.Dispose()
Write-Host "Created tray_icon.ico"
