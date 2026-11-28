"""Build the Godot Spine character rigs from the in-game flat-color reference and variant sheets."""

import argparse
import hashlib
import json
import math
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageOps

from duel_humanoid import add_painted_pieces, apply_paint, export_import_copy, standardize, unpack_atlas


HAIR = ('jade', 'cinnabar', 'ink', 'braid')
FACES = ('angular', 'soft', 'slender', 'bronze', 'freckled', 'blush')
EYES = ('jade', 'ink', 'cinnabar', 'violet', 'hazel')
BROWS = ('stern', 'even', 'arched', 'worried')
MOUTHS = ('calm', 'smile', 'grit', 'gasp')
ROBES = ('jade', 'cinnabar', 'indigo', 'violet')
PANTS = ('charcoal', 'ash')
BOOTS = ('dark', 'violet')
ACCESSORIES = ('diadem', 'ribbon', 'butterfly', 'pin')

# Paint order follows the reference. The cape and ponytail move behind the body.
SLOT_BONES = (
    ('hairRear', 'hair'), ('cape', 'cape'), ('pantsBack', 'thighBack'), ('bootBack', 'shinBack'),
    ('pantsFront', 'thighFront'), ('bootFront', 'shinFront'), ('armBack', 'armBack'),
    ('elbowBack', 'elbowBack'), ('robeTails', 'cloth'), ('robe', 'torso'), ('weapon', 'weapon'),
    ('handBack', 'handBack'), ('armFront', 'armFront'), ('elbowFront', 'elbowFront'), ('handFront', 'handFront'),
    ('face', 'head'), ('eyes', 'head'), ('brows', 'head'), ('mouth', 'head'),
    ('hairFront', 'head'), ('accessory', 'hair'),
)

HIERARCHY = (
    ('hip', 'root'), ('thighBack', 'hip'), ('shinBack', 'thighBack'),
    ('thighFront', 'hip'), ('shinFront', 'thighFront'), ('cloth', 'hip'), ('torso', 'hip'), ('cape', 'torso'),
    ('armBack', 'torso'), ('elbowBack', 'armBack'), ('handBack', 'elbowBack'), ('weapon', 'handBack'),
    ('armFront', 'torso'), ('elbowFront', 'armFront'), ('handFront', 'elbowFront'),
    ('head', 'torso'), ('hair', 'head'),
)


def polygon_mask(size, points):
    mask = Image.new('L', size)
    ImageDraw.Draw(mask).polygon([tuple(point) for point in points], fill=255)
    return mask


def crop_part(im, points):
    mask = polygon_mask(im.size, points)
    alpha = ImageChops.multiply(im.getchannel('A'), mask)
    bbox = alpha.point(lambda value: 255 if value >= 90 else 0).getbbox()
    if not bbox:
        raise ValueError('Empty cutout')
    result = im.crop(bbox)
    result.putalpha(alpha.crop(bbox))
    center = ((bbox[0] + bbox[2]) / 2, (bbox[1] + bbox[3]) / 2)
    return result, center, result.size


def detach_hair_ornament(source, design):
    # The clasp is drawn over the hair in the source pose. Replace its visible paint on the hair layer
    # so choosing no ornament leaves the hair, while the separate ornament restores the exact marks.
    mask = polygon_mask(source.size, design['cuts']['accessory'])
    result = source.copy()
    pixels = result.load()
    x0, y0, x1, y1 = mask.getbbox()
    male = design['orientation'] > 0
    for y in range(y0, y1):
        for x in range(x0, x1):
            r, g, b, alpha = pixels[x, y]
            if not alpha or not mask.getpixel((x, y)):
                continue
            ornamental = g - r > 6 and g > 75 if male else r - g > 19 and r > 75
            if ornamental:
                light = max(0, min(34, (max(r, g, b) - 70) // 5))
                base = (40, 33, 31) if male else (45, 30, 38)
                pixels[x, y] = tuple(channel + light for channel in base) + (alpha,)
    return result


def covered_regions(source, cuts, rig, slot):
    """Restore the cloth hidden under overlapping layers so it stays coherent when the bones separate."""
    if slot in ('pantsBack', 'pantsFront'):
        clothing = polygon_mask(source.size, cuts['robeTails'])
        if 'cape' in cuts:
            clothing = ImageChops.lighter(clothing, polygon_mask(source.size, cuts['cape']))
        fabric = source.copy()
        draw = ImageDraw.Draw(fabric)
        points = [tuple(point) for point in cuts[slot]]
        fill = '#443a3d' if rig == 'male' else '#503d41'
        draw.polygon(points, fill=fill)
        return Image.composite(fabric, source, clothing)
    if rig == 'female' and slot in ('armBack', 'elbowBack'):
        # The wood and its cord cover the cuff in the posed art. Restore the sleeve across that area.
        charm = polygon_mask(source.size, cuts['weapon'])
        fabric = source.copy()
        draw = ImageDraw.Draw(fabric)
        draw.polygon([(428, 789), (470, 779), (476, 823), (465, 846), (440, 852), (428, 842)], fill='#524341')
        draw.polygon([(423, 817), (449, 812), (474, 835), (472, 861), (452, 878), (428, 867)], fill='#cbb3a1')
        draw.line([(429, 820), (447, 827), (468, 825)], fill='#302524', width=7)
        draw.line([(433, 845), (454, 848), (472, 840)], fill='#4b3c39', width=5)
        return Image.composite(fabric, source, charm)
    return source


def read_reference(source, design, rig_name, pieces):
    parts = {}
    bare_hair = detach_hair_ornament(source, design)
    for slot, points in design['cuts'].items():
        im = bare_hair if slot in ('hairFront', 'hairRear') else covered_regions(source, design['cuts'], rig_name, slot)
        if slot == 'face':
            # The flat fill under the separate eye, brow and mouth keeps swaps clean.
            im = source.copy()
            draw = ImageDraw.Draw(im)
            sample = source.getpixel(tuple(design['skinSample']))
            for feature in ('eyes', 'brows', 'mouth'):
                draw.polygon([tuple(p) for p in design['cuts'][feature]], fill=sample)
        region, center, size = crop_part(im, points)
        if slot in ('eyes', 'brows', 'mouth'):
            # Keep the source ink and whites. Skin under the swappable face details belongs to the face slot.
            region.putdata([(r, g, b, 0 if r >= 145 and g >= 96 and r - g > 13 and r - b > 27 else a)
                            for r, g, b, a in region.get_flattened_data()])
        if design['orientation'] < 0:
            region = ImageOps.mirror(region)
        name = f'ref_{rig_name}_{slot}'
        region.thumbnail((244, 244), Image.Resampling.LANCZOS)
        pieces[name] = region
        parts[slot] = (name, center, size)
    return parts


def load_sheet(pieces, path, names, rows, xs=None, ys=None):
    with Image.open(path) as image:
        sheet = image.convert('RGBA')
    xs = xs or [round(i * sheet.width / 4) for i in range(5)]
    ys = ys or [round(i * sheet.height / rows) for i in range(rows + 1)]
    for i, name in enumerate(names):
        row, col = divmod(i, 4)
        columns = xs[row] if isinstance(xs[0], tuple) else xs
        part = sheet.crop((columns[col], ys[row], columns[col + 1], ys[row + 1]))
        bounds = part.getchannel('A').point(lambda value: 255 if value >= 96 else 0).getbbox()
        if not bounds:
            raise ValueError(f'Empty variant {name}')
        part = part.crop(bounds)
        part.thumbnail((177, 177), Image.Resampling.LANCZOS)
        pieces[name] = part


def expression_parts(pieces):
    paths = {
        'brow_even': ([(3, 12), (19, 6), (36, 8), (57, 17), (34, 14), (13, 15)], '#211917'),
        'brow_arched': ([(2, 18), (16, 6), (29, 3), (47, 7), (57, 14), (29, 9), (12, 14)], '#2d2221'),
        'brow_worried': ([(2, 16), (17, 12), (30, 7), (52, 3), (46, 10), (27, 16), (14, 20)], '#2b1e1c'),
        'eye_blink': ([(2, 8), (16, 14), (31, 17), (49, 10), (59, 5), (49, 15), (30, 21), (15, 19)], '#30211d'),
        'eye_hurt': ([(2, 16), (18, 9), (34, 4), (56, 10), (46, 14), (30, 10), (15, 17)], '#30211d'),
        'mouth_smile': ([(3, 5), (20, 10), (36, 9), (56, 3), (43, 14), (25, 16), (11, 11)], '#633a31'),
        'mouth_grit': ([(3, 10), (20, 4), (54, 4), (58, 16), (42, 22), (19, 21), (4, 16)], '#33201c'),
        'mouth_gasp': ([(5, 15), (12, 5), (29, 2), (49, 8), (57, 17), (41, 22), (20, 20)], '#482726'),
    }
    for name, (shape, ink) in paths.items():
        image = Image.new('RGBA', (61, 24))
        draw = ImageDraw.Draw(image)
        draw.polygon(shape, fill=ink)
        if name == 'mouth_grit':
            draw.polygon([(10, 11), (21, 8), (50, 8), (52, 13), (39, 17), (18, 17)], fill='#f5dfc8')
        if name == 'mouth_gasp':
            draw.polygon([(18, 15), (34, 13), (47, 17), (38, 19), (20, 18)], fill='#ae6654')
        pieces[name] = image.crop(image.getbbox())


def write_atlas(output, pieces):
    width, height = 2048, 2048
    atlas = Image.new('RGBA', (width, height))
    x, y, line_height = 3, 3, 0
    lines = ['duel_parts.png', f'size: {width},{height}', 'filter: Linear,Linear', 'pma: false']
    for name, part in sorted(pieces.items(), key=lambda item: -item[1].height):
        if x + part.width + 3 > width:
            x, y, line_height = 3, y + line_height + 6, 0
        if y + part.height + 3 > height:
            raise ValueError('Atlas is full')
        atlas.paste(part, (x, y))
        lines.extend((name, f'bounds: {x},{y},{part.width},{part.height}'))
        x += part.width + 6
        line_height = max(line_height, part.height)
    atlas.save(output / 'duel_parts.png', optimize=True)
    (output / 'duel_parts.atlas').write_text('\n'.join(lines) + '\n', encoding='utf-8')


# Local joint rotations keyed from the new guard stances. The observer owns world-space movement.
POSES = {
    'Ready': {},
    'ReadyCentered': {'torso': 5, 'armFront': -22, 'elbowFront': 25, 'armBack': 24, 'head': -3},
    'ReadyLow': {'torso': -10, 'armFront': 15, 'elbowFront': 12, 'armBack': 12, 'thighBack': -15, 'thighFront': 19, 'shinFront': -13},
    'Windup': {'torso': 12, 'head': -7, 'armBack': 26, 'elbowBack': -12, 'armFront': 22, 'elbowFront': 18, 'cape': -15, 'hair': -16, 'cloth': -9},
    'Strike': {'torso': -17, 'head': 9, 'armBack': -143, 'elbowBack': -8, 'weapon': -30, 'armFront': -70, 'elbowFront': -31, 'cape': -25, 'thighBack': -23, 'thighFront': 14, 'shinFront': -17},
    'Slash': {'torso': -22, 'head': 12, 'armBack': -150, 'elbowBack': -27, 'weapon': -45, 'armFront': -64, 'elbowFront': -24, 'cape': -29, 'hair': -23, 'cloth': -23, 'thighBack': -19, 'thighFront': 19, 'shinFront': -24},
    'Smash': {'torso': -25, 'head': 15, 'armBack': -133, 'elbowBack': -48, 'weapon': -47, 'armFront': 34, 'elbowFront': 21, 'cape': -24, 'hair': -26, 'thighBack': 13, 'thighFront': 30, 'shinBack': -16, 'shinFront': -29},
    'Shot': {'torso': -13, 'head': 9, 'armFront': 22, 'elbowFront': 5, 'armBack': -20, 'elbowBack': 23, 'cape': -21, 'hair': -11, 'cloth': -11, 'thighBack': -11},
    'Guard': {'torso': 7, 'head': -10, 'armBack': -66, 'elbowBack': -48, 'weapon': -30, 'armFront': 34, 'elbowFront': 62, 'handFront': -36, 'thighBack': -7, 'thighFront': 9, 'shinFront': -13},
    'Recoil': {'torso': 31, 'head': -11, 'armBack': 24, 'elbowBack': -25, 'armFront': -34, 'elbowFront': -21, 'cape': 23, 'hair': 32, 'cloth': 17, 'thighBack': -6, 'thighFront': -27, 'shinFront': 34},
    'Brace': {'torso': 13, 'head': -19, 'armBack': 14, 'armFront': -30, 'elbowFront': 36, 'cape': 16, 'hair': 11, 'thighBack': -20, 'thighFront': 32, 'shinBack': 25, 'shinFront': -38, 'cloth': 16},
    'Advance': {'torso': -21, 'head': 11, 'armBack': -45, 'elbowBack': -22, 'armFront': 18, 'elbowFront': 52, 'cape': -27, 'hair': -17, 'cloth': -17, 'thighBack': -25, 'thighFront': 20, 'shinBack': 14},
    'Withdraw': {'torso': 14, 'head': -15, 'armBack': 14, 'elbowBack': -21, 'armFront': -10, 'elbowFront': 28, 'cape': 16, 'hair': 17, 'cloth': 12, 'thighBack': -15, 'thighFront': -6, 'shinFront': 25},
    'Cast': {'torso': 7, 'head': -6, 'armBack': -22, 'elbowBack': -18, 'armFront': 30, 'elbowFront': 56, 'handFront': -25, 'hair': -11, 'cape': -9, 'thighFront': 7},
    'Evade': {'torso': 28, 'head': -28, 'armBack': 19, 'elbowBack': -32, 'armFront': -35, 'elbowFront': 46, 'cape': 18, 'hair': -26, 'cloth': 18, 'thighBack': -11, 'thighFront': 28, 'shinBack': 20, 'shinFront': -51},
    'Land': {'torso': -20, 'head': 14, 'armBack': 25, 'elbowBack': -21, 'armFront': -24, 'elbowFront': 27, 'cape': 14, 'hair': 20, 'cloth': 13, 'thighBack': -19, 'thighFront': 28, 'shinBack': 31, 'shinFront': -40},
    'FollowThrough': {'torso': -22, 'head': 9, 'armBack': -120, 'elbowBack': -41, 'weapon': -56, 'armFront': -48, 'elbowFront': -39, 'cape': -24, 'hair': -14, 'cloth': -11, 'thighBack': -12, 'thighFront': 16, 'shinFront': -12},
    'Kneel': {'torso': -19, 'head': -15, 'armBack': -36, 'elbowBack': -36, 'armFront': -69, 'elbowFront': 24, 'cape': -10, 'hair': 13, 'cloth': 11, 'thighBack': -44, 'thighFront': 35, 'shinBack': 86, 'shinFront': -58},
}

FEMALE_ANGLES = {
    'Strike': {'armBack': -172, 'elbowBack': 78, 'weapon': 107},
    'Slash': {'armBack': -184, 'elbowBack': 88, 'weapon': 110},
    'Smash': {'armBack': -172, 'elbowBack': 58, 'weapon': 128},
    'FollowThrough': {'armBack': -163, 'elbowBack': 32, 'weapon': 137},
    'Guard': {'armBack': -23, 'elbowBack': -9, 'weapon': 26},
    'Cast': {'armBack': -72, 'elbowBack': 45, 'weapon': 18},
    'Shot': {'armBack': -53, 'elbowBack': 23, 'weapon': 40},
}
CLIPS = {
    'Ready': [(0, 'Ready', .0), (1.2, 'Ready', .5), (2.4, 'Ready', .0)],
    'ReadyCentered': [(0, 'ReadyCentered', .0), (1.2, 'ReadyCentered', .5), (2.4, 'ReadyCentered', .0)],
    'ReadyLow': [(0, 'ReadyLow', .0), (1.2, 'ReadyLow', .5), (2.4, 'ReadyLow', .0)],
    'Windup': [(0, 'Ready', .2), (.10, 'Windup', 1)],
    'Strike': [(0, 'Windup', .85), (.10, 'Strike', 1), (.23, 'Strike', .73)],
    'Slash': [(0, 'Windup', 1), (.10, 'Slash', 1), (.24, 'FollowThrough', .9)],
    'Smash': [(0, 'Windup', 1), (.045, 'Guard', .9), (.10, 'Smash', 1), (.24, 'Smash', .72)],
    'Shot': [(0, 'Cast', .6), (.08, 'Shot', 1), (.24, 'Shot', .64)],
    'Guard': [(0, 'Guard', .8), (.075, 'Guard', 1), (.23, 'Guard', .74)],
    'Recoil': [(0, 'Guard', .7), (.047, 'Recoil', 1), (.14, 'Recoil', .8)],
    'Brace': [(0, 'Recoil', .73), (.075, 'Brace', 1), (.23, 'Brace', .77)],
    'Advance': [(0, 'Advance', .53), (.10, 'Advance', 1), (.22, 'Advance', .62), (.34, 'Advance', .98)],
    'Withdraw': [(0, 'Withdraw', .51), (.12, 'Withdraw', 1), (.25, 'Withdraw', .65)],
    'Cast': [(0, 'Guard', .34), (.14, 'Cast', 1), (.48, 'Cast', .92)],
    'Evade': [(0, 'Guard', .6), (.085, 'Evade', 1), (.25, 'Evade', .8)],
    'Land': [(0, 'Land', .6), (.045, 'Land', 1), (.16, 'Ready', .2)],
    'FollowThrough': [(0, 'FollowThrough', 1), (.23, 'Guard', .64)],
    'Kneel': [(0, 'Brace', .8), (.15, 'Kneel', 1), (.37, 'Kneel', 1)],
}




def rotate(x, y, degrees):
    cos, sin = math.cos(math.radians(degrees)), math.sin(math.radians(degrees))
    return x * cos - y * sin, x * sin + y * cos


def animation_data(bones, soles, female):
    animations = {}
    animated_bones = sorted({bone for pose in POSES.values() for bone in pose} | {'shinFront', 'shinBack'})
    for clip, entries in CLIPS.items():
        keys = []
        for time, pose, weight in entries:
            rotations = dict(POSES[pose])
            if female:
                rotations.update(FEMALE_ANGLES.get(pose, {}))
            if pose.startswith('Ready'):
                rotations['torso'] = rotations.get('torso', 0) - 4 * weight
                rotations.update(hair=-9 * weight, cape=-6 * weight, cloth=7 * weight)
            if pose == 'Advance':
                rotations['shinFront'] = -12 + 17 * math.sin(time * 22)
            if pose == 'Withdraw':
                rotations['shinBack'] = -14 * math.sin(time * 19)
            keys.append((time, {bone: degrees * weight for bone, degrees in rotations.items()}))

        def floor_offset(rotations):
            world = {'root': (0, 0, 0)}
            for bone in bones[1:]:
                x, y, angle = world[bone['parent']]
                dx, dy = rotate(bone['x'], bone['y'], angle)
                world[bone['name']] = (x + dx, y + dy, angle + rotations.get(bone['name'], 0))
            return -min(world[bone][1] + rotate(x, y, world[bone][2])[1]
                        for bone, points in soles.items() for x, y in points)

        tracks = {bone: {'rotate': [{'time': time, 'value': round(angles.get(bone, 0), 2)} for time, angles in keys]}
                  for bone in animated_bones}
        tracks['hip'] = {'translate': [{'time': time, 'y': round(floor_offset(angles), 2)} for time, angles in keys]}
        reaction = {'Strike': ('angry', 'grit'), 'Slash': ('angry', 'grit'), 'Smash': ('angry', 'grit'),
                    'Recoil': ('hurt', 'gasp'), 'Kneel': ('hurt', 'grit'), 'Brace': ('hurt', 'grit')}.get(clip)
        slots = {}
        if reaction:
            slots['eyes'] = {'attachment': [{'name': reaction[0]}]}
            slots['mouth'] = {'attachment': [{'name': reaction[1]}]}
            slots['brows'] = {'attachment': [{'name': 'stern' if reaction[0] == 'angry' else 'worried'}]}
        if clip.startswith('Ready'):
            slots['eyes'] = {'attachment': [{'time': 1.79, 'name': 'blink'}, {'time': 1.9, 'name': 'base'}]}
        animations[clip] = {'bones': tracks, 'slots': slots}
    return animations


def rig(name, design, reference, pieces):
    female = name == 'female'
    factor = 234 / design['height']
    direction = design['orientation']
    source_points = design['bones'] | {'root': design['origin']}
    slots = [dict(name=slot, bone=parent, attachment='base') for slot, parent in SLOT_BONES]
    slot_bones = dict(SLOT_BONES)
    skins = {'default': {}}

    def offset(point, origin):
        return ((point[0] - origin[0]) * factor * direction, (origin[1] - point[1]) * factor)

    bones = [{'name': 'root'}]
    for bone, parent in HIERARCHY:
        x, y = offset(source_points[bone], source_points[parent])
        bones.append(dict(name=bone, parent=parent, x=round(x, 3), y=round(y, 3)))
    soles = {bone: [offset(point, source_points[bone]) for point in points] for bone, points in design['soles'].items()}

    def attach(skin, slot, texture, position, size, rotation=0, key='base'):
        x, y = position
        width, height = size
        if height is None:
            height = width * pieces[texture].height / pieces[texture].width
        skins.setdefault(skin, {}).setdefault(slot, {})[key] = dict(
            path=texture, x=round(x, 2), y=round(y, 2), width=round(width, 2), height=round(height, 2), rotation=round(rotation, 2))

    def native(skin, slot, key='base', shrink=1):
        texture, center, size = reference[slot]
        attach(skin, slot, texture, offset(center, source_points[slot_bones[slot]]),
               (size[0] * factor, size[1] * factor * shrink), key=key)

    def variant(skin, slot, texture, size=None, dx=0, dy=0, rotation=0, key='base'):
        _, center, original = reference[slot]
        x, y = offset(center, source_points[slot_bones[slot]])
        attach(skin, slot, texture, (x + dx, y + dy), size or (original[0] * factor, original[1] * factor), rotation, key)

    identity_robe = 'cinnabar' if female else 'jade'
    identity_face = 'soft' if female else 'angular'
    identity_eye = 'cinnabar' if female else 'jade'
    identity_boot = 'violet' if female else 'dark'
    identity_accessory = 'ribbon' if female else 'diadem'

    for slot in ('handFront', 'handBack', 'elbowBack', 'elbowFront'):
        native('default', slot)

    for hair in HAIR:
        skin = 'hair/' + hair
        if hair == identity_robe:
            native(skin, 'hairFront')
            native(skin, 'hairRear')
        else:
            variant(skin, 'hairFront', 'hair_' + hair, (72 if female else 65, 52 if female else 53), dy=2)
            back = dict(jade=(-2, 12, 43), cinnabar=(-32, -5, 91), ink=(-14, -5, 47), braid=(-17, -18, 39))[hair]
            attach(skin, 'hairRear', 'back_' + hair, (back[0], back[1]), (back[2], None))

    for face in FACES:
        skin = 'face/' + face
        if face == identity_face:
            native(skin, 'face')
        else:
            alt = {'angular': 'slender', 'soft': 'blush'}.get(face, face)
            variant(skin, 'face', 'face_' + alt, (44 if face == 'slender' else 49, 46), dx=-2, dy=1)

    for eyes in EYES:
        skin = 'eyes/' + eyes
        for key in ('base', 'angry', 'hurt', 'blink'):
            if eyes == identity_eye and key in ('base', 'angry'):
                native(skin, 'eyes', key, .89 if key == 'angry' else 1)
            else:
                texture = 'eye_' + key if key in ('hurt', 'blink') else 'alt_eye_' + ('jade' if eyes == 'cinnabar' else eyes)
                variant(skin, 'eyes', texture, (14.5, 5 if key in ('hurt', 'blink') else 9), key=key)

    for brow in BROWS:
        skin = 'brows/' + brow
        for key in ('base', 'stern', 'worried'):
            choice = brow if key == 'base' else key
            if choice == 'stern':
                native(skin, 'brows', key)
            else:
                variant(skin, 'brows', 'brow_' + choice, (14, 5), rotation=-12, key=key)

    for mouth in MOUTHS:
        skin = 'mouth/' + mouth
        for key in ('base', 'grit', 'gasp'):
            choice = mouth if key == 'base' else key
            if choice == 'calm':
                native(skin, 'mouth', key)
            else:
                variant(skin, 'mouth', 'mouth_' + choice, (9, 3 if choice == 'smile' else 5), key=key)

    for accessory in ACCESSORIES:
        skin = 'accessory/' + accessory
        if accessory == identity_accessory:
            native(skin, 'accessory')
        else:
            variant(skin, 'accessory', 'ornament_' + accessory, (17 if accessory == 'pin' else 20, None))

    for robe in ROBES:
        skin = 'robe/' + robe
        if robe == identity_robe:
            for slot in ('robe', 'robeTails', 'armBack', 'armFront'):
                native(skin, slot)
            if not female:
                native(skin, 'cape')
        else:
            variant(skin, 'robe', 'robe_' + robe, (72, 61 if female else 71), dy=-3)
            variant(skin, 'robeTails', 'tails_' + robe, (102 if female else 93, 81), dy=2)
            for side in ('Back', 'Front'):
                slot = 'arm' + side
                variant(skin, slot, f'variant_{name}_{slot}_{robe}')

    for pants in PANTS:
        for side in ('Back', 'Front'):
            slot = 'pants' + side
            skin = 'pants/' + pants
            if pants == 'charcoal':
                native(skin, slot)
            else:
                parent, knee = source_points['thigh' + side], source_points['shin' + side]
                x, y = offset(knee, parent)
                angle = math.degrees(math.atan2(y, x)) + 90
                attach(skin, slot, 'alt_pants_' + side.lower(), (x * .44, y * .47), (57, math.hypot(x, y) * 1.52), angle)

    for boots in BOOTS:
        for side in ('Back', 'Front'):
            slot = 'boot' + side
            skin = 'boots/' + boots
            if boots == identity_boot:
                native(skin, slot)
            else:
                face = 'back' if female and side == 'Back' else 'front'
                variant(skin, slot, 'alt_boot_' + boots + '_' + face)

    for weapon in ('sword', 'talisman', 'spear', 'fan', 'bronze', 'saber'):
        skin = 'weapon/' + weapon
        if weapon == ('talisman' if female else 'sword'):
            native(skin, 'weapon')
            continue
        poses = {'sword': (-28, -12, 76, 204), 'talisman': (0, -23, 18, 0),
                 'spear': (-35, -17, 88, 204), 'fan': (-17, -8, 49, 201),
                 'bronze': (0, -20, 25, 0), 'saber': (-27, -12, 74, 199)}
        x, y, width, angle = poses[weapon]
        attach(skin, 'weapon', 'alt_' + weapon, (x, y), (width, None), angle)

    data = {'bones': bones, 'slots': slots, 'skins': [{'name': k, 'attachments': v} for k, v in skins.items()],
            'animations': animation_data(bones, soles, female)}
    identity = hashlib.sha256(json.dumps(data).encode()).hexdigest()[:16]
    return dict(skeleton=dict(hash=identity, spine='4.2.0', width=314, height=234, x=-152, y=0, images='./parts/'), **data)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('heads', type=Path, nargs='?')
    parser.add_argument('clothes', type=Path, nargs='?')
    parser.add_argument('props', type=Path, nargs='?')
    parser.add_argument('--from-atlas', type=Path, help='Use the existing published rigs and atlas as the source project.')
    parser.add_argument('--output', type=Path, default=Path('assets/art/battle/spine'))
    parser.add_argument('--import-copy', type=Path, help='Copy the two rigs and loose parts into a Spine 4.2 import folder.')
    parser.add_argument('--appearance-report', type=Path, help='Add each tested appearance as a dressed Spine import.')
    args = parser.parse_args()
    designs = json.loads(Path(__file__).with_name('duel_reference_parts.json').read_text(encoding='utf-8'))
    if args.from_atlas:
        if any((args.heads, args.clothes, args.props)):
            parser.error('--from-atlas already supplies the original wardrobe and animations.')
        pieces = unpack_atlas(args.from_atlas)
        rigs = {name: json.loads((args.from_atlas / f'duel_{name}.json').read_text(encoding='utf-8'))
                for name in ('male', 'female')}
    else:
        if not all((args.heads, args.clothes, args.props)):
            parser.error('Provide --from-atlas or all three original variant sheets.')
        rigs, pieces = rebuild_original(args, designs)

    add_painted_pieces(pieces, Path(__file__).with_name('art'))
    for name, result in rigs.items():
        standardize(result, designs[name])
        apply_paint(result, pieces, 'cinnabar' if name == 'female' else 'jade')

    args.output.mkdir(parents=True, exist_ok=True)
    write_atlas(args.output, pieces)
    for name, result in rigs.items():
        (args.output / f'duel_{name}.json').write_text(json.dumps(result, separators=(',', ':')) + '\n', encoding='utf-8')
    if args.import_copy:
        samples = json.loads(args.appearance_report.read_text(encoding='utf-8'))['samples'] if args.appearance_report else []
        export_import_copy(args.import_copy, rigs, pieces, samples)


def rebuild_original(args, designs):
    with Image.open(designs['source']) as sheet:
        source = sheet.convert('RGBA')
    pieces = {}
    originals = {name: read_reference(source, designs[name], name, pieces) for name in ('male', 'female')}

    load_sheet(pieces, args.heads, [f'hair_{name}' for name in HAIR] + [f'back_{name}' for name in HAIR]
               + [f'face_{name}' for name in FACES[2:]] + ['ornament_diadem', 'ornament_butterfly', 'ornament_pin', 'ornament_ribbon'],
               4, xs=((0, 327, 656, 983, 1312), (0, 291, 691, 991, 1312), (0, 327, 656, 983, 1312), (0, 327, 656, 983, 1312)),
               ys=(0, 318, 650, 940, 1199))
    load_sheet(pieces, args.clothes, [f'robe_{name}' for name in ROBES] + [f'tails_{name}' for name in ROBES]
               + ['sleeve_jade', 'sleeve_cinnabar', 'alt_pants_front', 'alt_pants_back']
               + ['alt_boot_dark_front', 'alt_boot_violet_front', 'alt_sword', 'alt_talisman'],
               4, xs=((0, 334, 655, 978, 1312), (0, 334, 658, 976, 1312),
                      (0, 334, 657, 989, 1312), (0, 334, 653, 1044, 1312)), ys=(0, 330, 627, 895, 1199))
    for name in ('sleeve_jade', 'sleeve_cinnabar'):
        pieces.pop(name)
    load_sheet(pieces, args.props, ['alt_spear', 'alt_fan', 'alt_bronze', 'alt_saber']
               + ['alt_eye_jade', 'alt_eye_ink', 'alt_eye_hazel', 'alt_eye_violet'], 2)
    for boot in BOOTS:
        pieces['alt_boot_' + boot + '_back'] = ImageOps.mirror(pieces['alt_boot_' + boot + '_front'])
    # Alternate sleeves follow the native seams at both ends, so sleeves and bracers stay joined through the clips.
    cloth_colors = {'jade': '#ddd1c0', 'cinnabar': '#bf755d', 'indigo': '#999cb6', 'violet': '#a3879f'}
    for name in originals:
        for slot in ('armFront', 'armBack'):
            native = pieces[originals[name][slot][0]]
            for robe, color in cloth_colors.items():
                if (name, robe) in (('male', 'jade'), ('female', 'cinnabar')):
                    continue
                sleeve = ImageOps.colorize(native.convert('L'), '#191719', color)
                sleeve.putalpha(native.getchannel('A'))
                pieces[f'variant_{name}_{slot}_{robe}'] = sleeve

    expression_parts(pieces)
    return {name: rig(name, designs[name], originals[name], pieces) for name in ('male', 'female')}, pieces


if __name__ == '__main__':
    main()
