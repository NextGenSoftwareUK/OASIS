cmake_minimum_required(VERSION 3.16)
project(oasis_m32_tool C)

set(CMAKE_C_STANDARD 11)

add_executable(oasis_m32_tool oasis_m32_tool.c)
target_include_directories(oasis_m32_tool PRIVATE .)

if(WIN32)
    target_link_libraries(oasis_m32_tool PRIVATE
        "${CMAKE_CURRENT_SOURCE_DIR}/../../../../../../UltimateDoomBuilder/Source/OGEditorSDK/Native/OGEditorClient.lib")
else()
    target_link_libraries(oasis_m32_tool PRIVATE dl)
endif()
