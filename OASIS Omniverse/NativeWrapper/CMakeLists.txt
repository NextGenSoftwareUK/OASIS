cmake_minimum_required(VERSION 3.10)
project(star_api_wrapper)

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
    star_api.cpp
)

# Header files
set(HEADERS
    star_api.h
)

# Create shared library
add_library(star_api SHARED ${SOURCES} ${HEADERS})

# Include directories
target_include_directories(star_api PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}
)

# Platform-specific libraries
if(PLATFORM_WINDOWS)
    # Windows: Use WinHTTP
    target_link_libraries(star_api winhttp)
else()
    # Linux/Mac: Use libcurl
    find_package(PkgConfig REQUIRED)
    pkg_check_modules(CURL REQUIRED libcurl)
    target_link_libraries(star_api ${CURL_LIBRARIES})
    target_include_directories(star_api PRIVATE ${CURL_INCLUDE_DIRS})
endif()

# Installation
install(TARGETS star_api
    LIBRARY DESTINATION lib
    ARCHIVE DESTINATION lib
    RUNTIME DESTINATION bin
)

install(FILES ${HEADERS}
    DESTINATION include/star_api
)

# Optional: Create static library as well
add_library(star_api_static STATIC ${SOURCES} ${HEADERS})
set_target_properties(star_api_static PROPERTIES OUTPUT_NAME star_api)
if(PLATFORM_WINDOWS)
    target_link_libraries(star_api_static winhttp)
else()
    target_link_libraries(star_api_static ${CURL_LIBRARIES})
endif()



