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
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private List<OverlayForm> overlays = new List<OverlayForm>();
        private FlowLayoutPanel gridOverlays;
        private int selectedIndex = -1;
        private Button btnAdd;
        private Button btnRemove;
        private NumericUpDown heightInput;
        private Label lblHeight;
        private CheckBox TaskbarHeightCheck;
        private CheckBox chkLockOverlays;
        private bool IsLocked;
        private bool isPaused = false;
        private const string ConfigFile = "GifOverlayConfig.json";
        private AppSettings currentSettings = new AppSettings();
        private bool isUpdatingUI = false;
        private ColorDialog colorDialog;
        private CheckBox chkChromaKey;
        private Button btnChromaColor;
        private ComboBox screenSelector;
        private Label lblScreen;


        public static int GifHeight { get; private set; } = 100;
        private const int MaxGifHeight = 400;
        private int screenHeight = Screen.PrimaryScreen.Bounds.Height;
        private int screenWidth = Screen.PrimaryScreen.Bounds.Width;
        private int taskbarHeight = Screen.PrimaryScreen.Bounds.Height - Screen.PrimaryScreen.WorkingArea.Height;
        private int baseY;


        public static Form1 Instance { get; private set; }

        public List<OverlayForm> Overlays => overlays;

        public Form1()
        {
            Instance = this;
            baseY = screenHeight - taskbarHeight - GifHeight;

            this.Text = "OwOverlays";
            this.Size = new Size(400, 450);
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            Color controlBack = Color.FromArgb(45, 45, 45);
            Color accentColor = Color.FromArgb(0, 120, 212);

            gridOverlays = new FlowLayoutPanel();
            gridOverlays.Location = new Point(10, 10);
            gridOverlays.Size = new Size(365, 200);
            gridOverlays.BackColor = controlBack;
            gridOverlays.AutoScroll = true;
            gridOverlays.Padding = new Padding(5);
            this.Controls.Add(gridOverlays);

            btnAdd = CreateModernButton("Agregar GIF", new Point(10, 220), new Size(175, 35), accentColor);
            btnAdd.Click += BtnAdd_Click;
            this.Controls.Add(btnAdd);

            btnRemove = CreateModernButton("Eliminar", new Point(200, 220), new Size(175, 35),
                Color.FromArgb(200, 50, 50));
            btnRemove.Click += BtnRemove_Click;
            this.Controls.Add(btnRemove);

            lblHeight = new Label();
            lblHeight.Text = "Altura del Overlay (px):";
            lblHeight.Location = new Point(10, 275);
            lblHeight.Size = new Size(180, 25);
            this.Controls.Add(lblHeight);

            heightInput = new NumericUpDown();
            heightInput.Location = new Point(200, 275);
            heightInput.Size = new Size(175, 25);
            heightInput.BackColor = controlBack;
            heightInput.ForeColor = Color.White;
            heightInput.BorderStyle = BorderStyle.FixedSingle;
            heightInput.Minimum = 10;
            heightInput.Maximum = MaxGifHeight;
            heightInput.Value = GifHeight;
            heightInput.ValueChanged += HeightInput_ValueChanged;
            this.Controls.Add(heightInput);

            TaskbarHeightCheck = new CheckBox();
            TaskbarHeightCheck.Text = "Respetar barra de tareas";
            TaskbarHeightCheck.Checked = true;
            TaskbarHeightCheck.Location = new Point(10, 310);
            TaskbarHeightCheck.Size = new Size(360, 25);
            TaskbarHeightCheck.CheckedChanged += TaskbarHeightCheck_CheckedChanged;
            this.Controls.Add(TaskbarHeightCheck);

            chkLockOverlays = new CheckBox();
            chkLockOverlays.Text = "Bloquear posiciones (Click-through)";
            chkLockOverlays.Checked = true;
            chkLockOverlays.Location = new Point(10, 340);
            chkLockOverlays.Size = new Size(365, 25);
            chkLockOverlays.CheckedChanged += ChkLockOverlays_CheckedChanged;
            this.Controls.Add(chkLockOverlays);

            lblScreen = new Label();
            lblScreen.Text = "Pantalla (Monitor):";
            lblScreen.Location = new Point(10, 375);
            lblScreen.Size = new Size(180, 25);
            this.Controls.Add(lblScreen);

            screenSelector = new ComboBox();
            screenSelector.Location = new Point(200, 375);
            screenSelector.Size = new Size(175, 25);
            screenSelector.DropDownStyle = ComboBoxStyle.DropDownList;
            screenSelector.BackColor = controlBack;
            screenSelector.ForeColor = Color.White;
            screenSelector.Enabled = false;
            screenSelector.SelectedIndexChanged += ScreenSelector_SelectedIndexChanged;
            this.Controls.Add(screenSelector);

            chkChromaKey = new CheckBox();
            chkChromaKey.Text = "Usar Chroma Key (Remover fondo)";
            chkChromaKey.Location = new Point(10, 410);
            chkChromaKey.Size = new Size(185, 25);
            chkChromaKey.Enabled = false;
            chkChromaKey.ForeColor = Color.White;
            chkChromaKey.CheckedChanged += ChkChromaKey_CheckedChanged;
            this.Controls.Add(chkChromaKey);

            btnChromaColor = CreateModernButton("Color", new Point(200, 410), new Size(80, 25), Color.Black);
            btnChromaColor.Enabled = false;
            btnChromaColor.Click += BtnChromaColor_Click;
            this.Controls.Add(btnChromaColor);

            Button btnEyedropper = CreateModernButton("Gotero", new Point(290, 410), new Size(85, 25), Color.FromArgb(60, 60, 60));
            btnEyedropper.Name = "btnEyedropper";
            btnEyedropper.Enabled = false;
            btnEyedropper.Click += BtnEyedropper_Click;
            this.Controls.Add(btnEyedropper);

            colorDialog = new ColorDialog();

            Label lblTolerance = new Label();
            lblTolerance.Text = "Tolerancia: 30";
            lblTolerance.Name = "lblTolerance";
            lblTolerance.ForeColor = Color.White;
            lblTolerance.Font = new Font("Segoe UI", 8F);
            lblTolerance.Location = new Point(10, 440);
            lblTolerance.Size = new Size(90, 20);
            this.Controls.Add(lblTolerance);

            TrackBar trackTolerance = new TrackBar();
            trackTolerance.Name = "trackTolerance";
            trackTolerance.Minimum = 10;
            trackTolerance.Maximum = 150;
            trackTolerance.Value = 30;
            trackTolerance.TickFrequency = 20;
            trackTolerance.Location = new Point(100, 435);
            trackTolerance.Size = new Size(275, 30);
            trackTolerance.Enabled = false;
            trackTolerance.ValueChanged += TrackTolerance_ValueChanged;
            this.Controls.Add(trackTolerance);

            Label lblHelp = new Label();
            lblHelp.Text = "Tip: Click derecho sobre un GIF desbloqueado para cerrar.";
            lblHelp.ForeColor = Color.Gray;
            lblHelp.Font = new Font("Segoe UI", 8F);
            lblHelp.Location = new Point(10, 475);
            lblHelp.Size = new Size(360, 20);
            this.Controls.Add(lblHelp);

            this.Size = new Size(400, 550);

            trayIcon = new NotifyIcon();
            trayIcon.Text = "Gestor de Overlays GIF";
            trayIcon.Visible = true;

            try
            {
                trayIcon.Icon = new Icon("tray_icon.ico");
            }
            catch
            {
                using (Bitmap bmp = new Bitmap(16, 16))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Cyan);
                    g.FillEllipse(Brushes.Magenta, 2, 2, 12, 12);
                    trayIcon.Icon = Icon.FromHandle(bmp.GetHicon());
                }
            }

            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Mostrar ventana", null, (s, e) => this.ShowWindow());
            trayMenu.Items.Add("Agregar GIF", null, (s, e) => BtnAdd_Click(null, null));
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add($"Bloquear overlays: {(IsLocked ? "ON" : "OFF")}", null,
                (s, e) => { chkLockOverlays.Checked = !chkLockOverlays.Checked; });
            trayMenu.Items.Add("Pausar visualización", null, (s, e) => TogglePause());
            trayMenu.Items.Add("Salir", null, (s, e) => ExitApplication());

            trayIcon.ContextMenuStrip = trayMenu;

            trayIcon.DoubleClick += (s, e) => this.ShowWindow();
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

        private void SaveConfig()
        {
            try
            {
                if (overlays.Count == 0)
                {
                    if (File.Exists(ConfigPath))
                        File.Delete(ConfigPath);
                    return;
                }

                currentSettings.Overlays.Clear();

                foreach (var overlay in overlays)
                {
                    if (!string.IsNullOrEmpty(overlay.GifFilePath) && File.Exists(overlay.GifFilePath))
                    {
                        currentSettings.Overlays.Add(new OverlayConfig(
                            overlay,
                            overlay.GifFilePath
                        ));
                    }
                }

                currentSettings.IsLocked = IsLocked;
                currentSettings.RespectTaskbar = TaskbarHeightCheck.Checked;
                currentSettings.GifHeight = GifHeight;

                string json = JsonConvert.SerializeObject(currentSettings, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar configuracin: {ex.Message}", "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void LoadConfig()
        {
            if (!File.Exists(ConfigPath)) return;

            try
            {
                string json = File.ReadAllText(ConfigPath);
                currentSettings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();

                GifHeight = currentSettings.GifHeight;
                heightInput.Value = Math.Min(Math.Max(GifHeight, (decimal)heightInput.Minimum),
                    (decimal)heightInput.Maximum);

                TaskbarHeightCheck.Checked = currentSettings.RespectTaskbar;
                taskbarHeight = TaskbarHeightCheck.Checked
                    ? (Screen.PrimaryScreen.Bounds.Height - Screen.PrimaryScreen.WorkingArea.Height)
                    : 0;
                baseY = screenHeight - taskbarHeight - GifHeight;

                foreach (var config in currentSettings.Overlays)
                {
                    if (File.Exists(config.FilePath))
                    {
                        try
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
                            overlay.RequestRemove += Overlay_RequestRemove;
                            overlays.Add(overlay);
                            AddGridItem(config.FilePath);
                            overlay.RefreshTransparency();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error cargando overlay: {ex.Message}");
                        }
                    }
                }

                chkLockOverlays.CheckedChanged -= ChkLockOverlays_CheckedChanged;
                chkLockOverlays.Checked = currentSettings.IsLocked;
                chkLockOverlays.CheckedChanged += ChkLockOverlays_CheckedChanged;

                IsLocked = currentSettings.IsLocked;
                foreach (var overlay in overlays)
                    overlay.SetLocked(IsLocked);

                if (trayMenu?.Items.Count > 3)
                    trayMenu.Items[3].Text = $"Bloquear overlays: {(IsLocked ? "ON" : "OFF")}";

                if (trayMenu?.Items.Count > 4)
                    trayMenu.Items[4].Text = isPaused ? "Reanudar visualización" : "Pausar visualización";

                UpdateTrayIcon();
                UpdateSelectionState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar configuracin: {ex.Message}\nSe iniciar con valores por defecto.",
                    "Error de carga", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    screenSelector.Items.Add($"Pantalla {i + 1} ({Screen.AllScreens[i].Bounds.Width}x{Screen.AllScreens[i].Bounds.Height})");
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
                    lbl.Text = $"Tolerancia: {selectedOverlay.ChromaKeyTolerance}";
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
                selectedIndex = (s as GifThumbnailItem).ItemIndex;
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

        private void ScreenSelector_SelectedIndexChanged(object sender, EventArgs e)
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

        private void ChkLockOverlays_CheckedChanged(object sender, EventArgs e)
        {
            IsLocked = chkLockOverlays.Checked;

            foreach (var overlay in overlays)
                overlay.SetLocked(IsLocked);

            if (trayMenu != null && trayMenu.Items.Count > 3)
                trayMenu.Items[3].Text = $"Bloquear overlays: {(IsLocked ? "ON" : "OFF")}";

            UpdateTrayIcon();
            SaveConfig();
        }

        private void ChkChromaKey_CheckedChanged(object sender, EventArgs e)
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

        private void BtnChromaColor_Click(object sender, EventArgs e)
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

        private void BtnEyedropper_Click(object sender, EventArgs e)
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
                Action<Color> handler = null;
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

        private void TrackTolerance_ValueChanged(object sender, EventArgs e)
        {
            if (isUpdatingUI) return;
            if (selectedIndex >= 0 && selectedIndex < overlays.Count && sender is TrackBar t)
            {
                overlays[selectedIndex].ChromaKeyTolerance = t.Value;
                overlays[selectedIndex].RefreshTransparency();
                if (this.Controls["lblTolerance"] is Label lbl)
                    lbl.Text = $"Tolerancia: {t.Value}";
                SaveConfig();
            }
        }

        private void TaskbarHeightCheck_CheckedChanged(object sender, EventArgs e)
        {
            taskbarHeight = TaskbarHeightCheck.Checked
                ? (Screen.PrimaryScreen.Bounds.Height - Screen.PrimaryScreen.WorkingArea.Height)
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

        private void HeightInput_ValueChanged(object sender, EventArgs e)
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

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Imágenes soportadas|*.gif;*.webp;*.png;*.jpg;*.jpeg|GIF|*.gif|WebP|*.webp|PNG|*.png|JPG|*.jpg;*.jpeg|Todos|*.*";
            openFileDialog.Multiselect = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                foreach (string gifPath in openFileDialog.FileNames)
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
                    MessageBox.Show($"Error al cargar el GIF: {ex.Message}", "Error", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                }
            }
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            if (selectedIndex >= 0)
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

        private void Overlay_RequestRemove(object sender, EventArgs e)
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
                trayMenu.Items[4].Text = isPaused ? "Reanudar visualización" : "Pausar visualización";
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
                    trayIcon.Icon?.Dispose();
                    trayIcon.Icon = Icon.FromHandle(bmp.GetHicon());
                }
            }
            catch
            {
            }
        }
    }

#pragma warning disable CA1416

    public class OverlayForm : Form
    {
        private Point mouseOffset;
        private bool isMouseDown = false;
        private bool _isLocked = true;

        public event EventHandler RequestRemove;
        public string GifFilePath { get; private set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public OverlayOrientation Orientation { get; set; } = OverlayOrientation.Inferior;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsSelected { get; set; } = false;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int ScreenIndex { get; set; } = 0;

        private System.Windows.Forms.Timer animationTimer;
        private SDImage originalImage;
        private bool isWebP;
        private List<Bitmap> webpFrames = new List<Bitmap>();
        private List<int> webpDelays = new List<int>();
        private int currentFrameIndex = 0;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool UseChromaKey { get; set; } = false;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ChromaKeyColor { get; set; } = Color.Black;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int ChromaKeyTolerance { get; set; } = 30;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsEyedropperMode { get; set; } = false;
        public event Action<Color> ColorPicked;

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
            this.MouseUp += (s, e) => isMouseDown = false;
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
                    float ar = (float)image.Width / image.Height;
                    int targetW, targetH;
                    if (Orientation == OverlayOrientation.Derecha || Orientation == OverlayOrientation.Izquierda)
                    {
                        targetW = height;
                        targetH = (int)(height * ar);
                    }
                    else
                    {
                        targetW = (int)(height * ar);
                        targetH = height;
                    }

                    this.ClientSize = new Size(targetW, targetH);

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
            if (isWebP) return; // WebP size is handled in LoadWebP
            if (originalImage == null) return;
            float ar = (float)originalImage.Width / originalImage.Height;
            if (Orientation == OverlayOrientation.Derecha || Orientation == OverlayOrientation.Izquierda)
                this.ClientSize = new Size(height, (int)(height * ar));
            else
                this.ClientSize = new Size((int)(height * ar), height);
        }

        public void UpdateWindowSize() => UpdateSize(Form1.GifHeight);

        private Point ApplySnap(Point desired, Size size)
        {
            int nx = desired.X, ny = desired.Y;
            Screen scr = Screen.FromPoint(desired);
            if (Math.Abs(nx - scr.Bounds.Left) < 20) nx = scr.Bounds.Left;
            else if (Math.Abs(nx + size.Width - scr.Bounds.Right) < 20) nx = scr.Bounds.Right - size.Width;
            if (Math.Abs(ny - scr.Bounds.Top) < 20) ny = scr.Bounds.Top;
            else if (Math.Abs(ny + size.Height - scr.Bounds.Bottom) < 20) ny = scr.Bounds.Bottom - size.Height;
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
            Bitmap target = null;
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

                    Bitmap frameToDraw = null;
                    if (isWebP)
                    {
                        if (webpFrames.Count > currentFrameIndex)
                            frameToDraw = webpFrames[currentFrameIndex];
                    }
                    
                    SDImage imageToDraw = isWebP ? (SDImage)frameToDraw : originalImage;
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
        public string FilePath { get; set; }
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
        public bool RespectTaskbar { get; set; } = true;
        public int GifHeight { get; set; } = 100;
    }
}