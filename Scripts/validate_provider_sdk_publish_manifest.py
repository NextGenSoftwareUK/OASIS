"""Validate SDK bridge project publish entries without building shared outputs.

Complements Docker/verify-provider-sdk-bridge.mjs, which validates actual files
and installed packages. This gate checks the source project's copy manifest;
it does not claim a completed publish or installed dependency verification.
"""
import argparse
from pathlib import Path
import posixpath
import re
import xml.etree.ElementTree as ET


def validate(project: Path, link_root: str) -> int:
    entries = {}
    for item in ET.parse(project).iter():
        if item.tag.rsplit('}', 1)[-1] not in ('None', 'Content'):
            continue
        link = item.get('Link', '').replace('\\', '/')
        if not link.startswith(link_root + '/'):
            continue
        if item.get('CopyToOutputDirectory') not in ('PreserveNewest', 'Always') or item.get('CopyToPublishDirectory') not in ('PreserveNewest', 'Always'):
            raise ValueError(f'Bridge entry lacks output/publish copy policy: {link}')
        source = (project.parent / (item.get('Include') or item.get('Update') or '').replace('\\', '/')).resolve()
        if not source.is_file():
            raise ValueError(f'Bridge entry is not an existing explicit source file: {source}')
        if link in entries:
            raise ValueError(f'Duplicate published bridge target: {link}')
        entries[link] = source
    for required in ('bridge.mjs', 'package.json', 'package-lock.json'):
        if f'{link_root}/{required}' not in entries:
            raise ValueError(f'Missing published bridge entry: {required}')
    pattern = re.compile(r'\bfrom\s*["\']([^"\']+)["\']|\bimport\s*\(\s*["\']([^"\']+)["\']\s*\)|\bimport\s*["\']([^"\']+)["\']')
    modules = 0
    for link, source in entries.items():
        if not link.endswith('.mjs'):
            continue
        modules += 1
        for match in pattern.finditer(source.read_text(encoding='utf-8-sig')):
            name = next(value for value in match.groups() if value is not None)
            if name.startswith('.'):
                target = posixpath.normpath(posixpath.join(posixpath.dirname(link), name))
                if target not in entries:
                    raise ValueError(f'{link} imports unpublished module {name} ({target})')
    return modules


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('project', type=Path)
    parser.add_argument('link_root')
    args = parser.parse_args()
    count = validate(args.project.resolve(), args.link_root)
    print(f'{args.link_root}: {count} modules, copy policies and relative import closure verified (source manifest only)')
