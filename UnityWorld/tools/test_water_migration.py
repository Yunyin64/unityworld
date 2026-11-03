"""Migration failure preflight, original backup and idempotence checks in disposable copies."""
import json
import shutil
import tempfile
from pathlib import Path
from migrate_water_types import migrate

source = Path(__file__).resolve().parents[1]
with tempfile.TemporaryDirectory(prefix='unityworld-water-migration-') as tmp:
    root = Path(tmp)
    shutil.copytree(source / 'Data', root / 'Data')
    for old in (source / 'tools/map_water_v2_backup').rglob('*.json'):
        dest = root / 'Data' / old.relative_to(source / 'tools/map_water_v2_backup')
        shutil.copy2(old, dest)
    old_aura = json.loads((root / 'Data/TileDefines/river_lake.json').read_text(encoding='utf-8-sig'))[0]['InitialAura']
    assert migrate(root) == 6
    assert migrate(root) == 0
    assert json.loads((root / 'Data/TileDefines/river.json').read_text(encoding='utf-8-sig'))[0]['InitialAura'] == old_aura
    assert (root / 'tools/map_water_v2_backup/TileDefines/river_lake.json').exists()
    assert all('river_lake' not in f.read_text(encoding='utf-8-sig') for d in ('TileDefines','RegionDefines','LandMarkDefines') for f in (root / 'Data' / d).glob('*.json'))
    target = root / 'Data/TileDefines/plain.json'
    rows = json.loads(target.read_text(encoding='utf-8'))
    rows[0]['NeighborWeights'] = {'river_lake': 1, 'river': 2}
    target.write_text(json.dumps(rows), encoding='utf-8')
    before = target.read_bytes()
    try:
        migrate(root)
        raise AssertionError('expected conflicting normalized key')
    except ValueError as error:
        assert 'conflicting' in str(error)
    assert target.read_bytes() == before
    rows[0]['NeighborWeights'] = {'missing-type': 1}
    target.write_text(json.dumps(rows), encoding='utf-8')
    try:
        migrate(root)
        raise AssertionError('expected unknown reference')
    except ValueError as error:
        assert 'unknown' in str(error)
print('WATER_MIGRATION_TESTS_OK assertions=9')
