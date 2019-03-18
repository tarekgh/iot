dotnet publish -r linux-arm
pushd .\bin\Debug\netcoreapp2.1\linux-arm\publish
C:\Users\tarekms\Desktop\pscp.exe -pw raspberry -v -r .\* pi@10.127.72.59:/home/pi/RGBMatrix
popd