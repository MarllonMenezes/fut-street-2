# Native recompilation output

This CMake target compiles the C translation emitted by XboxRecomp into a
native static library. It is an intermediate compile check; it does not yet
provide the Xbox kernel, memory map, graphics/audio runtime, or a game `.exe`.

The generated C and XBE are local build inputs and must stay outside this
repository. Run CMake with `RECOMP_GENERATED_DIR` pointing at the generated
directory, for example:

```powershell
cmake -S native_recomp -B artifacts/fut-street2-native/build -G Ninja `
  -DRECOMP_GENERATED_DIR="C:/path/to/recomp/gen"
cmake --build artifacts/fut-street2-native/build --config Release --parallel
```

On Windows with the portable Zig compiler, set `FUT_STREET2_ZIG` to the
`zig.exe` path and pass `-DRECOMP_ARCHIVER="D:/path/to/native_recomp/zig-ar.cmd"`.
The wrapper only dispatches Zig's bundled `ar` subcommand.

Use a C compiler supported by XboxRecomp and retain all build diagnostics.
The full runtime target remains separate because XboxRecomp's GPU and APU
libraries include emulation components; that runtime has not been integrated
or run here.

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
