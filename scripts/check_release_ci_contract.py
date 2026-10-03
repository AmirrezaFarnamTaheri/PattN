#!/usr/bin/env python3
"""Static regression gate for release orchestration and stale-run cancellation."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise SystemExit(f"{label}: missing required contract: {needle}")


def forbid(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise SystemExit(f"{label}: forbidden release contract remains: {needle}")


def main() -> int:
    build_all = read(".github/workflows/build-all.yml")
    build_linux = read(".github/workflows/build-linux.yml")
    loong = read("package-rhel-loong.sh")
    riscv_rpm = read("package-rhel-riscv.sh")
    release_lock = read("release-assets.lock.sh")
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

    # RISC-V must run on a real GitHub-hosted label. GitHub does not provide
    # an ubuntu-*-riscv hosted runner, so retained RISC-V releases use a pinned
    # official Ubuntu RISC-V image under QEMU on the standard x64 fleet.
    forbid(build_linux, "ubuntu-24.04-riscv", "build-linux.yml")
    require(build_linux, "riscv64:", "build-linux.yml")
    require(build_linux, "runs-on: ubuntu-26.04", "build-linux.yml")
    require(build_linux, "qemu-system-riscv64", "build-linux.yml")
    require(build_linux, "PATTN_UBUNTU_RISCV_IMAGE_SHA256", "build-linux.yml")
    require(release_lock, "PATTN_UBUNTU_RISCV_IMAGE_SHA256", "release-assets.lock.sh")
    require(riscv_rpm, "ubuntu|debian", "package-rhel-riscv.sh")
    require(riscv_rpm, "mkdir -p \"$RPM_TOPDIR\"/{BUILD,RPMS,SOURCES,SPECS,SRPMS}", "package-rhel-riscv.sh")

    # Every newly requested Code Test run actively cancels older work on the
    # same branch. The privileged canceller is loaded from the default branch
    # through workflow_run, so it never executes untrusted PR code.
    require(code_test, "cancel-in-progress: true", "test.yml")
    require(stale, "workflow_run:", "cancel-stale-tests.yml")
    require(stale, "workflows: [Code Test]", "cancel-stale-tests.yml")
    require(stale, "types: [requested]", "cancel-stale-tests.yml")
    require(stale, "actions: write", "cancel-stale-tests.yml")
    require(stale, "github.event.workflow_run.run_number", "cancel-stale-tests.yml")
    require(stale, ".run_number < $current_run_number", "cancel-stale-tests.yml")
    require(stale, "actions/workflows/test.yml/runs?per_page=100&branch=$CURRENT_BRANCH", "cancel-stale-tests.yml")
    require(stale, 'actions/runs/$run_id/cancel', "cancel-stale-tests.yml")
    require(stale, 'actions/runs/$run_id/force-cancel', "cancel-stale-tests.yml")

    print("release CI contracts are enforced")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
