"""Idempotent canonical river migration; backups stay outside runtime Data loading."""
import argparse
import json
import shutil
from pathlib import Path
from map_json_format import compact_json


def migrate(root):
    backup = root / 'tools' / 'map_water_v2_backup'
    changed = 0
    def convert(value):
        if isinstance(value, str):
            return 'river' if value == 'river_lake' else value
        if isinstance(value, list):
            return [convert(v) for v in value]
        if isinstance(value, dict):
            result = {}
            for key, v in value.items():
                target = convert(key)
                if target in result:
                    raise ValueError(f'conflicting canonical key {target}')
                result[target] = convert(v)
            return result
        return value
    plans = []
    types = set()
    content = []
    for folder in ('TileDefines', 'RegionDefines', 'LandMarkDefines'):
        for path in sorted((root / 'Data' / folder).glob('*.json')):
            original = json.loads(path.read_text(encoding='utf-8-sig'))
            result = convert(original)
            content.extend(result)
            if folder == 'TileDefines': types.update(item['ID'] for item in result)
            target = path.with_name('river.json') if path.name == 'river_lake.json' else path
            if result == original and target == path: continue
            if target != path and target.exists():
                existing = json.loads(target.read_text(encoding='utf-8-sig'))
                def semantics(rows):
                    return [{k:v for k,v in row.items() if k not in ('DisplayName','Description','Symbol')} for row in rows]
                if semantics(result) != semantics(existing): raise ValueError('river definition conflict')
            plans.append((folder,path,target,result))
    for item in content:
        refs = list(item.get('TerrainLayout',{}).values())
        for field in ('PlacementTypeIds','AllowedNeighborIds','ForbiddenNeighborIds','RiverTypeIds','LakeTypeIds','OceanTypeIds'):
            refs.extend(item.get(field,[]))
        refs.extend(item.get('NeighborWeights',{}))
        if any(ref not in types for ref in refs): raise ValueError('unknown water/type reference in '+item['ID'])
    for folder,path,target,result in plans:
        saved = backup / folder / path.name
        saved.parent.mkdir(parents=True, exist_ok=True)
        if not saved.exists(): shutil.copy2(path,saved)
        if target == path or not target.exists(): target.write_text(compact_json(result)+'\n',encoding='utf-8')
        if target != path: path.unlink()
        changed += 1
    return changed


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    print('WATER_MIGRATION_OK changed=' + str(migrate(args.root)))
