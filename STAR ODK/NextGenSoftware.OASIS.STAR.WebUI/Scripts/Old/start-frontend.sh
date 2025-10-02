#!/bin/bash
echo "Starting React Frontend Server..."
echo "Current directory: $(pwd)"
echo "Changing to ClientApp directory..."

cd ClientApp
echo "Now in directory: $(pwd)"
echo "Checking if package.json exists..."
ls -la package.json

echo "Starting npm start..."
npm start


