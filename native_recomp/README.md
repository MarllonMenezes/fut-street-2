# Native recompilation output

This CMake target compiles the C translation emitted by XboxRecomp into a
native static library. A separate `fut_street_2_no_emulation_runtime_support`
target supplies a null manual-override lookup (no hand-written overrides are
registered yet), diagnostics for unresolved indirect calls/untranslated
instructions, and disabled XboxRecomp NV2A pushbuffer survey/executor hooks.
The disabled hooks prevent `RECOMP_PB_EXEC` from reaching GPU command
execution through this path; they are not a renderer. The optional app target
can link a selected kernel subset and native compatibility libraries into a
host `.exe`, but does not provide a validated or complete game runtime.

The generated C and XBE are local build inputs and must stay outside this
repository. Run CMake with `RECOMP_GENERATED_DIR` pointing at the generated
directory, for example:

```powershell
cmake -S native_recomp -B artifacts/fut-street2-native/build -G Ninja `
  -DRECOMP_GENERATED_DIR="C:/path/to/recomp/gen"
cmake --build artifacts/fut-street2-native/build --config Release --parallel
ctest --test-dir artifacts/fut-street2-native/build -C Release --output-on-failure
```

On Windows with the portable Zig compiler, set `FUT_STREET2_ZIG` to the
`zig.exe` path and pass `-DRECOMP_ARCHIVER="D:/path/to/native_recomp/zig-ar.cmd"`.
The wrapper only dispatches Zig's bundled `ar` subcommand.

When configuring another CMake project that needs static libraries with Zig,
also pass
`-DCMAKE_USER_MAKE_RULES_OVERRIDE="D:/path/to/native_recomp/ZigArchiverOverride.cmake"`.

Use a C compiler supported by XboxRecomp and retain all build diagnostics.
The upstream umbrella `xboxrecomp` target links the APU/NV2A emulators and is
intentionally not used. The project builds only the audited kernel-source
subset and selected D3D8→D3D11, video, and input compatibility libraries.

The no-emulation support test verifies that manual lookup reports no override
and that the disabled instrumentation hooks return without processing guest
addresses; it also links the indirect-call diagnostics. It does not validate
XBE behavior, CPU API compatibility, graphics, audio, gameplay, or game
startup. A future manual-override implementation must replace
`recomp_manual_lookup.c` explicitly rather than inventing bindings. The runtime
diagnostics log reached `RECOMP_UNIMPL` sites and support opt-in trapping via
`RECOMP_UNIMPL_TRAP=1`; they do not make those translated instructions
implemented.

## Optional local host executable

The opt-in `fut_street_2_recompiled_app` target compiles a bootstrap against a
selected list of XboxRecomp kernel sources needed for memory, thunks and
dispatch. It deliberately excludes `nv2a_pb_scan.c`, `nv2a_pb_exec.c`,
`nv2a_vsh_interp.c`, `nv2a_combiner.c`, `xbox_apu` and `xbox_nv2a`. The bootstrap
requires an explicit `--launch <default.xbe> <game-data-directory>
<save-directory>` argument and rejects the upstream `RECOMP_AC97_READY` and
`RECOMP_PB_EXEC` opt-ins. Requiring a separate save directory avoids the
upstream per-user default; for bring-up, point both data and saves at disposable
directories outside the repository and keep the ISO as read-only input.
The bootstrap rejects `RECOMP_AC97_READY`, `RECOMP_PB_EXEC`, and
`RECOMP_VBLANK`; the last option synthesizes a fixed-rate vblank and writes
NV2A interrupt bits, so it is incompatible with this project's no-emulation
policy. CTest verifies this rejection before any XBE/data path is opened.
`--help` is the safe CLI smoke; the additional CTest launch uses dummy paths and
asserts that the guard exits before opening any input. A separate controlled
`--launch` bring-up has reached the translated entry and
worker/timer activity; it did not validate boot/menu or gameplay, and is
documented in `docs/FUT_STREET_2_RECOMPILACAO_NATIVA_2026-10-13.md`. A successful
link or CLI smoke alone is not game-execution evidence.

The caller supplies the pinned XboxRecomp source root and a semicolon-separated
`RECOMP_HOST_LIBRARIES` list. On Windows, `xbox_d3d8`, `xbox_video`, and
`xbox_input` were built selectively; small import libraries for the host's
`d3dcompiler_47.dll` and `xinput1_4.dll` were generated from the checked-in
export-name lists under `imports/` with Zig `dlltool`. No proprietary game input
or generated C belongs in this build configuration.

The initial local 15,605-function generation linked into a host `.exe` of about 165 MB;
it was not run. The pinned kernel sources emit compiler warnings. This milestone
proves zero unresolved link symbols and safe CLI startup only—not XBE boot,
correct ABI/stub behavior, rendering, gameplay, animation, or audio. The `.exe`,
import archives and game code remain ignored/local.

For the pinned XboxRecomp v0.13.1, the optional local patch in `patches/`
adds lifting of `movdqa` and `movdqu` through the existing 128-bit XMM move
path. Apply it from PowerShell with:

```powershell
git -C C:/path/to/xboxrecomp apply --unidiff-zero `
  'D:/projeto zeta/native_recomp/patches/0001-lift-movdqa-movdqu.patch'
```

This preserves the 128-bit data transfer but does not model the architectural
alignment fault of `movdqa`; it is an incremental translation improvement,
not a claim of complete instruction semantics.

Patch `0002-detect-late-alias-tail-jumps.patch` fixes a separate discovery-order
gap: aliases created after the first tail-jump scan are scanned in bounded
follow-up rounds, allowing a late alias that tail-jumps into a function body to
be emitted as a callable interior entry instead of an unresolved stub. Apply
it after patch 0001:

```powershell
git -C C:/path/to/xboxrecomp apply `
  'D:/projeto zeta/native_recomp/patches/0002-detect-late-alias-tail-jumps.patch'
```

The added synthetic test covers this discovery-order case. The patch improves
static function coverage; it does not establish ABI correctness or runtime
behavior for the title.

Patch `0003-alias-cross-function-conditional-branches.patch` follows patches
0001 and 0002. It adds aliases for direct conditional-branch targets when the
branch source has one owner, the target is a decoded instruction, and all target
owner ranges agree on the same body end. It does not merge or truncate the
owner function. Branches already inside the source body, same-tail-family
targets, and ambiguous target end ranges remain unresolved. Synthetic tests
cover unique ownership, common-end overlapping ownership, ambiguous ends,
and source-internal branches.

Patch `0004-resolve-dispatcher-object-shadows-in-multiwait.patch` follows
0001–0003. It resolves guest dispatcher-object addresses through XboxRecomp's
existing shadow table in `KeWaitForMultipleObjects`, then reuses the existing
opt-in adapter for events a title built directly in guest memory. The native
host enables `RECOMP_TITLE_KEVENTS` for this title because static disassembly
and the controlled translated-entry run showed a stack event header with
`Type=1`, `Size=4` at the observed multi-wait call site. Synchronization-event
`SignalState` is cleared only after the corresponding wait succeeds. This does
not implement arbitrary kernel objects or claim complete wait semantics.

The controlled host bring-up reached the translated entry and worker threads.
Before this patch, ordinal 158 (`KeWaitForMultipleObjects`) spun millions of
times in seconds with unsuccessful waits. With the title event adapter enabled,
the same call site returned `STATUS_WAIT_0` and advanced into its timer/DPC
cycle; rendering and the complete game boot remain unverified. The `.xbe`, ISO,
and extracted data used for bring-up stay local and outside Git.

Patch `0007-lift-salc-carry-to-al.patch` follows patches 0001–0006. For the
pinned XboxRecomp v0.13.1 source, apply it with `--unidiff-zero` after the prior
stack. It lowers SALC (`D6`) to `AL = CF ? 0xFF : 0`, and ensures a function
containing SALC declares the carry temporary. Intel's IA-32 architecture
compatibility section specifies this behavior for non-64-bit mode:
[Intel SDM, Vol. 3B, §25.18](https://cdrdv2-public.intel.com/874250/253669-090-sdm-vol-3b.pdf).
Focused lifter tests exercise carry-to-AL and the declaration path. This does
not establish that any of the three translated sites is reached at runtime.

```powershell
git -C C:/path/to/xboxrecomp apply --unidiff-zero `
  'D:/projeto zeta/native_recomp/patches/0007-lift-salc-carry-to-al.patch'
```

Patch `0008-lift-aam-aad-immediate-base.patch` follows patches 0001–0007. It
lowers `AAM imm8`/`AAD imm8` for non-zero immediate bases, including their
defined SF/ZF/PF result conditions while leaving undefined CF/AF/OF conditions
unclaimed. `AAM 0` remains untranslated because it raises guest `#DE` and this
runtime stack does not yet implement that exception path. The current generated
title has 13 AAM and one AAD site, all with non-zero bases; this is static
coverage only, not proof of reachability. The patch is tested on pinned
XboxRecomp v0.13.1 and does not execute the XBE.

```powershell
git -C C:/path/to/xboxrecomp apply --unidiff-zero `
  'D:/projeto zeta/native_recomp/patches/0008-lift-aam-aad-immediate-base.patch'
```

Patch `0009-lift-daa-das-and-track-af.patch` follows patches 0001–0008. It
tracks auxiliary carry only in functions where DAA/DAS or LAHF reads it, and
adds the AF effects needed by the supported arithmetic producers. DAA/DAS
sites use the Intel decimal-adjust transformations and update the defined
AL/CF/AF/SF/ZF/PF results; undefined flags are not promoted to known values.
This is static instruction lowering, not evidence that any site executes in
the game.

Patch `0010-guard-decimal-adjust-unknown-cf.patch` follows patches 0001–0009.
It conservatively lowers a DAA/DAS site only when a CF-producing instruction
appears earlier in that same basic block. It intentionally does not infer
inter-block or path-sensitive flag provenance; unsupported sites remain
`RECOMP_UNIMPL`, and their later flag snapshots are invalidated to avoid stale
conditions. This filter is a safety boundary, not a proof of reachability or
that all incoming paths define CF.

The clean 0001–0010 stack was tested against pinned XboxRecomp v0.13.1
(`193e2995ffaec871bb3be43a43bd66aa5fccfeb1`): 531 tests passed, 38 skipped;
generation translated 15,702/15,702 functions with zero generation failures,
71 call stubs, and 96 unsupported instruction sites across 29 mnemonics.
The 64 generated C translation units built and linked in Release, and CTest
passed 4/4. These are translator/build checks only: no XBE boot, rendering,
assets, gameplay, animation, or audio gate is claimed as validated.

Patch `0011-lift-aaa-aas-with-af-guard.patch` follows patches 0001–0010. It
lowers AAA/AAS only when a tracked AF definition precedes the site in the same
basic block. The instructions update AX and define AF/CF; all other arithmetic
flags remain unknown, so only carry conditions are lowered afterward. Sites
without AF provenance remain `RECOMP_UNIMPL`. This patch does not infer flag
state across blocks or calls and does not claim site reachability.

On the pinned v0.13.1 source, patches 0001–0011 pass 537 tests (38 skipped).
Generation remains 15,702/15,702 functions with zero failures and 71 unresolved
call stubs; unsupported instructions decreased from 96 to 94 across 29
mnemonics. Two AAS sites lowered; one AAA and one AAS site remain unsupported
because their incoming AF is not established by the conservative analysis.
Release compilation/link, CTest 4/4, and host `--help` pass. No XBE boot or
gameplay/render/audio gate is validated.

```powershell
git -C C:/path/to/xboxrecomp apply --unidiff-zero `
  'D:/projeto zeta/native_recomp/patches/0011-lift-aaa-aas-with-af-guard.patch'
```

Patch `0012-ignore-referenced-ascii-cc-boundary.patch` follows patches
0001–0011. The function detector now rejects a weak `CC`-padding boundary when
the candidate is an exact `DATA_IMM` target, has no direct control-flow xref, and
the bytes form a printable NUL-terminated ASCII run of at least eight bytes.
This prevents the XPP strings `\Device\MU_0` and `\Device\MU_x` from being
misidentified as functions while preserving candidates with direct control-
flow evidence. Tests cover both cases. On the pinned v0.13.1 source, the clean
patch stack passes 539 tests (38 skipped); generation removes one false
function, two call stubs, and two unsupported instruction sites. The generated
host still builds in Release and CTest passes 4/4; no game boot or fidelity gate
is claimed.

```powershell
git -C C:/path/to/xboxrecomp apply --unidiff-zero `
  'D:/projeto zeta/native_recomp/patches/0012-ignore-referenced-ascii-cc-boundary.patch'
```

Patch `0013-allow-back-branch-to-known-parent-body.patch` follows patches
0001–0012. A conditional branch from an overlapping tail alias may return to an
earlier instruction in the common parent body. This exception is accepted only
when all source owners and all target owners agree on the same body end, and a
source alias starts after the branch target. Other ambiguous ownership remains
unresolved. The focused alias suite passes 5/5; the full translator suite
passes 540 tests (38 skipped). Regeneration adds 16 translated entries and
removes 23 stubs overall (including aliases exposed by subsequent passes); it
does not reduce the 92 unsupported instruction sites. Release build, CTest 4/4
and safe `--help` pass. No XBE launch or fidelity claim is made.

```powershell
git -C C:/path/to/xboxrecomp apply --unidiff-zero `
  'D:/projeto zeta/native_recomp/patches/0013-allow-back-branch-to-known-parent-body.patch'
```

Patch `0014-recover-tail-targets-in-overlapping-parent-bodies.patch` follows
patches 0001–0013. Tail-jump discovery now inspects every body containing the
target instead of only the nearest preceding start. It emits a callable alias
only when all target owners agree on the body end; conflicting or uncovered
targets remain unresolved. This recovers tail exits that jump beyond a short
overlapping child into its shared parent body without extending either source
function or guessing an ABI. Synthetic tests cover both the accepted shared
end and conflicting ownership.

On the pinned v0.13.1 XBE pipeline, regeneration changed the output to 15,673
translated entries, 26 unresolved stubs and 93 untranslated instruction sites
across 28 mnemonics. The stub audit no longer classifies overlapping body
ranges; this is static translation evidence only. The 64 C translation units
and native host linked in Release, CTest passed 4/4 and `--help` passed. The
full translator suite passed 542 tests (38 skipped). No XBE was launched, so
boot, rendering, assets, gameplay and audio remain unvalidated.

```powershell
git -C C:/path/to/xboxrecomp apply --unidiff-zero `
  'D:/projeto zeta/native_recomp/patches/0014-recover-tail-targets-in-overlapping-parent-bodies.patch'
```
