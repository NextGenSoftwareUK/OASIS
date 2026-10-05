#!/bin/bash
# Test script to run after API restart

echo "=========================================="
echo "Testing Agent Registration After API Restart"
echo "=========================================="
echo ""

source venv/bin/activate

python test_agent_registration.py

echo ""
echo "=========================================="
echo "If you see 'Token: Yes' and wallet created,"
echo "then the API restart was successful!"
echo "=========================================="
