# MP3/WAV 批量轉 OGG 工具

這是一個 Windows 桌面工具，可批量將 `.mp3` / `.wav` 轉成 `.ogg`。

使用者操作教學請看 [USER_GUIDE.md](USER_GUIDE.md)。

## 功能

- 掃描指定資料夾內的 `.mp3` / `.wav`
- 可勾選要轉換的檔案
- 可套用檔名前綴
- 可依清單順序命名，例如 `sci-fi_001.ogg`
- 可設定起始編號與保留位數
- 可覆蓋既有 `.ogg`
- 轉換成功後，原始音檔會移到 `converted` 資料夾
- 轉換完成後自動重整清單
- 可自動下載 `ffmpeg.exe`
- 可用右上角 `☾ / ☀` 按鈕切換淺色 / 深色模式
- 內建音符圖示

## 開發環境

- Windows
- .NET 9 SDK

## 執行

已產生可直接測試的 exe：

```text
release\音檔轉OGG.exe
```

開發模式執行：

```powershell
dotnet run --project .\OggConverterExe\OggConverterExe.csproj
```

第一次使用如果找不到轉檔程式，請在工具內按「下載轉檔程式」。

## 發佈

產生自帶 runtime 的單一主程式：

```powershell
dotnet publish .\OggConverterExe\OggConverterExe.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o .\publish
```

發佈後若要離線使用，請把 `ffmpeg.exe` 放在：

```text
publish\tools\ffmpeg\bin\ffmpeg.exe
```

或直接在工具內按「下載轉檔程式」。
