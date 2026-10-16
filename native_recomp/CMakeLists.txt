cmake_minimum_required(VERSION 3.20)

project(FutStreet2RecompiledCode LANGUAGES C)

include(CTest)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)

set(RECOMP_GENERATED_DIR "" CACHE PATH
    "Directory containing XboxRecomp-generated C and headers")
set(RECOMP_ARCHIVER "" CACHE FILEPATH
    "Optional archiver executable or wrapper, e.g. zig-ar.cmd for Zig")
set(RECOMP_HOST_LIBRARIES "" CACHE STRING
    "Semicolon-separated native compatibility archives and system libraries")
set(RECOMP_XBOXRECOMP_SOURCE_DIR "" CACHE PATH
    "Pinned XboxRecomp source root used to compile the local host bootstrap")
set(RECOMP_DSP56300_INCLUDE_DIR "" CACHE PATH
    "Directory containing the pinned dsp56300 C FFI header")
set(RECOMP_DSP56300_LIBRARY "" CACHE FILEPATH
    "Pinned Windows static library built from dsp56300-emu-ffi")
set(RECOMP_DSP56300_LINK_LIBRARIES "" CACHE STRING
    "Additional platform libraries required by the dsp56300 static library")
option(FUT_STREET2_BUILD_HOST_APP
    "Build the statically recompiled host executable when runtime inputs are supplied"
    OFF)
option(FUT_STREET2_BUILD_MCPX_DSP_ADAPTER
    "Build the isolated MCPX DSP56300 adapter (does not enable AC97/APU boot)"
    OFF)

if(RECOMP_ARCHIVER)
    # Some compiler distributions (including Zig's Windows Clang driver) do
    # not ship a standalone `ar.exe`; let callers provide a small wrapper.
    set(CMAKE_AR "${RECOMP_ARCHIVER}")
endif()

if(NOT RECOMP_GENERATED_DIR)
    message(FATAL_ERROR
        "Set RECOMP_GENERATED_DIR to the local XboxRecomp output directory. "
        "Generated game code is intentionally not stored in Git.")
endif()

foreach(required_file recomp_types.h recomp_funcs.h recomp_dispatch.c)
    if(NOT EXISTS "${RECOMP_GENERATED_DIR}/${required_file}")
        message(FATAL_ERROR
            "Missing ${required_file} in RECOMP_GENERATED_DIR=${RECOMP_GENERATED_DIR}")
    endif()
endforeach()

file(GLOB_RECURSE RECOMP_GENERATED_SOURCES CONFIGURE_DEPENDS
    "${RECOMP_GENERATED_DIR}/recomp_*.c")

if(NOT RECOMP_GENERATED_SOURCES)
    message(FATAL_ERROR "No generated recomp_*.c files found in RECOMP_GENERATED_DIR")
endif()

list(LENGTH RECOMP_GENERATED_SOURCES RECOMP_SOURCE_COUNT)
message(STATUS "Compiling ${RECOMP_SOURCE_COUNT} XboxRecomp C translation units")

add_library(fut_street_2_recompiled_code STATIC ${RECOMP_GENERATED_SOURCES})
target_include_directories(fut_street_2_recompiled_code PUBLIC
    "${RECOMP_GENERATED_DIR}")

add_library(fut_street_2_no_emulation_runtime_support STATIC
    graphics_command_stream.c
    graphics_objects.c
    graphics_kelvin_state.c
    graphics_immediate.c
    graphics_fragment_program.c
    graphics_vertex_program.c
    graphics_surfaces.c
    graphics_textures.c
    graphics_draw_state.c
    graphics_software_state.c
    graphics_scanout.c
    guest_interrupt_gate.c
    graphics_crtc_ack.c
    no_emulation_optional_hooks.c
    mcpx_aci.c
    recomp_manual_lookup.c
    recomp_runtime_diagnostics.c)
target_include_directories(fut_street_2_no_emulation_runtime_support PUBLIC
    "${RECOMP_GENERATED_DIR}")

if(FUT_STREET2_BUILD_MCPX_DSP_ADAPTER)
    if(NOT RECOMP_DSP56300_INCLUDE_DIR OR
       NOT EXISTS "${RECOMP_DSP56300_INCLUDE_DIR}/dsp56300.h")
        message(FATAL_ERROR
            "Set RECOMP_DSP56300_INCLUDE_DIR to the pinned dsp56300 C FFI include directory")
    endif()
    if(NOT RECOMP_DSP56300_LIBRARY OR
       NOT EXISTS "${RECOMP_DSP56300_LIBRARY}")
        message(FATAL_ERROR
            "Set RECOMP_DSP56300_LIBRARY to the built dsp56300-emu-ffi static library")
    endif()

    add_library(fut_street_2_mcpx_dsp STATIC mcpx_dsp_adapter.c)
    target_include_directories(fut_street_2_mcpx_dsp PUBLIC
        "${CMAKE_CURRENT_SOURCE_DIR}"
        "${RECOMP_DSP56300_INCLUDE_DIR}")
    target_link_libraries(fut_street_2_mcpx_dsp PUBLIC
        "${RECOMP_DSP56300_LIBRARY}"
        ${RECOMP_DSP56300_LINK_LIBRARIES})

    add_executable(mcpx_dsp_probe mcpx_dsp_probe.c)
    target_link_libraries(mcpx_dsp_probe PRIVATE fut_street_2_mcpx_dsp)

    if(BUILD_TESTING)
        add_executable(mcpx_dsp_adapter_test tests/mcpx_dsp_adapter_test.c)
        # Assertions perform the isolated device operations as well as checks.
        # Release builds must not compile away the test itself.
        if(MSVC)
            target_compile_options(mcpx_dsp_adapter_test PRIVATE /UNDEBUG)
        else()
            target_compile_options(mcpx_dsp_adapter_test PRIVATE -UNDEBUG)
        endif()
        target_link_libraries(mcpx_dsp_adapter_test PRIVATE fut_street_2_mcpx_dsp)
        add_test(NAME mcpx_dsp_adapter COMMAND mcpx_dsp_adapter_test)
        set_tests_properties(mcpx_dsp_adapter PROPERTIES TIMEOUT 15)
        find_program(FUT_STREET2_POWERSHELL powershell.exe powershell REQUIRED)
        add_test(NAME mcpx_dsp_probe_contract
            COMMAND "${FUT_STREET2_POWERSHELL}" -NoProfile -ExecutionPolicy Bypass
                -File "${CMAKE_CURRENT_SOURCE_DIR}/tests/mcpx-dsp-probe-contract.ps1"
                "$<TARGET_FILE:mcpx_dsp_probe>")
        set_tests_properties(mcpx_dsp_probe_contract PROPERTIES TIMEOUT 15)
    endif()
endif()

if(BUILD_TESTING)
    add_executable(mcpx_aci_model_test tests/mcpx_aci_model_test.c
        mcpx_aci.c)
    target_include_directories(mcpx_aci_model_test PRIVATE
        "${CMAKE_CURRENT_SOURCE_DIR}")
    add_test(NAME mcpx_aci_model COMMAND mcpx_aci_model_test)
    set_tests_properties(mcpx_aci_model PROPERTIES TIMEOUT 10)

    if(WIN32)
        add_executable(mcpx_aci_host_test tests/mcpx_aci_host_test.c
            mcpx_aci_host.c mcpx_aci.c)
        target_include_directories(mcpx_aci_host_test PRIVATE
            "${CMAKE_CURRENT_SOURCE_DIR}")
        add_test(NAME mcpx_aci_host_trap COMMAND mcpx_aci_host_test)
        set_tests_properties(mcpx_aci_host_trap PROPERTIES TIMEOUT 10)
    endif()

    add_executable(graphics_crtc_ack_test tests/graphics_crtc_ack_test.c)
    target_include_directories(graphics_crtc_ack_test PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}")
    target_link_libraries(graphics_crtc_ack_test PRIVATE fut_street_2_no_emulation_runtime_support)
    add_test(NAME graphics_crtc_ack COMMAND graphics_crtc_ack_test)
    set_tests_properties(graphics_crtc_ack PROPERTIES TIMEOUT 10)
    add_executable(guest_interrupt_gate_test tests/guest_interrupt_gate_test.c)
    target_include_directories(guest_interrupt_gate_test PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}")
    target_link_libraries(guest_interrupt_gate_test PRIVATE fut_street_2_no_emulation_runtime_support)
    add_test(NAME guest_interrupt_gate COMMAND guest_interrupt_gate_test)
    set_tests_properties(guest_interrupt_gate PROPERTIES TIMEOUT 10)
    add_executable(graphics_scanout_test tests/graphics_scanout_test.c)
    target_include_directories(graphics_scanout_test PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}")
    target_link_libraries(graphics_scanout_test PRIVATE fut_street_2_no_emulation_runtime_support)
    add_test(NAME graphics_scanout COMMAND graphics_scanout_test)
    add_executable(graphics_immediate_test tests/graphics_immediate_test.c)
    target_include_directories(graphics_immediate_test PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}")
    target_link_libraries(graphics_immediate_test PRIVATE fut_street_2_no_emulation_runtime_support)
    add_test(NAME graphics_immediate COMMAND graphics_immediate_test)
    add_executable(graphics_software_state_test tests/graphics_software_state_test.c)
    target_include_directories(graphics_software_state_test PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}")
    target_link_libraries(graphics_software_state_test PRIVATE fut_street_2_no_emulation_runtime_support)
    add_test(NAME graphics_software_state COMMAND graphics_software_state_test)
    add_executable(graphics_kelvin_state_test tests/graphics_kelvin_state_test.c)
    target_include_directories(graphics_kelvin_state_test PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}")
    target_link_libraries(graphics_kelvin_state_test PRIVATE fut_street_2_no_emulation_runtime_support)
    add_test(NAME graphics_kelvin_state COMMAND graphics_kelvin_state_test)
    add_executable(graphics_objects_test tests/graphics_objects_test.c)
    target_include_directories(graphics_objects_test PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}")
    target_link_libraries(graphics_objects_test PRIVATE fut_street_2_no_emulation_runtime_support)
    add_test(NAME graphics_objects COMMAND graphics_objects_test)
    add_executable(graphics_command_stream_test tests/graphics_command_stream_test.c)
    target_include_directories(graphics_command_stream_test PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}")
    target_link_libraries(graphics_command_stream_test PRIVATE fut_street_2_no_emulation_runtime_support)
    add_test(NAME graphics_command_stream COMMAND graphics_command_stream_test)
    add_executable(no_emulation_runtime_support_test
        tests/no_emulation_runtime_support_test.c)
    target_link_libraries(no_emulation_runtime_support_test PRIVATE
        fut_street_2_no_emulation_runtime_support)
    add_test(NAME no_emulation_runtime_support
        COMMAND no_emulation_runtime_support_test)

    add_executable(recomp_packed_word_ops_test
        tests/recomp_packed_word_ops_test.c)
    target_include_directories(recomp_packed_word_ops_test PRIVATE
        "${RECOMP_GENERATED_DIR}")
    add_test(NAME recomp_packed_word_ops
        COMMAND recomp_packed_word_ops_test)

    find_program(FUT_STREET2_POWERSHELL powershell.exe powershell REQUIRED)
    add_test(NAME runtime_diagnostic_stop
        COMMAND "${FUT_STREET2_POWERSHELL}" -NoProfile -ExecutionPolicy Bypass
            -File "${CMAKE_CURRENT_SOURCE_DIR}/tests/runtime-diagnostic-stop.ps1"
            "$<TARGET_FILE:no_emulation_runtime_support_test>")
    add_test(NAME recomp_generated_dispatch_table
        COMMAND "${FUT_STREET2_POWERSHELL}" -NoProfile
            -ExecutionPolicy Bypass
            -File "${CMAKE_CURRENT_SOURCE_DIR}/tests/verify-generated-dispatch.ps1"
            "${RECOMP_GENERATED_DIR}")
endif()

if(FUT_STREET2_BUILD_HOST_APP)
    if(NOT WIN32)
        message(FATAL_ERROR "The current XboxRecomp native runtime integration is Windows-only")
    endif()
    if(NOT RECOMP_HOST_LIBRARIES OR
       NOT RECOMP_XBOXRECOMP_SOURCE_DIR)
        message(FATAL_ERROR
            "Set RECOMP_HOST_LIBRARIES and RECOMP_XBOXRECOMP_SOURCE_DIR from the audited no-emulation runtime build")
    endif()

    set(RECOMP_NO_EMULATION_KERNEL_SOURCES
        "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src/kernel/kernel_path.c"
        "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src/kernel/kernel_pool.c"
        "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src/kernel/kernel_rtl.c"
        "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src/kernel/kernel_memory.c"
        "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src/kernel/kernel_file.c"
        "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src/kernel/kernel_thread.c"
        "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src/kernel/kernel_sync.c"
        "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src/kernel/kernel_hal.c"
        "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src/kernel/kernel_xbox.c"
        "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src/kernel/kernel_ob.c"
        "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src/kernel/kernel_io.c"
        "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src/kernel/kernel_crypto.c"
        "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src/kernel/kernel_thunks.c"
        "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src/kernel/kernel_bridge.c"
        "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src/kernel/xbox_memory_layout.c"
        "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src/kernel/guest_vmem.c"
        "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src/kernel/icall_feedback.c"
        "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src/kernel/recomp_trace.c")

    foreach(runtime_source IN LISTS RECOMP_NO_EMULATION_KERNEL_SOURCES)
        if(NOT EXISTS "${runtime_source}")
            message(FATAL_ERROR "Missing no-emulation runtime source: ${runtime_source}")
        endif()
    endforeach()

    add_library(fut_street_2_no_emulation_kernel STATIC
        ${RECOMP_NO_EMULATION_KERNEL_SOURCES})
    target_include_directories(fut_street_2_no_emulation_kernel PUBLIC
        "${RECOMP_GENERATED_DIR}"
        "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src"
        "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src/kernel")
    target_compile_definitions(fut_street_2_no_emulation_kernel PRIVATE
        WIN32_LEAN_AND_MEAN NOMINMAX FUT_NATIVE_INTERRUPT_GATES=1)
    target_include_directories(fut_street_2_no_emulation_kernel PRIVATE
        "${CMAKE_CURRENT_SOURCE_DIR}")
    target_link_libraries(fut_street_2_no_emulation_kernel PUBLIC
        kernel32 bcrypt
        fut_street_2_no_emulation_runtime_support)

    add_executable(fut_street_2_recompiled_app host_bootstrap.c mcpx_aci_host.c graphics_native_backend.c graphics_native_scanout_dispatch.c graphics_native_constants.c graphics_native_vertex.c graphics_native_surfaces.c graphics_native_pipeline.c graphics_native_textures.c graphics_native_fragment.c graphics_native_immediate.c graphics_native_draw.c graphics_native_display.c graphics_native_vblank.c)
    target_include_directories(fut_street_2_recompiled_app PRIVATE
        "${CMAKE_CURRENT_SOURCE_DIR}"
        "${RECOMP_GENERATED_DIR}"
        "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src"
        "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src/kernel")
    target_compile_definitions(fut_street_2_recompiled_app PRIVATE
        WIN32_LEAN_AND_MEAN NOMINMAX)
    target_link_libraries(fut_street_2_recompiled_app PRIVATE
        fut_street_2_recompiled_code
        fut_street_2_no_emulation_kernel
        ${RECOMP_HOST_LIBRARIES} dxguid dxgi user32)
    # DSP experimentation remains isolated; never link it into the game host.

    add_executable(graphics_replay graphics_replay.c graphics_native_constants.c graphics_native_vertex.c graphics_native_surfaces.c graphics_native_pipeline.c graphics_native_textures.c graphics_native_fragment.c graphics_native_immediate.c graphics_native_draw.c graphics_native_display.c graphics_native_vblank.c)
    target_include_directories(graphics_replay PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}" "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src" "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src/kernel")
    target_compile_definitions(graphics_replay PRIVATE WIN32_LEAN_AND_MEAN NOMINMAX)
    target_link_libraries(graphics_replay PRIVATE fut_street_2_no_emulation_runtime_support ${RECOMP_HOST_LIBRARIES} dxguid dxgi user32)

    add_executable(no_emulation_kernel_file_contract_test
        tests/no_emulation_kernel_file_contract_test.c)
    add_executable(native_dpc_gate_test tests/native_dpc_gate_test.c)
    target_include_directories(native_dpc_gate_test PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}")
    target_link_libraries(native_dpc_gate_test PRIVATE fut_street_2_no_emulation_kernel ${RECOMP_HOST_LIBRARIES})
    add_test(NAME native_dpc_gate COMMAND native_dpc_gate_test)
    set_tests_properties(native_dpc_gate PROPERTIES TIMEOUT 10)
    add_executable(native_interrupt_ownership_test tests/native_interrupt_ownership_test.c)
    target_include_directories(native_interrupt_ownership_test PRIVATE
        "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src/kernel")
    add_test(NAME native_interrupt_ownership COMMAND native_interrupt_ownership_test)
    target_include_directories(no_emulation_kernel_file_contract_test PRIVATE
        "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src"
        "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src/kernel")
    target_compile_definitions(no_emulation_kernel_file_contract_test PRIVATE
        WIN32_LEAN_AND_MEAN NOMINMAX)
    target_link_libraries(no_emulation_kernel_file_contract_test PRIVATE
        fut_street_2_no_emulation_kernel
        ${RECOMP_HOST_LIBRARIES})

    if(BUILD_TESTING)
        add_executable(graphics_native_immediate_test tests/graphics_native_immediate_test.c graphics_native_immediate.c graphics_native_vertex.c graphics_native_constants.c)
        target_include_directories(graphics_native_immediate_test PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}")
        target_link_libraries(graphics_native_immediate_test PRIVATE fut_street_2_no_emulation_runtime_support d3d11 d3dcompiler)
        add_test(NAME graphics_native_immediate COMMAND graphics_native_immediate_test)
        set_tests_properties(graphics_native_immediate PROPERTIES TIMEOUT 15)
        add_executable(graphics_native_backend_test tests/graphics_native_backend_test.c graphics_native_surfaces.c graphics_native_vertex.c graphics_native_pipeline.c graphics_native_constants.c graphics_native_textures.c graphics_native_fragment.c graphics_native_immediate.c graphics_native_draw.c graphics_native_display.c graphics_native_vblank.c)
        target_include_directories(graphics_native_backend_test PRIVATE
            "${CMAKE_CURRENT_SOURCE_DIR}" "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src"
            "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src/kernel")
        target_compile_definitions(graphics_native_backend_test PRIVATE WIN32_LEAN_AND_MEAN NOMINMAX)
        target_link_libraries(graphics_native_backend_test PRIVATE fut_street_2_no_emulation_runtime_support d3d11 d3dcompiler dxguid dxgi user32)
        add_test(NAME graphics_native_backend COMMAND graphics_native_backend_test)
        set_tests_properties(graphics_native_backend PROPERTIES TIMEOUT 15)
        add_executable(graphics_native_scanout_dispatch_test
            tests/graphics_native_scanout_dispatch_test.c graphics_native_scanout_dispatch.c)
        target_include_directories(graphics_native_scanout_dispatch_test PRIVATE
            "${CMAKE_CURRENT_SOURCE_DIR}" "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src"
            "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src/kernel")
        target_link_libraries(graphics_native_scanout_dispatch_test PRIVATE
            fut_street_2_no_emulation_runtime_support)
        add_test(NAME graphics_native_scanout_dispatch COMMAND graphics_native_scanout_dispatch_test)
        set_tests_properties(graphics_native_scanout_dispatch PROPERTIES TIMEOUT 10)
        add_executable(graphics_native_display_test tests/graphics_native_display_test.c graphics_native_display.c graphics_native_constants.c graphics_native_vblank.c)
        target_include_directories(graphics_native_display_test PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}")
        target_link_libraries(graphics_native_display_test PRIVATE d3d11 dxgi dxguid user32)
        add_test(NAME graphics_native_display COMMAND graphics_native_display_test)
        set_tests_properties(graphics_native_display PROPERTIES TIMEOUT 15 SKIP_RETURN_CODE 77
            ENVIRONMENT "RECOMP_NATIVE_DISPLAY_ORIGIN=1")
        add_executable(graphics_native_vblank_test tests/graphics_native_vblank_test.c graphics_native_vblank.c)
        target_include_directories(graphics_native_vblank_test PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}")
        target_link_libraries(graphics_native_vblank_test PRIVATE dxgi dxguid user32)
        add_test(NAME graphics_native_vblank COMMAND graphics_native_vblank_test)
        set_tests_properties(graphics_native_vblank PROPERTIES TIMEOUT 12 SKIP_RETURN_CODE 77)
        add_executable(graphics_native_draw_test tests/graphics_native_draw_test.c graphics_native_draw.c graphics_native_immediate.c graphics_native_vertex.c graphics_native_pipeline.c graphics_native_surfaces.c graphics_native_constants.c)
        target_include_directories(graphics_native_draw_test PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}")
        target_link_libraries(graphics_native_draw_test PRIVATE fut_street_2_no_emulation_runtime_support d3d11 d3dcompiler)
        add_test(NAME graphics_native_draw COMMAND graphics_native_draw_test)
        set_tests_properties(graphics_native_draw PROPERTIES TIMEOUT 15)
        add_executable(graphics_native_fragment_test tests/graphics_native_fragment_test.c graphics_native_fragment.c graphics_native_pipeline.c graphics_native_textures.c graphics_native_constants.c)
        target_include_directories(graphics_native_fragment_test PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}")
        target_link_libraries(graphics_native_fragment_test PRIVATE fut_street_2_no_emulation_runtime_support d3d11 d3dcompiler dxguid)
        add_test(NAME graphics_native_fragment COMMAND graphics_native_fragment_test)
        set_tests_properties(graphics_native_fragment PROPERTIES TIMEOUT 15)
        add_executable(graphics_native_textures_test tests/graphics_native_textures_test.c graphics_native_textures.c graphics_native_constants.c graphics_native_fragment.c graphics_native_pipeline.c)
        add_executable(graphics_native_quincunx_test tests/graphics_native_quincunx_test.c graphics_native_textures.c graphics_native_constants.c graphics_native_immediate.c)
        target_include_directories(graphics_native_quincunx_test PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}")
        target_link_libraries(graphics_native_quincunx_test PRIVATE fut_street_2_no_emulation_runtime_support d3d11 d3dcompiler)
        add_test(NAME graphics_native_quincunx COMMAND graphics_native_quincunx_test)
        set_tests_properties(graphics_native_quincunx PROPERTIES TIMEOUT 15)
        target_include_directories(graphics_native_textures_test PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}")
        target_link_libraries(graphics_native_textures_test PRIVATE fut_street_2_no_emulation_runtime_support d3d11 d3dcompiler dxguid)
        add_test(NAME graphics_native_textures COMMAND graphics_native_textures_test)
        set_tests_properties(graphics_native_textures PROPERTIES TIMEOUT 15)
        add_executable(graphics_native_pipeline_test tests/graphics_native_pipeline_test.c graphics_native_pipeline.c graphics_native_constants.c)
        add_executable(graphics_native_aa_test tests/graphics_native_aa_test.c graphics_native_constants.c)
        add_executable(graphics_native_aa_interpolation_test tests/graphics_native_aa_interpolation_test.c graphics_native_vertex.c graphics_native_fragment.c graphics_native_textures.c graphics_native_pipeline.c graphics_native_immediate.c graphics_native_constants.c)
        target_include_directories(graphics_native_aa_interpolation_test PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}")
        target_link_libraries(graphics_native_aa_interpolation_test PRIVATE fut_street_2_no_emulation_runtime_support d3d11 d3dcompiler dxguid)
        add_test(NAME graphics_native_aa_interpolation COMMAND graphics_native_aa_interpolation_test)
        set_tests_properties(graphics_native_aa_interpolation PROPERTIES TIMEOUT 15)
        target_include_directories(graphics_native_aa_test PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}")
        target_link_libraries(graphics_native_aa_test PRIVATE d3d11 d3dcompiler)
        target_include_directories(graphics_native_pipeline_test PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}")
        target_link_libraries(graphics_native_pipeline_test PRIVATE fut_street_2_no_emulation_runtime_support d3d11 d3dcompiler)
        add_test(NAME graphics_native_pipeline COMMAND graphics_native_pipeline_test)
        set_tests_properties(graphics_native_pipeline PROPERTIES TIMEOUT 15)
        add_executable(graphics_native_surfaces_test tests/graphics_native_surfaces_test.c graphics_native_surfaces.c graphics_native_constants.c)
        target_include_directories(graphics_native_surfaces_test PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}")
        target_link_libraries(graphics_native_surfaces_test PRIVATE fut_street_2_no_emulation_runtime_support d3d11)
        add_test(NAME graphics_native_surfaces COMMAND graphics_native_surfaces_test)
        set_tests_properties(graphics_native_surfaces PROPERTIES TIMEOUT 15)
        add_executable(graphics_native_vertex_test tests/graphics_native_vertex_test.c graphics_native_vertex.c graphics_native_constants.c)
        target_include_directories(graphics_native_vertex_test PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}")
        target_link_libraries(graphics_native_vertex_test PRIVATE fut_street_2_no_emulation_runtime_support d3d11 d3dcompiler)
        add_test(NAME graphics_native_vertex COMMAND graphics_native_vertex_test)
        set_tests_properties(graphics_native_vertex PROPERTIES TIMEOUT 15)
        add_executable(graphics_native_constants_test tests/graphics_native_constants_test.c graphics_native_constants.c)
        target_include_directories(graphics_native_constants_test PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}")
        target_link_libraries(graphics_native_constants_test PRIVATE fut_street_2_no_emulation_runtime_support d3d11)
        add_test(NAME graphics_native_constants COMMAND graphics_native_constants_test)
        set_tests_properties(graphics_native_constants PROPERTIES TIMEOUT 15)
        add_test(NAME no_emulation_kernel_file_contract
            COMMAND no_emulation_kernel_file_contract_test)
    endif()

    if(CMAKE_C_COMPILER_ID MATCHES "Clang|GNU")
        target_link_options(fut_street_2_recompiled_app PRIVATE
            "-Wl,--no-undefined")
    endif()
    if(BUILD_TESTING)
        add_test(NAME recompiled_host_help
            COMMAND fut_street_2_recompiled_app --help)
        find_program(FUT_STREET2_POWERSHELL powershell.exe powershell REQUIRED)
        add_test(NAME recompiled_host_rejects_missing_preflight_xbe
            COMMAND "${FUT_STREET2_POWERSHELL}" -NoProfile -ExecutionPolicy Bypass
                -File "${CMAKE_CURRENT_SOURCE_DIR}/tests/preflight-rejects-missing-xbe.ps1"
                "$<TARGET_FILE:fut_street_2_recompiled_app>")
        add_test(NAME recompiled_host_rejects_synthetic_vblank
            COMMAND "${FUT_STREET2_POWERSHELL}" -NoProfile -ExecutionPolicy Bypass
                -File "${CMAKE_CURRENT_SOURCE_DIR}/tests/reject-synthetic-vblank.ps1"
                "$<TARGET_FILE:fut_street_2_recompiled_app>")
    endif()
endif()

if(MSVC)
    target_compile_options(fut_street_2_recompiled_code PRIVATE /bigobj)
endif()
