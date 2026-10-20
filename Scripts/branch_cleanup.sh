#!/usr/bin/env bash
# Branch cleanup for the OASIS parent repo and every submodule.
#
# Classifies every remote and local branch against origin/Development and, with --apply, deletes only branches whose
# work is provably preserved. Branches with unique work are never deleted; they are listed for a person to review
# (merge what is useful, archive-tag the rest). Default is a dry run that changes nothing.
#
#   Scripts/branch_cleanup.sh                 # audit only (safe)
#   Scripts/branch_cleanup.sh --apply         # delete safe remote + local branches
#   Scripts/branch_cleanup.sh --remote-only   # skip local branches
#   Scripts/branch_cleanup.sh --local-only    # skip remote branches
#   KEEP_BRANCHES='codex/my-live-work|feature/x' Scripts/branch_cleanup.sh   # extra branches to protect (regex)
#
# A branch is SAFE to delete when it is:
#   merged     - its tip is an ancestor of origin/Development
#   identical  - every commit has an identical patch already in origin/Development (git cherry)
#   preserved  - (local branches only) its tip is contained in a remote branch or tag, e.g. master or archive/*
# Always protected: Development, master, main, HEAD, promotion/*, submodule-sync/*, archive/*, branches checked out in
# any worktree of the parent or any submodule (protected in every repo), and anything matching KEEP_BRANCHES. See Docs/Devs/BRANCH_CLEANUP_RUNBOOK.md.

set -uo pipefail

APPLY=0; DO_REMOTE=1; DO_LOCAL=1
for arg in "$@"; do
  case "$arg" in
    --apply) APPLY=1 ;;
    --remote-only) DO_LOCAL=0 ;;
    --local-only) DO_REMOTE=0 ;;
    -h|--help) sed -n '2,22p' "$0"; exit 0 ;;
    *) echo "Unknown option: $arg" >&2; exit 2 ;;
  esac
done

ROOT=$(git rev-parse --show-toplevel) || exit 1
PROTECTED='^(Development|master|main|HEAD|promotion/.*|submodule-sync/.*|archive/.*)$'
KEEP=${KEEP_BRANCHES:-}
TARGET=origin/Development
REVIEW_FILE=$(mktemp)
trap 'rm -f "$REVIEW_FILE"' EXIT
total_safe=0; total_deleted=0

is_protected() { # $1 branch name, $2 worktree branch list
  [[ "$1" =~ $PROTECTED ]] && return 0
  [[ -n "$KEEP" && "$1" =~ ^($KEEP)$ ]] && return 0
  grep -qxF "$1" <<<"$2" && return 0
  return 1
}

classify() { # $1 repo dir, $2 ref; prints merged|identical|unique:N
  if git -C "$1" merge-base --is-ancestor "$2" "$TARGET" 2>/dev/null; then echo merged; return; fi
  local unique; unique=$(git -C "$1" cherry "$TARGET" "$2" 2>/dev/null | grep -c '^+')
  if [[ "$unique" == 0 ]]; then echo identical; else echo "unique:$unique"; fi
}

process_repo() { # $1 repo dir, $2 label
  local dir=$1 label=$2
  git -C "$dir" fetch -q --prune --tags origin 2>/dev/null || { echo "[$label] fetch failed, skipped"; return; }
  if ! git -C "$dir" rev-parse -q --verify "$TARGET" >/dev/null; then echo "[$label] no $TARGET, skipped"; return; fi
  local worktrees=$WORKTREE_BRANCHES current
  current=$(git -C "$dir" branch --show-current)

  if (( DO_REMOTE )); then
    local delete=() b name state
    while read -r b; do
      name=${b#origin/}
      [[ "$name" == "$b" || "$name" == origin ]] && continue
      is_protected "$name" "$worktrees" && continue
      state=$(classify "$dir" "$b")
      case $state in
        merged|identical) delete+=("$name"); echo "[$label] remote $name: $state -> safe" ;;
        *) echo "[$label] remote $name: ${state#unique:} commits not in Development -> REVIEW"
           echo "$label | remote | $name | ${state#unique:}" >> "$REVIEW_FILE" ;;
      esac
    done < <(git -C "$dir" branch -r --format='%(refname:short)')
    total_safe=$((total_safe + ${#delete[@]}))
    if (( APPLY )) && (( ${#delete[@]} )); then
      if git -C "$dir" push -q origin --delete "${delete[@]}" 2>/dev/null; then
        total_deleted=$((total_deleted + ${#delete[@]})); echo "[$label] deleted ${#delete[@]} remote branches"
      else echo "[$label] remote delete failed; nothing assumed deleted" >&2; fi
      git -C "$dir" fetch -q --prune origin
    fi
  fi

  if (( DO_LOCAL )); then
    local b state
    while read -r b; do
      [[ "$b" == "$current" ]] && continue
      is_protected "$b" "$worktrees" && continue
      state=$(classify "$dir" "$b")
      if [[ "$state" == unique:* && -n "$(git -C "$dir" for-each-ref --contains "$b" --format=x refs/remotes refs/tags | head -1)" ]]; then
        state=preserved
      fi
      case $state in
        merged|identical|preserved)
          total_safe=$((total_safe + 1))
          if (( APPLY )); then git -C "$dir" branch -q -D "$b" && total_deleted=$((total_deleted + 1)) && echo "[$label] deleted local $b ($state)"
          else echo "[$label] local $b: $state -> safe"; fi ;;
        *) echo "[$label] local $b: ${state#unique:} unpushed commits -> REVIEW"
           echo "$label | local | $b | ${state#unique:}" >> "$REVIEW_FILE" ;;
      esac
    done < <(git -C "$dir" branch --format='%(refname:short)')
  fi
}

# A branch checked out in any worktree of the parent or a submodule is in use. Agents work on the same branch name
# across the parent and its submodules, so that name is protected in every repo.
WORKTREE_BRANCHES=$( { git -C "$ROOT" worktree list --porcelain
  git -C "$ROOT" submodule foreach --quiet 'git worktree list --porcelain'; } 2>/dev/null |
  awk '/^branch /{sub("refs/heads/","",$2); print $2}' | sort -u)

process_repo "$ROOT" "OASIS"
while IFS= read -r path; do
  [[ -n "$path" && ( -d "$ROOT/$path/.git" || -f "$ROOT/$path/.git" ) ]] || continue
  process_repo "$ROOT/$path" "$path"
done < <(git -C "$ROOT" submodule foreach --quiet 'echo "$sm_path"')

echo
if (( APPLY )); then echo "Deleted $total_deleted of $total_safe safe branches."
else echo "Dry run: $total_safe branches are safe to delete. Re-run with --apply to delete them."; fi
if [[ -s "$REVIEW_FILE" ]]; then
  echo "Branches with work not in Development (review, merge what is useful, then archive-tag and delete):"
  echo "repo | where | branch | commits"
  cat "$REVIEW_FILE"
  exit 3
fi
echo "Nothing needs review."
