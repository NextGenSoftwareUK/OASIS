cmake_minimum_required(VERSION 3.16)
project(oasis_nr_plugin C)

set(CMAKE_C_STANDARD 11)

add_library(oasis_nr_plugin MODULE oasis_nr_plugin.c)
target_include_directories(oasis_nr_plugin PRIVATE .)

if(WIN32)
    # Link the OGEditorClient import library; dll must be deployed next to NetRadiant.exe
    target_link_libraries(oasis_nr_plugin PRIVATE
        "${CMAKE_CURRENT_SOURCE_DIR}/../../../../../../UltimateDoomBuilder/Source/OGEditorSDK/Native/OGEditorClient.lib")
else()
    target_link_libraries(oasis_nr_plugin PRIVATE dl)
endif()

# NetRadiant looks for plugins named lib*.so / *.dll in its plugins/ folder.
set_target_properties(oasis_nr_plugin PROPERTIES
    PREFIX ""
    OUTPUT_NAME "oasis_ogengine"
)
