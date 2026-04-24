using Timer = System.Windows.Forms.Timer;

namespace Renderer;

public sealed class Core
{
    private static Core _core;
    private Form _wnd;
    private VertexBuffer _vb;
    private LoadersFactory _loaders;
    private int _prevX, _prevY;
    private bool _mouseDown;
    private SplitContainer _splitContainer;
    private int _zoom = 10;
    private bool _e, _q, _a, _d, _s, _w; 

    private Matrix _rotationMatrix =
        Matrix.CreateIdentity() *
        Matrix.CreateRotateX(30) *
        Matrix.CreateRotateY(30);

    public static Core Instance => _core ??= new Core();

    public Form Window
    {
        get
        {
            if (_wnd == null)
            {
                _wnd = new Form
                {
                    Width = 1024,
                    Height = 768,
                    StartPosition = FormStartPosition.CenterScreen
                };

                _wnd.KeyDown += (_, e) => KeyDown(e.KeyCode);
                _wnd.KeyUp += (_, e) => KeyUp(e.KeyCode);

                _splitContainer = new SplitContainer
                {
                    Panel1MinSize = 150,
                    SplitterIncrement = 10,
                    FixedPanel = FixedPanel.Panel1,
                    Dock = DockStyle.Fill,
                    TabStop = false
                };

                _wnd.Controls.Add(_splitContainer);

                _wnd.Shown += (_, _) =>
                {
                    _splitContainer.SplitterDistance = 150;
                };

                RenderPanel.Cursor = Cursors.SizeAll;

                var menuSplitContainer = new SplitContainer();
                menuSplitContainer.Orientation = Orientation.Horizontal;
                menuSplitContainer.Dock = DockStyle.Fill;
                menuSplitContainer.SplitterIncrement = 10;
                menuSplitContainer.FixedPanel = FixedPanel.Panel1;
                menuSplitContainer.SplitterWidth = 1;
                menuSplitContainer.IsSplitterFixed = true;
                menuSplitContainer.TabStop = false;
                MenuPanel.Controls.Add(menuSplitContainer);

                var btnZoomIn = new Button
                {
                    Image = new Bitmap(Path.Combine(
                        Path.GetDirectoryName(Application.ExecutablePath) ?? string.Empty, "Graphics",
                        "zoomIn.jpg")),
                    AutoSize = true,
                    BackColor = Color.White,
                    TabStop = false
                };

                btnZoomIn.KeyDown += (_, e) => KeyDown(e.KeyCode);
                btnZoomIn.KeyUp += (_, e) => KeyUp(e.KeyCode);
                menuSplitContainer.Panel1.Controls.Add(btnZoomIn);

                var btnZoomOut = new Button
                {
                    Image = new Bitmap(Path.Combine(
                        Path.GetDirectoryName(Application.ExecutablePath) ?? string.Empty, "Graphics",
                        "zoomOut.jpg")),
                    AutoSize = true,
                    BackColor = Color.White,
                    Left = btnZoomIn.Left + btnZoomIn.Width,
                    TabStop = false
                };
                btnZoomOut.KeyDown += (_, e) => KeyDown(e.KeyCode);
                btnZoomOut.KeyUp += (_, e) => KeyUp(e.KeyCode);
                menuSplitContainer.Panel1.Controls.Add(btnZoomOut);

                btnZoomIn.Click += (_, _) => ZoomIn(btnZoomOut, btnZoomIn, 2);
                btnZoomOut.Click += (_, _) => ZoomOut(btnZoomOut, btnZoomIn, 2);

                menuSplitContainer.Panel1MinSize = Math.Max(btnZoomIn.Height, btnZoomOut.Height);

                MenuPanel.Controls.Add(menuSplitContainer);

                const int imgSize = 100;

                var imageList = new ImageList
                {
                    ImageSize = new Size(imgSize, imgSize)
                };

                var listView = new ListView
                {
                    Dock = DockStyle.Fill,
                    LargeImageList = imageList,
                    MultiSelect = false
                };

                int i = 0;
                var files = Directory.GetFiles(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath) ?? string.Empty, "Objects"));

                foreach (string fileName in files)
                {
                    var tmp = new Bitmap(imgSize, imgSize);

                    using var raw = new RawBitmap(tmp);
                    raw.Clear(Color.Azure);

                    VertexBuffer vb = Loaders.GetLoader(fileName).Load(fileName);
                    Vector volume = vb.Volume;
                    double scaleFactor = Math.Min(tmp.Width, tmp.Height) / 2.0 / Math.Max(volume.X, Math.Max(volume.Y, volume.Z));

                    Matrix matrix =
                        Matrix.CreateIdentity() *
                        Matrix.CreateScale(scaleFactor, scaleFactor, scaleFactor) *
                        Matrix.CreateRotateX(30) *
                        Matrix.CreateRotateY(30) *
                        Matrix.CreateTranslate(tmp.Width / 2.0, tmp.Height / 2.0, 0);

                    vb = vb.Multiply(matrix);
                    vb.CalcTrianglesNormals();
                    vb.DrawTriangles(raw, Color.GhostWhite, true);

                    imageList.Images.Add(tmp);
                    var item = new ListViewItem(Path.GetFileName(fileName), i++);

                    listView.Items.Add(item);
                }

                menuSplitContainer.Panel2.Controls.Add(listView);

                listView.SelectedIndexChanged += (_, _) =>
                {
                    if (listView.SelectedIndices.Count > 0)
                    {
                        LoadModel(files[listView.SelectedIndices[0]]);
                        DrawScene();
                    }
                };

                listView.KeyDown += (_, e) => KeyDown(e.KeyCode);
                listView.KeyUp += (_, e) => KeyUp(e.KeyCode);
                listView.Items[0].Selected = true;

                RenderPanel.MouseDown += (_, e) =>
                {
                    _mouseDown = true;
                    _prevX = e.X;
                    _prevY = e.Y;
                };

                RenderPanel.MouseUp += (_, _) =>
                {
                    _mouseDown = false;
                };

                RenderPanel.MouseMove += (_, e) =>
                {
                    if (_mouseDown)
                    {
                        _rotationMatrix =
                            _rotationMatrix *
                            Matrix.CreateRotateX(e.Y - _prevY) *
                            Matrix.CreateRotateY(_prevX - e.X);

                        _prevX = e.X;
                        _prevY = e.Y;

                        DrawScene();
                    }
                };

                RenderPanel.Paint += (_, _) => DrawScene();
                RenderPanel.Resize += (_, _) => DrawScene();

                var timer = new Timer
                {
                    Interval = 20
                };

                timer.Tick += (_, _) =>
                {
                    if ((_e && !_q) || (!_e && _q))
                    {
                        if (_e)
                        {
                            ZoomIn(btnZoomOut, btnZoomIn, 1);
                        }
                        else if (_q)
                        {
                            ZoomOut(btnZoomOut, btnZoomIn, 1);
                        }
                    }

                    const int angleSpeed = 5;

                    if ((_a && !_d) || (!_a && _d))
                    {
                        if (_a)
                        {
                            _rotationMatrix = _rotationMatrix * Matrix.CreateRotateY(angleSpeed);
                            DrawScene();
                        }
                        else if (_d)
                        {
                            _rotationMatrix = _rotationMatrix * Matrix.CreateRotateY(-angleSpeed);
                            DrawScene();
                        }
                    }

                    if ((_s && !_w) || (!_s && _w))
                    {
                        if (_s)
                        {
                            _rotationMatrix = _rotationMatrix * Matrix.CreateRotateX(angleSpeed);
                            DrawScene();
                        }
                        else if (_w)
                        {
                            _rotationMatrix = _rotationMatrix * Matrix.CreateRotateX(-angleSpeed);
                            DrawScene();
                        }
                    }
                };
                timer.Start();
            }

            return _wnd;
        }
    }

    private void KeyDown(Keys key)
    {
        switch (key)
        {
            case Keys.E:
                _e = true;
                break;

            case Keys.Q:
                _q = true;
                break;

            case Keys.A:
                _a = true;
                break;

            case Keys.D:
                _d = true;
                break;

            case Keys.S:
                _s = true;
                break;

            case Keys.W:
                _w = true;
                break;
        }
    }

    private void KeyUp(Keys key)
    {
        switch (key)
        {
            case Keys.E:
                _e = false;
                break;

            case Keys.Q:
                _q = false;
                break;

            case Keys.A:
                _a = false;
                break;

            case Keys.D:
                _d = false;
                break;

            case Keys.S:
                _s = false;
                break;

            case Keys.W:
                _w = false;
                break;
        }
    }

    private void ZoomIn(Button btnZoomOut, Button btnZoomIn, int step)
    {
        _zoom += step;
        btnZoomOut.Enabled = true;

        if (_zoom > 20)
        {
            _zoom = 20;
            btnZoomIn.Enabled = false;
        }

        DrawScene();
    }

    private void ZoomOut(Button btnZoomOut, Button btnZoomIn, int step)
    {
        _zoom -= step;
        btnZoomIn.Enabled = true;

        if (_zoom < 2)
        {
            _zoom = 2;
            btnZoomOut.Enabled = false;
        }

        DrawScene();
    }

    public void LoadModel(string fileName)
    {
        _vb = Loaders.GetLoader(fileName).Load(fileName);
    }

    public Panel RenderPanel => _splitContainer.Panel2;
    public Panel MenuPanel => _splitContainer.Panel1;

    public void DrawScene()
    {
        using var bmp = new Bitmap(RenderPanel.ClientSize.Width, RenderPanel.ClientSize.Height);

        using (var raw = new RawBitmap(bmp))
        {
            raw.Clear(Color.Azure);

            if (_vb != null)
            {
                Vector volume = _vb.Volume;
                double scaleFactor = Math.Min(bmp.Width, bmp.Height) / 2.0 / Math.Max(volume.X, Math.Max(volume.Y, volume.Z)) * (_zoom / 10.0);

                Matrix matrix =
                    Matrix.CreateIdentity() *
                    Matrix.CreateScale(scaleFactor, scaleFactor, scaleFactor) *
                    _rotationMatrix *
                    Matrix.CreateTranslate(bmp.Width / 2.0, bmp.Height / 2.0, 0);

                VertexBuffer tmpVb = _vb.Clone().Multiply(matrix);
                tmpVb.CalcTrianglesNormals();
                tmpVb.DrawTriangles(raw, Color.GhostWhite, true);

                matrix =
                    Matrix.CreateIdentity() *
                    Matrix.CreateScale(scaleFactor, scaleFactor, scaleFactor) *
                    _rotationMatrix *
                    Matrix.CreateScale(1, 1, 0) *
                    Matrix.CreateTranslate(-100 * (_zoom / 10.0), 100 * (_zoom / 10.0), -1000) *
                    Matrix.CreateTranslate(bmp.Width / 2.0, bmp.Height / 2.0, 0);

                tmpVb = _vb.Clone().Multiply(matrix);
                tmpVb.CalcTrianglesNormals();
                tmpVb.DrawTriangles(raw, Color.Gray, false);
            }
        }

        using (Graphics gfx = RenderPanel.CreateGraphics())
            gfx.DrawImageUnscaled(bmp, 0, 0);
    }

    public LoadersFactory Loaders => _loaders ??= new LoadersFactory();
    private Core() { }

    public void Run()
    {
        Application.EnableVisualStyles();
        Application.Run(Window);
    }

    [STAThread]
    public static void Main()
    {
        Instance.Run();
    }
}
