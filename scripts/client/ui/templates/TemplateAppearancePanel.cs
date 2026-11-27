using Godot;
using System;
using System.Linq;
using Xianxia.Client.Battle;

namespace Xianxia.Client.UI.Templates;

/// <summary>显示换装选项，把个人草稿的外观修改交回编辑器。</summary>
public partial class TemplateAppearancePanel : VBoxContainer
{
    private static readonly (string Label, string Clip)[] Demos =
    [ ("待机", "Ready"), ("直刺", "Strike"), ("斩击", "Slash"), ("重击", "Smash"),
        ("格挡", "Guard"), ("受击", "Recoil"), ("施法", "Cast"), ("闪避", "Evade"), ("落地", "Land"), ("败退", "Kneel") ];

    private OptionButton _group = null!, _variant = null!, _demo = null!;
    private ColorPickerButton _dye = null!;
    private Button _clearDye = null!, _roll = null!, _copy = null!;
    private BattleActorLook? _look;
    private bool _readOnly;

    internal event Action<BattleActorLook>? Changed;
    internal event Action<string>? DemoSelected;
    internal event Action? CopyRequested;

    /// <summary>绑定场景控件和部位目录。</summary>
    public override void _Ready()
    {
        _group = GetNode<OptionButton>("Part");
        _variant = GetNode<OptionButton>("Variant");
        _dye = GetNode<ColorPickerButton>("DyeRow/Dye");
        _clearDye = GetNode<Button>("DyeRow/Clear");
        _demo = GetNode<OptionButton>("Demo");
        _roll = GetNode<Button>("Roll");
        _copy = GetNode<Button>("Copy");
        foreach (var group in DuelWardrobe.Groups) _group.AddItem(group.Label);
        foreach (var demo in Demos) _demo.AddItem(demo.Label);
        _group.Select(0);
        _demo.Select(0);
        _group.ItemSelected += _ => RefreshOptions();
        _variant.ItemSelected += index => Change(part => part with { Id = SelectedGroup.Options[(int)index].Id });
        _demo.ItemSelected += index => DemoSelected?.Invoke(Demos[(int)index].Clip);
        _dye.ColorChanged += color => Change(part => part with { Dye = color.ToHtml(false) });
        _dye.GetPicker().EditAlpha = false;
        _clearDye.Pressed += () => Change(part => part with { Dye = null });
        _roll.Pressed += () =>
        {
            if (!_readOnly && _look != null)
                Changed?.Invoke(DuelWardrobe.Generate(_look.Rig, DuelWardrobe.StableSeed(Guid.NewGuid().ToString("N"))));
        };
        _copy.Pressed += () => CopyRequested?.Invoke();
        Hide();
    }

    private DuelWardrobe.Group SelectedGroup => DuelWardrobe.Groups[_group.Selected];

    internal void Bind(BattleActorLook? look, bool readOnly)
    {
        _look = look;
        _readOnly = readOnly;
        Visible = look != null;
        if (look != null) RefreshOptions();
    }

    private void RefreshOptions()
    {
        if (_look == null) return;
        var group = SelectedGroup;
        var part = _look.Parts[group.Id];
        if (_variant.ItemCount != group.Options.Length || _variant.GetItemText(0) != group.Options[0].Label)
        {
            _variant.Clear();
            foreach (var option in group.Options) _variant.AddItem(option.Label);
        }
        _variant.Select(Array.FindIndex(group.Options, option => option.Id == part.Id));
        _variant.Disabled = _readOnly;
        _dye.Disabled = _readOnly || !group.Dyeable || part.Id == "none";
        _clearDye.Disabled = _dye.Disabled || string.IsNullOrEmpty(part.Dye);
        _dye.Color = part.Dye is { } hex && Color.HtmlIsValid(hex) ? Color.FromHtml(hex) : Colors.White;
        _roll.Visible = !_readOnly;
        _copy.Visible = _readOnly;
    }

    private void Change(Func<BattleActorPart, BattleActorPart> edit)
    {
        if (_readOnly || _look == null) return;
        string group = SelectedGroup.Id;
        var previous = _look.Parts[group];
        var next = edit(previous);
        if (next != previous) Changed?.Invoke(_look.WithPart(group, next));
    }
}
