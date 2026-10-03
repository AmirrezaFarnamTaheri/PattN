#!/usr/bin/env python3
"""Static regression gate for release orchestration and stale-run cancellation."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise SystemExit(f"{label}: missing required contract: {needle}")


def main() -> int:
    build_all = read(".github/workflows/build-all.yml")
    build_linux = read(".github/workflows/build-linux.yml")
    loong = read("package-rhel-loong.sh")
    code_test = read(".github/workflows/test.yml")
    stale = read(".github/workflows/cancel-stale-tests.yml")

    # A release orchestrator is not successful merely because dispatch requests
    # were accepted. It must discover each child run, wait for completion, and
    # fail if a platform workflow does not conclude successfully.
    require(build_all, "Wait for platform release workflows", "build-all.yml")
    require(build_all, "actions/workflows/$workflow/runs", "build-all.yml")
    require(build_all, 'if [[ "$conclusion" != "success" ]]; then', "build-all.yml")
    require(build_all, "PLATFORM_WAIT_TIMEOUT_SECONDS", "build-all.yml")

    # The LoongArch VM wrapper must terminate promptly on a non-zero package
    # build instead of waiting forever for a success-only sentinel.
    require(build_linux, r"__BUILD_DONE__([0-9]+)", "build-linux.yml")
    require(build_linux, "expect_out(1,string)", "build-linux.yml")
    require(build_linux, "PATTN_DISCOVERY_PREBUILT=/workspace/prebuilt/pattn-discovery-loong64",
            "build-linux.yml")

    # When the helper is prebuilt on the trusted host, the guest packaging
    # environment must not pull an unneeded distro Go package.
    require(loong, 'if [[ -z "${PATTN_DISCOVERY_PREBUILT:-}" ]]; then', "package-rhel-loong.sh")
    require(loong, "deps+=(golang)", "package-rhel-loong.sh")

    # Every newer Code Test push cancels stale work on the same branch. Native
    # workflow concurrency handles PRs; the API cleanup handles trusted pushes.
    require(code_test, "cancel-in-progress: true", "test.yml")
    require(stale, "actions: write", "cancel-stale-tests.yml")
    require(stale, "actions/workflows/test.yml/runs", "cancel-stale-tests.yml")
    require(stale, 'actions/runs/$run_id/cancel', "cancel-stale-tests.yml")

    print("release CI contracts are enforced")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
