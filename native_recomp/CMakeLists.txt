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
option(FUT_STREET2_BUILD_HOST_APP
    "Build the statically recompiled host executable when runtime inputs are supplied"
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
    graphics_vertex_program.c
    graphics_surfaces.c
    graphics_draw_state.c
    no_emulation_optional_hooks.c
    recomp_manual_lookup.c
    recomp_runtime_diagnostics.c)
target_include_directories(fut_street_2_no_emulation_runtime_support PUBLIC
    "${RECOMP_GENERATED_DIR}")

if(BUILD_TESTING)
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
        WIN32_LEAN_AND_MEAN NOMINMAX)
    target_link_libraries(fut_street_2_no_emulation_kernel PUBLIC
        kernel32 bcrypt
        fut_street_2_no_emulation_runtime_support)

    add_executable(fut_street_2_recompiled_app host_bootstrap.c graphics_native_backend.c graphics_native_constants.c graphics_native_vertex.c graphics_native_surfaces.c graphics_native_pipeline.c)
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
        ${RECOMP_HOST_LIBRARIES})

    add_executable(no_emulation_kernel_file_contract_test
        tests/no_emulation_kernel_file_contract_test.c)
    target_include_directories(no_emulation_kernel_file_contract_test PRIVATE
        "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src"
        "${RECOMP_XBOXRECOMP_SOURCE_DIR}/src/kernel")
    target_compile_definitions(no_emulation_kernel_file_contract_test PRIVATE
        WIN32_LEAN_AND_MEAN NOMINMAX)
    target_link_libraries(no_emulation_kernel_file_contract_test PRIVATE
        fut_street_2_no_emulation_kernel
        ${RECOMP_HOST_LIBRARIES})

    if(BUILD_TESTING)
        add_executable(graphics_native_pipeline_test tests/graphics_native_pipeline_test.c graphics_native_pipeline.c graphics_native_constants.c)
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
