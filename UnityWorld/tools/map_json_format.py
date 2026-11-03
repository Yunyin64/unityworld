"""Readable small author JSON; importing this module never writes data."""
import json

def compact_json(value, level=0):
    pad='  '*level
    flat=json.dumps(value,ensure_ascii=False,separators=(', ', ': '))
    if len(flat)+len(pad)<=120: return flat
    if isinstance(value,dict):
        entries=[]
        for key,child in value.items():
            prefix='  '*(level+1)+json.dumps(key,ensure_ascii=False)+': '
            if key in ('TerrainLayout','LandMarkLayout') and isinstance(child,dict):
                pairs=[json.dumps(k,ensure_ascii=False)+': '+json.dumps(v,ensure_ascii=False) for k,v in child.items()]
                lines=[', '.join(pairs[i:i+5]) for i in range(0,len(pairs),5)]
                rendered='{\n'+',\n'.join('  '*(level+2)+line for line in lines)+'\n'+'  '*(level+1)+'}' if lines else '{}'
            else: rendered=compact_json(child,level+1)
            entries.append(prefix+rendered)
        grouped=[]
        for entry in entries:
            if grouped and '\n' not in grouped[-1] and '\n' not in entry and len(grouped[-1])+len(entry.lstrip())+2<=120:
                grouped[-1]+=', '+entry.lstrip()
            else: grouped.append(entry)
        return '{\n'+',\n'.join(grouped)+'\n'+pad+'}'
    if isinstance(value,list):
        entries=['  '*(level+1)+compact_json(v,level+1) for v in value]
        if all(not isinstance(v,(dict,list)) for v in value) or (value and all(isinstance(v,dict) and set(v)=={'Col','Row'} for v in value)):
            grouped=[]
            for entry in entries:
                if grouped and len(grouped[-1])+len(entry.lstrip())+2<=120: grouped[-1]+=', '+entry.lstrip()
                else: grouped.append(entry)
            entries=grouped
        return '[\n'+',\n'.join(entries)+'\n'+pad+']'
    return flat

