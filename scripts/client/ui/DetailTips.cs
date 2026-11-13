using Godot;

namespace Xianxia.Client.UI;

/// <summary>每个游戏窗口按需持有自己的浮层。业务控件只提供当前目标的数据。</summary>
public partial class DetailTips : CanvasLayer
{
    private const string LayerName = "ClientDetailTips";
    private static readonly PackedScene Card = GD.Load<PackedScene>("res://scenes/ui/CardTip.tscn");
    private static readonly PackedScene Buff = GD.Load<PackedScene>("res://scenes/ui/BuffTip.tscn");
    private Control? _source, _panel;
    private PackedScene? _scene;
    private string? _key;
    private Label? _title, _body;
    private TextureRect? _art;
    private float _designWidth;
    private Color _sceneTint;
    private Vector2 _available;
    private bool _useFullWidth;
    private ulong _measureAfter;
    private double _readingSeconds;

    public static void ShowCard(Control source, string key, string title, string description, Texture2D? art = null)
        => Show(source, Card, key, title, description, art);

    public static void ShowBuff(Control source, string key, string title, string description, Texture2D? art = null)
        => Show(source, Buff, key, title, description, art);

    public static void Hide(Control source)
    {
        if (!source.IsInsideTree()) return;
        var layer = source.GetWindow()?.GetNodeOrNull<DetailTips>(LayerName);
        if (layer?._source == source) layer.Clear();
    }

    private static void Show(Control source, PackedScene scene, string key, string title, string body, Texture2D? art)
    {
        if (!source.IsVisibleInTree() || source.GetViewport().GuiIsDragging()) return;
        var window = source.GetWindow();
        var layer = window.GetNodeOrNull<DetailTips>(LayerName);
        if (layer == null)
        {
            layer = new DetailTips { Name = LayerName, Layer = 30 };
            window.AddChild(layer);
        }
        layer.Bind(source, scene, key, title, body, art);
    }

    private void Bind(Control source, PackedScene scene, string key, string title, string body, Texture2D? art)
    {
        if (_panel == null || scene != _scene)
        {
            Clear();
            _scene = scene;
            _panel = scene.Instantiate<Control>();
            _sceneTint = _panel.Modulate;
            // 首次排版前 Label 可能只有一像素宽；保持参与布局并在测量完成后揭示。
            _panel.Modulate = new Color(_sceneTint, 0);
            _designWidth = Mathf.Max(_panel.Size.X, _panel.CustomMinimumSize.X);
            AddChild(_panel);
            _title = _panel.GetNode<Label>("%Title");
            _body = _panel.GetNode<Label>("%Description");
            _art = _panel.GetNode<TextureRect>("%Image");
            // 新增装饰子节点也允许鼠标穿透，否则提示框会抢走触发源的移出/拖放事件。
            _panel.MouseFilter = Control.MouseFilterEnum.Ignore;
            foreach (var child in _panel.FindChildren("*", "Control", true, false))
                ((Control)child).MouseFilter = Control.MouseFilterEnum.Ignore;
        }
        if (source != _source || _key != key)
            _readingSeconds = 0;
        if (source != _source)
        {
            DetachSource();
            _source = source;
            source.MouseExited += Clear;
            source.TreeExiting += Clear;
        }
        if (_key != key || _title!.Text != title || _body!.Text != body || _art!.Texture != art)
        {
            ResetMeasurement();
            _key = key;
            _title!.Text = title;
            _body!.Text = body;
            _art!.Texture = art;
            _body.Visible = body.Length != 0;
        }
        SetProcess(true);
        PositionPanel();
    }

    public override void _Process(double delta)
    {
        if (!IsInstanceValid(_source) || !_source!.IsVisibleInTree() || GetViewport().GuiIsDragging())
            Clear();
        else
        {
            _readingSeconds += delta;
            PositionPanel();
        }
    }

    private void PositionPanel()
    {
        if (_panel == null) return;
        var viewport = GetViewport();
        var area = viewport.GetVisibleRect().Grow(-6);
        if (!area.HasArea()) return;
        if (area.Size != _available)
        {
            _available = area.Size;
            ResetMeasurement();
        }
        bool measured = Engine.GetProcessFrames() >= _measureAfter;
        if (measured && _panel.Modulate != _sceneTint) _panel.Modulate = _sceneTint;
        var minimum = _panel.GetCombinedMinimumSize();
        var lineHeight = Mathf.Max(1, _body!.GetLineHeight() + _body.GetThemeConstant("line_spacing"));
        var bodyLines = Mathf.Max(1, _body.GetLineCount());
        float chromeHeight = minimum.Y - (_body.Visible ? _body.GetCombinedMinimumSize().Y : 0);
        if (measured && chromeHeight + bodyLines * lineHeight > area.Size.Y)
            _useFullWidth = true;
        float width = _useFullWidth ? area.Size.X : Mathf.Min(_designWidth, area.Size.X);
        // 长说明先借用剩余宽度；仍超高时按原字号分段轮显，滚轮继续交给背包。
        int linesPerPage = Mathf.Max(1, Mathf.FloorToInt((area.Size.Y - chromeHeight) / lineHeight));
        int limit = measured && bodyLines > linesPerPage ? linesPerPage : -1;
        int first = 0;
        if (limit > 0)
        {
            int pages = (bodyLines + limit - 1) / limit;
            int page = (int)(_readingSeconds / Mathf.Max(5, limit * 1.2f)) % pages;
            first = Mathf.Min(page * limit, bodyLines - limit);
        }
        if (_body.MaxLinesVisible != limit) _body.MaxLinesVisible = limit;
        if (_body.LinesSkipped != first) _body.LinesSkipped = first;
        _panel.Size = new Vector2(width, _panel.GetCombinedMinimumSize().Y);
        // 小于标题和一行正文的极小视口中，才退回整体缩放。
        float scale = Mathf.Min(1, Mathf.Min(area.Size.X / Mathf.Max(1, _panel.Size.X), area.Size.Y / Mathf.Max(1, _panel.Size.Y)));
        _panel.Scale = Vector2.One * scale;
        var size = _panel.Size * scale;
        var mouse = viewport.GetMousePosition();
        var at = mouse + new Vector2(16, 18);
        if (at.X + size.X > area.End.X) at.X = mouse.X - size.X - 12;
        if (at.Y + size.Y > area.End.Y) at.Y = mouse.Y - size.Y - 12;
        _panel.Position = at.Clamp(area.Position, (area.End - size).Max(area.Position)).Round();
    }

    private void ResetMeasurement()
    {
        // Container 的换行宽度在延迟排版后才稳定。先按场景宽度摆放，再决定是否借用更多空间。
        _useFullWidth = false;
        _measureAfter = Engine.GetProcessFrames() + 2;
    }

    private void DetachSource()
    {
        if (IsInstanceValid(_source))
        {
            _source!.MouseExited -= Clear;
            _source.TreeExiting -= Clear;
        }
        _source = null;
    }

    private void Clear()
    {
        DetachSource();
        if (IsInstanceValid(_panel))
        {
            _panel!.Hide();
            _panel.QueueFree();
        }
        _panel = null;
        _scene = null;
        _key = null;
        SetProcess(false);
    }
}
