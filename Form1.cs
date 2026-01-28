using Newtonsoft.Json;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace OwOverlays
{
    public partial class Form1 : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private List<OverlayForm> overlays = new List<OverlayForm>();
        private ListBox lstOverlays;
        private Button btnAdd;
        private Button btnRemove;
        private NumericUpDown heightInput;
        private Label lblHeight;
        private CheckBox TaskbarHeightCheck;
        private CheckBox chkLockOverlays;
        private bool IsLocked;
        private const string ConfigFile = "GifOverlayConfig.json";
        private AppSettings currentSettings = new AppSettings();
        private ComboBox orientationDropdown;
        private Label lblOrientation;
        private bool isUpdatingUI = false;


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

            lstOverlays = new ListBox();
            lstOverlays.Location = new Point(10, 10);
            lstOverlays.Size = new Size(365, 200);
            lstOverlays.BackColor = controlBack;
            lstOverlays.ForeColor = Color.White;
            lstOverlays.BorderStyle = BorderStyle.None;
            lstOverlays.DrawMode = DrawMode.OwnerDrawFixed;
            lstOverlays.ItemHeight = 25;
            lstOverlays.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
                e.DrawBackground();
                bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                using (SolidBrush bg = new SolidBrush(isSelected ? accentColor : controlBack))
                using (SolidBrush text = new SolidBrush(Color.White))
                {
                    e.Graphics.FillRectangle(bg, e.Bounds);
                    e.Graphics.DrawString(lstOverlays.Items[e.Index].ToString(), e.Font, text, e.Bounds.X + 5,
                        e.Bounds.Y + 3);
                }
            };
            this.Controls.Add(lstOverlays);

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

            lblOrientation = new Label();
            lblOrientation.Text = "Orientacion:";
            lblOrientation.Location = new Point(10, 375);
            lblOrientation.Size = new Size(180, 25);
            this.Controls.Add(lblOrientation);

            orientationDropdown = new ComboBox();
            orientationDropdown.Location = new Point(200, 375);
            orientationDropdown.Size = new Size(175, 25);
            orientationDropdown.DropDownStyle = ComboBoxStyle.DropDownList;
            orientationDropdown.BackColor = controlBack;
            orientationDropdown.ForeColor = Color.White;
            orientationDropdown.DataSource = Enum.GetValues(typeof(OverlayOrientation));
            orientationDropdown.Enabled = false;
            orientationDropdown.SelectedIndexChanged += OrientationDropdown_SelectedIndexChanged;
            this.Controls.Add(orientationDropdown);

            Label lblHelp = new Label();
            lblHelp.Text = "Tip: Click derecho sobre un GIF desbloqueado para cerrar.";
            lblHelp.ForeColor = Color.Gray;
            lblHelp.Font = new Font("Segoe UI", 8F);
            lblHelp.Location = new Point(10, 420);
            lblHelp.Size = new Size(360, 20);
            this.Controls.Add(lblHelp);

            this.Size = new Size(400, 500);

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

            lstOverlays.SelectedIndexChanged += (s, e) => UpdateSelectionState();

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
                MessageBox.Show($"Error al guardar configuraci�n: {ex.Message}", "Error", MessageBoxButtons.OK,
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
                            overlay.UpdateWindowSize();
                            overlay.Location = new Point(config.X, config.Y);
                            overlay.Show();
                            overlay.RequestRemove += Overlay_RequestRemove;
                            overlays.Add(overlay);
                            lstOverlays.Items.Add(Path.GetFileName(config.FilePath));
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

            for (int i = 0; i < overlays.Count; i++)
            {
                bool selected = (lstOverlays.SelectedIndex == i);
                if (overlays[i].IsSelected != selected)
                {
                    overlays[i].IsSelected = selected;
                    overlays[i].RefreshTransparency();
                }
            }

            isUpdatingUI = true;
            if (lstOverlays.SelectedIndex >= 0 && lstOverlays.SelectedIndex < overlays.Count)
            {
                orientationDropdown.Enabled = true;
                orientationDropdown.SelectedItem = overlays[lstOverlays.SelectedIndex].Orientation;
            }
            else
            {
                orientationDropdown.Enabled = false;
            }

            isUpdatingUI = false;
        }

        private void ClearSelection()
        {
            lstOverlays.SelectedIndex = -1;
            UpdateSelectionState();
        }

        private void OrientationDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isUpdatingUI) return;

            if (lstOverlays.SelectedIndex >= 0)
            {
                var selectedOverlay = overlays[lstOverlays.SelectedIndex];
                OverlayOrientation oldOrientation = selectedOverlay.Orientation;
                OverlayOrientation newOrientation = (OverlayOrientation)orientationDropdown.SelectedItem;

                if (oldOrientation != newOrientation)
                {
                    selectedOverlay.Orientation = newOrientation;
                    selectedOverlay.UpdateWindowSize();

                    if ((oldOrientation == OverlayOrientation.Izquierda ||
                         oldOrientation == OverlayOrientation.Derecha) &&
                        (newOrientation == OverlayOrientation.Izquierda ||
                         newOrientation == OverlayOrientation.Derecha))
                    {
                        if (newOrientation == OverlayOrientation.Izquierda)
                            selectedOverlay.Location = new Point(0, selectedOverlay.Location.Y);
                        else
                            selectedOverlay.Location = new Point(screenWidth - selectedOverlay.Width,
                                selectedOverlay.Location.Y);
                    }
                    else if ((oldOrientation == OverlayOrientation.Superior ||
                              oldOrientation == OverlayOrientation.Inferior) &&
                             (newOrientation == OverlayOrientation.Superior ||
                              newOrientation == OverlayOrientation.Inferior))
                    {
                        if (newOrientation == OverlayOrientation.Superior)
                            selectedOverlay.Location = new Point(selectedOverlay.Location.X, 0);
                        else selectedOverlay.Location = new Point(selectedOverlay.Location.X, baseY);
                    }
                    else
                    {
                        switch (newOrientation)
                        {
                            case OverlayOrientation.Inferior:
                                selectedOverlay.Location = new Point(selectedOverlay.Location.X, baseY);
                                break;
                            case OverlayOrientation.Superior:
                                selectedOverlay.Location = new Point(selectedOverlay.Location.X, 0);
                                break;
                            case OverlayOrientation.Izquierda:
                                selectedOverlay.Location = new Point(0, selectedOverlay.Location.Y);
                                break;
                            case OverlayOrientation.Derecha:
                                selectedOverlay.Location = new Point(screenWidth - selectedOverlay.Width,
                                    selectedOverlay.Location.Y);
                                break;
                        }
                    }

                    selectedOverlay.RefreshTransparency();
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
            openFileDialog.Filter = "GIF Files (*.gif)|*.gif";


            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string gifPath = openFileDialog.FileName;
                try
                {
                    OverlayForm newOverlay = new OverlayForm(gifPath, GifHeight);

                    newOverlay.Show();
                    newOverlay.SetLocked(IsLocked);
                    overlays.Add(newOverlay);
                    newOverlay.RequestRemove += Overlay_RequestRemove;
                    lstOverlays.Items.Add(Path.GetFileName(gifPath));

                    newOverlay.UpdateWindowSize();

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
                    newOverlay.RefreshTransparency();

                    SaveConfig();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar el GIF: {ex.Message}", "Error", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            if (lstOverlays.SelectedIndex >= 0)
            {
                int index = lstOverlays.SelectedIndex;
                OverlayForm toRemove = overlays[index];
                toRemove.Close();
                overlays.RemoveAt(index);
                lstOverlays.Items.RemoveAt(index);

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
                    lstOverlays.Items.RemoveAt(index);

                    if (lstOverlays.Items.Count > 0 && lstOverlays.SelectedIndex >= lstOverlays.Items.Count)
                        lstOverlays.SelectedIndex = lstOverlays.Items.Count - 1;

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
            lstOverlays.Items.Clear();


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

        private System.Windows.Forms.Timer animationTimer;
        private Image originalImage;

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
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.BackColor = Color.Black;

            originalImage = Image.FromFile(gifPath);
            UpdateSize(height);

            animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = 30;
            animationTimer.Tick += (s, e) =>
            {
                if (originalImage != null)
                {
                    ImageAnimator.UpdateFrames(originalImage);
                    MakeTransparent();
                }
            };

            ImageAnimator.Animate(originalImage, (o, ev) => { });
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
                if (e.Button == MouseButtons.Right && !_isLocked)
                    RequestRemove?.Invoke(this, EventArgs.Empty);
            };

            this.Load += (s, e) => MakeTransparent();
        }

        public void UpdateSize(int height)
        {
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
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            animationTimer?.Stop();
            animationTimer?.Dispose();
            originalImage?.Dispose();
            base.OnFormClosed(e);
        }

        private void MakeTransparent()
        {
            if (originalImage == null || this.IsDisposed) return;
            int w = this.ClientSize.Width, h = this.ClientSize.Height;
            if (w <= 0 || h <= 0) return;

            using (Bitmap bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                    switch (Orientation)
                    {
                        case OverlayOrientation.Izquierda:
                            g.TranslateTransform(w, 0);
                            g.RotateTransform(90);
                            g.DrawImage(originalImage, 0, 0, h, w);
                            break;
                        case OverlayOrientation.Superior:
                            g.TranslateTransform(w, h);
                            g.RotateTransform(180);
                            g.DrawImage(originalImage, 0, 0, w, h);
                            break;
                        case OverlayOrientation.Derecha:
                            g.TranslateTransform(0, h);
                            g.RotateTransform(270);
                            g.DrawImage(originalImage, 0, 0, h, w);
                            break;
                        default:
                            g.DrawImage(originalImage, 0, 0, w, h);
                            break;
                    }

                    if (IsSelected)
                    {
                        g.ResetTransform();
                        using (Pen p = new Pen(Color.Lime, 4)) g.DrawRectangle(p, 2, 2, w - 4, h - 4);
                    }
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

        public OverlayConfig()
        {
        }

        public OverlayConfig(OverlayForm overlay, string path)
        {
            FilePath = path;
            X = overlay.Location.X;
            Y = overlay.Location.Y;
            Orientation = overlay.Orientation;
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