```bash
dotnet build -f net9.0-android
dotnet publish -f net9.0-android -c Release
adb install -r ".\bin\Release\net9.0-android\com.pa5.cppcalculator-Signed.apk"
adb shell am start -n "com.pa5.cppcalculator/crc64faf7e36b039ecd74.MainActivity"
```
