"""Strict batch audit + lossless legacy map-data migration. Default/check never writes.

python tools/audit_world_map.py --check
python tools/audit_world_map.py --source tools/map_legacy_source --migrate --output ../docs/map-migration
python tools/audit_world_map.py --self-test
"""
import argparse, copy, hashlib, json, math, re, shutil, tempfile
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
KINDS=('TileDefines','RegionDefines','LandMarkDefines','TileModifierDefines')
LEGACY={0:'plain',1:'hill',2:'mountain',3:'river',4:'ocean',5:'desert',6:'forest'}

class AuditError(ValueError): pass

def pairs_hook(pairs):
    value={}; seen=set()
    for key,child in pairs:
        if key.casefold() in seen: raise AuditError(f'duplicate JSON field {key!r}')
        seen.add(key.casefold());value[key]=child
    return value

def read(path):
    try:
        return json.loads(path.read_text(encoding='utf-8-sig'),object_pairs_hook=pairs_hook,parse_constant=lambda s: (_ for _ in ()).throw(AuditError(f'non-finite number {s}')))
    except (ValueError,OSError) as error: raise AuditError(f'{path}: {error}') from error

def sources(root,kind):
    old=root/(kind+'.json')
    return ([old] if old.is_file() else [])+sorted((root/kind).rglob('*.json'))

def load(root,require_all=True):
    output={};locations={}
    for kind in KINDS:
        output[kind]={}
        files=sources(root,kind)
        if require_all and not files: raise AuditError(f'{root}/{kind}: missing definitions')
        for path in files:
            rows=read(path)
            if not isinstance(rows,list) or not rows: raise AuditError(f'{path}: expected nonempty array')
            for row in rows:
                ident=row.get('ID') if isinstance(row,dict) else None
                if not isinstance(ident,str) or not ident.strip(): raise AuditError(f'{path}: missing ID')
                if ident in output[kind]: raise AuditError(f'{path}: duplicate ID {ident}')
                output[kind][ident]=row;locations[(kind,ident)]=str(path)
    return output,locations

def coords(layout,path):
    if not isinstance(layout,dict): raise AuditError(f'{path}: coordinate map required')
    seen=set()
    for key in layout:
        if not re.fullmatch(r'[+-]?\d+,[+-]?\d+',key): raise AuditError(f'{path}: invalid axial coordinate {key!r}')
        q,r=map(int,key.split(','))
        if not (-2147483648<=q<=2147483647 and -2147483648<=r<=2147483647): raise AuditError(f'{path}/{key}: coordinate exceeds Int32')
        if (q,r) in seen: raise AuditError(f'{path}/{key}: duplicate normalized coordinate')
        seen.add((q,r))
    return seen

def finite_tree(value,path=''):
    if isinstance(value,float) and not math.isfinite(value): raise AuditError(f'{path}: non-finite number')
    if isinstance(value,dict):
        for key,child in value.items(): finite_tree(child,path+'.'+key)
        if 'Min' in value and 'Max' in value and not 0<=value['Min']<=value['Max']<=1: raise AuditError(f'{path}: invalid Min/Max')
        for key in ('FillWeight','SpawnWeight','MovementCost','PreferenceStrength'):
            if key in value and value[key]<0: raise AuditError(f'{path}.{key}: negative value')
        if 'NeighborWeights' in value and any(w<0 for w in value['NeighborWeights'].values()): raise AuditError(f'{path}: negative neighbor weight')
    if isinstance(value,list):
        for i,child in enumerate(value): finite_tree(child,f'{path}[{i}]')

def audit(root):
    data,locations=load(root)
    def refs(ids,kind,path):
        for ident in ids:
            if ident not in data[kind]: raise AuditError(f'{path}: unknown {kind} ID {ident!r}')
    for kind,collection in data.items():
        for ident,item in collection.items():
            path=locations[(kind,ident)]+'/'+ident
            finite_tree(item,path)
            if kind!='TileModifierDefines' and item.get('SchemaVersion')!=2: raise AuditError(f'{path}: SchemaVersion=2 required')
            if 'PlacementTerrains' in item or 'IsGuaranteed' in item: raise AuditError(f'{path}: legacy fields require migration')
            if kind=='TileDefines':
                for key in ('AllowedNeighborIds','ForbiddenNeighborIds'): refs(item.get(key,[]),'TileDefines',path+'.'+key)
                refs(item.get('NeighborWeights',{}),'TileDefines',path+'.NeighborWeights')
                if item.get('WaterKind','none') not in ('none','river','lake','ocean'): raise AuditError(f'{path}: invalid WaterKind')
                if item.get('BlocksAllMovement') and item.get('MovementModes'): raise AuditError(f'{path}: block/movement conflict')
            if kind=='RegionDefines':
                footprint=coords(item.get('TerrainLayout',{}),path+'.TerrainLayout')
                if not footprint: raise AuditError(f'{path}: empty footprint')
                lm=coords(item.get('LandMarkLayout',{}),path+'.LandMarkLayout')
                if not lm<=footprint: raise AuditError(f'{path}: landmark outside actual footprint')
                refs(item['TerrainLayout'].values(),'TileDefines',path+'.TerrainLayout')
                refs(item.get('LandMarkLayout',{}).values(),'LandMarkDefines',path+'.LandMarkLayout')
                refs(item.get('PlacementTypeIds',[]),'TileDefines',path+'.PlacementTypeIds')
                minimum=item.get('MinCount',0);maximum=item.get('MaxCount',-1)
                if minimum<0 or maximum < -1 or maximum!=-1 and maximum<minimum: raise AuditError(f'{path}: invalid MinCount/MaxCount')
                for field in ('KeyEnvironment','EnvironmentAdjustments','RiverCells','RiverPorts','KeyPoints'):
                    for point in item.get(field,[]):
                        if (point['Dq'],point['Dr']) not in footprint: raise AuditError(f'{path}.{field}: coordinate outside footprint')
                for point in item.get('RiverPorts',[]):
                    if not 0<=point['Direction']<6: raise AuditError(f'{path}: bad river port direction')
            if kind in ('RegionDefines','LandMarkDefines'):
                seen=set()
                for point in item.get('ForcedPositions',[]):
                    key=(point['Col'],point['Row'])
                    if not (0<=key[0]<200 and 0<=key[1]<200) or key in seen: raise AuditError(f'{path}: invalid or duplicate forced position')
                    seen.add(key)
            if kind=='LandMarkDefines':
                refs(item.get('PlacementTypeIds',[]),'TileDefines',path+'.PlacementTypeIds')
                refs(item.get('ModifierDefineIds',[]),'TileModifierDefines',path+'.ModifierDefineIds')
            if kind=='TileModifierDefines':
                duration=item.get('Duration',-1)
                if duration!=-1 and duration<=0: raise AuditError(f'{path}: invalid modifier duration')
    return dict(status='ok',counts={k:len(v) for k,v in data.items()},files=sum(len(sources(root,k)) for k in KINDS))

def map_type(value,path):
    if type(value) is int and value in LEGACY:return LEGACY[value]
    if isinstance(value,str) and value:return value
    raise AuditError(f'{path}: unknown legacy terrain value {value!r}')

def migrate_rows(kind,rows,report,path):
    result=copy.deepcopy(rows)
    for row in result:
        loc=f'{path}/{row.get("ID","<missing>")}'
        if kind=='TileModifierDefines':
            report.append(dict(path=loc,action='preserved AuraData as per-second rate; no initial aura conversion'))
            continue
        if row.get('SchemaVersion')==2:
            if 'IsGuaranteed' in row or 'PlacementTerrains' in row: raise AuditError(f'{loc}: mixed new and legacy fields')
            if kind=='RegionDefines' and any(not isinstance(v,str) for v in row.get('TerrainLayout',{}).values()): raise AuditError(f'{loc}: schema 2 contains legacy terrain numbers')
            continue
        if row.get('SchemaVersion') not in (None,1): raise AuditError(f'{loc}: unknown schema version')
        if kind=='RegionDefines':
            if 'MinCount' in row or 'PlacementTypeIds' in row: raise AuditError(f'{loc}: mixed new and legacy fields')
            footprint=coords(row.get('TerrainLayout',{}),loc+'.TerrainLayout')
            if not footprint: raise AuditError(f'{loc}: empty footprint')
            if not coords(row.get('LandMarkLayout',{}),loc+'.LandMarkLayout')<=footprint: raise AuditError(f'{loc}: landmark outside footprint')
            row['TerrainLayout']={k:map_type(v,loc+'.TerrainLayout/'+k) for k,v in row['TerrainLayout'].items()}
            row['MinCount']=1 if row.pop('IsGuaranteed',False) else 0
            row['PlacementTypeIds']=[map_type(v,loc+'.PlacementTerrains') for v in row.pop('PlacementTerrains',[])]
            report.append(dict(path=loc,action='kept exact footprint and landmarks; Width/Height remain metadata; IsGuaranteed -> MinCount',footprint=len(footprint),placementConditions=row['PlacementTypeIds']))
        elif kind=='LandMarkDefines':
            if 'PlacementTypeIds' in row: raise AuditError(f'{loc}: mixed new and legacy fields')
            row['PlacementTypeIds']=[map_type(v,loc+'.PlacementTerrains') for v in row.pop('PlacementTerrains',[])]
            row.setdefault('PlacementTags',[])
            row.setdefault('AuraOffset',dict(Jin=0,Mu=0,Shui=0,Huo=0,Tu=0))
            report.append(dict(path=loc,action='terrain IDs converted; initial offset zero; modifiers preserved as rates'))
        else: raise AuditError(f'{loc}: no legacy converter for {kind}; supply explicit schema 2')
        row['SchemaVersion']=2
    finite_tree(result,path)
    return result

def migrate(source,output=None):
    data,locations=load(source,require_all=False)
    report=[];pending={}
    for kind in KINDS:
        for path in sources(source,kind):
            rows=read(path);pending[path]=migrate_rows(kind,rows,report,str(path))
    if not pending: raise AuditError('no map definition source files')
    # Validate all conversions before writing anything; source is never changed.
    if output:
        output=output.resolve()
        if output==source.resolve() or source.resolve() in output.parents: raise AuditError('migration output must be outside source directory')
        for path,rows in pending.items():
            rel=path.relative_to(source)
            original=output/'source'/rel;target=output/rel
            original.parent.mkdir(parents=True,exist_ok=True);target.parent.mkdir(parents=True,exist_ok=True)
            if original.exists() and original.read_bytes()!=path.read_bytes(): raise AuditError(f'{original}: existing source backup differs')
            if not original.exists(): shutil.copy2(path,original)
            target.write_text(json.dumps(rows,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
        (output/'migration-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    return dict(status='migration-preview' if not output else 'migrated',files=len(pending),changes=len(report),report=report)

def self_test():
    n=0
    original=[dict(ID='old',Width=8,Height=8,IsGuaranteed=True,PlacementTerrains=[2],TerrainLayout={'0,0':2,'1,0':6},LandMarkLayout={},ExtraTerrainIds=['retained'],AuraProfile={'Jin':1})]
    report=[];new=migrate_rows('RegionDefines',original,report,'fixture')
    assert new[0]['TerrainLayout']=={'0,0':'mountain','1,0':'forest'};n+=1
    assert new[0]['PlacementTypeIds']==['mountain'] and new[0]['MinCount']==1 and new[0]['ExtraTerrainIds']==['retained'];n+=1
    assert original[0]['IsGuaranteed'] and len(new[0]['TerrainLayout'])==2;n+=1
    assert migrate_rows('RegionDefines',new,[],'fixture')==new;n+=1
    bad=copy.deepcopy(original);bad[0]['TerrainLayout']['0,0']=99
    for action in (lambda:migrate_rows('RegionDefines',bad,[],'fixture'),lambda:json.loads('{"ID":1,"ID":2}',object_pairs_hook=pairs_hook),lambda:coords({'0,0':'a','+0,0':'b'},'fixture'),lambda:finite_tree({'SpawnWeight':-1},'fixture')):
        try:action()
        except AuditError:n+=1
        else:raise AssertionError('expected explicit rejection')
    return dict(status='self-test-ok',assertions=n)

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source',type=Path,default=ROOT/'Data');parser.add_argument('--check',action='store_true');parser.add_argument('--migrate',action='store_true');parser.add_argument('--output',type=Path);parser.add_argument('--self-test',action='store_true')
    args=parser.parse_args()
    try:
        result=self_test() if args.self_test else migrate(args.source,args.output) if args.migrate else audit(args.source)
        print(json.dumps(result,ensure_ascii=False,indent=2))
    except (AuditError,KeyError,TypeError) as error:
        print(json.dumps(dict(status='error',detail=str(error)),ensure_ascii=False));return 1
    return 0
if __name__=='__main__':raise SystemExit(main())
