# Android Release (imzalı AAB)

Anahtar ve şifreler **asla** repoya girmez; ortam değişkenlerinden okunur. `MYG_KEYSTORE` tanımsızsa Release build imzasız devam eder.

1. **Keystore üret** (bir kez):
   ```
   keytool -genkeypair -v -keystore myg-rider.keystore -alias myg-rider -keyalg RSA -keysize 2048 -validity 10000
   ```
   Keystore'u repo dışında sakla ve yedekle. Kaybı = mağazada güncelleme yapamamak.

2. **Ortam değişkenlerini ayarla** (PowerShell):
   ```powershell
   $env:MYG_KEYSTORE   = "C:\yol\myg-rider.keystore"
   $env:MYG_KEY_ALIAS  = "myg-rider"
   $env:MYG_STORE_PASS = "..."
   $env:MYG_KEY_PASS   = "..."
   ```

3. **Yayınla**:
   ```
   dotnet publish src/MYG.RideR.Mobile/MYG.RideR.Mobile.csproj -f net10.0-android -c Release
   ```

4. **Çıktı**: `src/MYG.RideR.Mobile/bin/Release/net10.0-android/publish/*.aab`

5. **Sürüm artır** (her yayında): `MYG.RideR.Mobile.csproj` içinde `ApplicationDisplayVersion` (görünen sürüm) ve `ApplicationVersion` (tamsayı, her yüklemede artmalı).
