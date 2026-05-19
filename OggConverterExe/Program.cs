using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;

namespace OggConverterExe;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm : Form
{
    private readonly TextBox _inputDirText = new();
    private readonly TextBox _outputDirText = new();
    private readonly TextBox _ffmpegText = new();
    private readonly TextBox _prefixText = new();
    private readonly TextBox _sequenceStartText = new();
    private readonly CheckBox _overwriteCheck = new();
    private readonly CheckBox _sequenceCheck = new();
    private readonly DataGridView _grid = new();
    private readonly ProgressBar _progressBar = new();
    private readonly Label _statusLabel = new();
    private readonly Label _countLabel = new();
    private readonly TextBox _logBox = new();
    private readonly Button _startButton;
    private readonly Button _scanButton;
    private readonly Button _applyNameButton;
    private readonly Button _downloadButton;
    private readonly Button _themeButton;
    private readonly List<Button> _buttons = [];
    private readonly List<Button> _secondaryButtons = [];
    private readonly List<Label> _labels = [];
    private readonly List<TextBox> _textBoxes = [];
    private readonly List<Panel> _cards = [];
    private readonly List<Control> _appBackControls = [];

    private readonly string _appDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    private bool _isRenaming;
    private bool _darkMode;

    public MainForm()
    {
        Text = "MP3/WAV 批量轉 OGG";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? Icon;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1120, 820);
        Size = new Size(1220, 860);
        Font = new Font("Microsoft JhengHei UI", 9F);
        BackColor = Theme.AppBack;

        _startButton = CreateButton("開始轉換", Theme.Accent, Color.White);
        _scanButton = CreateButton("重新掃描", Theme.ButtonBack, Theme.Text);
        _applyNameButton = CreateButton("套用命名", Theme.ButtonBack, Theme.Text);
        _downloadButton = CreateButton("下載轉檔程式", Theme.ButtonBack, Theme.Text);
        _themeButton = CreateButton("☾", Theme.ButtonBack, Theme.Text);

        BuildLayout();
        WireEvents();

        _inputDirText.Text = _appDir;
        _outputDirText.Text = Path.Combine(_appDir, "ogg");
        _sequenceStartText.Text = "001";
        ApplyTheme();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        TryAutoResolveFfmpeg();
        RefreshFileList();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Theme.AppBack
        };
        _appBackControls.Add(root);
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 192));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 138));
        Controls.Add(root);

        var titlePanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.AppBack };
        _appBackControls.Add(titlePanel);
        var title = new Label
        {
            Text = "音檔批量轉 OGG",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 19F, FontStyle.Bold),
            ForeColor = Theme.Text,
            Location = new Point(0, 4)
        };
        _labels.Add(title);
        var subtitle = new Label
        {
            Text = "支援 MP3 / WAV，成功轉換後自動整理原始檔到 converted 資料夾",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 9.5F),
            ForeColor = Theme.Muted,
            Location = new Point(2, 42)
        };
        _labels.Add(subtitle);
        _themeButton.Size = new Size(44, 34);
        _themeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _themeButton.Location = new Point(titlePanel.Width - _themeButton.Width, 8);
        _themeButton.Font = new Font(Font.FontFamily, 13F, FontStyle.Bold);
        titlePanel.Resize += (_, _) => _themeButton.Location = new Point(titlePanel.Width - _themeButton.Width, 8);
        titlePanel.Controls.Add(title);
        titlePanel.Controls.Add(subtitle);
        titlePanel.Controls.Add(_themeButton);
        root.Controls.Add(titlePanel, 0, 0);

        var settings = CreateCard();
        settings.Padding = new Padding(16, 14, 16, 14);
        var settingsGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 4,
            BackColor = Theme.CardBack
        };
        settingsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        settingsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        settingsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        settingsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        settingsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        settingsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 144));
        settingsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        settingsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        settingsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        settingsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        settings.Controls.Add(settingsGrid);
        root.Controls.Add(settings, 0, 1);

        AddPathRow(settingsGrid, 0, "輸入資料夾", _inputDirText, () => BrowseFolder(_inputDirText));
        AddPathRow(settingsGrid, 1, "輸出資料夾", _outputDirText, () => BrowseFolder(_outputDirText));
        AddPathRow(settingsGrid, 2, "轉檔程式", _ffmpegText, BrowseFfmpeg);

        settingsGrid.Controls.Add(CreateLabel("檔名前綴"), 0, 3);
        settingsGrid.Controls.Add(StyleTextBox(_prefixText), 1, 3);
        settingsGrid.Controls.Add(_applyNameButton, 2, 3);
        settingsGrid.Controls.Add(_overwriteCheck, 3, 3);
        settingsGrid.Controls.Add(_downloadButton, 4, 3);
        settingsGrid.Controls.Add(_startButton, 5, 3);

        _overwriteCheck.Text = "覆蓋舊檔";
        _overwriteCheck.ForeColor = Theme.Text;
        _overwriteCheck.BackColor = Theme.CardBack;
        _overwriteCheck.Dock = DockStyle.Fill;
        _overwriteCheck.TextAlign = ContentAlignment.MiddleLeft;

        var gridCard = CreateCard();
        gridCard.Padding = new Padding(1);
        ConfigureGrid();
        gridCard.Controls.Add(_grid);
        root.Controls.Add(gridCard, 0, 2);

        var actionRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            BackColor = Theme.AppBack
        };
        _appBackControls.Add(actionRow);
        actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
        actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
        actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        actionRow.Controls.Add(_sequenceCheck, 0, 0);
        actionRow.Controls.Add(CreateLabel("起始編號"), 1, 0);
        actionRow.Controls.Add(StyleTextBox(_sequenceStartText), 2, 0);
        actionRow.Controls.Add(_scanButton, 4, 0);
        actionRow.Controls.Add(_countLabel, 5, 0);
        root.Controls.Add(actionRow, 0, 3);

        _sequenceCheck.Text = "按照順序命名";
        _sequenceCheck.ForeColor = Theme.Text;
        _sequenceCheck.BackColor = Theme.AppBack;
        _sequenceCheck.Dock = DockStyle.Fill;
        _sequenceCheck.TextAlign = ContentAlignment.MiddleLeft;

        _countLabel.Text = "檔案：0";
        _countLabel.Dock = DockStyle.Fill;
        _countLabel.TextAlign = ContentAlignment.MiddleRight;
        _countLabel.ForeColor = Theme.Muted;

        var logCard = CreateCard();
        logCard.Padding = new Padding(12, 10, 12, 10);
        var logLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Theme.CardBack };
        logLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        logLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        logLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _statusLabel.Text = "待命";
        _statusLabel.ForeColor = Theme.Muted;
        _statusLabel.Dock = DockStyle.Fill;
        _progressBar.Dock = DockStyle.Fill;
        _logBox.Dock = DockStyle.Fill;
        _logBox.Multiline = true;
        _logBox.ReadOnly = true;
        _logBox.BorderStyle = BorderStyle.None;
        _logBox.ScrollBars = ScrollBars.Vertical;
        _logBox.BackColor = Theme.CardBack;
        _logBox.ForeColor = Theme.Text;
        _logBox.Font = new Font("Cascadia Mono", 9F);
        logLayout.Controls.Add(_statusLabel, 0, 0);
        logLayout.Controls.Add(_progressBar, 0, 1);
        logLayout.Controls.Add(_logBox, 0, 2);
        logCard.Controls.Add(logLayout);
        root.Controls.Add(logCard, 0, 4);
    }

    private void AddPathRow(TableLayoutPanel parent, int row, string label, TextBox textBox, Action browse)
    {
        parent.Controls.Add(CreateLabel(label), 0, row);
        var styled = StyleTextBox(textBox);
        parent.Controls.Add(styled, 1, row);
        parent.SetColumnSpan(styled, 4);
        parent.Controls.Add(CreateButton("瀏覽...", Theme.ButtonBack, Theme.Text, browse), 5, row);
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.BackgroundColor = Theme.CardBack;
        _grid.BorderStyle = BorderStyle.None;
        _grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        _grid.EnableHeadersVisualStyles = false;
        _grid.GridColor = Theme.Line;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = true;
        _grid.RowTemplate.Height = 34;
        _grid.DefaultCellStyle.BackColor = Theme.CardBack;
        _grid.DefaultCellStyle.ForeColor = Theme.Text;
        _grid.DefaultCellStyle.SelectionBackColor = Theme.Selection;
        _grid.DefaultCellStyle.SelectionForeColor = Theme.Text;
        _grid.DefaultCellStyle.Font = new Font(Font.FontFamily, 9.5F);
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.Header;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.Text;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold);
        _grid.ColumnHeadersHeight = 38;

        _grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Convert", HeaderText = "轉換", FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SourceName", HeaderText = "原始檔案", ReadOnly = true, FillWeight = 45 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "OutputName", HeaderText = "輸出 .ogg 檔名", FillWeight = 45 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SourcePath", Visible = false });

        _grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_grid.IsCurrentCellDirty)
            {
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };
        _grid.CellValueChanged += (_, e) =>
        {
            if (!_isRenaming &&
                e.RowIndex >= 0 &&
                e.ColumnIndex == _grid.Columns["Convert"]!.Index &&
                _sequenceCheck.Checked)
            {
                ApplyNaming();
            }
        };
    }

    private void WireEvents()
    {
        _scanButton.Click += (_, _) => RefreshFileList();
        _startButton.Click += async (_, _) => await StartConversionAsync();
        _applyNameButton.Click += (_, _) => ApplyNaming();
        _downloadButton.Click += async (_, _) => await DownloadFfmpegAsync();
        _themeButton.Click += (_, _) => ToggleTheme();
        _sequenceCheck.CheckedChanged += (_, _) => ApplyNaming();
        _sequenceStartText.TextChanged += (_, _) =>
        {
            if (_sequenceCheck.Checked)
            {
                TryApplyNaming();
            }
        };
    }

    private void TryAutoResolveFfmpeg()
    {
        var ffmpeg = ResolveFfmpeg();
        if (ffmpeg is null)
        {
            AddLog("找不到轉檔程式。請按「下載轉檔程式」或手動選擇 ffmpeg.exe。");
            return;
        }

        _ffmpegText.Text = ffmpeg;
        AddLog($"已找到轉檔程式：{ffmpeg}");
    }

    private void RefreshFileList()
    {
        _grid.Rows.Clear();
        if (!Directory.Exists(_inputDirText.Text))
        {
            AddLog("輸入資料夾不存在。");
            UpdateCount();
            return;
        }

        var files = Directory.EnumerateFiles(_inputDirText.Text)
            .Where(path => string.Equals(Path.GetExtension(path), ".mp3", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(Path.GetExtension(path), ".wav", StringComparison.OrdinalIgnoreCase))
            .OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        foreach (var file in files)
        {
            var sourceName = Path.GetFileName(file);
            var outputName = $"{_prefixText.Text}{Path.GetFileNameWithoutExtension(file)}.ogg";
            _grid.Rows.Add(true, sourceName, outputName, file);
        }

        if (_sequenceCheck.Checked)
        {
            ApplyNaming();
        }

        UpdateCount();
        AddLog($"已載入 {files.Count} 個音訊檔。");
    }

    private void ApplyNaming()
    {
        try
        {
            _isRenaming = true;
            if (_sequenceCheck.Checked)
            {
                var sequence = GetSequenceStart();
                var number = sequence.Number;
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    if (row.IsNewRow || !IsChecked(row))
                    {
                        continue;
                    }

                    row.Cells["OutputName"].Value = CreateSequentialName(_prefixText.Text, number, sequence.Padding);
                    number++;
                }

                AddLog("已套用順序命名。");
                return;
            }

            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.IsNewRow || !IsChecked(row))
                {
                    continue;
                }

                var sourceName = Convert.ToString(row.Cells["SourceName"].Value) ?? "";
                row.Cells["OutputName"].Value = $"{_prefixText.Text}{Path.GetFileNameWithoutExtension(sourceName)}.ogg";
            }

            AddLog("已套用檔名前綴。");
        }
        finally
        {
            _isRenaming = false;
        }
    }

    private void TryApplyNaming()
    {
        try
        {
            ApplyNaming();
        }
        catch (Exception ex)
        {
            AddLog(ex.Message);
        }
    }

    private async Task StartConversionAsync()
    {
        try
        {
            SetUiEnabled(false);
            var ffmpeg = ResolveFfmpeg(_ffmpegText.Text) ?? throw new InvalidOperationException("找不到轉檔程式。");
            var outputDir = _outputDirText.Text.Trim();
            if (string.IsNullOrWhiteSpace(outputDir))
            {
                throw new InvalidOperationException("請選擇輸出資料夾。");
            }

            Directory.CreateDirectory(outputDir);
            if (_sequenceCheck.Checked)
            {
                ApplyNaming();
            }

            var rows = _grid.Rows.Cast<DataGridViewRow>()
                .Where(row => !row.IsNewRow && IsChecked(row))
                .ToList();
            if (rows.Count == 0)
            {
                throw new InvalidOperationException("沒有勾選要轉換的檔案。");
            }

            _progressBar.Minimum = 0;
            _progressBar.Maximum = rows.Count;
            _progressBar.Value = 0;
            _statusLabel.Text = "轉換中...";

            var success = 0;
            var skipped = 0;
            var failed = 0;

            foreach (var row in rows)
            {
                var sourcePath = Convert.ToString(row.Cells["SourcePath"].Value) ?? "";
                var sourceName = Convert.ToString(row.Cells["SourceName"].Value) ?? "";
                var outputName = NormalizeOggName(Convert.ToString(row.Cells["OutputName"].Value) ?? "");
                if (string.IsNullOrWhiteSpace(outputName))
                {
                    skipped++;
                    AddLog($"略過 {sourceName}：輸出檔名是空的。");
                    StepProgress();
                    continue;
                }

                var outputPath = Path.Combine(outputDir, outputName);
                if (File.Exists(outputPath) && !_overwriteCheck.Checked)
                {
                    skipped++;
                    AddLog($"略過已存在檔案：{outputName}");
                    StepProgress();
                    continue;
                }

                AddLog($"轉換：{sourceName} -> {outputName}");
                var exitCode = await RunFfmpegAsync(ffmpeg, sourcePath, outputPath, _overwriteCheck.Checked);
                if (exitCode == 0)
                {
                    success++;
                    TryMoveConvertedSource(sourcePath);
                }
                else
                {
                    failed++;
                    AddLog($"失敗：{sourceName}");
                }

                StepProgress();
            }

            _statusLabel.Text = "完成";
            AddLog($"完成。成功：{success}，略過：{skipped}，失敗：{failed}。");
            RefreshFileList();
            _statusLabel.Text = "完成";
            MessageBox.Show($"完成。\n成功：{success}\n略過：{skipped}\n失敗：{failed}", "MP3/WAV 轉 OGG", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "錯誤";
            AddLog(ex.Message);
            MessageBox.Show(ex.Message, "MP3/WAV 轉 OGG", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetUiEnabled(true);
        }
    }

    private async Task<int> RunFfmpegAsync(string ffmpeg, string sourcePath, string outputPath, bool overwrite)
    {
        var writeMode = overwrite ? "-y" : "-n";
        var args = $"-hide_banner -loglevel error {writeMode} -i \"{sourcePath}\" -map 0:a:0 -vn -c:a libvorbis -q:a 5 \"{outputPath}\"";
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ffmpeg,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            }
        };

        process.Start();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var error = await errorTask;
        if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
        {
            AddLog(error.Trim());
        }

        return process.ExitCode;
    }

    private async Task DownloadFfmpegAsync()
    {
        try
        {
            SetUiEnabled(false);
            _statusLabel.Text = "下載中...";
            AddLog("正在取得 ffmpeg 下載資訊...");

            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("VFX-tool-ogg-converter");
            var json = await http.GetStringAsync("https://api.github.com/repos/BtbN/FFmpeg-Builds/releases/latest");
            using var doc = JsonDocument.Parse(json);
            var asset = doc.RootElement.GetProperty("assets").EnumerateArray()
                .Select(item => new
                {
                    Name = item.GetProperty("name").GetString() ?? "",
                    Url = item.GetProperty("browser_download_url").GetString() ?? ""
                })
                .FirstOrDefault(item =>
                    item.Name.Contains("win64", StringComparison.OrdinalIgnoreCase) &&
                    item.Name.Contains("gpl", StringComparison.OrdinalIgnoreCase) &&
                    item.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                    !item.Name.Contains("shared", StringComparison.OrdinalIgnoreCase));

            if (asset is null)
            {
                throw new InvalidOperationException("找不到可下載的 Windows 版 ffmpeg。");
            }

            var tempRoot = Path.Combine(Path.GetTempPath(), "vfx_ffmpeg_" + Guid.NewGuid().ToString("N"));
            var zipPath = Path.Combine(tempRoot, "ffmpeg.zip");
            var extractDir = Path.Combine(tempRoot, "extract");
            Directory.CreateDirectory(tempRoot);
            try
            {
                AddLog($"正在下載：{asset.Name}");
                await DownloadFileWithProgressAsync(http, asset.Url, zipPath);

                AddLog("正在解壓縮...");
                _statusLabel.Text = "解壓縮中...";
                _progressBar.Style = ProgressBarStyle.Marquee;
                ZipFile.ExtractToDirectory(zipPath, extractDir, true);
                var ffmpeg = Directory.EnumerateFiles(extractDir, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault()
                    ?? throw new InvalidOperationException("下載檔案中找不到 ffmpeg.exe。");

                _statusLabel.Text = "安裝中...";
                var binDir = Path.Combine(_appDir, "tools", "ffmpeg", "bin");
                Directory.CreateDirectory(binDir);
                var targetFfmpeg = Path.Combine(binDir, "ffmpeg.exe");
                File.Copy(ffmpeg, targetFfmpeg, true);
                _ffmpegText.Text = targetFfmpeg;
                AddLog($"轉檔程式已準備完成：{targetFfmpeg}");
                _progressBar.Style = ProgressBarStyle.Blocks;
                _progressBar.Minimum = 0;
                _progressBar.Maximum = 100;
                _progressBar.Value = 100;
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, true);
                }
            }
        }
        catch (Exception ex)
        {
            AddLog(ex.Message);
            MessageBox.Show(ex.Message, "下載轉檔程式", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _progressBar.Style = ProgressBarStyle.Blocks;
            _statusLabel.Text = "待命";
            SetUiEnabled(true);
        }
    }

    private async Task DownloadFileWithProgressAsync(HttpClient http, string url, string destinationPath)
    {
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        await using var source = await response.Content.ReadAsStreamAsync();
        await using var target = File.Create(destinationPath);

        if (totalBytes is null or <= 0)
        {
            _progressBar.Style = ProgressBarStyle.Marquee;
            _statusLabel.Text = "下載中...";
            await source.CopyToAsync(target);
            return;
        }

        _progressBar.Style = ProgressBarStyle.Blocks;
        _progressBar.Minimum = 0;
        _progressBar.Maximum = 100;
        _progressBar.Value = 0;

        var buffer = new byte[1024 * 128];
        long downloaded = 0;
        var lastPercent = -1;

        while (true)
        {
            var read = await source.ReadAsync(buffer);
            if (read == 0)
            {
                break;
            }

            await target.WriteAsync(buffer.AsMemory(0, read));
            downloaded += read;

            var percent = (int)Math.Clamp(downloaded * 100 / totalBytes.Value, 0, 100);
            if (percent == lastPercent)
            {
                continue;
            }

            lastPercent = percent;
            _progressBar.Value = percent;
            _statusLabel.Text = $"下載中... {percent}%";
            Application.DoEvents();
        }

        _progressBar.Value = 100;
        _statusLabel.Text = "下載完成";
    }

    private string? ResolveFfmpeg(string? explicitPath = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath) && File.Exists(explicitPath))
        {
            return Path.GetFullPath(explicitPath);
        }

        var candidates = new[]
        {
            Path.Combine(_appDir, "ffmpeg.exe"),
            Path.Combine(_appDir, "ffmpeg", "bin", "ffmpeg.exe"),
            Path.Combine(_appDir, "tools", "ffmpeg.exe"),
            Path.Combine(_appDir, "tools", "ffmpeg", "bin", "ffmpeg.exe")
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private void TryMoveConvertedSource(string sourcePath)
    {
        try
        {
            var sourceDir = Path.GetDirectoryName(sourcePath);
            if (string.IsNullOrWhiteSpace(sourceDir) || !File.Exists(sourcePath))
            {
                return;
            }

            var convertedDir = Path.Combine(sourceDir, "converted");
            Directory.CreateDirectory(convertedDir);
            var target = GetUniquePath(Path.Combine(convertedDir, Path.GetFileName(sourcePath)));
            File.Move(sourcePath, target);
            AddLog($"已移動原始檔到 converted：{Path.GetFileName(target)}");
        }
        catch (Exception ex)
        {
            AddLog($"原始檔移動失敗：{ex.Message}");
        }
    }

    private static string GetUniquePath(string path)
    {
        if (!File.Exists(path))
        {
            return path;
        }

        var folder = Path.GetDirectoryName(path) ?? "";
        var name = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        var index = 1;
        string candidate;
        do
        {
            candidate = Path.Combine(folder, $"{name}_{index}{extension}");
            index++;
        } while (File.Exists(candidate));

        return candidate;
    }

    private (long Number, int Padding) GetSequenceStart()
    {
        var value = string.IsNullOrWhiteSpace(_sequenceStartText.Text) ? "1" : _sequenceStartText.Text.Trim();
        if (!value.All(char.IsDigit))
        {
            throw new InvalidOperationException("起始編號只能輸入數字，例如 001。");
        }

        return (long.Parse(value), value.Length);
    }

    private static string CreateSequentialName(string prefix, long number, int padding)
    {
        var separator = string.IsNullOrWhiteSpace(prefix) || prefix.EndsWith('_') ? "" : "_";
        return $"{prefix}{separator}{number.ToString($"D{padding}")}.ogg";
    }

    private static string NormalizeOggName(string name)
    {
        name = name.Trim();
        if (name.Length == 0)
        {
            return "";
        }

        return Path.GetExtension(name).Length == 0 ? $"{name}.ogg" : name;
    }

    private static bool IsChecked(DataGridViewRow row) => row.Cells["Convert"].Value is bool value && value;

    private void BrowseFolder(TextBox target)
    {
        using var dialog = new FolderBrowserDialog { SelectedPath = Directory.Exists(target.Text) ? target.Text : _appDir };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            target.Text = dialog.SelectedPath;
        }
    }

    private void BrowseFfmpeg()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "選擇轉檔程式 ffmpeg.exe",
            Filter = "ffmpeg.exe|ffmpeg.exe|執行檔 (*.exe)|*.exe|所有檔案 (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _ffmpegText.Text = dialog.FileName;
        }
    }

    private void SetUiEnabled(bool enabled)
    {
        foreach (var button in _buttons)
        {
            button.Enabled = enabled;
        }

        _sequenceCheck.Enabled = enabled;
        _sequenceStartText.Enabled = enabled;
        _overwriteCheck.Enabled = enabled;
        _grid.Enabled = enabled;
    }

    private void StepProgress()
    {
        if (_progressBar.Value < _progressBar.Maximum)
        {
            _progressBar.Value++;
        }
        Application.DoEvents();
    }

    private void UpdateCount() => _countLabel.Text = $"檔案：{_grid.Rows.Count}";

    private void AddLog(string message)
    {
        _logBox.AppendText($"{DateTime.Now:HH:mm:ss}  {message}{Environment.NewLine}");
        _logBox.SelectionStart = _logBox.TextLength;
        _logBox.ScrollToCaret();
    }

    private void ToggleTheme()
    {
        _darkMode = !_darkMode;
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        Theme.UseDark(_darkMode);
        BackColor = Theme.AppBack;
        _themeButton.Text = _darkMode ? "☀" : "☾";

        foreach (var control in _appBackControls)
        {
            control.BackColor = Theme.AppBack;
        }

        foreach (var card in _cards)
        {
            card.BackColor = Theme.CardBack;
        }

        ApplyContainerColors(this);

        foreach (var label in _labels)
        {
            label.ForeColor = label.Font.Bold ? Theme.Text : Theme.Muted;
            label.BackColor = label.Parent?.BackColor ?? Theme.AppBack;
        }

        foreach (var box in _textBoxes)
        {
            box.BackColor = Theme.InputBack;
            box.ForeColor = Theme.Text;
        }

        foreach (var button in _secondaryButtons)
        {
            button.BackColor = Theme.ButtonBack;
            button.ForeColor = Theme.Text;
            button.FlatAppearance.MouseOverBackColor = Theme.ButtonHover;
        }

        _startButton.BackColor = Theme.Accent;
        _startButton.ForeColor = Color.White;
        _startButton.FlatAppearance.MouseOverBackColor = Theme.AccentHover;
        _themeButton.Font = new Font(Font.FontFamily, 13F, FontStyle.Bold);

        _overwriteCheck.ForeColor = Theme.Text;
        _overwriteCheck.BackColor = Theme.CardBack;
        _sequenceCheck.ForeColor = Theme.Text;
        _sequenceCheck.BackColor = Theme.AppBack;
        _countLabel.ForeColor = Theme.Muted;
        _countLabel.BackColor = Theme.AppBack;
        _statusLabel.ForeColor = Theme.Muted;
        _statusLabel.BackColor = Theme.CardBack;

        _logBox.BackColor = Theme.LogBack;
        _logBox.ForeColor = Theme.Text;

        _grid.BackgroundColor = Theme.CardBack;
        _grid.GridColor = Theme.Line;
        _grid.DefaultCellStyle.BackColor = Theme.CardBack;
        _grid.DefaultCellStyle.ForeColor = Theme.Text;
        _grid.DefaultCellStyle.SelectionBackColor = Theme.Selection;
        _grid.DefaultCellStyle.SelectionForeColor = Theme.Text;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.Header;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.Text;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Theme.AlternateRow;
        _grid.AlternatingRowsDefaultCellStyle.ForeColor = Theme.Text;
        _grid.Refresh();
    }

    private void ApplyContainerColors(Control control)
    {
        foreach (Control child in control.Controls)
        {
            if (child is Panel or TableLayoutPanel)
            {
                if (_appBackControls.Contains(child))
                {
                    child.BackColor = Theme.AppBack;
                }
                else if (_cards.Contains(child) || IsInsideCard(child))
                {
                    child.BackColor = Theme.CardBack;
                }
                else
                {
                    child.BackColor = Theme.AppBack;
                }
            }

            ApplyContainerColors(child);
        }
    }

    private bool IsInsideCard(Control control)
    {
        var parent = control.Parent;
        while (parent is not null)
        {
            if (_cards.Contains(parent))
            {
                return true;
            }

            parent = parent.Parent;
        }

        return false;
    }

    private Panel CreateCard()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.CardBack,
            Margin = new Padding(0, 0, 0, 12)
        };
        _cards.Add(panel);
        return panel;
    }

    private Label CreateLabel(string text)
    {
        var label = new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            ForeColor = Theme.Muted,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _labels.Add(label);
        return label;
    }

    private TextBox StyleTextBox(TextBox box)
    {
        box.Dock = DockStyle.Fill;
        box.BorderStyle = BorderStyle.FixedSingle;
        box.BackColor = Theme.InputBack;
        box.ForeColor = Theme.Text;
        box.Margin = new Padding(0, 2, 8, 3);
        if (!_textBoxes.Contains(box))
        {
            _textBoxes.Add(box);
        }
        return box;
    }

    private Button CreateButton(string text, Color backColor, Color foreColor, Action? click = null)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            BackColor = backColor,
            ForeColor = foreColor,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(6, 2, 0, 3),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        if (click is not null)
        {
            button.Click += (_, _) => click();
        }

        _buttons.Add(button);
        if (backColor != Theme.Accent)
        {
            _secondaryButtons.Add(button);
        }
        return button;
    }

    private static class Theme
    {
        public static Color AppBack { get; private set; } = Color.FromArgb(245, 247, 250);
        public static Color CardBack { get; private set; } = Color.White;
        public static Color Header { get; private set; } = Color.FromArgb(239, 243, 248);
        public static Color Text { get; private set; } = Color.FromArgb(29, 36, 48);
        public static Color Muted { get; private set; } = Color.FromArgb(92, 104, 120);
        public static Color Line { get; private set; } = Color.FromArgb(224, 230, 238);
        public static Color Selection { get; private set; } = Color.FromArgb(220, 237, 255);
        public static Color AlternateRow { get; private set; } = Color.FromArgb(250, 252, 255);
        public static Color InputBack { get; private set; } = Color.White;
        public static Color LogBack { get; private set; } = Color.White;
        public static Color ButtonBack { get; private set; } = Color.FromArgb(231, 236, 244);
        public static Color ButtonHover { get; private set; } = Color.FromArgb(220, 228, 239);
        public static Color Accent { get; private set; } = Color.FromArgb(37, 99, 235);
        public static Color AccentHover { get; private set; } = Color.FromArgb(29, 78, 216);

        public static void UseDark(bool dark)
        {
            if (!dark)
            {
                AppBack = Color.FromArgb(245, 247, 250);
                CardBack = Color.White;
                Header = Color.FromArgb(239, 243, 248);
                Text = Color.FromArgb(29, 36, 48);
                Muted = Color.FromArgb(92, 104, 120);
                Line = Color.FromArgb(224, 230, 238);
                Selection = Color.FromArgb(220, 237, 255);
                AlternateRow = Color.FromArgb(250, 252, 255);
                InputBack = Color.White;
                LogBack = Color.White;
                ButtonBack = Color.FromArgb(231, 236, 244);
                ButtonHover = Color.FromArgb(220, 228, 239);
                Accent = Color.FromArgb(37, 99, 235);
                AccentHover = Color.FromArgb(29, 78, 216);
                return;
            }

            AppBack = Color.FromArgb(17, 24, 39);
            CardBack = Color.FromArgb(31, 41, 55);
            Header = Color.FromArgb(55, 65, 81);
            Text = Color.FromArgb(243, 244, 246);
            Muted = Color.FromArgb(180, 190, 205);
            Line = Color.FromArgb(75, 85, 99);
            Selection = Color.FromArgb(30, 64, 110);
            AlternateRow = Color.FromArgb(35, 46, 61);
            InputBack = Color.FromArgb(17, 24, 39);
            LogBack = Color.FromArgb(15, 23, 42);
            ButtonBack = Color.FromArgb(55, 65, 81);
            ButtonHover = Color.FromArgb(75, 85, 99);
            Accent = Color.FromArgb(59, 130, 246);
            AccentHover = Color.FromArgb(37, 99, 235);
        }
    }
}

