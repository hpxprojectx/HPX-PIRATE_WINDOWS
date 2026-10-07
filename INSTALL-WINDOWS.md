# Cara Install HPX PIRATE di Windows

Panduan ini untuk **Windows 10 (1809+) / Windows 11, 64-bit**.

> HPX PIRATE adalah aplikasi **Windows desktop (.exe)**, bukan APK Android.

> **PENTING (v1.1.0+):** Rilis sekarang **self-contained** — runtime .NET 8 sudah
> dibundel di dalam aplikasi. Jadi **tidak perlu install .NET apa pun**. Cukup jalankan
> aplikasinya. Ini memperbaiki masalah "tidak bisa dibuka di Windows 10".

* * *

## Ringkasan cepat

| Cara | Butuh apa | Cocok untuk |
| --- | --- | --- |
| **A. Installer** (`HPX-PIRATE-Setup.exe`) | Tidak ada (runtime .NET sudah ikut) | Pengguna biasa, paling rapi |
| **B. Portable** (ekstrak `publish`) | Tidak ada (runtime .NET sudah ikut) | Coba cepat / tanpa install |
| **C. Build dari source** | .NET 8 SDK | Developer |

* * *

## Cara A — Pakai Installer (`HPX-PIRATE-Setup.exe`)

Installer ini dihasilkan dari script Inno Setup di repo. Karena file `.exe` installer harus dikompilasi **di Windows**, langkahnya:

### A.1 Build installer (sekali saja, di Windows)

1.  Install **.NET 8 SDK** dan **Inno Setup 6** ([https://jrsoftware.org/isdl.php](https://jrsoftware.org/isdl.php)).
2.  Buka **PowerShell sebagai Administrator** di folder repo, lalu jalankan:
    
    ```powershell
    powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1
    ```
    
    Script ini otomatis membuat build **self-contained** (runtime .NET ikut dibundel).
3.  Hasilnya: **`dist\HPX-PIRATE-Setup.exe`**.

### A.2 Jalankan installer

1.  Klik dua kali **`HPX-PIRATE-Setup.exe`**.
2.  Klik **Yes** pada prompt UAC (aplikasi butuh hak admin untuk restore point & tuning sistem).
3.  Ikuti wizard → **Next** → **Install**. Tidak ada langkah tambahan dan **tidak perlu install .NET**.
4.  Klik **Finish**. Aplikasi bisa dibuka dari **Start Menu** atau **Desktop**.

### A.3 Uninstall

**Settings → Apps → Installed apps → HPX PIRATE → Uninstall** (atau dari Start Menu → folder HPX PIRATE → Uninstall).

> Data pengguna di `%APPDATA%\HPX Pirate` sengaja **tidak** dihapus. Hapus manual kalau perlu.

* * *

## Cara B — Portable (tanpa install)

Cocok kalau kamu hanya punya folder hasil build (`publish/`).

1.  Ekstrak **`HPX-PIRATE-publish-win-x64.zip`** ke folder pilihanmu, misalnya `C:\HPX-PIRATE\`. Pastikan \*\*semua\*\* file ikut terekstrak (termasuk folder `amd64\` dan file-file `.dll`).
    -   **Tidak perlu install .NET** — runtime sudah ada di dalam folder ini.
2.  Klik kanan **`HPXPirate.exe`** → **Run as administrator**. (Bisa juga klik dua kali; akan muncul prompt UAC.)
3.  Aplikasi langsung terbuka. Tidak ada proses install.

> ⚠️ Jangan memindahkan `HPXPirate.exe` sendirian — ia butuh file `.dll` di folder yang sama. Pindahkan **seluruh folder**.
> 
> 💡 Agar tidak selalu muncul prompt UAC: klik kanan `HPXPirate.exe` → **Properties** → tab **Compatibility** → centang **Run this program as an administrator** → **OK**.

### Membuat shortcut manual

Klik kanan `HPXPirate.exe` → **Send to** → **Desktop (create shortcut)**.

* * *

## Cara C — Build dari source (developer)

Di Windows dengan **.NET 8 SDK** terpasang:

```powershell
# 1) build
dotnet build src\HPXPirate\HPXPirate.csproj -c Release

# 2) jalankan
dotnet run --project src\HPXPirate\HPXPirate.csproj -c Release

# 3) buat folder siap-distribusi (self-contained, tanpa perlu .NET di target)
dotnet publish src\HPXPirate\HPXPirate.csproj -c Release -r win-x64 --self-contained true -o publish
```

Ingin versi **single-file** (satu file .exe besar)?

```powershell
dotnet publish src\HPXPirate\HPXPirate.csproj -c Release -r win-x64 ^
  --self-contained true -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true -o publish-single
```

* * *

## Pertama kali dibuka

1.  Terima prompt **UAC** (Yes).
2.  **Performance** → pilih tweak → **Apply**.
    -   Sebuah **System Restore Point** dibuat otomatis **hanya jika** ada yang benar-benar perlu diubah.
    -   Setiap perubahan bisa di-**UNDO**.
    -   Kalau sistemmu sudah optimal, aplikasi akan bilang **"Already optimal"** dan **tidak mengubah apa pun**.
3.  **Monitor** → **START** untuk melihat FPS/frame time (jalankan sambil main game).
4.  **FPS Boost** → pilih target FPS → **Boost** untuk tweak FPS nyata + lihat hasil sebelum/sesudah.
5.  **Mouse Tuning** / **Keyboard Tuning** → atur sensitivitas mouse & repeat rate keyboard.
6.  **Profiles** → simpan profil per-game.
7.  **Settings** → aktifkan **Start with Windows** / **Minimize to tray** kalau mau.

* * *

## Troubleshooting

| Masalah | Solusi |
| --- | --- |
| Aplikasi tidak mau dibuka di Windows 10 | Pakai versi **self-contained** (v1.1.0+). Kalau masih pakai build lama (framework-dependent), install **.NET 8 Desktop Runtime (x64)** dari [https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0). |
| Muncul pesan butuh **.NET** | Kamu memakai build lama. Pakai installer/paket **v1.1.0+** yang sudah self-contained. |
| Aplikasi tidak mau dibuka / langsung tertutup | Jalankan **Run as administrator**; cek log di `%APPDATA%\HPX Pirate\logs\`. |
| Windows SmartScreen memblokir | Klik **More info** → **Run anyway** (app belum ditandatangani digital). |
| FPS tidak muncul | Tekan **START** di Monitor **saat game sedang berjalan**; sebagian game ber-anti-cheat memblokir ETW. |
| Suhu/load CPU-GPU "n/a" | Jalankan sebagai admin; sensor tertentu mungkin butuh driver/versi lebih baru. |
| Optimizer bilang "Already optimal" | Itu benar — tidak ada yang perlu diubah, jadi tidak ada yang diubah. |
| Ingin balikin semua perubahan | **Optimizer → UNDO ALL**, **FPS Boost → Roll back all**, atau pakai System Restore Windows. |

* * *

## Lokasi data & log

```
%APPDATA%\HPX Pirate\
├── settings.json
├── fpsboost.json
├── logs\
├── profiles\        (dan profiles\backups\)
└── history\
```

Buka cepat: tekan **Win + R**, ketik `%APPDATA%\HPX Pirate`, Enter.
