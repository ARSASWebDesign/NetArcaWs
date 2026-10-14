# Release License Compliance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task.

**Goal:** Ship required Tool legal notices and enforce LGPL-oriented license/source checks before releases.
**Architecture:** Extend NetArcaWs.Build with deterministic licensed dependency inventory and package audit; commit reviewed Tool legal materials; integrate checks/source evidence into existing release flow.
**Tech Stack:** .NET10, System.Text.Json/XML/IO.Compression/Cryptography, xUnitv3/AwesomeAssertions, current GitHub workflows.
**Spec:** docs/superpowers/specs/2026-10-14-release-license-compliance-design.md

## Global Constraints
- No new dependencies, Python, framework/version bump, provider removal, paid services, publication or authenticated ARCA calls.
- Preserve LGPL attribution, optional provider boundaries, existing OIDC/pinned-action/immutable-artifact rules.
- User already authorized this concrete compliance work; execute and report independent reviews without repeated iteration approvals.

## Review Focus
- Missing licenses/NOTICE/copyright or binary origins must fail, not silently skip.
- Version/license/content drift versus locked graphs must be detected.
- SNI is reference-only: its native DLLs cannot enter our release artifacts.
- Source archive must match build commit and cannot include ignored/untracked secrets.
- Publish credentials must never execute repository build/audit code.

### Task1: Dependency/legal inventory, Tool notices and .NET package checks
- [ ] Add licenses --update, licenses --check, licenses --check-packages DIRECTORY to existing Program CLI; deterministic inventory/policy and committed Tool legal material under licenses/third-party.
- [ ] Tests RED/GREEN for metadata drift/unknown licenses/hash/path/missing notices/Tool binary coverage/SNI rejection.
- [ ] Package Tool committed materials through csproj. Audit actual13 nupkgs for base GPL/LGPL/notices and PDF OFL.
- [ ] Add consumer licensing doc/SNI policy and THIRD-PARTY notices explaining verified boundaries; no fabricated legal clearance.
- [ ] Focused tests/local generation/package audit, explicit-file commit and independent review.

### Task2: CI/release source and licensing gate
- [ ] Consume Task1 CLI before/after pack in CI and release build. Add .NET exact-commit source/evidence generation/validation through existing release helpers, preserving source/commit/hash correlation.
- [ ] Extend release allowlist/checksums/upload and publisher shell verification/non-replacement for source archive/license evidence, no repo execution with publish credentials.
- [ ] Tests RED/GREEN for workflow order, evidence/source corruption, allowlist and release safeguards. Document release and contributor procedures; generate wiki via .NET.
- [ ] Focused tests, explicit-file commit, independent review.

### Final verification
- [ ] Locked restore, Release build/full tests, all13 packs, license metadata and artifact gates, actual Tool install/help and fresh-cache package consumers.
- [ ] Whole-change review and scoped fixes; retain branch/local evidence, no push/release.
