"""Assemble the painted duel parts on the project's shared Spine 4.2 humanoid."""

import hashlib
import json
import math
from copy import deepcopy
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageOps


OLD_NAMES = {
    'root': 'Root', 'hip': 'Hips', 'torso': 'Spine', 'head': 'Head',
    'armFront': 'UpperArm_L', 'elbowFront': 'Forearm_L', 'handFront': 'Hand_L',
    'armBack': 'UpperArm_R', 'elbowBack': 'Forearm_R', 'handBack': 'Hand_R',
    'thighFront': 'Thigh_L', 'shinFront': 'Shin_L', 'thighBack': 'Thigh_R', 'shinBack': 'Shin_R',
    'cloth': 'cloth', 'cape': 'cape', 'hair': 'hair', 'weapon': 'weapon',
}
PARENTS = {
    'Root': None, 'Hips': 'Root', 'Spine': 'Hips', 'Chest': 'Spine', 'Neck': 'Chest', 'Head': 'Neck',
    'Clavicle_L': 'Chest', 'UpperArm_L': 'Clavicle_L', 'Forearm_L': 'UpperArm_L', 'Hand_L': 'Forearm_L',
    'Clavicle_R': 'Chest', 'UpperArm_R': 'Clavicle_R', 'Forearm_R': 'UpperArm_R', 'Hand_R': 'Forearm_R',
    'Thigh_L': 'Hips', 'Shin_L': 'Thigh_L', 'Foot_L': 'Shin_L',
    'Thigh_R': 'Hips', 'Shin_R': 'Thigh_R', 'Foot_R': 'Shin_R',
    'cloth': 'Hips', 'cape': 'Chest', 'hair': 'Head', 'weapon': 'Hand_R',
}
SLOT_OVERRIDES = {'robe': 'Chest', 'bootFront': 'Foot_L', 'bootBack': 'Foot_R'}
ROBE_PARTS = ('robe', 'neck', 'robeTails', 'armBack', 'elbowBack', 'handBack', 'armFront', 'elbowFront', 'handFront')
ROBES = ('jade', 'cinnabar', 'indigo', 'violet')
LOWER_PARTS = ('pantsFront', 'shinFront', 'bootFront', 'pantsBack', 'shinBack', 'bootBack')

# Small ranks draw first. All joins have distinct layers so the covering edge is intentional.
DEPTH = {
    'cape': -100, 'hairRear': -90,
    'shinBack': -80, 'bootBack': -78, 'pantsBack': -76,
    'shinFront': -66, 'bootFront': -64, 'pantsFront': -62,
    'elbowBack': 54, 'armBack': 58, 'weapon': 62, 'handBack': 66,
    'robeTails': -12, 'robe': 0,
    'elbowFront': -40, 'armFront': -36, 'handFront': -32,
    'face': 30, 'eyes': 32, 'brows': 34, 'mouth': 36, 'hairFront': 40, 'accessory': 44,
}

# Hand-placed cuts on the transparent drawings. Coordinates are fractions of the original part.
# The cut removes a drawn opening or duplicate tab, while retaining the original registration frame.
FAR_CUFF = ((0, .49), (.085, .54), (.125, .63), (.161, .74), (.173, .87), (.212, .97), (.183, 1), (0, 1))
THIGH_TAB = {
    'pantsFront': ((0, .40), (.28, .38), (.35, .40), (.405, .49), (.391, .61), (.374, .72), (.34, .815), (0, .825)),
    'pantsBack': ((0, .43), (.22, .425), (.284, .447), (.326, .51), (.32, .61), (.30, .75), (.25, .80), (0, .81)),
}

# Slash arcs through the space below the chin. The right fist leads, while the free hand stays lower.
SLASH_TIMES = (0, .05, .10, .16, .24)
SLASH_BODY = {
    'Spine': (12, 0, -17, -21, -12),
    'Head': (-7, 0, 14, 17, 10),
    'UpperArm_L': (22, -3, -54, -46, -20),
    'Forearm_L': (18, 20, 43, 34, 22),
}
SLASH_GRIP = {
    'jade': {
        'UpperArm_R': (26, 100, 179, 176, 155), 'Forearm_R': (-12, 18, 22, 17, 23),
        'Hand_R': (0, -4, 0, 6, 11), 'weapon': (0, -121, -54, -74, -83),
    },
    'cinnabar': {
        'UpperArm_R': (26, 98, 153, 144, 127), 'Forearm_R': (-12, 82, 125, 125, 115),
        'Hand_R': (0, -14, -33, -36, -31), 'weapon': (0, -166, -229, -214, -190),
    },
}
FORWARD_ATTACKS = {
    'Strike': (
        (0, .10, .23),
        ((0, 0), (13, -6), (7, -5)),
        {'Spine': (10.2, -14, -12), 'Head': (-5.95, 12, 10),
         'UpperArm_L': (18.7, -50, -43), 'Forearm_L': (15.3, 46, 31)},
        {'jade': {'UpperArm_R': (22.1, 173, 171), 'Forearm_R': (-10.2, 26, 20),
                  'Hand_R': (0, -2, 2), 'weapon': (0, -43, -56)},
         'cinnabar': {'UpperArm_R': (22.1, 149, 147), 'Forearm_R': (-10.2, 126, 122),
                      'Hand_R': (0, -32, -28), 'weapon': (0, -226, -214)}},
    ),
    'Smash': (
        (0, .045, .10, .24),
        ((0, 0), (1, 1), (11, -7), (5, -5)),
        {'Spine': (12, 6.3, -24, -16), 'Head': (-7, -9, 15, 13),
         'UpperArm_L': (22, 30.6, -43, -36), 'Forearm_L': (18, 55.8, 27, 25)},
        {'jade': {'UpperArm_R': (26, -59.4, 178, 172), 'Forearm_R': (-12, -43.2, 24, 23),
                  'Hand_R': (0, 0, 0, 5), 'weapon': (0, 0, -90, -89)},
         'cinnabar': {'UpperArm_R': (26, -20.7, 159, 143), 'Forearm_R': (-12, -8.1, 126, 119),
                      'Hand_R': (0, 0, -35, -32), 'weapon': (0, 23.4, -228, -214)}},
    ),
}

# Coordinates in each loose PNG: the closed hand covers the grip, and the other point defines its forward axis.
PROP_ANCHORS = {
    'ref_male_weapon': ((.88, .16), (.025, .97), 77, 0),
    'alt_sword': ((.16, .51), (.97, .51), 76, 0),
    'alt_spear': ((.29, .55), (.97, .53), 86, 0),
    'alt_saber': ((.17, .51), (.95, .07), 74, 0),
    'alt_fan': ((.09, .89), (.78, .21), 45, 12),
    'ref_female_weapon': ((.71, .09), (.62, .91), 25, 50),
    'alt_talisman': ((.51, .11), (.51, .93), 18, 50),
    'alt_bronze': ((.50, .11), (.50, .92), 21, 50),
}
REST_WEAPON_AIM = -140
WEAPON_AIM = {
    'Windup': (-140, -155),
    'Slash': (-155, -78, -7, -32, -51),
    'Strike': (-155, 0, -13),
    'Smash': (-155, 140, -49, -65),
    'Shot': (160, -152, -145),
    'Guard': (139, 122, 145),
    'FollowThrough': (-51, -140),
    'Cast': (154, 160, 160),
    'Kneel': (-140, -140, -140),
}
REAR_HAIR = {
    'back_jade': ((.63, .56), 43, 0),
    'ref_male_hairRear': ((.65, .79), 42, 0),
    'back_cinnabar': ((.56, .45), 71, -12),
    'back_ink': ((.72, .48), 47, 0),
    'back_braid': ((.25, .15), 35, -66),
}


def world_points(bones):
    points = {}
    for bone in bones:
        x, y = points.get(bone.get('parent'), (0, 0))
        points[bone['name']] = x + bone.get('x', 0), y + bone.get('y', 0)
    return points


def standardize(rig, design):
    if rig['bones'][0]['name'] == 'Root':
        return
    points = {OLD_NAMES[name]: point for name, point in world_points(rig['bones']).items()}
    origin_x, origin_y = design['origin']
    scale = 234 / design['height']
    for name, (x, y) in design['humanoid'].items():
        points[name] = (x - origin_x) * scale * design['orientation'], (origin_y - y) * scale

    # Keep the old joint locations except for the far shoulder fitted to the painted armhole.
    # The inserted parents have neutral rest rotations, so the existing keyed angles still apply.
    rig['bones'] = []
    for name, parent in PARENTS.items():
        bone = {'name': name}
        if parent is not None:
            x, y = points[name]
            px, py = points[parent]
            bone.update(parent=parent, x=round(x - px, 3), y=round(y - py, 3))
        rig['bones'].append(bone)

    slots = []
    for slot in rig['slots']:
        name = slot['name']
        former = OLD_NAMES[slot['bone']]
        current = SLOT_OVERRIDES.get(name, former)
        if former != current:
            # Keep positions in existing Mod declarations in the original slot coordinate space.
            shift = tuple(round(a - b, 3) for a, b in zip(points[former], points[current]))
            slot.update(attachmentOffsetX=shift[0], attachmentOffsetY=shift[1])
            for skin in rig['skins']:
                for part in skin['attachments'].get(name, {}).values():
                    part['x'] = round(part.get('x', 0) + shift[0], 3)
                    part['y'] = round(part.get('y', 0) + shift[1], 3)
        if name.startswith('boot'):
            slots.append({'name': name.replace('boot', 'shin'), 'bone': former, 'attachment': 'base'})
        slots.append(dict(slot, bone=current))
    rig['slots'] = slots
    for animation in rig['animations'].values():
        animation['bones'] = {OLD_NAMES[name]: tracks for name, tracks in animation['bones'].items()}


def read_painted_sheet(path, columns, rows):
    with Image.open(path) as source:
        sheet = source.convert('RGBA')
    # Trim only transparency between items that were painted as separate objects.
    alpha = sheet.getchannel('A').point(lambda value: value if value > 4 else 0)
    sheet.putalpha(alpha)
    mask = alpha.point(lambda value: 255 if value >= 96 else 0)

    def divisions(extents, size, count):
        if count == 1:
            return [0, size]
        empty = [offset for offset, present in enumerate(extents) if not present]
        boundaries = [0]
        for index in range(1, count):
            expected = size * index / count
            candidates = [offset for offset in empty if abs(offset - expected) <= size * .1]
            if not candidates:
                raise ValueError(f'{path}: paintings cross the gutter near {expected:.0f}.')
            boundaries.append(min(candidates, key=lambda offset: abs(offset - expected)))
        return boundaries + [size]

    xs = divisions(mask.resize((sheet.width, 1), Image.Resampling.BOX).tobytes(), sheet.width, columns)
    ys = divisions(mask.resize((1, sheet.height), Image.Resampling.BOX).tobytes(), sheet.height, rows)
    parts = []
    for y0, y1 in zip(ys, ys[1:]):
        for x0, x1 in zip(xs, xs[1:]):
            component = sheet.crop((x0, y0, x1, y1))
            bounds = component.getchannel('A').point(lambda value: 255 if value >= 24 else 0).getbbox()
            if bounds is None:
                raise ValueError(f'{path}: a painted component is missing.')
            component = component.crop(bounds)
            component.thumbnail((196, 196), Image.Resampling.LANCZOS)
            parts.append(component)
    return parts


def add_painted_pieces(pieces, directory):
    for style in ROBES:
        for slot, image in zip(ROBE_PARTS, read_painted_sheet(directory / f'{style}.png', 3, 3)):
            if slot in ('robe', 'neck'):
                continue
            if slot == 'armBack':
                image = trim(image, FAR_CUFF)
            pieces[f'paint_{slot}_{style}'] = image
        pieces.pop(f'paint_neck_{style}', None)
    for style, image in zip(ROBES, read_painted_sheet(directory / 'torso-neck.png', 2, 2)):
        pieces[f'paint_robe_{style}'] = image
    for style in ('dark', 'light'):
        for slot, image in zip(LOWER_PARTS, read_painted_sheet(directory / f'lower-{style}.png', 3, 2)):
            if style == 'dark' and slot in THIGH_TAB:
                image = trim(image, THIGH_TAB[slot])
            pieces[f'paint_{slot}_{style}'] = ImageOps.mirror(image) if slot == 'bootBack' else image
    for style, image in zip(('jade', 'cinnabar'), read_painted_sheet(directory / 'cape.png', 2, 1)):
        pieces[f'paint_cape_{style}'] = image


def trim(image, contour):
    width, height = image.width * 4, image.height * 4
    keep = Image.new('L', (width, height), 255)
    ImageDraw.Draw(keep).polygon([(round(x * width), round(y * height)) for x, y in contour], fill=0)
    image.putalpha(ImageChops.multiply(image.getchannel('A'), keep.resize(image.size, Image.Resampling.LANCZOS)))
    return image


def apply_depth(rig):
    for slot in rig['slots']:
        slot['depth'] = DEPTH[slot['name']]
    rig['slots'].sort(key=lambda slot: slot['depth'])

    # On the slash the sword arm crosses the chest, then moves back as the actor recovers.
    order = [slot['name'] for slot in rig['slots']]
    sword_arm = ['elbowBack', 'armBack', 'weapon', 'handBack']
    front = [slot for slot in order if slot not in sword_arm]
    chest = front.index('robe') + 1
    front[chest:chest] = sword_arm
    offsets = [{'slot': slot, 'offset': front.index(slot) - index} for index, slot in enumerate(order)
               if front[index] != slot]
    for clip in ('Strike', 'Slash', 'Smash'):
        rig['animations'][clip]['drawOrder'] = [{'time': .075, 'offsets': offsets}]
    rig['animations']['FollowThrough']['drawOrder'] = [{'time': 0, 'offsets': offsets}, {'time': .085}]


def apply_attack_poses(rig, identity):
    slash = SLASH_BODY | SLASH_GRIP[identity]
    attacks = {
        'Slash': (SLASH_TIMES, ((0, 0), (0, -1), (13, -5), (10, -5), (3, -3)), slash),
    }
    for clip, (times, shoulder, body, grip) in FORWARD_ATTACKS.items():
        attacks[clip] = times, shoulder, body | grip[identity]
    for clip, (times, shoulder, pose) in attacks.items():
        bones = rig['animations'][clip]['bones']
        for bone, angles in pose.items():
            bones[bone] = {'rotate': [{'time': time, 'value': angle} for time, angle in zip(times, angles)]}
        bones['Clavicle_R'] = {'translate': [dict(time=time, x=x, y=y) for time, (x, y) in zip(times, shoulder)]}

    recovery = rig['animations']['FollowThrough']['bones']
    for bone, angles in slash.items():
        # Start the gameplay recovery at the pose where the editor's Slash preview ends.
        guard = recovery.get(bone, {}).get('rotate', [{'value': 0}])[-1]['value']
        recovery[bone] = {'rotate': [{'time': 0, 'value': angles[-1]}, {'time': .23, 'value': guard}]}
    recovery['Clavicle_R'] = {'translate': [{'time': 0, 'x': 3, 'y': -3}, {'time': .23, 'x': 0, 'y': 0}]}


def sample_rotation(tracks, time):
    keys = tracks.get('rotate', [])
    for first, last in zip(keys, keys[1:]):
        if time < last['time']:
            blend = (time - first['time']) / (last['time'] - first['time'])
            return first['value'] + (last['value'] - first['value']) * blend
    return keys[-1]['value'] if keys else 0


def apply_kneel_pose(rig, identity):
    tracks = rig['animations']['Kneel']['bones']
    bones = {bone['name']: bone for bone in rig['bones']}
    times = [key['time'] for key in tracks['Hips']['translate']]
    # The near foot supports the body and the far knee folds under it. Release the raised guard.
    for name, end in {'UpperArm_R': 94 if identity == 'jade' else 82, 'Forearm_R': -13 if identity == 'jade' else -12,
                      'Hand_R': 12 if identity == 'jade' else 15, 'Thigh_R': 26, 'Shin_R': -120,
                      'Foot_R': 55, 'Foot_L': 23}.items():
        start = round(float(sample_rotation(tracks.get(name, {}), 0)), 2)
        tracks[name] = {'rotate': [{'time': time, 'value': end if time > 0 else start} for time in times]}

    def vertical_offset(name, degrees):
        radians = math.radians(degrees)
        bone = bones[name]
        return bone['x'] * math.sin(radians) + bone['y'] * math.cos(radians)

    for key in tracks['Hips']['translate'][1:]:
        thigh_angle = sample_rotation(tracks['Thigh_L'], key['time'])
        shin_angle = thigh_angle + sample_rotation(tracks['Shin_L'], key['time'])
        ankle_y = (bones['Hips']['y'] + bones['Thigh_L']['y'] + vertical_offset('Shin_L', thigh_angle)
                   + vertical_offset('Foot_L', shin_angle))
        key['y'] = round(24 * .60 - ankle_y, 2)  # The ankle is at 40% of the 24-unit boot height.


def apply_prop_rig(rig, pieces, identity):
    bone = next(bone for bone in rig['bones'] if bone['name'] == 'weapon')
    slot = next(slot for slot in rig['slots'] if slot['name'] == 'weapon')
    grip_x, grip_y = (-7, 1) if identity == 'jade' else (-3, 9)
    old_x = bone['x'] + slot.get('attachmentOffsetX', 0)
    old_y = bone['y'] + slot.get('attachmentOffsetY', 0)
    bone.update(x=grip_x, y=grip_y)
    # Existing external art authored on the former bone retains its setup coordinates.
    slot.update(attachmentOffsetX=round(old_x - grip_x, 3), attachmentOffsetY=round(old_y - grip_y, 3))

    for skin in rig['skins']:
        for part in skin['attachments'].get('weapon', {}).values():
            grip, tip, width, bias = PROP_ANCHORS[part['path']]
            image = pieces[part['path']]
            height = width * image.height / image.width
            dx, dy = (tip[0] - grip[0]) * width, (grip[1] - tip[1]) * height
            angle = math.radians(REST_WEAPON_AIM + bias) - math.atan2(dy, dx)
            x, y = (.5 - grip[0]) * width, (grip[1] - .5) * height
            part.update(width=width, height=round(height, 2), rotation=round(math.degrees(angle) % 360, 2),
                        x=round(x * math.cos(angle) - y * math.sin(angle), 2),
                        y=round(x * math.sin(angle) + y * math.cos(angle), 2))

    for name, clip in rig['animations'].items():
        tracks = clip['bones']
        if name not in WEAPON_AIM:
            if name in ('Ready', 'ReadyCentered', 'ReadyLow'):
                tracks['weapon'] = {'rotate': [{'time': 0, 'value': 0}, {'time': 2.4, 'value': 0}]}
            continue
        times = [key['time'] for key in tracks['weapon']['rotate']]
        # Bake the authored world aims into Spine-local keys by subtracting the current hand chain.
        times_and_aims = dict(zip(times, WEAPON_AIM[name], strict=True))
        chain = ('Hips', 'Spine', 'Chest', 'Clavicle_R', 'UpperArm_R', 'Forearm_R', 'Hand_R')
        all_times = sorted(set(times) | {key['time'] for parent in chain for key in tracks.get(parent, {}).get('rotate', [])})
        aims = []
        for time, aim in times_and_aims.items():
            if aims:
                prior = aims[-1]['value']
                aim = prior + ((aim - prior + 180) % 360 - 180)
            aims.append({'time': time, 'value': aim})
        tracks['weapon'] = {'rotate': [dict(time=time, value=round(sample_rotation({'rotate': aims}, time)
            - REST_WEAPON_AIM - sum(sample_rotation(tracks.get(parent, {}), time) for parent in chain), 2)) for time in all_times]}


def apply_head_rig(rig, pieces, identity):
    hair = next(bone for bone in rig['bones'] if bone['name'] == 'hair')
    rear = next(slot for slot in rig['slots'] if slot['name'] == 'hairRear')
    accessory = next(slot for slot in rig['slots'] if slot['name'] == 'accessory')
    root_x, root_y = (-18, 39) if identity == 'jade' else (-17, 35)
    rear.update(attachmentOffsetX=round(hair['x'] + rear.get('attachmentOffsetX', 0) - root_x, 3),
                attachmentOffsetY=round(hair['y'] + rear.get('attachmentOffsetY', 0) - root_y, 3))
    if accessory['bone'] == 'hair':
        accessory.update(bone='Head', attachmentOffsetX=hair['x'], attachmentOffsetY=hair['y'])
    hair.update(x=root_x, y=root_y)

    for skin in rig['skins']:
        slots = skin['attachments']
        if 'hairRear' in slots:
            part = slots['hairRear']['base']
            if part['path'] == 'ref_female_hairRear':
                # The original rear cutout includes fingers from the reference illustration.
                part['path'] = 'back_cinnabar'
            (u, v), width, angle = REAR_HAIR[part['path']]
            image = pieces[part['path']]
            height = width * image.height / image.width
            dx, dy = (.5 - u) * width, (v - .5) * height
            radians = math.radians(angle)
            part.update(width=width, height=round(height, 2), rotation=angle,
                        x=round(dx * math.cos(radians) - dy * math.sin(radians), 2),
                        y=round(dx * math.sin(radians) + dy * math.cos(radians), 2))
        if 'hairFront' in slots and slots['hairFront']['base']['path'] == 'ref_female_hairFront':
            slots['hairFront']['base'].update(path='hair_cinnabar', width=65, height=48, x=-1, y=26)
        if 'face' in slots and slots['face']['base']['path'] == 'ref_female_face':
            # Remove the baked bangs and eye shadow from the skin that accepts arbitrary front hair and eyes.
            slots['face']['base'].update(path='face_blush', x=-4.63, y=13.55, width=46, height=43)
        if 'accessory' in slots:
            part = slots['accessory']['base']
            part.update(x=root_x + 3, y=root_y + 5, width=round(min(16, part['width']), 2),
                        height=round(min(16, part['width']) * pieces[part['path']].height / pieces[part['path']].width, 2))


def apply_paint(rig, pieces, identity):
    # Neck remains in the bone hierarchy. The painted throat and collar travel together with the chest.
    rig['slots'] = [slot for slot in rig['slots'] if slot['name'] != 'neck']
    for skin in rig['skins']:
        skin['attachments'].pop('neck', None)
    for animation in rig['animations'].values():
        animation.get('slots', {}).pop('neck', None)
    apply_depth(rig)
    apply_attack_poses(rig, identity)
    apply_kneel_pose(rig, identity)
    points = world_points(rig['bones'])
    slots = {slot['name']: slot['bone'] for slot in rig['slots']}
    skins = {skin['name']: skin['attachments'] for skin in rig['skins']}

    def place(skin, slot, art, pivot, at, width, height=None, rotation=0):
        image = pieces[art]
        height = height if height is not None else width * image.height / image.width
        ax, ay = at
        bx, by = points[slots[slot]]
        dx, dy = (.5 - pivot[0]) * width, (pivot[1] - .5) * height
        radians = math.radians(rotation)
        x = ax - bx + dx * math.cos(radians) - dy * math.sin(radians)
        y = ay - by + dx * math.sin(radians) + dy * math.cos(radians)
        skins.setdefault(skin, {})[slot] = {'base': {
            'path': art, 'x': round(x, 2), 'y': round(y, 2), 'width': round(width, 2),
            'height': round(height, 2), 'rotation': round(rotation, 2), 'depth': DEPTH[slot],
        }}

    def fit(skin, slot, art, anchors, first, last, thickness=1):
        # Map painted joint centers to the same shoulder/elbow/wrist points for every outfit.
        image = pieces[art]
        a, b = anchors
        sx, sy = (b[0] - a[0]) * image.width, (a[1] - b[1]) * image.height * thickness
        tx, ty = last[0] - first[0], last[1] - first[1]
        scale = math.hypot(tx, ty) / math.hypot(sx, sy)
        rotation = math.degrees(math.atan2(ty, tx) - math.atan2(sy, sx))
        rotation = (rotation + 180) % 360 - 180
        place(skin, slot, art, a, first, image.width * scale, image.height * scale * thickness, rotation)

    neck, hips = points['Neck'], points['Hips']
    for style in ROBES:
        skin = 'robe/' + style
        art = lambda slot: f'paint_{slot}_{style}'
        # Chest covers the far shoulder flange. The near arm and wrist are drawn in front of it.
        belt = (hips[0] + 1, hips[1] + 1)
        neck_top = (neck[0] + 2, neck[1] + 5)
        place(skin, 'robe', art('robe'), (.53, .90), belt, 67 if identity == 'jade' else 51,
              math.dist(belt, neck_top) * 1.32, -9 if identity == 'jade' else 3)
        place(skin, 'robeTails', art('robeTails'), (.51, .15), (belt[0], belt[1] - 3), 92, 70,
              -13 if identity == 'jade' else 6)
        for suffix, side in (('Front', 'L'), ('Back', 'R')):
            near = side == 'L'
            shoulder, elbow, hand = (points[f'{bone}_{side}'] for bone in ('UpperArm', 'Forearm', 'Hand'))
            slot = 'arm' + suffix
            if not near:
                # The sleeve tip stops before the wrist instead of exposing the painted hollow cuff.
                elbow = (elbow[0] + 3, elbow[1] - 2)
            fit(skin, slot, art(slot), ((.17, .31), (.78, .68)) if near else ((.76, .34), (.21, .75)),
                shoulder, elbow, .68 if near else .57)
            slot = 'elbow' + suffix
            fit(skin, slot, art(slot), ((.20, .37), (.84, .63)) if near else ((.75, .40), (.18, .63)),
                elbow, hand, .76 if near else .77)
            slot = 'hand' + suffix
            contact = hand if near else (hand[0] + 2, hand[1] + 2)
            place(skin, slot, art(slot), (.24, .62) if near else (.78, .51), contact,
                  34 if near else 22, rotation=-19 if near else (-55 if identity == 'cinnabar' else 5))
        if style in ('jade', 'cinnabar'):
            place(skin, 'cape', art('cape'), (.90, .11), points['cape'], 119 if style == 'jade' else 90,
                  128 if style == 'jade' else 103)

    for suffix, side in (('Front', 'L'), ('Back', 'R')):
        near = side == 'L'
        thigh, shin, foot = (points[f'{bone}_{side}'] for bone in ('Thigh', 'Shin', 'Foot'))
        hip_join = (thigh[0] * .86 + hips[0] * .14, thigh[1] * .86 + hips[1] * .14)
        knee_join = (shin[0] * .90 + thigh[0] * .10, shin[1] * .90 + thigh[1] * .10)
        for style, artwork in (('charcoal', 'dark'), ('ash', 'light')):
            slot = 'pants' + suffix
            fit('pants/' + style, slot, f'paint_{slot}_{artwork}', ((.25, .22), (.81, .85)), hip_join, shin, .74)
        for style, artwork in (('dark', 'dark'), ('violet', 'light')):
            slot = 'shin' + suffix
            anchors = ((.75, .22), (.23, .86)) if artwork == 'dark' else ((.45, .23), (.69, .86))
            fit('boots/' + style, slot, f'paint_{slot}_{artwork}', anchors, knee_join, foot, .68 if near else .65)
            slot = 'boot' + suffix
            place('boots/' + style, slot, f'paint_{slot}_{artwork}', (.25, .40) if near else (.75, .40),
                  foot, 45 if near else 39, 24 if near else 22)

    # The base skin keeps a sensible stance even when a partial legacy look is equipped.
    for slot in ('robe', 'robeTails', 'armBack', 'elbowBack', 'handBack', 'armFront', 'elbowFront', 'handFront'):
        skins['default'][slot] = skins['robe/' + identity][slot]
    for suffix in ('Front', 'Back'):
        for slot, style in (('pants' + suffix, 'pants/charcoal'), ('shin' + suffix, 'boots/dark'),
                            ('boot' + suffix, 'boots/dark')):
            skins['default'][slot] = skins[style][slot]
    rig['skins'] = [{'name': name, 'attachments': parts} for name, parts in skins.items()]
    apply_prop_rig(rig, pieces, identity)
    apply_head_rig(rig, pieces, identity)
    stable = {key: value for key, value in rig.items() if key != 'skeleton'}
    rig['skeleton']['hash'] = hashlib.sha256(json.dumps(stable).encode()).hexdigest()[:16]


def unpack_atlas(directory):
    lines = (directory / 'duel_parts.atlas').read_text(encoding='utf-8').splitlines()[4:]
    with Image.open(directory / 'duel_parts.png') as image:
        pieces = {}
        for name, bounds in zip(lines[::2], lines[1::2]):
            left, top, width, height = map(int, bounds.partition(':')[2].split(','))
            pieces[name] = image.crop((left, top, left + width, top + height))
    return pieces


def dressed_rig(rig, sample):
    result = deepcopy(rig)
    skins = {skin['name']: skin['attachments'] for skin in result['skins']}
    outfit = skins['default']
    for slot, part in sample['look']['parts'].items():
        if part.get('sourceId') != 'game' or part.get('dye') is not None:
            raise ValueError(f'Appearance {sample["index"]} needs a built-in, undyed attachment at {slot}.')
        group, *_, choice = part['id'].split('/')
        if slot == 'stance':
            result['animations']['Ready'] = deepcopy(result['animations'][choice])
        elif attachments := skins.get(f'{group}/{choice}', {}).get(slot):
            outfit[slot] = attachments

    for frame in sample['frames']:
        pose = frame['pose']
        animation = result['animations'][pose['clip']]
        paths = []
        for slot in result['slots']:
            name = slot['name']
            key = slot.get('attachment')
            for change in animation.get('slots', {}).get(name, {}).get('attachment', []):
                if change.get('time', 0) <= pose['seconds']:
                    key = change.get('name')
            if part := outfit.get(name, {}).get(key):
                paths.append((name, part['path']))
        seen = {(part['slot'], part['image']) for part in frame['slots']}
        if set(paths) != seen:
            raise ValueError(f'{sample["index"]} {pose}: exported attachments differ from Godot: {set(paths) ^ seen}.')
    return result


def export_import_copy(target, rigs, pieces, samples=()):
    target = Path(target)
    folder = target / 'parts'
    folder.mkdir(parents=True, exist_ok=True)
    used = set()
    dressed = {}
    for sample in samples:
        name = sample['look']['rig']
        dressed[f'NPC_{sample["index"]:02}_{name}'] = dressed_rig(rigs[name], sample)
    # Give the two source rigs a complete visible setup, while keeping all wardrobe skins editable.
    sources = {}
    for name, rig in rigs.items():
        example = next((sample for sample in samples if sample['look']['rig'] == name), None)
        sources[f'duel_{name}'] = dressed_rig(rig, example) if example else deepcopy(rig)
    for name, rig in (sources | dressed).items():
        for slot in rig['slots']:
            for field in ('depth', 'attachmentOffsetX', 'attachmentOffsetY'):
                slot.pop(field, None)
        for skin in rig['skins']:
            used.update(part['path'] for attachments in skin['attachments'].values() for part in attachments.values())
            for attachments in skin['attachments'].values():
                for part in attachments.values():
                    part.pop('depth', None)
        content = {key: value for key, value in rig.items() if key != 'skeleton'}
        rig['skeleton']['hash'] = hashlib.sha256(json.dumps(content, separators=(',', ':')).encode()).hexdigest()[:16]
        (target / f'{name}.json').write_text(json.dumps(rig, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    for name in sorted(used):
        pieces[name].save(folder / (name + '.png'))
