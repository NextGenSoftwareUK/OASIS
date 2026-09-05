add_library(dm_oasis MODULE
            oasis.cpp
            OASISPanel.cpp)

target_compile_options(dm_oasis PUBLIC ${SIGC_CFLAGS})
target_link_libraries(dm_oasis PUBLIC wxutil)

# Link OGEditorClient.dll import library (Windows) or shared object (Linux/macOS).
# The dll/so is expected next to the DarkRadiant executable at runtime.
if(WIN32)
    target_link_libraries(dm_oasis PUBLIC
        "${CMAKE_SOURCE_DIR}/libs/OGEditorClient/OGEditorClient.lib")
    target_include_directories(dm_oasis PRIVATE
        "${CMAKE_SOURCE_DIR}/libs/OGEditorClient")
else()
    target_link_libraries(dm_oasis PUBLIC dl)   # dlopen used for runtime load on Linux
    target_compile_definitions(dm_oasis PRIVATE OGEDITOR_DLOPEN)
    target_include_directories(dm_oasis PRIVATE
        "${CMAKE_SOURCE_DIR}/libs/OGEditorClient")
endif()

set_target_properties(
    dm_oasis PROPERTIES LIBRARY_OUTPUT_DIRECTORY "${DR_STAGING_PLUGINSDIR}"
)
