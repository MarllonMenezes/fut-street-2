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
`--help` is safe and is the only application invocation covered by CTest. A
successful link or CLI smoke is not game-execution evidence.

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
