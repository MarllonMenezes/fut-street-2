# CMake's Zig compiler identification does not publish Zig's bundled archiver
# as CMAKE_AR. Set this in CMAKE_USER_MAKE_RULES_OVERRIDE for the static
# runtime libraries, after compiler detection has initialized the C rules.
if(DEFINED ENV{FUT_STREET2_ZIG})
    set(CMAKE_AR "${CMAKE_CURRENT_LIST_DIR}/zig-ar.cmd")
endif()
