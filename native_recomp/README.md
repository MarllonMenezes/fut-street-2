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
requires an explicit `--launch <default.xbe> <game-data-directory>` argument
and rejects the upstream `RECOMP_AC97_READY` and `RECOMP_PB_EXEC` opt-ins.
`--help` is safe and is the only application invocation covered by CTest. A
successful link or CLI smoke is not game-execution evidence.

The caller supplies the pinned XboxRecomp source root and a semicolon-separated
`RECOMP_HOST_LIBRARIES` list. On Windows, `xbox_d3d8`, `xbox_video`, and
`xbox_input` were built selectively; small import libraries for the host's
`d3dcompiler_47.dll` and `xinput1_4.dll` were generated from the checked-in
export-name lists under `imports/` with Zig `dlltool`. No proprietary game input
or generated C belongs in this build configuration.

The local 15,605-function generation linked into a host `.exe` of about 165 MB;
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
