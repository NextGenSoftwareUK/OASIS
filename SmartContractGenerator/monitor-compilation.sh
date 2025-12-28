#!/bin/bash
# Monitor Solana contract compilation progress

echo "🔍 Monitoring Solana Contract Compilation"
echo "=========================================="
echo ""

# Find the build cache directory
BUILD_DIR=$(find /private/var/folders -name "anchor_build_cache" -type d 2>/dev/null | head -1)
if [ -z "$BUILD_DIR" ]; then
    BUILD_DIR=$(find /tmp -name "anchor_build_cache" -type d 2>/dev/null | head -1)
fi

if [ -n "$BUILD_DIR" ]; then
    LATEST_BUILD=$(find "$BUILD_DIR" -maxdepth 1 -type d -name "[0-9a-f]*-[0-9a-f]*-[0-9a-f]*" | sort -r | head -1)
    if [ -n "$LATEST_BUILD" ]; then
        echo "📁 Build directory: $LATEST_BUILD"
        echo ""
        
        # Count compiled crates
        if [ -d "$LATEST_BUILD/target/release/deps" ]; then
            COMPILED=$(find "$LATEST_BUILD/target/release/deps" -name "*.rlib" 2>/dev/null | wc -l | tr -d ' ')
            echo "✅ Compiled dependencies: $COMPILED"
        fi
        
        # Check if program is compiling
        if [ -d "$LATEST_BUILD/programs" ]; then
            PROGRAM_DIR=$(find "$LATEST_BUILD/programs" -name "*.so" 2>/dev/null | head -1)
            if [ -n "$PROGRAM_DIR" ]; then
                echo "✅ Program compiled: $(basename "$PROGRAM_DIR")"
            else
                echo "⏳ Program still compiling..."
            fi
        fi
    fi
fi

echo ""
echo "🔧 Active compilation processes:"
ps aux | grep -E "rustc.*anchor_build_cache" | grep -v grep | wc -l | xargs -I {} echo "   {} rustc processes running"

echo ""
echo "⏱️  Expected time:"
echo "   • First build: 8-10 minutes (compiling 200+ dependencies)"
echo "   • Subsequent builds: 2-3 minutes (using cache)"
echo ""
echo "💡 Tip: This is normal! Rust compiles everything from source."


