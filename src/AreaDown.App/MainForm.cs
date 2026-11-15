using System.Diagnostics;
using System.Drawing;
using System.Text.Json;
using AreaDown.Core;
using AreaDown.Downloader;
using Microsoft.Web.WebView2.WinForms;

namespace AreaDown.App;

/// <summary>Voryda Down's video-first download workspace.</summary>
public sealed class MainForm : Form
{
    private static readonly Color Canvas = Color.FromArgb(246, 247, 249);
    private static readonly Color Ink = Color.FromArgb(30, 36, 42);
    private static readonly Color Muted = Color.FromArgb(104, 114, 124);
    private static readonly Color Green = Color.FromArgb(31, 139, 84);
    private readonly TextBox _url = new();
    private readonly ComboBox _quality = new();
    private readonly TextBox _start = new();
    private readonly TextBox _end = new();
    private readonly TextBox _folder = new();
    private readonly TextBox _fileName = new();
    private readonly CheckBox _embedMetadata = new();
    private readonly CheckBox _embedThumbnail = new();
    private readonly CheckBox _subtitles = new();
    private readonly PictureBox _thumbnail = new();
    private readonly WebView2 _videoPlayer = new();
    private readonly ClipTimelineControl _timeline = new();
    private readonly Label _clipTimes = new();
    private readonly Label _videoTitle = new();
    private readonly Label _videoInfo = new();
    private readonly Label _status = new();
    private readonly Label _toolStatus = new();
    private readonly ProgressBar _progress = new();
    private readonly Button _previewButton = new();
    private readonly Button _downloadButton = new();
    private readonly Button _cancelButton = new();
    private readonly Button _browseButton = new();
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _trayMenu;
    private AppSettings _settings;
    private YtDlpProcess? _downloader;
    private bool _isDownloading;
    private bool _isLoadingPreview;
    private bool _updatingTimelineFields;
    private bool _playerNavigationHandlerAttached;
    private VideoPreview? _preview;
    private string? _previewUrl;

    public MainForm()
    {
        _settings = AppSettings.Load();
        Text = "Voryda Down";
        MinimumSize = new Size(920, 700);
        Size = new Size(1180, 860);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Canvas;
        Font = new Font("Segoe UI", 9f);
        Icon = LoadIcon();
        BuildInterface();
        _trayMenu = new ContextMenuStrip();
        _trayMenu.Items.Add("Mostrar Voryda Down", null, (_, _) => ShowFromTray());
        _trayMenu.Items.Add("Salir", null, (_, _) => CloseApp());
        _notifyIcon = new NotifyIcon { Icon = LoadIcon() ?? SystemIcons.Application, Text = "Voryda Down", Visible = false, ContextMenuStrip = _trayMenu };
        _notifyIcon.DoubleClick += (_, _) => ShowFromTray();
        Resize += (_, _) => { if (WindowState == FormWindowState.Minimized) HideToTray(); };
        FormClosing += MainForm_FormClosing;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _downloader?.Dispose(); _notifyIcon?.Dispose(); _trayMenu?.Dispose(); _thumbnail.Image?.Dispose(); }
        base.Dispose(disposing);
    }

    private void BuildInterface()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(28, 20, 28, 20), ColumnCount = 1, RowCount = 4, BackColor = Canvas };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        Controls.Add(root);

        var header = new Panel { Dock = DockStyle.Fill };
        var logo = new Label { Text = "V", Font = new Font("Segoe UI", 19, FontStyle.Bold), ForeColor = Color.White, BackColor = Green, TextAlign = ContentAlignment.MiddleCenter, Size = new Size(44, 44), Location = new Point(0, 5) };
        var title = new Label { Text = "Voryda Down", Font = new Font("Segoe UI", 19, FontStyle.Bold), ForeColor = Ink, AutoSize = true, Location = new Point(56, 0) };
        var subtitle = new Label { Text = "Descarga medios con toda la información antes de empezar.", ForeColor = Muted, AutoSize = true, Location = new Point(58, 36) };
        var settings = LinkButton("Ajustes", (_, _) => OpenSettings()); settings.Anchor = AnchorStyles.Top | AnchorStyles.Right; settings.Location = new Point(Width - 250, 12);
        var help = LinkButton("Ayuda", (_, _) => OpenLink("https://github.com/smouj/voryda-down")); help.Anchor = AnchorStyles.Top | AnchorStyles.Right; help.Location = new Point(Width - 145, 12);
        header.Controls.AddRange([logo, title, subtitle, settings, help]);
        header.Resize += (_, _) => { help.Left = header.ClientSize.Width - help.Width - 4; settings.Left = help.Left - settings.Width - 8; };
        root.Controls.Add(header, 0, 0);

        var urlPanel = Card(); urlPanel.Dock = DockStyle.Fill; urlPanel.Padding = new Padding(12, 8, 12, 8);
        var urlLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        urlLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); urlLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        _url.Dock = DockStyle.Fill; _url.Font = new Font("Segoe UI", 10); _url.BorderStyle = BorderStyle.FixedSingle; _url.PlaceholderText = "Pega aquí el enlace del video…"; _url.Text = _settings.LastUrl ?? "";
        _url.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _ = LoadPreviewAsync(); } };
        _url.TextChanged += Url_TextChanged;
        _previewButton.Text = "Ver video"; StyleButton(_previewButton, Green, Color.White); _previewButton.Dock = DockStyle.Fill; _previewButton.Margin = new Padding(10, 0, 0, 0); _previewButton.Click += async (_, _) => await LoadPreviewAsync();
        urlLayout.Controls.Add(_url, 0, 0); urlLayout.Controls.Add(_previewButton, 1, 0); urlPanel.Controls.Add(urlLayout); root.Controls.Add(urlPanel, 0, 1);

        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(0, 14, 0, 0) };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58)); body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        var previewCard = Card(); previewCard.Dock = DockStyle.Fill; previewCard.Margin = new Padding(0, 0, 10, 0); previewCard.Padding = new Padding(16);
        var previewLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26)); previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 58)); previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25)); previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 17));
        previewLayout.Controls.Add(Heading("VISTA PREVIA"), 0, 0);
        var imageHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(20, 24, 28), Margin = new Padding(0, 8, 0, 10) };
        _thumbnail.Dock = DockStyle.Fill; _thumbnail.SizeMode = PictureBoxSizeMode.Zoom;
        var imagePlaceholder = new Label { Text = "Pega un enlace y pulsa «Ver video»", ForeColor = Color.White, BackColor = Color.FromArgb(20, 24, 28), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 11) };
        _videoPlayer.Dock = DockStyle.Fill; _videoPlayer.Visible = false;
        imageHost.Controls.Add(_thumbnail); imageHost.Controls.Add(_videoPlayer); imageHost.Controls.Add(imagePlaceholder); imagePlaceholder.BringToFront();
        _thumbnail.Tag = imagePlaceholder;
        previewLayout.Controls.Add(imageHost, 0, 1);
        var details = new Panel { Dock = DockStyle.Fill };
        _videoTitle.Text = "Aún no hay ningún video"; _videoTitle.Font = new Font("Segoe UI", 14, FontStyle.Bold); _videoTitle.ForeColor = Ink; _videoTitle.Dock = DockStyle.Top; _videoTitle.Height = 48; _videoTitle.AutoEllipsis = true;
        _videoInfo.Text = "La vista previa mostrará título, canal, duración, fecha y formatos disponibles."; _videoInfo.ForeColor = Muted; _videoInfo.Dock = DockStyle.Fill;
        details.Controls.Add(_videoInfo); details.Controls.Add(_videoTitle);
        previewLayout.Controls.Add(details, 0, 2);
        var timelinePanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0, 4, 0, 0) };
        timelinePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 60)); timelinePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        _timeline.Dock = DockStyle.Fill; _timeline.Enabled = false; _timeline.RangeChanged += Timeline_RangeChanged;
        _clipTimes.Text = "Fragmento · 00:00 — duración completa"; _clipTimes.ForeColor = Muted; _clipTimes.Dock = DockStyle.Fill; _clipTimes.TextAlign = ContentAlignment.MiddleLeft;
        timelinePanel.Controls.Add(_timeline, 0, 0); timelinePanel.Controls.Add(_clipTimes, 0, 1);
        previewLayout.Controls.Add(timelinePanel, 0, 3); previewCard.Controls.Add(previewLayout); body.Controls.Add(previewCard, 0, 0);

        var optionsCard = Card(); optionsCard.Dock = DockStyle.Fill; optionsCard.Margin = new Padding(10, 0, 0, 0); optionsCard.Padding = new Padding(18, 16, 18, 14); optionsCard.AutoScroll = true;
        var options = new FlowLayoutPanel { Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 0, 4, 4) };
        void FitOptions()
        {
            options.Width = Math.Max(180, optionsCard.ClientSize.Width - 24);
            foreach (Control child in options.Controls) child.Width = Math.Max(160, options.ClientSize.Width - 8);
        }
        optionsCard.Resize += (_, _) => FitOptions();
        options.SizeChanged += (_, _) => FitOptions();
        options.Controls.Add(Heading("OPCIONES DE DESCARGA"));
        options.Controls.Add(FieldLabel("Calidad"));
        _quality.DropDownStyle = ComboBoxStyle.DropDownList; _quality.Items.AddRange(["Mejor disponible (hasta 1080p)", "1080p", "720p", "480p", "Solo audio (M4A)"]); _quality.SelectedIndex = QualityToIndex(_settings.LastQuality); _quality.Dock = DockStyle.Top; _quality.Height = 32;
        options.Controls.Add(_quality);
        var clipLabel = FieldLabel("Recorte por tiempo · opcional"); clipLabel.Margin = new Padding(0, 12, 0, 3); options.Controls.Add(clipLabel);
        var clipPanel = new TableLayoutPanel { Dock = DockStyle.Top, Height = 34, ColumnCount = 4, RowCount = 1 };
        clipPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40)); clipPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); clipPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34)); clipPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _start.PlaceholderText = "00:00"; _end.PlaceholderText = "00:00"; _start.Dock = _end.Dock = DockStyle.Fill;
        clipPanel.Controls.Add(new Label { Text = "De", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0); clipPanel.Controls.Add(_start, 1, 0); clipPanel.Controls.Add(new Label { Text = "a", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(8, 7, 0, 0) }, 2, 0); clipPanel.Controls.Add(_end, 3, 0); options.Controls.Add(clipPanel);
        var folderLabel = FieldLabel("Guardar en"); folderLabel.Margin = new Padding(0, 12, 0, 3); options.Controls.Add(folderLabel);
        var folderPanel = new TableLayoutPanel { Dock = DockStyle.Top, Height = 34, ColumnCount = 2 }; folderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); folderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        _folder.Text = _settings.SaveFolder; _folder.Dock = DockStyle.Fill; _folder.ReadOnly = true;
        _browseButton.Text = "Elegir…"; _browseButton.Dock = DockStyle.Fill; _browseButton.Click += BrowseButton_Click; folderPanel.Controls.Add(_folder); folderPanel.Controls.Add(_browseButton, 1, 0); options.Controls.Add(folderPanel);
        var nameLabel = FieldLabel("Nombre de archivo"); nameLabel.Margin = new Padding(0, 12, 0, 3); options.Controls.Add(nameLabel);
        _fileName.Text = _settings.FileName; _fileName.Dock = DockStyle.Top; _fileName.PlaceholderText = "%(title)s.%(ext)s"; options.Controls.Add(_fileName);
        var extrasLabel = FieldLabel("Incluir en el archivo"); extrasLabel.Margin = new Padding(0, 12, 0, 3); options.Controls.Add(extrasLabel);
        _embedMetadata.Text = "Metadatos del video"; _embedMetadata.Checked = _settings.EmbedMetadata; _embedMetadata.AutoSize = true; options.Controls.Add(_embedMetadata);
        _embedThumbnail.Text = "Miniatura como portada (audio)"; _embedThumbnail.Checked = _settings.EmbedThumbnail; _embedThumbnail.AutoSize = true; options.Controls.Add(_embedThumbnail);
        _subtitles.Text = "Subtítulos disponibles"; _subtitles.Checked = _settings.WriteSubtitles; _subtitles.AutoSize = true; options.Controls.Add(_subtitles);
        var legal = new Label { Text = "Descarga solo contenido que tengas permiso para guardar.", ForeColor = Muted, AutoSize = true, Margin = new Padding(0, 12, 0, 0) }; options.Controls.Add(legal);
        var projectLinks = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false, Margin = new Padding(0, 8, 0, 0) };
        foreach (var (caption, target) in new[] { ("Voryda Snap", "https://github.com/smouj/voryda-snap"), ("Voryda Rec", "https://github.com/smouj/voryda-rec"), ("Voryda Cut", "https://github.com/smouj/voryda-cut") })
        { var link = new LinkLabel { Text = caption, AutoSize = true, LinkColor = Green, Margin = new Padding(0, 0, 12, 0) }; link.LinkClicked += (_, _) => OpenLink(target); projectLinks.Controls.Add(link); }
        options.Controls.Add(projectLinks);
        FitOptions();
        optionsCard.Controls.Add(options); body.Controls.Add(optionsCard, 1, 0); root.Controls.Add(body, 0, 2);
        _start.TextChanged += StartTextChanged;
        _end.TextChanged += StartTextChanged;

        var footer = Card(); footer.Dock = DockStyle.Fill; footer.Padding = new Padding(14, 10, 14, 10); footer.Margin = new Padding(0, 14, 0, 0);
        var footerLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2 };
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 102)); footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108)); footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); footerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 55)); footerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        _status.Text = "Listo para empezar"; _status.ForeColor = Ink; _status.Dock = DockStyle.Fill; _status.TextAlign = ContentAlignment.MiddleLeft;
        _downloadButton.Text = "Descargar"; StyleButton(_downloadButton, Green, Color.White); _downloadButton.Dock = DockStyle.Fill; _downloadButton.Click += async (_, _) => await DownloadButton_Click();
        _cancelButton.Text = "Cancelar"; StyleButton(_cancelButton, Color.FromArgb(245, 246, 247), Ink); _cancelButton.Dock = DockStyle.Fill; _cancelButton.Enabled = false; _cancelButton.Click += CancelButton_Click;
        var openFolder = LinkButton("Abrir carpeta", (_, _) => OpenFolder()); openFolder.Dock = DockStyle.Fill;
        _progress.Dock = DockStyle.Fill; _progress.Margin = new Padding(0, 4, 14, 3); _progress.Style = ProgressBarStyle.Continuous;
        _toolStatus.Text = "Comprobando herramientas…"; _toolStatus.ForeColor = Muted; _toolStatus.Dock = DockStyle.Fill; _toolStatus.TextAlign = ContentAlignment.MiddleLeft;
        footerLayout.Controls.Add(_status, 0, 0); footerLayout.Controls.Add(_downloadButton, 1, 0); footerLayout.Controls.Add(_cancelButton, 2, 0); footerLayout.Controls.Add(openFolder, 3, 0);
        footerLayout.Controls.Add(_progress, 0, 1); footerLayout.SetColumnSpan(_progress, 2); footerLayout.Controls.Add(_toolStatus, 2, 1); footerLayout.SetColumnSpan(_toolStatus, 2);
        footer.Controls.Add(footerLayout); root.Controls.Add(footer, 0, 3);
        Shown += (_, _) => RefreshToolStatus();
    }

    private async Task LoadPreviewAsync()
    {
        var url = _url.Text.Trim();
        if (!ValidateUrl(url)) return;
        if (_isLoadingPreview) return;
        SetPreviewLoading(true); SetStatus("Consultando información del video…");
        try
        {
            var json = await YtDlpProcess.GetVideoInfoAsync(url);
            _preview = VideoPreview.Parse(json);
            _previewUrl = url;
            _videoTitle.Text = _preview.Title;
            _videoInfo.Text = string.Join("\r\n", new[] { _preview.Uploader, _preview.Duration, _preview.UploadDate, _preview.Resolution, _preview.FileSize, _preview.FormatSummary }.Where(x => !string.IsNullOrWhiteSpace(x)));
            if (_preview.DurationSeconds is > 0)
            {
                _timeline.SetDuration(TimeSpan.FromSeconds(_preview.DurationSeconds.Value));
                _timeline.Enabled = true;
                UpdateTimelineFields();
            }
            if (!string.IsNullOrWhiteSpace(_preview.ThumbnailUrl))
            {
                using var client = new HttpClient(); var bytes = await client.GetByteArrayAsync(_preview.ThumbnailUrl);
                using var stream = new MemoryStream(bytes); var image = Image.FromStream(stream); var previous = _thumbnail.Image; _thumbnail.Image = new Bitmap(image); previous?.Dispose();
                if (_thumbnail.Tag is Control placeholder) placeholder.Visible = false;
            }
            if (!string.IsNullOrWhiteSpace(_preview.WebpageUrl))
            {
                try
                {
                    await _videoPlayer.EnsureCoreWebView2Async();
                    if (!_playerNavigationHandlerAttached)
                    {
                        _videoPlayer.CoreWebView2.NavigationCompleted += (_, navigation) =>
                        {
                            if (!navigation.IsSuccess)
                            {
                                _videoPlayer.Visible = false;
                                _thumbnail.Visible = true;
                                if (_thumbnail.Tag is Control placeholder) placeholder.Visible = _thumbnail.Image is null;
                            }
                        };
                        _playerNavigationHandlerAttached = true;
                    }
                    _videoPlayer.Source = new Uri(_preview.WebpageUrl);
                    _videoPlayer.Visible = true;
                    _thumbnail.Visible = false;
                    if (_thumbnail.Tag is Control placeholder) placeholder.Visible = false;
                }
                catch { _videoPlayer.Visible = false; _thumbnail.Visible = true; }
            }
            _settings = _settings with { LastUrl = url }; AppSettings.Save(_settings);
            SetStatus("Vista previa lista · revisa los detalles antes de descargar");
        }
        catch (Exception ex) { _preview = null; _previewUrl = null; _videoTitle.Text = "No se pudo cargar la vista previa"; _videoInfo.Text = ex.Message; SetStatus("No se pudo consultar este enlace"); }
        finally { SetPreviewLoading(false); }
    }

    private void Timeline_RangeChanged(object? sender, EventArgs e) => UpdateTimelineFields();

    private void Url_TextChanged(object? sender, EventArgs e)
    {
        if (_previewUrl is null || string.Equals(_url.Text.Trim(), _previewUrl, StringComparison.Ordinal)) return;
        _preview = null;
        _previewUrl = null;
        _timeline.Enabled = false;
        _videoPlayer.Visible = false;
        _thumbnail.Visible = true;
        var old = _thumbnail.Image;
        _thumbnail.Image = null;
        old?.Dispose();
        if (_thumbnail.Tag is Control placeholder) placeholder.Visible = true;
        _videoTitle.Text = "Aún no hay ningún video";
        _videoInfo.Text = "Carga la vista previa para ver sus detalles y seleccionar un fragmento.";
        _start.Clear();
        _end.Clear();
    }

    private void UpdateTimelineFields()
    {
        var start = _timeline.StartTime;
        var end = _timeline.EndTime;
        _updatingTimelineFields = true;
        _start.Text = start > TimeSpan.Zero ? FormatTimestamp(start) : string.Empty;
        _end.Text = end < _timeline.Duration ? FormatTimestamp(end) : string.Empty;
        _updatingTimelineFields = false;
        _clipTimes.Text = start <= TimeSpan.Zero && end >= _timeline.Duration
            ? $"Fragmento · 00:00 — {FormatTimestamp(_timeline.Duration)} (completo)"
            : $"Fragmento seleccionado · {FormatTimestamp(start)} — {FormatTimestamp(end)}";
    }

    private void StartTextChanged(object? sender, EventArgs e) => UpdateTimelineFromFields();

    private void UpdateTimelineFromFields()
    {
        if (!_timeline.Enabled || _updatingTimelineFields) return;
        if (!TimestampParser.TryParse(_start.Text, out var start) || !TimestampParser.TryParse(_end.Text, out var end)) return;
        var startTime = start ?? TimeSpan.Zero;
        var endTime = end ?? _timeline.Duration;
        if (endTime >= startTime) _timeline.SetRange(startTime, endTime);
        startTime = _timeline.StartTime;
        endTime = _timeline.EndTime;
        _clipTimes.Text = startTime <= TimeSpan.Zero && endTime >= _timeline.Duration
            ? $"Fragmento · 00:00 — {FormatTimestamp(_timeline.Duration)} (completo)"
            : $"Fragmento seleccionado · {FormatTimestamp(startTime)} — {FormatTimestamp(endTime)}";
    }

    private static string FormatTimestamp(TimeSpan value) => value.TotalHours >= 1 ? value.ToString(@"h\:mm\:ss") : value.ToString(@"m\:ss");

    private async Task DownloadButton_Click()
    {
        var url = _url.Text.Trim(); if (!ValidateUrl(url)) return;
        if (_preview is null || !string.Equals(_previewUrl, url, StringComparison.Ordinal))
        {
            var confirmPreview = MessageBox.Show("Todavía no has cargado la vista previa de este enlace. ¿Quieres consultarla ahora?", "Voryda Down", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (confirmPreview != DialogResult.Yes) return;
            await LoadPreviewAsync();
            if (_preview is null || !string.Equals(_previewUrl, url, StringComparison.Ordinal)) return;
        }
        var folder = _folder.Text.Trim();
        try { Directory.CreateDirectory(folder); }
        catch (Exception ex) { MessageBox.Show($"No se puede usar la carpeta: {ex.Message}", "Voryda Down", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        if (!TimestampParser.TryParse(_start.Text, out var start) || !TimestampParser.TryParse(_end.Text, out var end))
        { MessageBox.Show("Usa segundos, MM:SS o HH:MM:SS para el recorte.", "Voryda Down", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        var startTime = start ?? TimeSpan.Zero;
        if (end.HasValue && end.Value <= startTime) { MessageBox.Show("El final del recorte debe ir después del inicio.", "Voryda Down", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        var clip = start.HasValue || end.HasValue ? new TimestampClip(startTime, end) : null;
        var quality = IndexToQuality(_quality.SelectedIndex);
        _settings = _settings with { LastUrl = url, LastQuality = quality, SaveFolder = folder, FileName = string.IsNullOrWhiteSpace(_fileName.Text) ? "%(title)s.%(ext)s" : _fileName.Text.Trim(), EmbedMetadata = _embedMetadata.Checked, EmbedThumbnail = _embedThumbnail.Checked, WriteSubtitles = _subtitles.Checked };
        AppSettings.Save(_settings);
        var settings = new DownloadSettings(url, quality, clip, folder, _settings.FileName) { EmbedMetadata = _settings.EmbedMetadata, EmbedThumbnail = _settings.EmbedThumbnail, WriteSubtitles = _settings.WriteSubtitles };
        await RunDownloadAsync(settings);
    }

    private async Task RunDownloadAsync(DownloadSettings settings)
    {
        SetDownloading(true); _progress.Value = 0; SetStatus("Iniciando descarga…");
        try
        {
            _downloader = new YtDlpProcess(); _downloader.ProgressReceived += OnProgressReceived; _downloader.Exited += OnDownloadExited;
            string? ffmpeg = null; try { ffmpeg = YtDlpProcess.FindFfmpeg(); } catch (FileNotFoundException) { }
            _downloader.Start(settings, ffmpeg);
        }
        catch (Exception ex) { SetDownloading(false); SetStatus("No se pudo iniciar la descarga"); MessageBox.Show(ex.Message, "Voryda Down", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        await Task.CompletedTask;
    }

    private void OnProgressReceived(object? sender, DownloadEvent evt)
    {
        if (InvokeRequired) { BeginInvoke(new Action(() => OnProgressReceived(sender, evt))); return; }
        switch (evt.Type)
        {
            case DownloadEventType.Progress:
                if (evt.Percent.HasValue) _progress.Value = Math.Clamp((int)Math.Round(evt.Percent.Value), 0, 100);
                SetStatus(string.Join(" · ", new[] { evt.Size, evt.Speed is null ? null : "Velocidad " + evt.Speed, evt.Eta is null ? null : "Restan " + evt.Eta }.Where(x => x is not null)) is { Length: > 0 } value ? value : "Descargando…"); break;
            case DownloadEventType.Merging: SetStatus("Combinando audio y video…"); break;
            case DownloadEventType.Destination: SetStatus("Guardando: " + Path.GetFileName(evt.FilePath ?? "video")); break;
            case DownloadEventType.Extracting: SetStatus("Procesando audio…"); break;
            case DownloadEventType.Error: SetStatus("Error: " + evt.FilePath); break;
        }
    }

    private void OnDownloadExited(object? sender, int code)
    {
        if (InvokeRequired) { BeginInvoke(new Action(() => OnDownloadExited(sender, code))); return; }
        SetStatus(code == 0 ? "Descarga completada ✓" : _isDownloading ? $"La descarga falló (código {code})" : "Descarga cancelada");
        if (code == 0) _progress.Value = 100;
        _downloader?.Dispose(); _downloader = null; SetDownloading(false);
    }

    private void OpenSettings()
    {
        using var dialog = new SettingsForm(_settings);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _settings = dialog.Settings with { LastQuality = IndexToQuality(_quality.SelectedIndex), EmbedMetadata = _embedMetadata.Checked, EmbedThumbnail = _embedThumbnail.Checked, WriteSubtitles = _subtitles.Checked }; _folder.Text = _settings.SaveFolder; _quality.SelectedIndex = QualityToIndex(_settings.LastQuality);
        _fileName.Text = _settings.FileName; _embedMetadata.Checked = _settings.EmbedMetadata; _embedThumbnail.Checked = _settings.EmbedThumbnail; _subtitles.Checked = _settings.WriteSubtitles; AppSettings.Save(_settings); RefreshToolStatus();
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog { Description = "Elige dónde guardar tus descargas", UseDescriptionForTitle = true, SelectedPath = _folder.Text };
        if (dialog.ShowDialog(this) == DialogResult.OK) { _folder.Text = dialog.SelectedPath; _settings = _settings with { SaveFolder = dialog.SelectedPath }; AppSettings.Save(_settings); }
    }

    private void CancelButton_Click(object? sender, EventArgs e) { _downloader?.Cancel(); SetStatus("Cancelando…"); }
    private void OpenFolder() { try { Directory.CreateDirectory(_folder.Text); Process.Start(new ProcessStartInfo("explorer.exe", _folder.Text) { UseShellExecute = true }); } catch (Exception ex) { MessageBox.Show(ex.Message, "Voryda Down"); } }
    private static void OpenLink(string url) { try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch (Exception ex) { MessageBox.Show("No se pudo abrir el enlace: " + ex.Message, "Voryda Down", MessageBoxButtons.OK, MessageBoxIcon.Error); } }

    private void RefreshToolStatus()
    {
        var downloader = "yt-dlp: " + (TryFind(YtDlpProcess.FindYtDlp) ? "listo" : "no encontrado");
        var ffmpeg = "FFmpeg: " + (TryFind(YtDlpProcess.FindFfmpeg) ? "listo" : "no encontrado");
        _toolStatus.Text = downloader + "   ·   " + ffmpeg;
        _toolStatus.ForeColor = downloader.EndsWith("listo") ? Muted : Color.FromArgb(168, 100, 35);
    }
    private static bool TryFind(Func<string> find) { try { _ = find(); return true; } catch { return false; } }

    private bool ValidateUrl(string value)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)) return true;
        MessageBox.Show("Introduce un enlace completo que empiece por http:// o https://.", "Voryda Down", MessageBoxButtons.OK, MessageBoxIcon.Warning); _url.Focus(); return false;
    }
    private void SetPreviewLoading(bool loading) { _isLoadingPreview = loading; _previewButton.Enabled = !loading; _url.Enabled = !loading && !_isDownloading; _previewButton.Text = loading ? "Cargando…" : "Ver video"; }
    private void SetDownloading(bool value) { _isDownloading = value; _downloadButton.Enabled = !value; _cancelButton.Enabled = value; _previewButton.Enabled = !value && !_isLoadingPreview; _url.Enabled = !value && !_isLoadingPreview; _quality.Enabled = !value; _start.Enabled = !value; _end.Enabled = !value; _browseButton.Enabled = !value; _fileName.Enabled = !value; _embedMetadata.Enabled = !value; _embedThumbnail.Enabled = !value; _subtitles.Enabled = !value; }
    private void SetStatus(string value) => _status.Text = value;
    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e) { if (_isDownloading && MessageBox.Show("Hay una descarga en curso. ¿Quieres salir?", "Voryda Down", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No) { e.Cancel = true; return; } if (_isDownloading) _downloader?.Cancel(); _notifyIcon.Visible = false; }
    private void ShowFromTray() { WindowState = FormWindowState.Normal; Show(); Activate(); }
    private void HideToTray() { _notifyIcon.Visible = true; Hide(); }
    private void CloseApp() { _notifyIcon.Visible = false; Close(); }
    private static string IndexToQuality(int i) => i switch { 1 => "1080p", 2 => "720p", 3 => "480p", 4 => "audio", _ => "best" };
    private static int QualityToIndex(string q) => q switch { "1080p" => 1, "720p" => 2, "480p" => 3, "audio" => 4, _ => 0 };
    private static Panel Card() => new() { BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
    private static Label Heading(string text) => new() { Text = text, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Green, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 0, 0, 6) };
    private static Label FieldLabel(string text) => new() { Text = text, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Ink, AutoSize = true, Margin = new Padding(0, 8, 0, 3) };
    private static Button LinkButton(string text, EventHandler click) { var b = new Button { Text = text, FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = Green, Cursor = Cursors.Hand, AutoSize = true }; b.FlatAppearance.BorderColor = Color.FromArgb(220, 226, 230); b.Click += click; return b; }
    private static void StyleButton(Button button, Color background, Color foreground) { button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderSize = 0; button.BackColor = background; button.ForeColor = foreground; button.Font = new Font("Segoe UI", 9, FontStyle.Bold); button.Cursor = Cursors.Hand; }
    private static Icon? LoadIcon() { try { var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AreaDown.ico"); if (File.Exists(path)) return new Icon(path); path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "assets", "AreaDown.ico"); return File.Exists(path) ? new Icon(path) : null; } catch { return null; } }
}

internal sealed record VideoPreview(string Title, string? Uploader, string? Duration, string? UploadDate, string? Resolution, string? FileSize, string? ThumbnailUrl, string? FormatSummary, string? WebpageUrl, double? DurationSeconds)
{
    public static VideoPreview Parse(string json)
    {
        using var doc = JsonDocument.Parse(json); var root = doc.RootElement;
        string? Get(string name) => root.TryGetProperty(name, out var p) && p.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined ? p.ToString() : null;
        double? durationSeconds = null; string? duration = null; if (root.TryGetProperty("duration", out var d) && d.TryGetDouble(out var seconds)) { durationSeconds = seconds; duration = "Duración · " + TimeSpan.FromSeconds(seconds).ToString(seconds >= 3600 ? "h\\:mm\\:ss" : "m\\:ss"); }
        var date = Get("upload_date"); if (date is { Length: 8 }) date = $"{date[..4]}-{date[4..6]}-{date[6..]}";
        var height = Get("height"); var resolution = height is not null ? $"Resolución · {height}p" : null;
        var size = Get("filesize_approx") ?? Get("filesize"); if (long.TryParse(size, out var bytes)) size = "Tamaño aprox. · " + (bytes / 1048576d).ToString("N1") + " MB"; else size = null;
        string? formats = null; if (root.TryGetProperty("formats", out var list) && list.ValueKind == JsonValueKind.Array) { var heights = list.EnumerateArray().Where(f => f.TryGetProperty("height", out _)).Select(f => f.GetProperty("height").GetInt32()).Distinct().OrderDescending().Take(5).ToArray(); if (heights.Length > 0) formats = "Calidades · " + string.Join(" / ", heights.Select(h => h + "p")); }
        return new VideoPreview(Get("title") ?? "Video sin título", Get("uploader") ?? Get("channel"), duration, date is null ? null : "Publicado · " + date, resolution, size, Get("thumbnail"), formats, Get("webpage_url"), durationSeconds);
    }
}

internal sealed class SettingsForm : Form
{
    private readonly TextBox _folder = new(); private readonly TextBox _filename = new();
    public AppSettings Settings { get; private set; }
    public SettingsForm(AppSettings settings)
    {
        Settings = settings; Text = "Ajustes · Voryda Down"; FormBorderStyle = FormBorderStyle.FixedDialog; StartPosition = FormStartPosition.CenterParent; MinimizeBox = false; MaximizeBox = false; ClientSize = new Size(520, 225); BackColor = Color.FromArgb(246, 247, 249);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 6 }; root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); Controls.Add(root);
        root.Controls.Add(new Label { Text = "Preferencias", Font = new Font("Segoe UI", 15, FontStyle.Bold), ForeColor = Color.FromArgb(30, 36, 42), Dock = DockStyle.Fill });
        root.Controls.Add(new Label { Text = "Carpeta predeterminada", Dock = DockStyle.Fill, ForeColor = Color.FromArgb(104, 114, 124) });
        var folder = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 }; folder.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); folder.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86)); _folder.Text = settings.SaveFolder; _folder.Dock = DockStyle.Fill; _folder.ReadOnly = true; var browse = new Button { Text = "Cambiar…", Dock = DockStyle.Fill }; browse.Click += (_, _) => { using var d = new FolderBrowserDialog { SelectedPath = _folder.Text, Description = "Elige tu carpeta de descargas", UseDescriptionForTitle = true }; if (d.ShowDialog(this) == DialogResult.OK) _folder.Text = d.SelectedPath; }; folder.Controls.Add(_folder); folder.Controls.Add(browse); root.Controls.Add(folder);
        root.Controls.Add(new Label { Text = "Plantilla del nombre de archivo (variables yt-dlp admitidas)", Dock = DockStyle.Fill, ForeColor = Color.FromArgb(104, 114, 124) }); _filename.Text = settings.FileName; _filename.Dock = DockStyle.Fill; root.Controls.Add(_filename);
        root.Controls.Add(new Label { Text = "Las preferencias de calidad, metadatos y subtítulos se guardan desde la pantalla principal.", Dock = DockStyle.Fill, ForeColor = Color.FromArgb(104, 114, 124) });
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft }; var save = new Button { Text = "Guardar", DialogResult = DialogResult.OK, Width = 100 }; save.Click += (_, _) => Settings = settings with { SaveFolder = _folder.Text, FileName = string.IsNullOrWhiteSpace(_filename.Text) ? "%(title)s.%(ext)s" : _filename.Text.Trim() }; var cancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Width = 100 }; actions.Controls.Add(save); actions.Controls.Add(cancel); root.Controls.Add(actions); AcceptButton = save; CancelButton = cancel;
    }
}
