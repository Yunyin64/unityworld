using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using Xianxia.Client.Templates;
using static Xianxia.Client.UI.Templates.TemplateEditorWidgets;

namespace Xianxia.Client.UI.Templates;

public partial class TemplateEditorView
{
    private void RefreshDetails()
    {
        Clear(_details);_fieldControls.Clear();_limits=null!;
        if(_draft==null){FlowText(_details,"选择一个项目模板，或新建个人模板。",20);return;}
        var target=_draft;
        var heading=new HBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};_details.AddChild(heading);
        string visual=EffectiveVisual(target);
        var portrait=_skin.Portrait(visual);
        if(portrait!=null)heading.AddChild(new TextureRect{Texture=portrait,CustomMinimumSize=new(66,66),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered});
        FlowText(heading,DefinitionName(target.NpcFields),25);
        FlowText(_details,_readOnly?"项目模板 · 复制后编辑":"我的模板 · 可编辑",14);
        FlowText(_details,"基础信息",21);
        DetailText("模板名称","DisplayName",target.NpcFields.DisplayName,(n,v)=>n.DisplayName=v);
        DetailText("模板 ID","ID",target.NpcFields.ID,(n,v)=>n.ID=v);
        var type=new OptionButton{Disabled=_readOnly};
        type.AddItem("人物",(int)NpcTypes.NpcType.Human);type.AddItem("怪物",(int)NpcTypes.NpcType.Monster);type.AddItem("动物",(int)NpcTypes.NpcType.Animal);
        type.Select(type.GetItemIndex((int)target.NpcFields.NpcType));Row(_details,"种族",type);_fieldControls["NpcType"]=type;
        type.ItemSelected+=i=>{if(CanChange(target)){target.NpcFields.NpcType=(NpcTypes.NpcType)type.GetItemId((int)i);DraftEdited(target);}};
        var gender=new OptionButton{Disabled=_readOnly};gender.AddItem("男 / 雄性",0);gender.AddItem("女 / 雌性",1);gender.AddItem("未知",2);
        gender.Select((int)target.NpcFields.Gender);Row(_details,"性别",gender);_fieldControls["Gender"]=gender;
        gender.ItemSelected+=i=>{if(CanChange(target)){target.NpcFields.Gender=(NpcTypes.Gender)i;DraftEdited(target);}};
        FlowButton(_details,"姓名 / 道号 · "+target.NpcFields.Surname+target.NpcFields.GivenName,()=>ShowIdentityDialog(target),false,28);
        FlowText(_details,"基础属性",21);
        var stats=new GridContainer{Columns=2,SizeFlagsHorizontal=SizeFlags.ExpandFill};stats.AddThemeConstantOverride("h_separation",16);stats.AddThemeConstantOverride("v_separation",6);_details.AddChild(stats);
        foreach(var pair in new[]{("QiXue","气血"),("TiPo","体魄"),("QiGan","气感"),("LingJi","灵机"),("ShenShi","神识"),("WuXing","悟性"),("JiYuan","机缘"),("MeiLi","魅力")})
        {
            var def=Catalog.GetStat(pair.Item1);
            var spin=new SpinBox{MinValue=def?.MinValue??-999999,MaxValue=def?.MaxValue??999999,Step=1,
                Value=target.NpcFields.InitStat.GetValueOrDefault(pair.Item1,(int)(def?.DefaultValue??0)),Editable=!_readOnly,CustomMinimumSize=new(106,32)};
            var row=new HBoxContainer{CustomMinimumSize=new(184,34)};stats.AddChild(row);
            var label=FlowText(row,pair.Item2,17,false);label.CustomMinimumSize=new(56,32);label.SizeFlagsHorizontal=SizeFlags.ShrinkBegin;row.AddChild(spin);
            _fieldControls["InitStat."+pair.Item1]=spin;_fieldControls["InitStat["+pair.Item1+"]"]=spin;
            spin.ValueChanged+=v=>{if(CanChange(target)){target.NpcFields.InitStat[pair.Item1]=(int)v;DraftEdited(target);}};
        }
        _limits=FlowText(_details,"",20);
        var visualOptions=new OptionButton{Disabled=_readOnly};
        visualOptions.AddItem("仅显示名称");visualOptions.AddItem("已有 · 玉剑修士");visualOptions.AddItem("已有 · 赤衣修士");
        visualOptions.Select(visual=="jade_swordsman"?1:visual=="crimson_cultivator"?2:0);
        Row(_details,"外观",visualOptions);
        visualOptions.ItemSelected+=i=>{if(CanChange(target)){string key=i==1?"jade_swordsman":i==2?"crimson_cultivator":"none";target.Presentation.ActorVisualKey=key;target.Presentation.PortraitKey=key;DraftEdited(target);}};
        FlowButton(_details,"扩展字段 / 当前支持状态",()=>ShowExtendedFields(target),false,30);
        if(target.UnresolvedBindings.Count>0)
        {
            FlowText(_details,$"待处理导入项：{target.UnresolvedBindings.Count}",17);
            foreach(var binding in target.UnresolvedBindings.ToArray())
                FlowButton(_details,binding.FieldPath+" · 查看",()=>ShowUnresolved(target,binding),false,30);
        }
        _details.AddChild(new HSeparator());
        RefreshCardDetails(target);
    }

    private void RefreshCardDetails(TemplateDraft target)
    {
        FlowText(_details,"选中卡牌",22);
        var entry=target.Entries.FirstOrDefault(e=>e.EntryId==_selectedEntryId);
        string? id=entry?.CardDefineId??_selectedCardId;
        var card=id==null?null:Catalog.GetCard(id);
        if(card==null){FlowText(_details,id==null?"点击背包或卡牌库中的卡牌查看。":"定义已缺失："+id,16);if(entry!=null)FlowButton(_details,"移除缺失条目",()=>RemoveEntry(entry.EntryId));return;}
        var tile=new TemplateCardTile{CustomMinimumSize=new(160,108)};_details.AddChild(tile);
        tile.Configure(card,_art.Card(card.ID),_art.CardTranspose(card.ID),false,CardLibraryQuery.UnavailableReason(card));
        FlowText(_details,card.ID,13);
        FlowText(_details,"占格 "+card.Size+"    冷却 "+card.Cooldown+" Tick",17);
        FlowText(_details,"灵元需求："+(card.ManaCost.Count==0?"无":string.Join(" · ",card.ManaCost.Select(p=>ObserverArtCatalog.ElementName(p.Key)+" × "+p.Value))),16);
        FlowText(_details,"关键词："+string.Join(" · ",card.Keywords.Select(k=>_keywordLabels.TryGetValue(k,out var l)?l.DisplayName:k)),15);
        FlowText(_details,card.Desc,16);
        var unavailable=CardLibraryQuery.UnavailableReason(card);if(unavailable!=null)FlowText(_details,"不可装配："+unavailable,15);
        if(entry==null)
        {
            var add=FlowButton(_details,"添加到未放置列表",()=>
            {
                if(!CanChange(target))return;
                if(target.TryAddCard(Catalog,card.ID,null,out var error))
                {_selectedEntryId=target.Entries.Last().EntryId;_selectedCardId=null;_notice="已加入未放置列表，请拖入背包。";RefreshEditor();}
                else ShowNotice(error);
            });add.Disabled=_readOnly||unavailable!=null;return;
        }
        FlowText(_details,"实例 "+entry.EntryId[..Math.Min(8,entry.EntryId.Length)]+(entry.Position is {} pos?$" · ({pos.X}, {pos.Y})":" · 未放置"),14);
        if(Catalog.GetCultivation(card.ID) is {} gongfa)
        {
            var points=new SpinBox{MinValue=0,MaxValue=gongfa.MaxPoint,Step=1,Value=entry.GongFaProgress??0,Editable=!_readOnly};
            Row(_details,"修炼进度",points);
            points.ValueChanged+=v=>{if(CanChange(target)){entry.GongFaProgress=(int)v;DraftEdited(target);}};
            if(entry.EntryId!=target.MainEntryId)
            {
                var main=FlowButton(_details,"设为主功法",()=>{if(CanChange(target)){if(target.SetMainCard(Catalog,entry.EntryId,out var error)){_notice="主功法已切换；请检查未放置卡。";RefreshEditor();}else ShowNotice(error);}});main.Disabled=_readOnly;
            }
        }
        var equipIds=new List<string?>{null};var equip=new OptionButton{Disabled=_readOnly};equip.AddItem("无装备绑定");
        foreach(var def in Catalog.Equips){equipIds.Add(def.ID);equip.AddItem(DefinitionName(def));if(def.ID==entry.EquipDefineId)equip.Select(equipIds.Count-1);}
        if(entry.EquipDefineId!=null&&!equipIds.Contains(entry.EquipDefineId)){equipIds.Add(entry.EquipDefineId);equip.AddItem("缺失："+entry.EquipDefineId);equip.Select(equipIds.Count-1);}
        Row(_details,"装备模板",equip);equip.ItemSelected+=i=>{if(CanChange(target)){entry.EquipDefineId=equipIds[(int)i];DraftEdited(target);RefreshDetails();}};
        var parentIds=new List<string?>{null};var parent=new OptionButton{Disabled=_readOnly};parent.AddItem("无父装备");
        foreach(var other in target.Entries.Where(e=>e.EntryId!=entry.EntryId&&e.EquipDefineId!=null))
        {
            parentIds.Add(other.EntryId);parent.AddItem((Catalog.GetCard(other.CardDefineId)?.DisplayName??other.CardDefineId)+" #"+other.EntryId[..Math.Min(6,other.EntryId.Length)]);
            if(other.EntryId==entry.ParentEntryId)parent.Select(parentIds.Count-1);
        }
        bool missingParent=entry.ParentEntryId!=null&&!parentIds.Contains(entry.ParentEntryId);
        if(missingParent){parentIds.Add(entry.ParentEntryId);parent.AddItem("原父装备已失效 · 请重绑");parent.Select(parentIds.Count-1);parent.SetItemDisabled(0,true);}
        Row(_details,"所属装备",parent);parent.ItemSelected+=i=>{if(CanChange(target)){entry.ParentEntryId=parentIds[(int)i];DraftEdited(target);RefreshDetails();}};
        var remove=FlowButton(_details,"移除此卡",()=>RemoveEntry(entry.EntryId));remove.Disabled=_readOnly||entry.EntryId==target.MainEntryId;
    }

    private void DetailText(string label,string field,string value,Action<NpcDefine,string> assign)
    {
        var target=_draft!;var edit=new LineEdit{Text=value,Editable=!_readOnly,CustomMinimumSize=new(0,32)};Row(_details,label,edit);_fieldControls[field]=edit;
        edit.TextChanged+=text=>{if(CanChange(target)){assign(target.NpcFields,text);DraftEdited(target);}};
    }
    private bool CanChange(TemplateDraft target)=>!_readOnly&&ReferenceEquals(_draft,target);
    private void DraftEdited(TemplateDraft target){target.MarkDirty();_notice="";RefreshAfterEdit();}
    private string EffectiveVisual(TemplateDraft draft)=>!string.IsNullOrEmpty(draft.Presentation.ActorVisualKey)?draft.Presentation.ActorVisualKey:
        _skin.LookupTemplate(draft.SourceTemplateId)?.ActorVisualKey??"none";
    private void ShowIdentityDialog(TemplateDraft target)
    {
        var dialog=new ConfirmationDialog{Title="姓名与道号",OkButtonText="应用",CancelButtonText="取消"};
        var list=new VBoxContainer{CustomMinimumSize=new(420,156)};dialog.AddChild(list);
        var surname=new LineEdit{Text=target.NpcFields.Surname,Editable=!_readOnly};Row(list,"姓",surname);
        var given=new LineEdit{Text=target.NpcFields.GivenName,Editable=!_readOnly};Row(list,"名",given);
        var dao=new LineEdit{Text=target.NpcFields.DaoTitle,Editable=!_readOnly};Row(list,"道号",dao);
        dialog.GetOkButton().Disabled=_readOnly;
        dialog.Confirmed+=()=>{if(CanChange(target)){target.NpcFields.Surname=surname.Text;target.NpcFields.GivenName=given.Text;target.NpcFields.DaoTitle=dao.Text;DraftEdited(target);RefreshDetails();}dialog.QueueFree();};
        dialog.Canceled+=()=>dialog.QueueFree();AddChild(dialog);TemplateDialogLayout.Show(dialog,new(480,250));
    }
    private void ShowExtendedFields(TemplateDraft target)
    {
        var dialog=new AcceptDialog{Title="扩展字段与支持状态",OkButtonText="关闭"};
        TemplateDialogLayout.AddScrollableText(dialog,
            $"初始年龄：{target.NpcFields.InitAge}（当前出生流程尚未消费）\n初始境界：{target.NpcFields.InitCultivationLevel}（尚未接通）\n特质：{string.Join("、",target.NpcFields.InitialTraits)}（尚未生效）\n"+
            "五行亲和由 Soul 生成；道途尚未由模板主功法回写。\n这些已有字段会保留在草稿和导出中。\n\n额外属性：\n"+
            string.Join("\n",target.NpcFields.InitStat.Where(p=>!new[]{"QiXue","TiPo","QiGan","LingJi","ShenShi","WuXing","JiYuan","MeiLi"}.Contains(p.Key)).Select(p=>p.Key+" = "+p.Value))+
            "\n\n旧卡组："+string.Join("、",target.NpcFields.InitCardDeck));
        dialog.Confirmed+=()=>dialog.QueueFree();AddChild(dialog);TemplateDialogLayout.Show(dialog,new(650,360));
    }
    private void ShowUnresolved(TemplateDraft target,UnresolvedTemplateBinding binding)
    {
        var dialog=new ConfirmationDialog{Title="未解析的导入项",OkButtonText="移除该错误项",CancelButtonText="保留"};
        TemplateDialogLayout.AddScrollableText(dialog,binding.FieldPath+"\n"+binding.Reason+"\n\n"+binding.Original.GetRawText());
        dialog.GetOkButton().Disabled=_readOnly;
        dialog.Confirmed+=()=>{if(CanChange(target)){target.UnresolvedBindings.Remove(binding);target.MarkDirty();RefreshEditor();}dialog.QueueFree();};
        dialog.Canceled+=()=>dialog.QueueFree();AddChild(dialog);TemplateDialogLayout.Show(dialog,new(650,300));
    }
}
