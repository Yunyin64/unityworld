using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityWorld.Game.Data;
using Xianxia.Client.Templates;
using static Xianxia.Client.UI.Templates.TemplateEditorWidgets;

namespace Xianxia.Client.UI.Templates;

/// <summary>卡库负责搜索、筛选与卡牌条目；选择和刷新请求交给所属的编辑页面。</summary>
public partial class TemplateCardLibraryPanel : Panel
{
    private readonly CardLibraryFilter _filter = new();
    private readonly List<string> _elements = new();
    private IReadOnlyList<CardDefine> _definitions = Array.Empty<CardDefine>();
    private IReadOnlyDictionary<string, KeywordLabel> _keywordLabels = new Dictionary<string, KeywordLabel>();
    private ObserverArtCatalog? _art;
    private bool _canDrag, _keywordsExpanded;
    private double _searchDelay = -1;
    private Label _count = null!;
    private LineEdit _search = null!;
    private Button _expand = null!;
    private HFlowContainer _keywords = null!, _cards = null!;
    private ScrollContainer _keywordScroll = null!, _results = null!;
    private OptionButton _element = null!, _size = null!, _compatibility = null!;
    private Control[] _filterRow = Array.Empty<Control>();

    public event Action? RequestedRefresh;
    public event Action<string>? CardSelected;

    public override void _Ready()
    {
        _count = GetNode<Label>("%CardCount");
        _search = GetNode<LineEdit>("%CardSearch");
        _expand = GetNode<Button>("%ExpandKeywords");
        _keywords = GetNode<HFlowContainer>("%KeywordList");
        _cards = GetNode<HFlowContainer>("%CardList");
        _keywordScroll = GetNode<ScrollContainer>("%KeywordScroll");
        _results = GetNode<ScrollContainer>("%CardResults");
        _element = GetNode<OptionButton>("%ElementFilter");
        _size = GetNode<OptionButton>("%SizeFilter");
        _compatibility = GetNode<OptionButton>("%CompatibilityFilter");
        _filterRow = new Control[] { GetNode<Label>("%ElementHeading"), _element,
            GetNode<Label>("%SizeHeading"), _size, _compatibility };

        // 即使目录加载失败，也不能把编辑器里的示意卡片、关键词和计数展示给玩家。
        Clear(_keywords);
        Clear(_cards);
        _count.Text = "";
        SetProcess(false);

        GetNode<Button>("%RefreshLibrary").Pressed += () => RequestedRefresh?.Invoke();
        GetNode<Button>("%ClearFilters").Pressed += ClearFilters;
        _expand.Pressed += ToggleKeywordArea;
        _search.TextChanged += text =>
        {
            _filter.Text = text;
            _searchDelay = 0.18;
            SetProcess(true);
        };
        _element.ItemSelected += index =>
        {
            if (index < 0 || index >= _elements.Count) return;
            _filter.Element = _elements[(int)index];
            RefreshResults();
        };
        _size.ItemSelected += index =>
        {
            _filter.Size = index == 0 ? null : _size.GetItemId((int)index);
            RefreshResults();
        };
        _compatibility.AddItem("全部状态");
        _compatibility.AddItem("可装配");
        _compatibility.ItemSelected += index =>
        {
            _filter.OnlyAvailable = index == 1;
            RefreshResults();
        };
    }

    /// <summary>一次绑定使用同一代目录数据；切换草稿或重载目录时保留当前筛选。</summary>
    public void Bind(IReadOnlyList<CardDefine> cards, IReadOnlyDictionary<string, KeywordLabel> keywordLabels,
        ObserverArtCatalog art, bool canDrag)
    {
        _definitions = cards;
        _keywordLabels = keywordLabels;
        _art = art;
        _canDrag = canDrag;
        RefreshFilters();
        RefreshResults();
    }

    public IReadOnlyList<string> ReconcileFilters(IReadOnlyList<CardDefine> cards) =>
        CardLibraryQuery.Reconcile(_filter, cards);

    public override void _Process(double delta)
    {
        _searchDelay -= delta;
        if (_searchDelay <= 0) RefreshResults();
    }

    private void ClearFilters()
    {
        _filter.Clear();
        _search.Text = "";
        RefreshFilters();
        RefreshResults();
    }

    private void RefreshFilters()
    {
        Clear(_keywords);
        var all = AddKeywordButton("全部", _filter.Keywords.Count == 0);
        all.Pressed += () =>
        {
            _filter.Keywords.Clear();
            RefreshFilters();
            RefreshResults();
        };
        foreach (var option in CardLibraryQuery.Keywords(_definitions, _keywordLabels))
        {
            var button = AddKeywordButton(option.DisplayName, _filter.Keywords.Contains(option.Id));
            button.TooltipText = option.Id;
            button.Toggled += pressed =>
            {
                if (pressed) _filter.Keywords.Add(option.Id);
                else _filter.Keywords.Remove(option.Id);
                RefreshFilters();
                RefreshResults();
            };
        }

        _elements.Clear();
        _elements.Add("");
        _elements.Add("None");
        _element.Clear();
        _element.AddItem("全部元素");
        _element.AddItem("无灵元需求");
        foreach (string id in _definitions.SelectMany(card => card.ManaCost.Keys).Distinct().OrderBy(id => id, StringComparer.Ordinal))
        {
            _elements.Add(id);
            _element.AddItem(ObserverArtCatalog.ElementName(id));
        }
        _element.Select(Math.Max(0, _elements.IndexOf(_filter.Element)));

        _size.Clear();
        _size.AddItem("不限", 0);
        foreach (var size in _definitions.Select(card => card.Size).Distinct().OrderBy(size => size))
        {
            _size.AddItem($"{size} 格", size);
            if (_filter.Size == size) _size.Select(_size.ItemCount - 1);
        }
        _compatibility.Select(_filter.OnlyAvailable ? 1 : 0);
    }

    private Button AddKeywordButton(string title, bool pressed)
    {
        var button = new Button
        {
            Text = title,
            ThemeTypeVariation = "KeywordButton",
            ToggleMode = true,
            ButtonPressed = pressed,
            CustomMinimumSize = new(70, 30)
        };
        _keywords.AddChild(button);
        return button;
    }

    private void RefreshResults()
    {
        SetProcess(false);
        if (_art == null) return;
        Clear(_cards);
        var matches = CardLibraryQuery.Search(_definitions, _filter);
        _count.Text = $"{matches.Count} 张";
        foreach (var card in matches)
        {
            var tile = new TemplateCardTile { CustomMinimumSize = new(132, 102) };
            _cards.AddChild(tile);
            tile.Configure(card, _art.Card(card.ID), _art.CardTranspose(card.ID), _canDrag,
                CardLibraryQuery.UnavailableReason(card));
            tile.Selected += id => CardSelected?.Invoke(id);
        }
        if (matches.Count == 0)
            FlowText(_cards, "没有符合条件的卡牌，可清空筛选。", 17);
    }

    private void ToggleKeywordArea()
    {
        // 只改变相对于设计位置的高度：重排仍沿用该子场景中可编辑的边距和锚点。
        _keywordsExpanded = !_keywordsExpanded;
        Vector2 shift = Vector2.Down * (_keywordsExpanded ? 32 : -32);
        _keywordScroll.Size += shift;
        _expand.Text = _keywordsExpanded ? "收起" : "展开";
        _results.Position += shift;
        _results.Size -= shift;
        foreach (var control in _filterRow) control.Position += shift;
    }
}
