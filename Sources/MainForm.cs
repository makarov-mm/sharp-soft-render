using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace Renderer;

/// <summary>
/// The window: a column of model thumbnails with zoom buttons on the left and
/// the software-rendered view on the right. Every frame is rendered on the CPU
/// into a <see cref="RenderTarget"/>, copied into a bitmap and painted.
/// </summary>
internal sealed class MainForm : Form
{
    private const int ModelsColumnWidth = 150;   // logical pixels
    private const int ThumbnailSize = 100;
    private const int KeyRepeatMs = 20;           // held keys act 50 times a second
    private const float KeyRotationDegrees = 5f;  // per repeat
    private const string Title = "Software Renderer";

    private readonly ModelLoaders _loaders = new(TextureIO.FromFile);
    private readonly Viewport _viewport = new();
    private readonly List<(string Name, Mesh Mesh)> _models = [];
    private readonly HashSet<Keys> _keysDown = [];

    private readonly RenderPanel _view;
    private readonly ListView _modelList;
    private readonly Button _zoomIn, _zoomOut;
    private readonly Timer _keyRepeat;

    private RenderTarget? _target;
    private Bitmap? _frame;
    private Point _lastMouse;
    private string _modelName = "";

    public MainForm()
    {
        // control sizes below are logical (96-dpi) pixels; WinForms scales them
        // for the monitor's DPI. Images are scaled by hand with LogicalToDeviceUnits.
        Text = Title;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96f, 96f);
        ClientSize = new Size(1024, 768);
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;

        _zoomIn = ZoomButton("zoomIn.jpg", +2);
        _zoomOut = ZoomButton("zoomOut.jpg", -2);
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Margin = Padding.Empty, Padding = Padding.Empty };
        toolbar.Controls.AddRange([_zoomIn, _zoomOut]);

        _modelList = new ListView
        {
            Dock = DockStyle.Fill,
            MultiSelect = false,
            HideSelection = false,
            LargeImageList = new ImageList { ImageSize = LogicalToDeviceUnits(new Size(ThumbnailSize, ThumbnailSize)), ColorDepth = ColorDepth.Depth32Bit },
        };
        _modelList.SelectedIndexChanged += (_, _) => OnModelSelected();

        _view = new RenderPanel { Dock = DockStyle.Fill, Cursor = Cursors.SizeAll };
        _view.Resize += (_, _) => RenderFrame();
        _view.MouseEnter += (_, _) => _view.Focus();   // so the wheel zooms the view, not the list
        _view.MouseDown += (_, e) => _lastMouse = e.Location;
        _view.MouseMove += (_, e) => OnViewDrag(e);
        _view.MouseWheel += (_, e) => Zoom(Math.Sign(e.Delta));

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            Panel1MinSize = ModelsColumnWidth,
            TabStop = false,
        };
        split.Panel1.Controls.Add(_modelList);
        split.Panel1.Controls.Add(toolbar);
        split.Panel2.Controls.Add(_view);
        Controls.Add(split);
        Shown += (_, _) => split.SplitterDistance = LogicalToDeviceUnits(ModelsColumnWidth);

        _keyRepeat = new Timer { Interval = KeyRepeatMs };
        _keyRepeat.Tick += (_, _) => OnKeyRepeat();
        _keyRepeat.Start();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        LoadModels(Path.Combine(AppContext.BaseDirectory, "Objects"));
        if (_modelList.Items.Count > 0) _modelList.Items[0].Selected = true;
    }

    // ---- models -----------------------------------------------------------------

    private void LoadModels(string directory)
    {
        if (!Directory.Exists(directory)) return;

        Size thumbnail = _modelList.LargeImageList!.ImageSize;
        var preview = new Viewport { DrawShadow = false };
        var target = new RenderTarget(thumbnail.Width, thumbnail.Height);

        foreach (string file in Directory.GetFiles(directory).Where(_loaders.CanLoad).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
        {
            Mesh mesh;
            try
            {
                mesh = _loaders.Load(file);
            }
            catch (Exception e)   // a broken file is skipped, whatever went wrong in parsing it
            {
                MessageBox.Show(this, $"{Path.GetFileName(file)}: {e.Message}", Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                continue;
            }

            preview.Model = mesh;
            preview.Render(target);
            var image = new Bitmap(target.Width, target.Height);
            target.CopyTo(image);

            _modelList.LargeImageList.Images.Add(image);
            _modelList.Items.Add(new ListViewItem(Path.GetFileName(file), _models.Count));
            _models.Add((Path.GetFileName(file), mesh));
        }
    }

    private void OnModelSelected()
    {
        if (_modelList.SelectedIndices.Count == 0) return;
        (_modelName, _viewport.Model) = _models[_modelList.SelectedIndices[0]];
        RenderFrame();
    }

    // ---- rendering --------------------------------------------------------------

    private void RenderFrame()
    {
        Size size = _view.ClientSize;
        if (size.Width <= 0 || size.Height <= 0) return;

        if (_target is null || _target.Width != size.Width || _target.Height != size.Height)
        {
            _target = new RenderTarget(size.Width, size.Height);
            _frame?.Dispose();
            _frame = new Bitmap(size.Width, size.Height);
        }

        long start = Stopwatch.GetTimestamp();
        _viewport.Render(_target);
        _target.CopyTo(_frame!);
        double ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

        _view.Frame = _frame;
        _view.Invalidate();
        Text = _viewport.Model is null ? Title : $"{Title} - {_modelName}   {ms:F1} ms   {_viewport.Model.TriangleCount:N0} triangles";
    }

    // ---- input ------------------------------------------------------------------

    private void OnViewDrag(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        Rotate(e.Y - _lastMouse.Y, _lastMouse.X - e.X);
        _lastMouse = e.Location;
    }

    private void Rotate(float aboutXDegrees, float aboutYDegrees)
    {
        _viewport.Rotation *= Viewport.RotationX(aboutXDegrees) * Viewport.RotationY(aboutYDegrees);
        RenderFrame();
    }

    private void Zoom(int step)
    {
        int zoom = Math.Clamp(_viewport.Zoom + step, Viewport.MinZoom, Viewport.MaxZoom);
        _zoomIn.Enabled = zoom < Viewport.MaxZoom;
        _zoomOut.Enabled = zoom > Viewport.MinZoom;
        if (zoom == _viewport.Zoom) return;
        _viewport.Zoom = zoom;
        RenderFrame();
    }

    // W/S tilt, A/D turn, Q/E zoom, as long as the key is held.
    private void OnKeyRepeat()
    {
        if (_keysDown.Count == 0) return;
        float tilt = Axis(Keys.S, Keys.W) * KeyRotationDegrees;
        float turn = Axis(Keys.A, Keys.D) * KeyRotationDegrees;
        if (tilt != 0f || turn != 0f) Rotate(tilt, turn);
        int zoom = Axis(Keys.E, Keys.Q);
        if (zoom != 0) Zoom(zoom);
    }

    private int Axis(Keys positive, Keys negative) =>
        (_keysDown.Contains(positive) ? 1 : 0) - (_keysDown.Contains(negative) ? 1 : 0);

    private static bool IsControlKey(Keys key) => key is Keys.W or Keys.A or Keys.S or Keys.D or Keys.Q or Keys.E;

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (IsControlKey(e.KeyCode))
        {
            _keysDown.Add(e.KeyCode);
            e.Handled = e.SuppressKeyPress = true;   // keep the list view from jumping to "W..."
        }
        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        _keysDown.Remove(e.KeyCode);
        base.OnKeyUp(e);
    }

    protected override void OnDeactivate(EventArgs e)
    {
        _keysDown.Clear();   // key-ups go to another window from now on
        base.OnDeactivate(e);
    }

    private Button ZoomButton(string icon, int step)
    {
        var button = new Button { Image = LoadIcon(icon), AutoSize = true, BackColor = Color.White, TabStop = false, Margin = Padding.Empty };
        button.Click += (_, _) => Zoom(step);
        return button;
    }

    // An embedded image (see the csproj), scaled to the monitor's DPI.
    private Image LoadIcon(string name)
    {
        using Stream stream = typeof(MainForm).Assembly.GetManifestResourceStream($"Renderer.Graphics.{name}")
                              ?? throw new FileNotFoundException($"Embedded resource {name} is missing.");
        using Image image = Image.FromStream(stream);
        return new Bitmap(image, LogicalToDeviceUnits(image.Size));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _keyRepeat.Dispose();
            _frame?.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <summary>Shows the last rendered frame; flicker-free thanks to double buffering.</summary>
    private sealed class RenderPanel : Panel
    {
        public Bitmap? Frame { get; set; }

        public RenderPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            SetStyle(ControlStyles.Selectable, true);   // focusable, for mouse-wheel zoom
            TabStop = false;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (Frame is null || Frame.Size != ClientSize) base.OnPaintBackground(e);   // else the frame covers everything
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Frame is not null) e.Graphics.DrawImageUnscaled(Frame, 0, 0);
            base.OnPaint(e);
        }
    }
}
