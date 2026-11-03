"""Author deterministic, explicit development fixtures; not a runtime map generator."""
import json, math
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / 'Data'
LEGACY = Path(__file__).parent / 'map_legacy_source'

from map_json_format import compact_json

def save(name, value):
    if name in ('TileDefines.json','RegionDefines.json','LandMarkDefines.json'):
        directory=DATA/Path(name).stem
        directory.mkdir(exist_ok=True)
        for item in value:
            (directory/(item['ID']+'.json')).write_text(compact_json([item])+'\n',encoding='utf-8')
        old=(DATA/name).resolve()
        if old.parent != DATA.resolve(): raise ValueError('aggregate target escaped Data')
        if old.exists(): old.unlink()
    else: (DATA/name).write_text(compact_json(value)+'\n',encoding='utf-8')
def aura(values): return dict(zip(('Jin','Mu','Shui','Huo','Tu'), values))
def env(e=.5,m=.5,t=.5,emin=0,emax=1,mmin=0,mmax=1,tmin=0,tmax=1,strength=6):
    return dict(Elevation=dict(Min=emin,Max=emax),Moisture=dict(Min=mmin,Max=mmax),Temperature=dict(Min=tmin,Max=tmax),PreferredElevation=e,PreferredMoisture=m,PreferredTemperature=t,PreferenceStrength=strength)
# Independently configured same-level tile definitions. Legacy seven aura values preserved exactly.
rows = [
('plain','平原','#b9c77c',(.30,.50,.60),3,(1,1,1,1,1),'平野',1),
('hill','丘陵','#9caa67',(.48,.48,.55),4,(1,.8,.8,.8,1.5),'丘陵',1.4),
('mountain','山地','#8a907c',(.72,.42,.45),5,(2,.5,.8,.8,1.2),'山类',2.2),
('river','河流','#509bb6',(.30,.70,.55),8,(.8,1.2,2.5,.5,.8),'水系',2.5),
('ocean','东海','#356a97',(.03,.92,.55),8,(.5,.8,4,.3,.5),'水系',3),
('desert','沙漠','#d7b36a',(.40,.15,.75),4,(1,.3,.3,2.5,1.5),'干旱',2),
('forest','森林','#4b8250',(.42,.72,.58),5,(.8,2.5,1.2,.5,.8),'林木',1.6),
('forest_mountain','林山','#496b4e',(.70,.75,.55),5,(1.5,2,.9,.5,1.1),'山类 林木',2.4),
('barren_mountain','荒山','#a59480',(.70,.20,.60),4,(2,.2,.3,1.2,1.6),'山类 干旱',2.4),
('volcano','火山','#9c5547',(.75,.20,.85),0,(1,.1,.2,4,2),'山类 炎热',3.5),
('snow_mountain','雪山','#d9e2e0',(.88,.45,.15),2,(1.8,.2,2,.1,1.4),'山类 寒冷',3.5),
('plateau','高原','#b2a47c',(.80,.30,.32),4,(1.4,.6,.7,.8,2),'高地',2),
('loess_hill','黄土丘陵','#c2a15e',(.50,.32,.55),4,(1.2,.6,.6,1,2.2),'丘陵 干旱',1.6),
('basin','盆地','#a0b379',(.24,.58,.62),3,(.8,1.3,1.2,.8,1.8),'平野',1.2),
('grassland','草原','#9ab75e',(.43,.40,.52),5,(.8,1.8,.8,.7,1),'草地',1.2),
('wetland','湿地','#608f7e',(.22,.86,.65),3,(.5,1.8,2,.3,1.2),'湿润',2.2),
('rainforest','雨林','#285b42',(.40,.90,.90),4,(.6,3,1.8,.8,.8),'林木 湿润 炎热',2.4),
('gobi','戈壁','#bba88a',(.56,.12,.50),4,(1.7,.2,.2,1.5,1.8),'干旱',1.8),
('karst','喀斯特峰林','#769580',(.60,.78,.75),3,(1.3,1.3,1.6,.5,1.3),'山类 林木',2.2),
('red_cliff','丹霞山','#b47a5c',(.60,.50,.72),3,(1.6,.6,.7,1.5,1.7),'山类',2.4),
('bamboo_forest','竹海','#658e4c',(.42,.80,.72),3,(.6,3,1.1,.5,.7),'林木',1.6),
('alpine_meadow','高山草甸','#9da884',(.78,.55,.28),3,(1,1.6,1,.2,1.2),'高地 草地 寒冷',1.7),
('coast','滨海平地','#b9c3a5',(.12,.75,.68),3,(.6,1.1,2,.4,1.2),'海岸',1.1),
('lake','湖泊','#559aaf',(.20,.90,.50),2,(.6,1.4,3,.3,.8),'水系',2.5),
('great_wall','绝界长城','#514f56',(.75,.25,.50),0,(2,.1,.1,.1,3),'世界边界',1),
('dry_valley','干热河谷','#b29567',(.30,.30,.82),3,(1,.7,.5,2,1.5),'干旱 炎热',1.5),
]
tiles=[]
for ident,name,color,pref,weight,initial,tags,cost in rows:
    rule=env(*pref)
    if ident in ('desert','gobi','barren_mountain'): rule['Moisture']['Max']=.62
    if ident=='rainforest': rule['Moisture']['Min']=.50; rule['Temperature']['Min']=.55
    if ident=='snow_mountain': rule['Temperature']['Max']=.47; rule['Elevation']['Min']=.50
    if ident=='forest_mountain': rule['Moisture']['Min']=.30; rule['Elevation']['Min']=.40
    if ident in ('mountain','plateau','alpine_meadow'): rule['Elevation']['Min']=.35
    if ident=='wetland': rule['Moisture']['Min']=.45; rule['Elevation']['Max']=.65
    if ident=='coast': rule['Elevation']['Max']=.38
    if ident=='plain': rule=env(*pref,strength=3) # Explicit broad candidate; no implicit fallback in generator.
    water='river' if ident=='river' else ident if ident in ('ocean','lake') else 'none'
    modes=[] if ident=='great_wall' else ['boat','fly'] if water!='none' else ['walk','fly']
    tiles.append(dict(SchemaVersion=2,ID=ident,DisplayName=name,Description=f'开发样例：{name}代表一片地域，包含格内小尺度景观；不等同真实中国位置。',Tags=tags.split(),Color=color,Symbol=name[0],InitialAura=aura(initial),AllowFill=ident not in ('volcano','great_wall'),FillWeight=weight,Environment=rule,AllowedNeighborIds=[],ForbiddenNeighborIds=[],NeighborWeights={ident:8},MovementModes=modes,MovementCost=cost,WaterKind=water,BlocksAllMovement=ident=='great_wall',ResourceRefs=[],EncounterRefs=[]))
# Preference by geological family. Same-type growth remains strongest.
for tile in tiles:
    for other in tiles:
        if tile['ID'] != other['ID'] and set(tile['Tags']) & set(other['Tags']): tile['NeighborWeights'][other['ID']]=1.6
save('TileDefines.json',tiles)

legacy=json.loads((LEGACY/'RegionDefines.json').read_text(encoding='utf-8-sig'))
legacy_map=['plain','hill','mountain','river','ocean','desert','forest']
for item in legacy:
    item['SchemaVersion']=2
    item['MinCount']=1 if item.pop('IsGuaranteed',False) else 0
    item['PlacementTypeIds']=[legacy_map[v] for v in item.pop('PlacementTerrains',[])]
    item['TerrainLayout']={k:legacy_map[v] for k,v in item['TerrainLayout'].items()}
regions=legacy

def disk(radius): return [(q,r) for r in range(-radius,radius+1) for q in range(-radius,radius+1) if max(abs(q),abs(r),abs(-q-r))<=radius]
def region(ident,name,center,inner,outer,radius,pref,min_count=0,hole=False,landmark=None):
    footprint=disk(radius)
    if hole: footprint=[p for p in footprint if p not in ((1,-1),(-2,1))]
    layout={f'{q},{r}':center if (q,r)==(0,0) else inner if max(abs(q),abs(r),abs(q+r))<radius-1 else outer for q,r in footprint}
    er=env(*pref,strength=3)
    # Broad but real hard thresholds make authored pieces selective without brittle per-cell equality.
    if center in ('mountain','forest_mountain','barren_mountain','snow_mountain','plateau'): er['Elevation']['Min']=.26
    if center=='forest_mountain': er['Moisture']['Min']=.28
    if center=='barren_mountain': er['Moisture']['Max']=.72
    if center=='snow_mountain': er['Temperature']['Max']=.50
    if center=='rainforest': er['Temperature']['Min']=.50; er['Moisture']['Min']=.40
    obj=dict(SchemaVersion=2,ID=ident,DisplayName=name,MinCount=min_count,MaxCount=-1,SpawnWeight=5,MinSpacing=radius*2+3,Width=radius*2+1,Height=radius*2+1,TerrainLayout=layout,LandMarkLayout={'0,0':landmark} if landmark else {},ExtraTerrainIds=[],AuraProfile=aura((0,0,0,0,0)),Tags=['开发样例','中国地貌参考'],AnchorEnvironment=er,OverallEnvironment=er,RequiredEnvironmentFraction=.65,KeyEnvironment=[],EnvironmentAdjustments=[],RiverCells=[],RiverPorts=[],KeyPoints=[dict(ID='center',Dq=0,Dr=0)],PlacementTypeIds=[])
    regions.append(obj)
    return obj
region('region_taihang','太行叠嶂样例','mountain','mountain','hill',5,(.70,.45,.45),1,landmark='natural_stone_gate')
region('region_qinling','秦岭林山样例','forest_mountain','forest_mountain','forest',5,(.68,.72,.52),1,hole=True,landmark='natural_ancient_tree')
region('region_qilian','祁连荒山样例','barren_mountain','barren_mountain','gobi',4,(.70,.25,.40),1,landmark='natural_mineral_vein')
region('region_changbai','长白火山样例','volcano','mountain','forest',4,(.65,.55,.38),landmark='natural_volcanic_pillar')
region('region_kunlun','昆仑雪山样例','snow_mountain','snow_mountain','alpine_meadow',5,(.82,.40,.20),landmark='natural_snow_spring')
region('region_tibetan','高原草甸样例','plateau','alpine_meadow','plateau',6,(.76,.42,.28),hole=True)
region('region_loess','黄土沟壑样例','loess_hill','loess_hill','grassland',5,(.52,.32,.53))
region('region_sichuan','蜀地盆地样例','basin','plain','hill',6,(.30,.65,.67),landmark='natural_leyline_eye')
region('region_central_plain','中州平野样例','plain','plain','grassland',6,(.32,.48,.58))
region('region_yunmeng','云梦湿地样例','wetland','wetland','plain',4,(.25,.82,.65),hole=True,landmark='natural_reed_marsh')
region('region_lingnan','岭南雨林样例','rainforest','rainforest','karst',5,(.43,.83,.84),landmark='natural_ancient_tree')
region('region_gobi','大漠戈壁样例','gobi','gobi','desert',5,(.48,.18,.60))
region('region_guilin','桂地峰林样例','karst','karst','bamboo_forest',4,(.56,.75,.74),hole=True,landmark='natural_karst_cave')
region('region_danxia','赤壁丹霞样例','red_cliff','red_cliff','hill',4,(.56,.48,.70))
region('region_bamboo','蜀南竹海样例','bamboo_forest','bamboo_forest','forest',4,(.42,.76,.69),landmark='natural_ancient_tree')
# Three fixed water cells and four fixed banks. Entry/exit neighbors deliberately outside footprint.
for direction,label in ((0,'东流'),(1,'东南流'),(5,'东北流')):
    dirs=[(1,0),(0,1),(-1,1),(-1,0),(0,-1),(1,-1)]
    dq,dr=dirs[direction]
    river=[(n*dq,n*dr) for n in range(3)]
    excluded={(-dq,-dr),(3*dq,3*dr)}
    bankset=set()
    for q,r in river:
        for bq,br in disk(2):
            cell=(q+bq,r+br)
            if cell not in excluded and cell not in river: bankset.add(cell)
    banks=sorted(bankset,key=lambda p:(max(abs(p[0]),abs(p[1]),abs(sum(p))),p[1],p[0]))[:4]
    layout={f'{q},{r}':'river' for q,r in river}
    layout.update({f'{q},{r}':'forest_mountain' if q%2 else 'hill' for q,r in banks})
    obj=dict(SchemaVersion=2,ID=f'region_river_valley_{direction}',DisplayName=f'{label}山谷样例',MinCount=1 if direction==0 else 0,MaxCount=8,SpawnWeight=4,MinSpacing=12,Width=7,Height=7,TerrainLayout=layout,LandMarkLayout={},ExtraTerrainIds=[],AuraProfile=aura((0,0,.2,0,0)),Tags=['开发样例','涉水地区'],AnchorEnvironment=env(strength=0),OverallEnvironment=env(strength=0),RequiredEnvironmentFraction=1,KeyEnvironment=[],EnvironmentAdjustments=[],RiverCells=[dict(Dq=q,Dr=r,InDirections=[(direction+3)%6],OutDirection=direction,IsSource=False,IsSink=False) for q,r in river],RiverPorts=[dict(Dq=0,Dr=0,Direction=(direction+3)%6,IsInlet=True),dict(Dq=2*dq,Dr=2*dr,Direction=direction,IsInlet=False)],KeyPoints=[],PlacementTypeIds=[])
    regions.append(obj)
next(r for r in regions if r['ID']=='region_taihang')['LandMarkLayout']['1,0']='building_old_observatory'
# Six compact fixed templates. All anchors have even Offset row parity, so repeated rectangles tile exactly.
def fixed_region(ident,name,tile,width,height,positions,conditions):
    layout={f'{col-row//2},{row}':tile for row in range(height) for col in range(width)}
    return dict(SchemaVersion=2,ID=ident,DisplayName=name,Width=width,Height=height,
        ForcedPositions=[dict(Col=col,Row=row) for col,row in positions],
        FixedEnvironment=dict(zip(('Elevation','Moisture','Temperature'),conditions)),
        BlendFixedEnvironment=True,AllowEnvironmentCliff=tile in ('ocean','great_wall'),
        FixedVariation=dict(Elevation=.012 if tile=='ocean' else .055,Moisture=.04,Temperature=.035),
        TerrainLayout=layout,LandMarkLayout={},SpawnWeight=0,MaxCount=0,MinSpacing=999,
        Tags=['开发样例','固定地区'])
regions.extend([
    fixed_region('fixed_west_wall','西界长城','great_wall',5,10,[(0,r) for r in range(0,200,10)],(.75,.25,.5)),
    fixed_region('fixed_east_sea','东方海域','ocean',10,10,[(190,r) for r in range(0,200,10)],(.02,.95,.6)),
    fixed_region('fixed_north_mountains','北境群山','mountain',10,10,[(c,0) for c in range(5,115,10)],(.86,.35,.18)),
    fixed_region('fixed_north_gobi','北境大漠','gobi',5,10,[(c,0) for c in range(115,190,5)],(.60,.12,.25)),
    fixed_region('fixed_south_desert','南境沙漠','desert',5,10,[(c,190) for c in range(5,80,5)],(.40,.12,.92)),
    fixed_region('fixed_south_rainforest','南境雨林','rainforest',10,10,[(c,190) for c in range(80,190,10)],(.35,.92,.90)),
])
# Three authored lake pieces. Connected water corridors reach the declared ports.
for ident,name,w,h,variant in [('lake_plain','平原大湖样例',15,11,0),('lake_mountain','狭长山间湖样例',19,9,1),('lake_wetland','湖群湿地样例',17,13,2)]:
    layout={}
    mid=h//2
    for r in range(h):
        for col in range(w):
            if variant==2 and col==0 and r==0: continue # Authored footprint hole, not implicit land.
            q=col-r//2
            nx=(col-(w-1)/2)/((w-3)/2); ny=(r-mid)/((h-3)/2)
            wet=nx*nx+ny*ny < (1 if variant!=2 else .86+.22*math.cos(col))
            if r==mid: wet=True
            # Port mouths are three cells across so a wide main river can enter without erasing shore.
            if col in (0,w-1) and abs(r-mid)<=1: wet=True
            tile='lake' if wet else ('mountain' if variant==1 else 'wetland' if variant==2 else 'plain')
            # Authored island; it does not split the connected lake.
            if variant==0 and col==w//2 and r==mid-2: tile='forest'
            layout[f'{q},{r}']=tile
    shores=[k for k,v in layout.items() if v!='lake']
    landmarks={shores[0]:'natural_reed_marsh'}
    if variant==0: landmarks[shores[1]]='building_old_observatory'
    regions.append(dict(SchemaVersion=2,ID=ident,DisplayName=name,Width=w,Height=h,
        GenerationPhase='lake',Lake=dict(Role='closed' if variant==2 else 'flowthrough',Ports=[dict(Dq=-(mid//2),Dr=mid,Direction=3,IsInlet=True),dict(Dq=w-1-mid//2,Dr=mid,Direction=0,IsInlet=False)] if variant!=2 else [dict(Dq=-(mid//2),Dr=mid,Direction=3,IsInlet=True)]),
        MinCount=1,MaxCount=1,SpawnWeight=0,MinSpacing=25,TerrainLayout=layout,
        LandMarkLayout=landmarks,AuraProfile=aura((0,0,0,0,0)),
        AnchorEnvironment=env(strength=0),OverallEnvironment=env(strength=0),Tags=['开发样例','大湖']))
save('RegionDefines.json',regions)

landmarks=json.loads((LEGACY/'LandMarkDefines.json').read_text(encoding='utf-8-sig'))
for lm in landmarks:
    lm['SchemaVersion']=2;lm['PlacementTypeIds']=[legacy_map[v] for v in lm.pop('PlacementTerrains',[])];lm['PlacementTags']=[];lm['AuraOffset']=aura((0,0,0,0,0))
# The original three retain zero offsets. New samples explicitly separate initial values and future rates.
for ident,name,tags,offset,mods in [
('natural_stone_gate','天门石阙',['山类'],(2,0,0,0,1),[]),
('natural_ancient_tree','千年古木',['林木'],(0,4,1,0,0),['aura_ancient_forest']),
('natural_mineral_vein','露天矿脉',['山类'],(3,0,0,0,2),[]),
('natural_snow_spring','冰川灵泉',['寒冷'],(0,0,4,0,0),[]),
('natural_reed_marsh','芦荡灵泽',['湿润'],(0,2,3,0,0),[]),
('natural_karst_cave','溶洞天窗',['山类'],(1,0,2,0,1),[]),
]:
    landmarks.append(dict(SchemaVersion=2,ID=ident,DisplayName=name,IsNatural=True,IsSingleton=False,SpawnWeight=1,PlacementTypeIds=[],PlacementTags=tags,PlacementExtraTerrains=[],ModifierDefineIds=mods,AuraOffset=aura(offset),Tags=['开发样例','自然奇观']))
landmarks.append(dict(SchemaVersion=2,ID='building_old_observatory',DisplayName='古观星台',IsNatural=False,IsSingleton=False,SpawnWeight=0,PlacementTypeIds=[],PlacementTags=[],PlacementExtraTerrains=[],ModifierDefineIds=[],AuraOffset=aura((0,0,0,3,0)),Tags=['开发样例','人工建筑']))
save('LandMarkDefines.json',landmarks)

print(json.dumps(dict(tiles=len(tiles),regions=len(regions),landmarks=len(landmarks),fixedTemplates=[dict(id=r['ID'],cells=len(r['TerrainLayout']),anchors=len(r['ForcedPositions'])) for r in regions if r.get('ForcedPositions')]),ensure_ascii=False))
