```bash
dotnet build -f net9.0-android
dotnet publish -f net9.0-android -c Release
adb install -r ".\bin\Release\net9.0-android\com.pa5.cppcalculator-Signed.apk"
adb shell am start -n "com.pa5.cppcalculator/crc64faf7e36b039ecd74.MainActivity"
```

## C#을 C++로의 변경이 어려워서 우선은 C#으로 제출했습니다. 다시 한 번 검토를 부탁드립니다.
