using Godot;
using System;
using System.IO;

namespace Xianxia.Client.UI;

/// <summary>窗口尺寸决定可用空间；只有玩家设置的倍率才改变一个逻辑像素的显示大小。</summary>
public partial class ClientUiScale : Node
{
    private const int Step = 25;
    private const string SettingsPath = "user://ui-scale.cfg";
    public int Percent { get; private set; } = 100;
    public event Action<int>? Changed;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        LoadPreference();
        // 独立试战在另一个进程运行，切回原窗口后也沿用最近一次设置。
        GetWindow().FocusEntered += LoadPreference;
        GetTree().NodeAdded += OnNodeAdded;
    }

    public override void _ExitTree()
    {
        GetWindow().FocusEntered -= LoadPreference;
        GetTree().NodeAdded -= OnNodeAdded;
    }

    public override void _Input(InputEvent input) => HandleShortcut(input, GetWindow());

    private void OnNodeAdded(Node node)
    {
        // Godot 的模态对话框有独立的输入视口。它已经继承根窗口的显示变换，只连接快捷键。
        if (node is not Window window) return;
        void OnInput(InputEvent input) => HandleShortcut(input, window);
        void Disconnect()
        {
            window.WindowInput -= OnInput;
            window.TreeExiting -= Disconnect;
        }
        window.WindowInput += OnInput;
        window.TreeExiting += Disconnect;
    }

    private void HandleShortcut(InputEvent input, Window window)
    {
        if (input is not InputEventKey { Pressed: true, Echo: false, CtrlPressed: true, AltPressed: false, MetaPressed: false } key) return;
        int percent;
        switch (key.Keycode)
        {
            case Key.Equal:
            case Key.Plus:
            case Key.KpAdd: percent = Percent + Step; break;
            case Key.Minus:
            case Key.KpSubtract: percent = Percent - Step; break;
            case Key.Key0:
            case Key.Kp0: percent = 100; break;
            default: return;
        }
        window.SetInputAsHandled();
        SetPercent(percent);
    }

    public void SetPercent(int value)
    {
        int next = Math.Clamp((int)Math.Round(value / (double)Step) * Step, 75, 150);
        if (next == Percent) return;
        Apply(next);
        // 写完再替换；其他已打开的游戏窗口不会读到写了一半的配置。
        string target = ProjectSettings.GlobalizePath(SettingsPath);
        string temporary = target + "." + System.Environment.ProcessId + ".tmp";
        var config = new ConfigFile();
        config.SetValue("ui", "percent", next);
        try
        {
            var error = config.Save(temporary);
            if (error != Error.Ok) throw new IOException(error.ToString());
            File.Move(temporary, target, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            GD.PushWarning("本次界面缩放已生效，保存设置失败：" + ex.Message);
        }
    }

    private void LoadPreference()
    {
        var config = new ConfigFile();
        int value = 100;
        if (Godot.FileAccess.FileExists(SettingsPath) && config.Load(SettingsPath) == Error.Ok)
        {
            var stored = config.GetValue("ui", "percent", 100);
            if (stored.VariantType == Variant.Type.Int && stored.AsInt64() is 75 or 100 or 125 or 150)
                value = stored.AsInt32();
        }
        Apply(value);
    }

    private void Apply(int value)
    {
        Percent = value;
        GetWindow().ContentScaleFactor = value / 100f;
        Changed?.Invoke(value);
    }
}
