#!/bin/bash
# Quick test runner for MNEE Hackathon Submission

echo "=========================================="
echo "MNEE Hackathon - Testing Menu"
echo "=========================================="
echo ""
echo "1. Quick Registration Test (30 seconds)"
echo "2. Start Data Analyzer Agent"
echo "3. Start Image Generator Agent"
echo "4. Run Full Payment Demo"
echo "5. Run All Tests"
echo ""
read -p "Select option (1-5): " choice

cd /Volumes/Storage/OASIS_CLEAN/mnee-hackathon-submission
source venv/bin/activate

case $choice in
    1)
        echo "Running registration test..."
        python test_agent_registration.py
        ;;
    2)
        echo "Starting Data Analyzer Agent..."
        python demo/data_analyzer_agent.py
        ;;
    3)
        echo "Starting Image Generator Agent..."
        python demo/image_generator_agent.py
        ;;
    4)
        echo "Running full payment demo..."
        echo "Make sure service agents are running first!"
        python demo/run_demo.py
        ;;
    5)
        echo "Running all tests..."
        echo ""
        echo "=== Test 1: Registration ==="
        python test_agent_registration.py
        echo ""
        echo "=== Test 2: Full Demo ==="
        echo "Note: Start service agents in separate terminals first"
        python demo/run_demo.py
        ;;
    *)
        echo "Invalid option"
        exit 1
        ;;
esac
