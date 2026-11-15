using System.Drawing.Drawing2D;

namespace AreaDown.App;

/// <summary>A two-handle range selector for choosing a video segment.</summary>
internal sealed class ClipTimelineControl : Control
{
    private static readonly Color TrackColor = Color.FromArgb(222, 228, 231);
    private static readonly Color SelectionColor = Color.FromArgb(31, 139, 84);
    private TimeSpan _duration = TimeSpan.FromMinutes(1);
    private TimeSpan _start;
    private TimeSpan _end = TimeSpan.FromMinutes(1);
    private DragHandle _dragging;

    public event EventHandler? RangeChanged;
    public TimeSpan StartTime => _start;
    public TimeSpan EndTime => _end;
    public TimeSpan Duration => _duration;

    public ClipTimelineControl()
    {
        DoubleBuffered = true;
        Height = 42;
        MinimumSize = new Size(120, 38);
        Cursor = Cursors.Hand;
        AccessibleName = "Selector del fragmento de video";
    }

    public void SetDuration(TimeSpan duration)
    {
        _duration = duration > TimeSpan.Zero ? duration : TimeSpan.FromMinutes(1);
        _start = TimeSpan.Zero;
        _end = _duration;
        Invalidate();
        RangeChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetRange(TimeSpan start, TimeSpan end)
    {
        _start = Clamp(start, TimeSpan.Zero, _duration);
        _end = Clamp(end, _start, _duration);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var track = TrackBounds;
        using var trackBrush = new SolidBrush(TrackColor);
        using var rangeBrush = new SolidBrush(SelectionColor);
        e.Graphics.FillRoundedRectangle(trackBrush, track, 4);
        var startX = XFor(_start);
        var endX = XFor(_end);
        e.Graphics.FillRoundedRectangle(rangeBrush, Rectangle.FromLTRB(startX, track.Top, Math.Max(startX + 2, endX), track.Bottom), 4);
        DrawHandle(e.Graphics, startX, track);
        DrawHandle(e.Graphics, endX, track);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        var startX = XFor(_start);
        var endX = XFor(_end);
        _dragging = Math.Abs(e.X - startX) <= Math.Abs(e.X - endX) ? DragHandle.Start : DragHandle.End;
        Capture = true;
        UpdateFromMouse(e.X);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragging != DragHandle.None && Capture) UpdateFromMouse(e.X);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left) return;
        _dragging = DragHandle.None;
        Capture = false;
    }

    private Rectangle TrackBounds => new(10, Math.Max(8, Height / 2 - 4), Math.Max(1, Width - 20), 8);
    private int XFor(TimeSpan time) => TrackBounds.Left + (int)Math.Round(TrackBounds.Width * time.TotalSeconds / _duration.TotalSeconds);

    private void UpdateFromMouse(int x)
    {
        var track = TrackBounds;
        var ratio = Math.Clamp((double)(x - track.Left) / track.Width, 0, 1);
        var time = TimeSpan.FromSeconds(Math.Round(_duration.TotalSeconds * ratio));
        if (_dragging == DragHandle.Start) _start = Clamp(time, TimeSpan.Zero, _end);
        else if (_dragging == DragHandle.End) _end = Clamp(time, _start, _duration);
        Invalidate();
        RangeChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void DrawHandle(Graphics graphics, int x, Rectangle track)
    {
        using var brush = new SolidBrush(Color.White);
        using var pen = new Pen(SelectionColor, 3);
        graphics.FillEllipse(brush, x - 8, track.Top - 7, 16, 22);
        graphics.DrawEllipse(pen, x - 8, track.Top - 7, 16, 22);
    }

    private static TimeSpan Clamp(TimeSpan value, TimeSpan minimum, TimeSpan maximum) => value < minimum ? minimum : value > maximum ? maximum : value;
    private enum DragHandle { None, Start, End }
}

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle rectangle, int radius)
    {
        var diameter = radius * 2;
        using var path = new GraphicsPath();
        path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        graphics.FillPath(brush, path);
    }
}
