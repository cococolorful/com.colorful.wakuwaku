set(TARGET_NAME NativeRenderer)

file(GLOB_RECURSE HEADER_FILES "*.h")
file(GLOB_RECURSE SOURCE_FILES "*.cpp")

add_library (${TARGET_NAME} SHARED ${HEADER_FILES} ${SOURCE_FILES})
# 包含子项目。
target_include_directories(${TARGET_NAME} PUBLIC ${CMAKE_CURRENT_LIST_DIR})
