#!/usr/bin/env python3
"""Require Development dependency advances to retain the previously pinned history.
Read-only: never fetch, checkout, merge, stage or rewrite a dependency.
"""
import argparse
import json
import pathlib
import subprocess

class AncestryError(RuntimeError):
    pass

def run_git(repo, *args):
    return subprocess.run(["git", "-c", "gc.auto=0", "-c", "maintenance.auto=false", *args],
        cwd=repo, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

def git(repo, *args):
    result=run_git(repo,*args)
    if result.returncode:
        raise AncestryError("Required Git history is unavailable; fetch the reviewed base and candidate before validation.")
    return result.stdout

def gitlinks(root, ref):
    commit=git(root,"rev-parse","--verify","--end-of-options",ref+"^{commit}").decode().strip()
    result={}
    for row in git(root,"ls-tree","-rz",commit).split(bytes([0])):
        if not row: continue
        header,path=row.split(bytes([9]),1)
        mode,kind,oid=header.split()
        if mode==b"160000":
            result[path.decode("utf-8")]=oid.decode("ascii")
    return result

def validate(root, base_ref, target_ref):
    root=pathlib.Path(root).resolve()
    before,after=gitlinks(root,base_ref),gitlinks(root,target_ref)
    rows=[]
    for name in sorted(before.keys() & after.keys()):
        old,new=before[name],after[name]
        if old==new: continue
        row={"path":name,"before":old,"after":new}
        module=(root/name).resolve()
        if not module.is_relative_to(root):
            row["status"]="invalid_module_path"
        elif not module.is_dir():
            row["status"]="module_uninitialized"
        else:
            top=run_git(module,"rev-parse","--show-toplevel")
            if top.returncode or pathlib.Path(top.stdout.decode().strip()).resolve()!=module:
                # An empty module directory otherwise silently answers from its parent repository.
                row["status"]="module_uninitialized"
            elif any(run_git(module,"cat-file","-e",oid+"^{commit}").returncode for oid in (old,new)):
                row["status"]="history_unavailable"
            else:
                result=run_git(module,"merge-base","--is-ancestor",old,new)
                row["status"]="descendant" if result.returncode==0 else (
                    "history_not_preserved" if result.returncode==1 else "history_unavailable")
        rows.append(row)
    return rows

def main(argv=None):
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root",type=pathlib.Path,default=pathlib.Path(__file__).resolve().parent.parent)
    parser.add_argument("--base",required=True)
    parser.add_argument("--ref",required=True)
    args=parser.parse_args(argv)
    try:
        rows=validate(args.root,args.base,args.ref)
    except (AncestryError,OSError,ValueError) as cause:
        print(json.dumps({"accepted":False,"error":str(cause)}))
        return 1
    accepted=all(row["status"]=="descendant" for row in rows)
    print(json.dumps({"accepted":accepted,"changedDependencies":rows}))
    if not accepted:
        print("Dependency history was not proved. Merge the required histories and qualify the combined source; do not replace the existing pin with a divergent tip.")
    return 0 if accepted else 1

if __name__=="__main__":
    raise SystemExit(main())
