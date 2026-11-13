using Godot;
using System;

namespace Xianxia.Client.UI.Templates;

/// <summary>弹窗使用父视口的逻辑像素，缩放由所属窗口统一处理。底部按钮始终留在可见区。</summary>
internal static class TemplateDialogLayout
{
    public static Label AddScrollableText(AcceptDialog dialog, string text)
    {
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var label = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin
        };
        dialog.AddChild(scroll);
        scroll.AddChild(label);
        SetMessage(dialog, label, text);
        return label;
    }

    public static void SetMessage(AcceptDialog dialog, Label label, string text)
    {
        // DialogText 仍可供已有验收和辅助工具读取，页面只绘制可滚动的正文。
        dialog.DialogText = text;
        dialog.GetLabel().Hide();
        label.Text = text;
    }

    public static void Show(AcceptDialog dialog, Vector2I preferred = default, Label? compactMessage = null)
    {
        var owner = dialog.GetParent().GetWindow();
        Vector2I originalMin = dialog.MinSize;
        Rect2I Bounds()
        {
            // 嵌入窗口的位置从内容区左上角算起，标题栏绘在这个坐标上方。
            var viewport = (Vector2I)owner.GetVisibleRect().Size.Floor();
            int title = dialog.Borderless ? 0 : dialog.GetThemeConstant("title_height", "Window");
            return new Rect2I(new Vector2I(8, title + 8), (viewport - new Vector2I(16, title + 16)).Max(Vector2I.One));
        }

        Vector2I Target(Rect2I bounds)
        {
            if (preferred != Vector2I.Zero) return preferred.Min(bounds.Size);
            var natural = (Vector2I)dialog.GetContentsMinimumSize().Ceil();
            if (compactMessage == null) return natural.Min(bounds.Size);
            var font = compactMessage.GetThemeFont("font");
            int size = compactMessage.GetThemeFontSize("font_size");
            int textWidth = Mathf.CeilToInt(font.GetStringSize(compactMessage.Text, fontSize: size).X);
            int width = Math.Min(bounds.Size.X, Math.Max(natural.X, Math.Min(540, textWidth + 40)));
            float height = font.GetMultilineStringSize(compactMessage.Text, width: Math.Max(1, width - 40), fontSize: size).Y;
            return new Vector2I(width, Math.Min(bounds.Size.Y, natural.Y + Mathf.CeilToInt(height) + 8));
        }

        void Fit()
        {
            if (!dialog.Visible || dialog.IsQueuedForDeletion()) return;
            var bounds = Bounds();
            dialog.MinSize = originalMin.Min(bounds.Size);
            dialog.Size = Target(bounds);
            dialog.Position = bounds.Position + (bounds.Size - dialog.Size) / 2;
        }

        owner.SizeChanged += Fit;
        // 保存失败的原因可能比原来的离开提示长，让正文重新换行并保留底部动作。
        if (compactMessage != null) compactMessage.MinimumSizeChanged += Fit;
        dialog.TreeExiting += () =>
        {
            owner.SizeChanged -= Fit;
            if (compactMessage != null) compactMessage.MinimumSizeChanged -= Fit;
        };
        var initial = Bounds();
        dialog.MinSize = originalMin.Min(initial.Size);
        dialog.PopupCentered(Target(initial));
        Fit();
    }
}
