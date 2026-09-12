cmake_minimum_required(VERSION 3.10)
project(ogengine_wrapper)

set(CMAKE_CXX_STANDARD 11)
set(CMAKE_C_STANDARD 99)

# Platform-specific settings
if(WIN32)
    set(PLATFORM_WINDOWS TRUE)
else()
    set(PLATFORM_UNIX TRUE)
endif()

# Source files
set(SOURCES
    ogengine.cpp
)

# Header files
set(HEADERS
    ogengine.h
)

# Create shared library
add_library(ogengine SHARED ${SOURCES} ${HEADERS})

# Include directories
target_include_directories(ogengine PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}
)

# Platform-specific libraries
if(PLATFORM_WINDOWS)
    # Windows: Use WinHTTP
    target_link_libraries(ogengine winhttp)
else()
    # Linux/Mac: Use libcurl
    find_package(PkgConfig REQUIRED)
    pkg_check_modules(CURL REQUIRED libcurl)
    target_link_libraries(ogengine ${CURL_LIBRARIES})
    target_include_directories(ogengine PRIVATE ${CURL_INCLUDE_DIRS})
endif()

# Installation
install(TARGETS ogengine
    LIBRARY DESTINATION lib
    ARCHIVE DESTINATION lib
    RUNTIME DESTINATION bin
)

install(FILES ${HEADERS}
    DESTINATION include/ogengine
)

# Optional: Create static library as well
add_library(ogengine_static STATIC ${SOURCES} ${HEADERS})
set_target_properties(ogengine_static PROPERTIES OUTPUT_NAME ogengine)
if(PLATFORM_WINDOWS)
    target_link_libraries(ogengine_static winhttp)
else()
    target_link_libraries(ogengine_static ${CURL_LIBRARIES})
endif()



