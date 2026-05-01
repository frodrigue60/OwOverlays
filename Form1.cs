using Newtonsoft.Json;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ISImage = SixLabors.ImageSharp.Image;
using ISColor = SixLabors.ImageSharp.Color;
// Resolve ambiguities by aliasing System.Drawing types
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using Image = System.Drawing.Image;
using SDImage = System.Drawing.Image;
using SDColor = System.Drawing.Color;

namespace OwOverlays
{
    public partial class Form1 : Form
    {
        private NotifyIcon? trayIcon;
        private ContextMenuStrip? trayMenu;
        private List<OverlayForm> overlays = new List<OverlayForm>();
        private FlowLayoutPanel gridOverlays = null!;
        private int selectedIndex = -1;
        private Button btnAdd = null!;
        private Button btnRemove = null!;
        private NumericUpDown heightInput = null!;
        private Label lblHeight = null!;
        private CheckBox TaskbarHeightCheck = null!;
        private CheckBox chkLockOverlays = null!;
        private bool IsLocked;
        private bool isPaused = false;
        private const string ConfigFile = "GifOverlayConfig.json";
        private AppSettings currentSettings = new AppSettings();
        private bool isUpdatingUI = false;
        private ColorDialog colorDialog = null!;
        private CheckBox chkChromaKey = null!;
        private Button btnChromaColor = null!;
        private ComboBox screenSelector = null!;
        private Label lblScreen = null!;
        private CheckBox chkAlwaysOnTop = null!;


        public static int GifHeight { get; private set; } = 100;
        private const int MaxGifHeight = 400;
        private int screenHeight = Screen.PrimaryScreen?.Bounds.Height ?? 1080;
        private int screenWidth = Screen.PrimaryScreen?.Bounds.Width ?? 1920;
        private int taskbarHeight = (Screen.PrimaryScreen?.Bounds.Height ?? 1080) - (Screen.PrimaryScreen?.WorkingArea.Height ?? 1040);
        private int baseY;


        public static Form1 Instance { get; private set; } = null!;
        public bool RespectTaskbarSetting => TaskbarHeightCheck.Checked;

        public List<OverlayForm> Overlays => overlays;

        public Form1()
        {
            Instance = this;
            baseY = screenHeight - taskbarHeight - GifHeight;

            this.Text = "OwOverlays";
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = true;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.Padding = new Padding(0, 0, 15, 15);

            Color controlBack = Color.FromArgb(45, 45, 45);
            Color accentColor = Color.FromArgb(0, 120, 212);

            // --- Contenedor Principal ---
            FlowLayoutPanel mainContainer = new FlowLayoutPanel();
            mainContainer.FlowDirection = FlowDirection.TopDown;
            mainContainer.WrapContents = false;
            mainContainer.AutoSize = true;
            mainContainer.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            mainContainer.Padding = new Padding(10);
            this.Controls.Add(mainContainer);

            // 1. Overlays Grid
            gridOverlays = new FlowLayoutPanel();
            gridOverlays.Size = new Size(365, 200);
            gridOverlays.BackColor = controlBack;
            gridOverlays.AutoScroll = true;
            gridOverlays.Padding = new Padding(5);
            gridOverlays.Margin = new Padding(0, 0, 0, 10);
            mainContainer.Controls.Add(gridOverlays);

            // 2. Main Buttons Row
            FlowLayoutPanel rowButtons = CreateRowLayout();
            btnAdd = CreateModernButton("Add GIF", Point.Empty, new Size(180, 35), accentColor);
            btnAdd.Click += BtnAdd_Click;
            btnRemove = CreateModernButton("Remove", Point.Empty, new Size(180, 35), Color.FromArgb(200, 50, 50));
            btnRemove.Click += BtnRemove_Click;
            rowButtons.Controls.Add(btnAdd);
            rowButtons.Controls.Add(btnRemove);
            mainContainer.Controls.Add(rowButtons);

            // 3. Height Row
            FlowLayoutPanel rowHeight = CreateRowLayout();
            lblHeight = new Label { Text = "Overlay Height (px):", AutoSize = true, Margin = new Padding(0, 7, 0, 0) };
            heightInput = new NumericUpDown { Size = new Size(175, 25), BackColor = controlBack, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Minimum = 10, Maximum = MaxGifHeight, Value = GifHeight };
            heightInput.ValueChanged += HeightInput_ValueChanged;
            rowHeight.Controls.Add(lblHeight);
            rowHeight.Controls.Add(heightInput);
            mainContainer.Controls.Add(rowHeight);

            // 4. System Checks
            TaskbarHeightCheck = new CheckBox { Text = "Respect taskbar", Checked = true, AutoSize = true, Margin = new Padding(0, 5, 0, 5) };
            TaskbarHeightCheck.CheckedChanged += TaskbarHeightCheck_CheckedChanged;
            mainContainer.Controls.Add(TaskbarHeightCheck);

            chkLockOverlays = new CheckBox { Text = "Lock positions (Click-through)", Checked = true, AutoSize = true, Margin = new Padding(0, 0, 0, 5) };
            chkLockOverlays.CheckedChanged += ChkLockOverlays_CheckedChanged;
            mainContainer.Controls.Add(chkLockOverlays);

            chkAlwaysOnTop = new CheckBox { Text = "Always on Top", Checked = true, AutoSize = true, Margin = new Padding(0, 0, 0, 10) };
            chkAlwaysOnTop.CheckedChanged += ChkAlwaysOnTop_CheckedChanged;
            mainContainer.Controls.Add(chkAlwaysOnTop);

            // 5. Screen Row
            FlowLayoutPanel rowScreen = CreateRowLayout();
            lblScreen = new Label { Text = "Monitor (Screen):", AutoSize = true, Margin = new Padding(0, 7, 0, 0) };
            screenSelector = new ComboBox { Size = new Size(175, 25), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = controlBack, ForeColor = Color.White, Enabled = false };
            screenSelector.SelectedIndexChanged += ScreenSelector_SelectedIndexChanged;
            rowScreen.Controls.Add(lblScreen);
            rowScreen.Controls.Add(screenSelector);
            mainContainer.Controls.Add(rowScreen);

            // 6. Chroma Key Row
            chkChromaKey = new CheckBox { Text = "Chroma Key (Remove background)", AutoSize = true, Enabled = false, Margin = new Padding(0, 5, 0, 5) };
            chkChromaKey.CheckedChanged += ChkChromaKey_CheckedChanged;
            mainContainer.Controls.Add(chkChromaKey);

            FlowLayoutPanel rowChromaTools = CreateRowLayout();
            btnChromaColor = CreateModernButton("Color", Point.Empty, new Size(115, 25), Color.Black);
            btnChromaColor.Enabled = false;
            btnChromaColor.Click += BtnChromaColor_Click;
            
            Button btnEyedropper = CreateModernButton("Eyedropper", Point.Empty, new Size(115, 25), Color.FromArgb(60, 60, 60));
            btnEyedropper.Name = "btnEyedropper";
            btnEyedropper.Enabled = false;
            btnEyedropper.Click += BtnEyedropper_Click;

            rowChromaTools.Controls.Add(btnChromaColor);
            rowChromaTools.Controls.Add(btnEyedropper);
            mainContainer.Controls.Add(rowChromaTools);

            // 7. Tolerance Row
            FlowLayoutPanel rowTolerance = CreateRowLayout();
            Label lblTolerance = new Label { Text = "Tolerance: 30", Name = "lblTolerance", ForeColor = Color.White, Font = new Font("Segoe UI", 8F), AutoSize = true, Margin = new Padding(0, 5, 0, 0) };
            TrackBar trackTolerance = new TrackBar { Name = "trackTolerance", Minimum = 10, Maximum = 150, Value = 30, TickFrequency = 20, Size = new Size(240, 30), Enabled = false };
            trackTolerance.ValueChanged += TrackTolerance_ValueChanged;
            rowTolerance.Controls.Add(lblTolerance);
            rowTolerance.Controls.Add(trackTolerance);
            mainContainer.Controls.Add(rowTolerance);

            // 9. Presets (Import/Export)
            FlowLayoutPanel rowPresets = CreateRowLayout();
            Button btnSavePreset = CreateModernButton("Export Preset", Point.Empty, new Size(180, 30), Color.FromArgb(60, 60, 60));
            btnSavePreset.Click += (s, e) => ExportPreset();
            Button btnLoadPreset = CreateModernButton("Import Preset", Point.Empty, new Size(180, 30), Color.FromArgb(60, 60, 60));
            btnLoadPreset.Click += (s, e) => ImportPreset();
            rowPresets.Controls.Add(btnSavePreset);
            rowPresets.Controls.Add(btnLoadPreset);
            mainContainer.Controls.Add(rowPresets);

            // 10. Help
            Label lblHelp = new Label { Text = "Tip: Right-click on an unlocked GIF to close it.", ForeColor = Color.Gray, Font = new Font("Segoe UI", 8F), AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
            mainContainer.Controls.Add(lblHelp);

            colorDialog = new ColorDialog();

            // --- Drag & Drop Support ---
            this.AllowDrop = true;
            this.DragEnter += Form1_DragEnter;
            this.DragDrop += Form1_DragDrop;
            gridOverlays.AllowDrop = true;
            gridOverlays.DragEnter += Form1_DragEnter;
            gridOverlays.DragDrop += Form1_DragDrop;

            //this.Size = new Size(400, 550); // Removed for AutoSize

            trayIcon = new NotifyIcon();
            if (trayIcon != null)
            {
                trayIcon.Text = "Gestor de Overlays GIF";
                trayIcon.Visible = true;
            }

            try
            {
                if (trayIcon != null) trayIcon.Icon = new Icon("tray_icon.ico");
            }
            catch
            {
                using (Bitmap bmp = new Bitmap(16, 16))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Cyan);
                    g.FillEllipse(Brushes.Magenta, 2, 2, 12, 12);
                    if (trayIcon != null) trayIcon.Icon = Icon.FromHandle(bmp.GetHicon());
                }
            }

            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Mostrar ventana", null, (s, e) => this.ShowWindow());
            trayMenu.Items.Add("Agregar GIF", null, (s, e) => BtnAdd_Click(null, EventArgs.Empty));
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add($"Bloquear overlays: {(IsLocked ? "ON" : "OFF")}", null,
                (s, e) => { chkLockOverlays.Checked = !chkLockOverlays.Checked; });
            trayMenu.Items.Add("Pausar visualización", null, (s, e) => TogglePause());
            trayMenu.Items.Add("Salir", null, (s, e) => ExitApplication());

            if (trayIcon != null) trayIcon.ContextMenuStrip = trayMenu;

            if (trayIcon != null) trayIcon.DoubleClick += (s, e) => this.ShowWindow();
            this.Resize += (s, e) =>
            {
                if (this.WindowState == FormWindowState.Minimized)
                {
                    ClearSelection();
                    this.Hide();
                }
            };

            this.Size = new Size(400, 500);

            this.FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    ClearSelection();
                    this.WindowState = FormWindowState.Minimized;
                    this.Hide();
                }
                else if (e.CloseReason == CloseReason.ApplicationExitCall ||
                         e.CloseReason == CloseReason.WindowsShutDown)
                {
                    CleanupTrayIcon();
                }
            };

            LoadConfig();
        }

        private string ConfigPath => Path.Combine(Application.StartupPath, ConfigFile);

        public void SaveConfig(string? customPath = null)
        {
            string path = customPath ?? ConfigPath;
            try
            {
                currentSettings.Overlays = overlays.Select(o => new OverlayConfig
                {
                    FilePath = o.GifFilePath,
                    X = o.Location.X,
                    Y = o.Location.Y,
                    Orientation = o.Orientation,
                    UseChromaKey = o.UseChromaKey,
                    ChromaKeyColorHex = ColorTranslator.ToHtml(o.ChromaKeyColor),
                    ChromaKeyTolerance = o.ChromaKeyTolerance,
                    ScreenIndex = o.ScreenIndex
                }).ToList();
                currentSettings.GifHeight = GifHeight;
                currentSettings.RespectTaskbar = TaskbarHeightCheck.Checked;
                currentSettings.IsLocked = IsLocked;
                currentSettings.AlwaysOnTop = chkAlwaysOnTop.Checked;

                string json = JsonConvert.SerializeObject(currentSettings, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                if (customPath != null) MessageBox.Show("Error saving preset: " + ex.Message);
            }
        }

        private void LoadConfig(string? customPath = null)
        {
            string path = customPath ?? ConfigPath;
            if (!File.Exists(path)) return;

            try
            {
                string json = File.ReadAllText(path);
                currentSettings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();

                if (customPath != null)
                {
                    while (overlays.Count > 0)
                    {
                        overlays[0].Close();
                        overlays.RemoveAt(0);
                    }
                }

                GifHeight = currentSettings.GifHeight;
                if (heightInput != null) heightInput.Value = Math.Min(Math.Max(GifHeight, (decimal)heightInput.Minimum), (decimal)heightInput.Maximum);

                TaskbarHeightCheck.Checked = currentSettings.RespectTaskbar;
                taskbarHeight = TaskbarHeightCheck.Checked
                    ? ((Screen.PrimaryScreen?.Bounds.Height ?? 0) - (Screen.PrimaryScreen?.WorkingArea.Height ?? 0))
                    : 0;
                baseY = screenHeight - taskbarHeight - GifHeight;

                foreach (var config in currentSettings.Overlays)
                {
                    if (File.Exists(config.FilePath))
                    {
                        var overlay = new OverlayForm(config.FilePath, GifHeight);
                        overlay.Orientation = config.Orientation;
                        overlay.UseChromaKey = config.UseChromaKey;
                        overlay.ChromaKeyColor = ColorTranslator.FromHtml(config.ChromaKeyColorHex);
                        overlay.ChromaKeyTolerance = config.ChromaKeyTolerance;
                        overlay.ScreenIndex = config.ScreenIndex;
                        overlay.Show();
                        overlay.UpdateWindowSize();
                        overlay.Location = new Point(config.X, config.Y);
                        overlay.SetLocked(currentSettings.IsLocked);
                        overlay.RequestRemove += Overlay_RequestRemove;
                        overlays.Add(overlay);
                    }
                }

                IsLocked = currentSettings.IsLocked;
                chkLockOverlays.Checked = IsLocked;

                chkAlwaysOnTop.Checked = currentSettings.AlwaysOnTop;
                foreach (var overlay in overlays)
                    overlay.SetZOrder(currentSettings.AlwaysOnTop);

                UpdateTrayIcon();
            }
            catch (Exception ex)
            {
                if (customPath != null) MessageBox.Show("Error loading preset: " + ex.Message);
            }
        }

        private void UpdateSelectionState()
        {
            if (overlays == null) return;

            Color accentColor = Color.FromArgb(0, 120, 212);
            Color controlBack = Color.FromArgb(45, 45, 45);

            for (int i = 0; i < overlays.Count; i++)
            {
                bool selected = (selectedIndex == i);
                if (overlays[i].IsSelected != selected)
                {
                    overlays[i].IsSelected = selected;
                    overlays[i].RefreshTransparency();
                }
                
                if (gridOverlays.Controls.Count > i)
                {
                    var item = gridOverlays.Controls[i] as GifThumbnailItem;
                    item?.UpdateSelection(selected, accentColor, controlBack);
                }
            }

            isUpdatingUI = true;
            if (selectedIndex >= 0 && selectedIndex < overlays.Count)
            {
                var selectedOverlay = overlays[selectedIndex];

                // Screen selector
                screenSelector.Enabled = true;
                screenSelector.Items.Clear();
                for (int i = 0; i < Screen.AllScreens.Length; i++)
                {
                    screenSelector.Items.Add($"Monitor {i + 1} ({Screen.AllScreens[i].Bounds.Width}x{Screen.AllScreens[i].Bounds.Height})");
                }
                if (selectedOverlay.ScreenIndex >= 0 && selectedOverlay.ScreenIndex < screenSelector.Items.Count)
                    screenSelector.SelectedIndex = selectedOverlay.ScreenIndex;
                else
                    screenSelector.SelectedIndex = 0;

                chkChromaKey.Enabled = true;
                chkChromaKey.Checked = selectedOverlay.UseChromaKey;

                btnChromaColor.Enabled = true;
                btnChromaColor.BackColor = selectedOverlay.ChromaKeyColor;

                if (this.Controls["btnEyedropper"] is Button btnEyedropper)
                    btnEyedropper.Enabled = true;

                if (this.Controls["trackTolerance"] is TrackBar track)
                {
                    track.Enabled = selectedOverlay.UseChromaKey;
                    track.Value = Math.Max(track.Minimum, Math.Min(track.Maximum, selectedOverlay.ChromaKeyTolerance));
                }
                if (this.Controls["lblTolerance"] is Label lbl)
                    lbl.Text = $"Tolerance: {selectedOverlay.ChromaKeyTolerance}";
            }
            else
            {
                screenSelector.Enabled = false;
                chkChromaKey.Enabled = false;
                btnChromaColor.Enabled = false;

                if (this.Controls["btnEyedropper"] is Button btnEyedropper)
                    btnEyedropper.Enabled = false;

                if (this.Controls["trackTolerance"] is TrackBar track)
                    track.Enabled = false;
            }

            isUpdatingUI = false;
        }

        private void ClearSelection()
        {
            selectedIndex = -1;
            UpdateSelectionState();
        }

        private void AddGridItem(string filePath)
        {
            int index = gridOverlays.Controls.Count;
            var item = new GifThumbnailItem(filePath, index, Color.FromArgb(0, 120, 212), Color.FromArgb(45, 45, 45));
            item.Click += (s, e) =>
            {
                selectedIndex = (s as GifThumbnailItem)?.ItemIndex ?? -1;
                UpdateSelectionState();
            };
            gridOverlays.Controls.Add(item);
        }

        private void RebuildGrid()
        {
            gridOverlays.Controls.Clear();
            for (int i = 0; i < overlays.Count; i++)
            {
                AddGridItem(overlays[i].GifFilePath);
            }
            UpdateSelectionState();
        }

        private void ScreenSelector_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (isUpdatingUI) return;
            if (selectedIndex >= 0 && selectedIndex < overlays.Count && screenSelector.SelectedIndex >= 0)
            {
                var selectedOverlay = overlays[selectedIndex];
                int newScreenIndex = screenSelector.SelectedIndex;

                if (newScreenIndex < Screen.AllScreens.Length && newScreenIndex != selectedOverlay.ScreenIndex)
                {
                    Screen newScreen = Screen.AllScreens[newScreenIndex];
                    selectedOverlay.ScreenIndex = newScreenIndex;

                    // Move to center of new screen
                    int centerX = newScreen.Bounds.Left + (newScreen.Bounds.Width - selectedOverlay.Width) / 2;
                    int centerY = newScreen.Bounds.Top + (newScreen.Bounds.Height - selectedOverlay.Height) / 2;
                    selectedOverlay.Location = new Point(centerX, centerY);

                    SaveConfig();
                }
            }
        }

        private void ChkLockOverlays_CheckedChanged(object? sender, EventArgs e)
        {
            IsLocked = chkLockOverlays.Checked;

            foreach (var overlay in overlays)
                overlay.SetLocked(IsLocked);

            if (trayMenu != null && trayMenu.Items.Count > 3)
                trayMenu.Items[3].Text = $"Bloquear overlays: {(IsLocked ? "ON" : "OFF")}";

            UpdateTrayIcon();
            SaveConfig();
        }

        private void ChkChromaKey_CheckedChanged(object? sender, EventArgs e)
        {
            if (isUpdatingUI) return;
            if (selectedIndex >= 0 && selectedIndex < overlays.Count)
            {
                overlays[selectedIndex].UseChromaKey = chkChromaKey.Checked;
                overlays[selectedIndex].RefreshTransparency();

                if (this.Controls["trackTolerance"] is TrackBar track)
                    track.Enabled = chkChromaKey.Checked;

                SaveConfig();
            }
        }

        private void BtnChromaColor_Click(object? sender, EventArgs e)
        {
            if (selectedIndex >= 0 && selectedIndex < overlays.Count)
            {
                colorDialog.Color = overlays[selectedIndex].ChromaKeyColor;
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    overlays[selectedIndex].ChromaKeyColor = colorDialog.Color;
                    btnChromaColor.BackColor = colorDialog.Color;
                    overlays[selectedIndex].RefreshTransparency();
                    SaveConfig();
                }
            }
        }

        private void BtnEyedropper_Click(object? sender, EventArgs e)
        {
            if (selectedIndex >= 0 && selectedIndex < overlays.Count)
            {
                var overlay = overlays[selectedIndex];

                // Temporarily unlock to allow clicking
                int style = GetWindowLong(overlay.Handle, -20);
                SetWindowLong(overlay.Handle, -20, style & ~0x20);

                overlay.Cursor = Cursors.Cross;
                overlay.IsEyedropperMode = true;
                overlay.BringToFront();

                // Subscribe to color picked event (one-time)
                Action<Color>? handler = null;
                handler = (pickedColor) =>
                {
                    overlay.ChromaKeyColor = pickedColor;
                    btnChromaColor.BackColor = pickedColor;
                    overlay.RefreshTransparency();
                    SaveConfig();

                    // Restore locked state
                    if (IsLocked)
                    {
                        int s = GetWindowLong(overlay.Handle, -20);
                        SetWindowLong(overlay.Handle, -20, s | 0x20);
                    }

                    overlay.ColorPicked -= handler;
                };
                overlay.ColorPicked += handler;
            }
        }

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private void TrackTolerance_ValueChanged(object? sender, EventArgs e)
        {
            if (isUpdatingUI) return;
            if (selectedIndex >= 0 && selectedIndex < overlays.Count && sender is TrackBar t)
            {
                overlays[selectedIndex].ChromaKeyTolerance = t.Value;
                overlays[selectedIndex].RefreshTransparency();
                if (this.Controls.Find("lblTolerance", true).FirstOrDefault() is Label lbl)
                    lbl.Text = $"Tolerance: {t.Value}";
                SaveConfig();
            }
        }

        private void TaskbarHeightCheck_CheckedChanged(object? sender, EventArgs e)
        {
            taskbarHeight = TaskbarHeightCheck.Checked
                ? ((Screen.PrimaryScreen?.Bounds.Height ?? 0) - (Screen.PrimaryScreen?.WorkingArea.Height ?? 0))
                : 0;

            baseY = screenHeight - taskbarHeight - GifHeight;

            foreach (var overlay in overlays)
            {
                if (overlay.Orientation == OverlayOrientation.Inferior)
                {
                    overlay.Location = new Point(overlay.Location.X, baseY);
                }
            }

            SaveConfig();
        }

        private void UpdateAllOverlaysPosition()
        {
            int nextX_Inf = 0, nextX_Sup = 0;
            int nextY_Izq = 0, nextY_Der = 0;

            foreach (var overlay in overlays)
            {
                switch (overlay.Orientation)
                {
                    case OverlayOrientation.Inferior:
                        overlay.Location = new Point(nextX_Inf, baseY);
                        nextX_Inf += overlay.Width;
                        if (nextX_Inf + overlay.Width > screenWidth) nextX_Inf = 0;
                        break;
                    case OverlayOrientation.Superior:
                        overlay.Location = new Point(nextX_Sup, 0);
                        nextX_Sup += overlay.Width;
                        if (nextX_Sup + overlay.Width > screenWidth) nextX_Sup = 0;
                        break;
                    case OverlayOrientation.Izquierda:
                        overlay.Location = new Point(0, nextY_Izq);
                        nextY_Izq += overlay.Height;
                        if (nextY_Izq + overlay.Height > screenHeight) nextY_Izq = 0;
                        break;
                    case OverlayOrientation.Derecha:
                        overlay.Location = new Point(screenWidth - overlay.Width, nextY_Der);
                        nextY_Der += overlay.Height;
                        if (nextY_Der + overlay.Height > screenHeight) nextY_Der = 0;
                        break;
                }
            }
        }

        private void HeightInput_ValueChanged(object? sender, EventArgs e)
        {
            GifHeight = (int)heightInput.Value;
            baseY = screenHeight - taskbarHeight - GifHeight;

            foreach (var overlay in overlays)
            {
                overlay.UpdateWindowSize();

                switch (overlay.Orientation)
                {
                    case OverlayOrientation.Inferior:
                        overlay.Location = new Point(overlay.Location.X, baseY);
                        break;
                    case OverlayOrientation.Superior:
                        overlay.Location = new Point(overlay.Location.X, 0);
                        break;
                    case OverlayOrientation.Derecha:
                        overlay.Location = new Point(screenWidth - overlay.Width, overlay.Location.Y);
                        break;
                    case OverlayOrientation.Izquierda:
                        overlay.Location = new Point(0, overlay.Location.Y);
                        break;
                }

                overlay.RefreshTransparency();
            }

            SaveConfig();
        }

        private void AddOverlay(string gifPath)
        {
            try
            {
                OverlayForm newOverlay = new OverlayForm(gifPath, GifHeight);
                
                int nextX = 0;
                foreach (var o in overlays)
                {
                    if (o != newOverlay && o.Orientation == OverlayOrientation.Inferior)
                    {
                        if (o.Location.X + o.Width > nextX) nextX = o.Location.X + o.Width;
                    }
                }

                if (nextX + newOverlay.Width > screenWidth) nextX = 0;

                newOverlay.Location = new Point(nextX, baseY);
                newOverlay.SetLocked(IsLocked);
                newOverlay.SetZOrder(chkAlwaysOnTop.Checked);
                overlays.Add(newOverlay);
                newOverlay.RequestRemove += Overlay_RequestRemove;
                AddGridItem(gifPath);

                newOverlay.UpdateWindowSize();
                newOverlay.RefreshTransparency();
                if (isPaused) newOverlay.Hide();
                else newOverlay.Show();

                SaveConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading GIF: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ChkAlwaysOnTop_CheckedChanged(object? sender, EventArgs e)
        {
            foreach (var overlay in overlays)
            {
                overlay.SetZOrder(chkAlwaysOnTop.Checked);
            }
            SaveConfig();
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Supported Images|*.gif;*.webp;*.png;*.jpg;*.jpeg|GIF|*.gif|WebP|*.webp|All Files|*.*";
                ofd.Title = "Select Overlay Image";
                ofd.Multiselect = true;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    foreach (string file in ofd.FileNames)
                    {
                        AddOverlay(file);
                    }
                }
            }
        }

        private void BtnRemove_Click(object? sender, EventArgs e)
        {
            if (selectedIndex >= 0)
            {
                if (MessageBox.Show("Are you sure you want to remove the selected overlay?", "Confirm Removal",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    int index = selectedIndex;
                    OverlayForm toRemove = overlays[index];
                    toRemove.Close();
                    overlays.RemoveAt(index);
                    
                    selectedIndex = -1;
                    RebuildGrid();
                    SaveConfig();
                }
            }
        }

        private void Overlay_RequestRemove(object? sender, EventArgs e)
        {
            if (sender is OverlayForm overlay)
            {
                int index = overlays.IndexOf(overlay);
                if (index >= 0)
                {
                    overlay.Close();
                    overlays.RemoveAt(index);
                    
                    if (selectedIndex == index) selectedIndex = -1;
                    else if (selectedIndex > index) selectedIndex--;

                    RebuildGrid();
                    SaveConfig();
                }
            }
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
            this.Activate();
        }

        private void TogglePause()
        {
            isPaused = !isPaused;
            foreach (var overlay in overlays)
            {
                overlay.SetPaused(isPaused);
            }

            if (trayMenu?.Items.Count > 4)
                trayMenu.Items[4].Text = isPaused ? "Resume visualization" : "Pause visualization";
        }

        public class GifThumbnailItem : System.Windows.Forms.Panel
        {
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public int ItemIndex { get; set; }
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public string GifPath { get; set; }
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public bool IsItemSelected { get; set; }

            private System.Windows.Forms.PictureBox pbPreview;

            public GifThumbnailItem(string filePath, int index, System.Drawing.Color accentColor, System.Drawing.Color backColor)
            {
                this.GifPath = filePath;
                this.ItemIndex = index;
                this.Size = new System.Drawing.Size(100, 100);
                this.Margin = new System.Windows.Forms.Padding(5);
                this.BackColor = backColor;
                this.Cursor = System.Windows.Forms.Cursors.Hand;

                pbPreview = new System.Windows.Forms.PictureBox();
                pbPreview.Size = new System.Drawing.Size(80, 80);
                pbPreview.Location = new System.Drawing.Point(10, 10);
                pbPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
                pbPreview.BackColor = System.Drawing.Color.Transparent;
                try
                {
                    string ext = System.IO.Path.GetExtension(filePath).ToLower();
                    if (ext == ".webp")
                    {
                        using (var image = ISImage.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(filePath))
                        {
                            using (var ms = new MemoryStream())
                            {
                                image.Frames.CloneFrame(0).SaveAsPng(ms);
                                ms.Seek(0, SeekOrigin.Begin);
                                pbPreview.Image = new System.Drawing.Bitmap(ms);
                            }
                        }
                    }
                    else
                    {
                        using (System.Drawing.Image img = System.Drawing.Image.FromFile(filePath))
                        {
                            pbPreview.Image = new System.Drawing.Bitmap(img);
                        }
                    }
                }
                catch { }
                this.Controls.Add(pbPreview);

                pbPreview.Click += (s, e) => this.OnClick(e);
            }

            public void UpdateSelection(bool selected, System.Drawing.Color accentColor, System.Drawing.Color backColor)
            {
                IsItemSelected = selected;
                this.BackColor = selected ? accentColor : backColor;
            }
        }

        private void CleanupTrayIcon()
        {
            if (trayIcon != null)
            {
                trayIcon.DoubleClick -= (s, e) => this.ShowWindow();
                trayIcon.ContextMenuStrip = null;
                trayIcon.Visible = false;
                trayIcon.Icon?.Dispose();
                trayIcon.Dispose();
                trayIcon = null;
            }

            if (trayMenu != null)
            {
                trayMenu.Dispose();
                trayMenu = null;
            }
        }


        private void ExportPreset()
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Preset JSON|*.json";
                sfd.Title = "Export Overlay Preset";
                sfd.FileName = "my_overlay_preset.json";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    SaveConfig(sfd.FileName);
                    MessageBox.Show("Preset exported successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ImportPreset()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Preset JSON|*.json";
                ofd.Title = "Import Overlay Preset";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    LoadConfig(ofd.FileName);
                    isUpdatingUI = true;
                    UpdateSelectionState();
                    RebuildGrid();
                    isUpdatingUI = false;
                    SaveConfig(); // Guardar como configuración principal
                }
            }
        }

        private void Form1_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
                bool hasSupported = files.Any(f => {
                    string ext = Path.GetExtension(f).ToLower();
                    return ext == ".gif" || ext == ".webp";
                });
                
                if (hasSupported)
                    e.Effect = DragDropEffects.Copy;
                else
                    e.Effect = DragDropEffects.None;
            }
        }

        private void Form1_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
                foreach (string file in files)
                {
                    string ext = Path.GetExtension(file).ToLower();
                    if (ext == ".gif" || ext == ".webp")
                    {
                        AddOverlay(file);
                    }
                }
            }
        }

        private void ExitApplication()
        {
            SaveConfig();
            while (overlays.Count > 0)
            {
                var overlay = overlays[0];
                overlay.RequestRemove -= Overlay_RequestRemove;
                overlay.Close();
                overlays.RemoveAt(0);
            }

            overlays.Clear();
            RebuildGrid();


            CleanupTrayIcon();

            Application.Exit();

            Application.ApplicationExit += (s, e) => CleanupTrayIcon();
        }

        private FlowLayoutPanel CreateRowLayout()
        {
            return new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 5)
            };
        }

        private Button CreateModernButton(string text, Point location, Size size, Color backColor)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Location = location;
            btn.Size = size;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = backColor;
            btn.ForeColor = Color.White;
            btn.Cursor = Cursors.Hand;
            btn.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            btn.MouseEnter += (s, e) => btn.BackColor = ControlPaint.Light(backColor);
            btn.MouseLeave += (s, e) => btn.BackColor = backColor;

            return btn;
        }

        private void UpdateTrayIcon()
        {
            try
            {
                using (Bitmap bmp = new Bitmap(16, 16))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(IsLocked ? Color.Red : Color.LimeGreen);
                    g.FillEllipse(Brushes.White, 4, 4, 8, 8);
                    if (IsLocked) g.FillEllipse(Brushes.Red, 5, 5, 6, 6);
                    trayIcon?.Icon?.Dispose();
                    if (trayIcon != null) trayIcon.Icon = Icon.FromHandle(bmp.GetHicon());
                }
            }
            catch
            {
            }
        }
    }

#pragma warning disable CA1416, CS8618

    public class OverlayForm : Form
    {
        private Point mouseOffset;
        private bool isMouseDown = false;
        private bool _isLocked = true;

        public event EventHandler? RequestRemove;
        public string GifFilePath { get; private set; } = "";

        private OverlayOrientation _orientation = OverlayOrientation.Inferior;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public OverlayOrientation Orientation 
        { 
            get => _orientation;
            set 
            {
                if (_orientation != value)
                {
                    _orientation = value;
                    UpdateWindowSize();
                    RefreshTransparency();
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsSelected { get; set; } = false;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int ScreenIndex { get; set; } = 0;

        private System.Windows.Forms.Timer animationTimer = null!;
        private SDImage? originalImage;
        private bool isWebP;
        private List<Bitmap> webpFrames = new List<Bitmap>();
        private List<int> webpDelays = new List<int>();
        private int currentFrameIndex = 0;
        private float _aspectRatio = 1f;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool UseChromaKey { get; set; } = false;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ChromaKeyColor { get; set; } = Color.Black;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int ChromaKeyTolerance { get; set; } = 30;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsEyedropperMode { get; set; } = false;
        public event Action<Color>? ColorPicked;

        public void SetZOrder(bool topMost)
        {
            this.TopMost = topMost;
            if (!topMost)
            {
                this.SendToBack();
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80000;
                return cp;
            }
        }

        public OverlayForm(string gifPath, int height)
        {
            GifFilePath = gifPath;
            var forceHandle = this.Handle; // Force window handle creation to allow early configuration
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.BackColor = Color.Black;

            string ext = Path.GetExtension(gifPath).ToLower();
            isWebP = (ext == ".webp");

            if (isWebP)
            {
                LoadWebP(gifPath, height);
            }
            else
            {
                originalImage = SDImage.FromFile(gifPath);
                _aspectRatio = (float)originalImage.Width / originalImage.Height;
                UpdateSize(height);
                ImageAnimator.Animate(originalImage, (o, ev) => { });
            }

            animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = isWebP && webpDelays.Count > 0 ? webpDelays[0] : 30;
            animationTimer.Tick += (s, e) =>
            {
                if (isWebP)
                {
                    if (webpFrames.Count > 0)
                    {
                        currentFrameIndex = (currentFrameIndex + 1) % webpFrames.Count;
                        animationTimer.Interval = webpDelays[currentFrameIndex];
                        MakeTransparent();
                    }
                }
                else if (originalImage != null)
                {
                    ImageAnimator.UpdateFrames(originalImage);
                    MakeTransparent();
                }
            };

            animationTimer.Start();

            this.MouseDown += (s, e) =>
            {
                if (!_isLocked && e.Button == MouseButtons.Left)
                {
                    isMouseDown = true;
                    mouseOffset = new Point(-e.X, -e.Y);
                }
            };
            this.MouseMove += (s, e) =>
            {
                if (isMouseDown)
                {
                    Point mousePos = Control.MousePosition;
                    mousePos.Offset(mouseOffset.X, mouseOffset.Y);
                    this.Location = ApplySnap(mousePos, this.Size);
                }
            };
            this.MouseUp += (s, e) => 
            {
                if (isMouseDown)
                {
                    isMouseDown = false;
                    Form1.Instance?.SaveConfig();
                }
            };
            this.MouseClick += (s, e) =>
            {
                if (IsEyedropperMode && e.Button == MouseButtons.Left)
                {
                    // Calculate position in original image coordinates
                    float scaleX = (float)(isWebP && webpFrames.Count > 0 ? webpFrames[0].Width : (originalImage?.Width ?? 1)) / this.ClientSize.Width;
                    float scaleY = (float)(isWebP && webpFrames.Count > 0 ? webpFrames[0].Height : (originalImage?.Height ?? 1)) / this.ClientSize.Height;
                    int imgX = (int)(e.X * scaleX);
                    int imgY = (int)(e.Y * scaleY);

                    Color picked = GetColorAt(imgX, imgY);
                    IsEyedropperMode = false;
                    this.Cursor = Cursors.Default;
                    ColorPicked?.Invoke(picked);
                    return;
                }
                if (e.Button == MouseButtons.Right && !_isLocked)
                    RequestRemove?.Invoke(this, EventArgs.Empty);
            };

            this.Load += (s, e) => MakeTransparent();
        }

        private void LoadWebP(string path, int height)
        {
            try
            {
                using (var image = ISImage.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(path))
                {
                    _aspectRatio = (float)image.Width / image.Height;
                    UpdateSize(height);

                    for (int i = 0; i < image.Frames.Count; i++)
                    {
                        var frame = image.Frames[i];
                        var frameMetadata = frame.Metadata.GetWebpMetadata();
                        int delay = (int)frameMetadata.FrameDelay;
                        if (delay <= 0) delay = 100;
                        webpDelays.Add(delay);

                        using (var ms = new MemoryStream())
                        {
                            using (var frameImage = image.Frames.CloneFrame(i))
                            {
                                frameImage.SaveAsPng(ms);
                            }
                            ms.Seek(0, SeekOrigin.Begin);
                            webpFrames.Add(new Bitmap(ms));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading WebP: {ex.Message}");
            }
        }

        public void UpdateSize(int height)
        {
            if (Orientation == OverlayOrientation.Derecha || Orientation == OverlayOrientation.Izquierda)
                this.ClientSize = new Size(height, (int)(height * _aspectRatio));
            else
                this.ClientSize = new Size((int)(height * _aspectRatio), height);
        }

        public void UpdateWindowSize() => UpdateSize(Form1.GifHeight);

        private Point ApplySnap(Point desired, Size size)
        {
            int nx = desired.X, ny = desired.Y;
            Screen scr = Screen.FromPoint(desired);
            
            int threshold = 40; // Increased threshold for better snapping feeling
            
            Rectangle boundary = Form1.Instance.RespectTaskbarSetting ? scr.WorkingArea : scr.Bounds;
            
            bool snappedLeft = Math.Abs(nx - boundary.Left) < threshold;
            bool snappedRight = Math.Abs(nx + size.Width - boundary.Right) < threshold;
            bool snappedTop = Math.Abs(ny - boundary.Top) < threshold;
            bool snappedBottom = Math.Abs(ny + size.Height - boundary.Bottom) < threshold;

            if (snappedLeft) 
            { 
                nx = boundary.Left; 
                this.Orientation = OverlayOrientation.Izquierda; 
            }
            else if (snappedRight) 
            { 
                nx = boundary.Right - size.Width; 
                this.Orientation = OverlayOrientation.Derecha; 
            }
            else if (snappedTop) 
            { 
                ny = boundary.Top; 
                this.Orientation = OverlayOrientation.Superior; 
            }
            else if (snappedBottom) 
            { 
                ny = boundary.Bottom - size.Height; 
                this.Orientation = OverlayOrientation.Inferior; 
            }

            return new Point(nx, ny);
        }

        public void RefreshTransparency() => MakeTransparent();

        public void SetLocked(bool locked)
        {
            _isLocked = locked;
            int style = GetWindowLong(this.Handle, -20);
            if (locked) style |= 0x20;
            else style &= ~0x20;
            SetWindowLong(this.Handle, -20, style);
            RefreshTransparency();
        }

        public void SetPaused(bool paused)
        {
            if (paused)
            {
                animationTimer.Stop();
                this.Hide();
            }
            else
            {
                animationTimer.Start();
                this.Show();
                MakeTransparent();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            animationTimer?.Stop();
            animationTimer?.Dispose();
            originalImage?.Dispose();
            foreach (var bmp in webpFrames) bmp.Dispose();
            webpFrames.Clear();
            base.OnFormClosed(e);
        }

        public Color GetColorAt(int x, int y)
        {
            Bitmap? target = null;
            if (isWebP && webpFrames.Count > 0)
                target = webpFrames[0];
            else if (originalImage is Bitmap bmp)
                target = bmp;
            else if (originalImage != null)
                target = new Bitmap(originalImage);

            if (target == null || x < 0 || y < 0 || x >= target.Width || y >= target.Height)
                return Color.Black;

            return target.GetPixel(x, y);
        }

        private void MakeTransparent()
        {
            if (this.IsDisposed) return;
            bool hasContent = isWebP ? (webpFrames.Count > 0) : (originalImage != null);
            if (!hasContent) return;
            int w = this.ClientSize.Width, h = this.ClientSize.Height;
            if (w <= 0 || h <= 0) return;

            using (Bitmap bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                    Bitmap? frameToDraw = null;
                    if (isWebP)
                    {
                        if (webpFrames.Count > currentFrameIndex)
                            frameToDraw = webpFrames[currentFrameIndex];
                    }
                    
                    SDImage? imageToDraw = isWebP ? (SDImage?)frameToDraw : originalImage;
                    if (imageToDraw == null) return;

                    switch (Orientation)
                    {
                        case OverlayOrientation.Izquierda:
                            g.TranslateTransform(w, 0);
                            g.RotateTransform(90);
                            g.DrawImage(imageToDraw, 0, 0, h, w);
                            break;
                        case OverlayOrientation.Superior:
                            g.TranslateTransform(w, h);
                            g.RotateTransform(180);
                            g.DrawImage(imageToDraw, 0, 0, w, h);
                            break;
                        case OverlayOrientation.Derecha:
                            g.TranslateTransform(0, h);
                            g.RotateTransform(270);
                            g.DrawImage(imageToDraw, 0, 0, h, w);
                            break;
                        default:
                            g.DrawImage(imageToDraw, 0, 0, w, h);
                            break;
                    }

                    if (IsSelected)
                    {
                        g.ResetTransform();
                        using (Pen p = new Pen(Color.Lime, 4)) g.DrawRectangle(p, 2, 2, w - 4, h - 4);
                    }
                }

                if (UseChromaKey)
                {
                    ApplyChromaKey(bmp, ChromaKeyColor, ChromaKeyTolerance);
                }

                IntPtr screenDc = GetDC(IntPtr.Zero);
                IntPtr memDc = CreateCompatibleDC(screenDc);
                IntPtr hBmp = bmp.GetHbitmap(Color.FromArgb(0));
                IntPtr oldBmp = SelectObject(memDc, hBmp);

                POINT pSize = new POINT(w, h), pSrc = new POINT(0, 0);
                BLENDFUNCTION blend = new BLENDFUNCTION
                { BlendOp = 0, BlendFlags = 0, SourceConstantAlpha = 255, AlphaFormat = 1 };

                UpdateLayeredWindow(this.Handle, screenDc, IntPtr.Zero, ref pSize, memDc, ref pSrc, 0, ref blend, 2);

                SelectObject(memDc, oldBmp);
                DeleteObject(hBmp);
                DeleteDC(memDc);
                ReleaseDC(IntPtr.Zero, screenDc);
            }
        }

        private void ApplyChromaKey(Bitmap bmp, Color chromaColor, int tolerance)
        {
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int stride = bmpData.Stride;

                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        int idx = y * stride + x * 4;
                        byte b = ptr[idx];
                        byte g = ptr[idx + 1];
                        byte r = ptr[idx + 2];
                        byte a = ptr[idx + 3];

                        // Calculate color distance
                        int dr = r - chromaColor.R;
                        int dg = g - chromaColor.G;
                        int db = b - chromaColor.B;
                        double distance = Math.Sqrt(dr * dr + dg * dg + db * db);

                        if (distance <= tolerance)
                        {
                            // Full transparency for close colors
                            ptr[idx + 3] = 0;
                        }
                        else if (distance <= tolerance * 1.5)
                        {
                            // Graduated transparency for edge colors
                            double factor = (distance - tolerance) / (tolerance * 0.5);
                            ptr[idx + 3] = (byte)(a * factor);
                        }
                    }
                }
            }

            bmp.UnlockBits(bmpData);
        }

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr h, int i);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr h, int i, int v);

        [DllImport("user32.dll")]
        private static extern bool UpdateLayeredWindow(IntPtr h, IntPtr d, IntPtr p, ref POINT s, IntPtr m, ref POINT o,
            int c, ref BLENDFUNCTION b, int f);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr h);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr h, IntPtr d);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr h);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr h, IntPtr o);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr h);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr o);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X, Y;

            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BLENDFUNCTION
        {
            public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat;
        }
    }


    public enum OverlayOrientation
    {
        Superior,
        Inferior,
        Izquierda,
        Derecha
    }

    [Serializable]
    public class OverlayConfig
    {
        public string FilePath { get; set; } = "";
        public int X { get; set; }
        public int Y { get; set; }
        public OverlayOrientation Orientation { get; set; } = OverlayOrientation.Inferior;
        public bool UseChromaKey { get; set; } = false;
        public string ChromaKeyColorHex { get; set; } = "#000000";
        public int ChromaKeyTolerance { get; set; } = 30;
        public int ScreenIndex { get; set; } = 0;

        public OverlayConfig()
        {
        }

        public OverlayConfig(OverlayForm overlay, string path)
        {
            FilePath = path;
            X = overlay.Location.X;
            Y = overlay.Location.Y;
            Orientation = overlay.Orientation;
            UseChromaKey = overlay.UseChromaKey;
            ChromaKeyColorHex = ColorTranslator.ToHtml(overlay.ChromaKeyColor);
            ChromaKeyTolerance = overlay.ChromaKeyTolerance;
            ScreenIndex = overlay.ScreenIndex;
        }
    }

    [Serializable]
    public class AppSettings
    {
        public List<OverlayConfig> Overlays { get; set; } = new List<OverlayConfig>();
        public bool IsLocked { get; set; } = true;
        public bool AlwaysOnTop { get; set; } = true;
        public bool RespectTaskbar { get; set; } = true;
        public int GifHeight { get; set; } = 100;
    }
}