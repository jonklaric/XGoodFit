"""Helpers for porting an upstream file by exact, checked replacements (docs/porting-guide.md, §1).

A port script copies an upstream file and changes only what the port needs:

    import sys; sys.path.insert(0, 'scripts/porting'); import xgf_port
    xgf_port.port('upstream/WPF-Framework/src/GenericControls/', 'src/Framework/GenericControls/',
                  'Windows/NameDialog.xaml', [(old, new), (old, new, count)], dst='Windows/NameDialog.axaml')

Each replacement must match the expected number of times (1 unless given), or the script stops.
Matching ignores trailing spaces and tabs at line ends (upstream has some; editors strip them), so the
rest of the file stays as upstream wrote it. Output is UTF-8 without a BOM, with LF line endings.
"""
import os
import re

# The repository root: this file is scripts/porting/xgf_port.py.
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def _pattern(old):
    lines = old.split('\n')
    return re.compile('[ \t]*\n'.join(re.escape(line.rstrip(' \t')) for line in lines))


def apply(s, reps, label=''):
    """Applies the replacements (old, new) or (old, new, count) to the text s."""
    for rep in reps:
        old, new = rep[0], rep[1]
        count = rep[2] if len(rep) > 2 else 1
        pattern = _pattern(old)
        found = len(pattern.findall(s))
        assert found == count, (label, old, found)
        s = pattern.sub(lambda _m: new, s)
    return s


def port(upstream_dir, dest_dir, rel, reps, dst=None):
    """Ports upstream_dir/rel to dest_dir/(dst or rel) with the replacements reps."""
    os.chdir(REPO)
    s = open(os.path.join(upstream_dir, rel), encoding='utf-8-sig').read()
    s = apply(s, reps, rel)
    out = os.path.join(dest_dir, dst or rel)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    with open(out, 'w', encoding='utf-8', newline='\n') as f:
        f.write(s)
    return s


def edit(path, reps):
    """Applies replacements to a file in the repository."""
    os.chdir(REPO)
    s = open(path, encoding='utf-8-sig').read()
    s = apply(s, reps, path)
    with open(path, 'w', encoding='utf-8', newline='\n') as f:
        f.write(s)
    return s
