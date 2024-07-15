#!/bin/bash

# Set the base directory to your solution's root directory
BASE_DIR=~/Documents/Coding/meteor-avalonia

# Navigate to the base directory
cd $BASE_DIR

# Run tests and collect code coverage for all test projects
dotnet test --collect:"XPlat Code Coverage"

# Find all coverage.cobertura.xml files in the TestResults directory
COVERAGE_FILES=$(find . -type f -name "coverage.cobertura.xml")

# Check if there are any coverage.cobertura.xml files
if [ -n "$COVERAGE_FILES" ]; then
    # Join the coverage files with semicolon
    COVERAGE_FILES_STRING=$(echo $COVERAGE_FILES | tr ' ' ';')

    # Generate a combined coverage report
    reportgenerator -reports:"$COVERAGE_FILES_STRING" -targetdir:./CoverageReport
    echo "Coverage report generated at ./CoverageReport"
else
    echo "Coverage report file not found!"
fi
