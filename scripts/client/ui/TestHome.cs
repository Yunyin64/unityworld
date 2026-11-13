using Godot;

namespace Xianxia.Client.UI;

/// <summary>首页只负责切换工具，进入这里时不建立游戏世界。</summary>
public partial class TestHome : Control
{
    private ClientUiScale? _settings;
    private HSlider _slider = null!;
    private Label _percent = null!;

    public override void _Ready()
    {
        if (GameBootstrap.CurrentMode != ClientHostMode.TestHome) return;
        _settings = GetNode<ClientUiScale>("/root/ClientUiScale");
        _slider = GetNode<HSlider>("%ScaleSlider");
        _percent = GetNode<Label>("%ScalePercent");
        _slider.ValueChanged += value => _settings.SetPercent((int)value);
        _settings.Changed += RefreshScale;
        RefreshScale(_settings.Percent);
        GetNode<Button>("%MapTest").Pressed += () => GameBootstrap.OpenTool(ClientHostMode.Exploration);
        GetNode<Button>("%TemplateEditor").Pressed += () => GameBootstrap.OpenTool(ClientHostMode.TemplateEditor);
    }

    private void RefreshScale(int percent)
    {
        _slider.SetValueNoSignal(percent);
        _percent.Text = $"{percent}%";
    }

    public override void _ExitTree()
    {
        if (_settings != null) _settings.Changed -= RefreshScale;
        _settings = null;
    }
}
