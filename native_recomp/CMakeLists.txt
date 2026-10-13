cmake_minimum_required(VERSION 3.20)

project(FutStreet2RecompiledCode LANGUAGES C)

set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)

set(RECOMP_GENERATED_DIR "" CACHE PATH
    "Directory containing XboxRecomp-generated C and headers")
set(RECOMP_ARCHIVER "" CACHE FILEPATH
    "Optional archiver executable or wrapper, e.g. zig-ar.cmd for Zig")

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

if(MSVC)
    target_compile_options(fut_street_2_recompiled_code PRIVATE /bigobj)
endif()
