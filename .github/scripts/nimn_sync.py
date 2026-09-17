"""Merge released PattN source into NimN; never resolve conflicts by discarding a side."""
import json
import os
from pathlib import Path
import re
import subprocess


def run(*args):
    return subprocess.check_output(args, text=True).strip()


def validate_tag(tag):
    if not re.fullmatch(r"\d+\.\d+\.\d+-P\d+", tag):
        raise ValueError("Unrecognized upstream release tag; manual review required")
    return tag


def candidate_tag(base, upstream_sha):
    if not re.fullmatch(r"\d+\.\d+\.\d+", base):
        raise ValueError("Unsupported base version")
    # Numeric identifier keeps same-base PattN releases newer than nimn.3.
    import time
    return f"v{base}-nimn.{int(time.time())}.{upstream_sha[:8]}"


def main():
    if os.environ.get("GITHUB_REPOSITORY", "herbert-kara/NimN") != "herbert-kara/NimN":
        raise RuntimeError("This automation only writes the personal NimN fork")
    upstream = json.loads(run("gh", "api", "repos/patterniha/PattN/releases/latest"))
    tag = validate_tag(upstream["tag_name"])
    state_path = Path(".github/nimn-upstream.json")
    state = json.loads(state_path.read_text())
    # A successful branch advance can precede a failed public release edit.
    # Keep the candidate in committed state and resume it before newer upstreams.
    pending = state.get("candidate")
    if pending:
        if not re.fullmatch(r"v\d+\.\d+\.\d+-nimn\.\d+\.[0-9a-f]{8}", pending):
            raise ValueError("Invalid stored candidate")
        releases = json.loads(run("gh", "api", "--paginate", "--slurp", "repos/herbert-kara/NimN/releases?per_page=100"))
        completed = any(r["tag_name"] == pending and not r["draft"] and not r["prerelease"]
                        for page in releases for r in page)
        if not completed:
            with open(os.environ["GITHUB_OUTPUT"], "a") as output:
                output.write(f"tag={pending}\n")
            print(f"Resuming publication of {pending}")
            return
    if state["tag"] == tag:
        print(f"Already integrated {tag}; nothing to publish")
        return
    run("git", "fetch", "--no-tags", "https://github.com/patterniha/PattN.git", f"refs/tags/{tag}")
    sha = run("git", "rev-parse", "FETCH_HEAD^{commit}")
    run("git", "merge-base", "--is-ancestor", state["sha"], sha)
    original = run("git", "rev-parse", "HEAD")
    run("git", "config", "user.name", "NimN sync")
    run("git", "config", "user.email", "41898282+github-actions[bot]@users.noreply.github.com")
    # Never use -X ours/theirs: a conflict is a required human review.
    run("git", "merge", "--no-commit", "--no-ff", sha)
    # Automation/updater safety code may not be replaced by upstream.
    protected = [".github/scripts", ".github/nimn-assets.json", ".github/nimn-upstream.json", ".github/workflows/nimn-release.yml", ".github/workflows/nimn-sync.yml"]
    for path in protected:
        if run("git", "diff", "--name-only", original, "--", path):
            raise RuntimeError(f"Upstream modified protected automation: {path}")
    run("python3", ".github/scripts/nimn_guard.py")
    base = re.search(r"<Version>([^<]+)</Version>", Path("v2rayN/Directory.Build.props").read_text())[1]
    release_tag = candidate_tag(base, sha)
    state_path.write_text(json.dumps({"tag": tag, "sha": sha, "candidate": release_tag}, indent=2) + "\n")
    run("git", "add", str(state_path))
    run("git", "commit", "-m", f"Merge PattN {tag}, preserving NimN customizations")
    run("git", "tag", release_tag)
    run("git", "push", "origin", f"refs/tags/{release_tag}")
    # GITHUB_TOKEN tag pushes do not trigger workflows: caller invokes workflow_call.
    with open(os.environ["GITHUB_OUTPUT"], "a") as output:
        output.write(f"tag={release_tag}\n")
    print(f"Prepared {release_tag}; default branch advances only after successful build/release")


if __name__ == "__main__":
    main()
