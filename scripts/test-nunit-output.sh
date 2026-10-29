#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
mkdir -p TestResults/nunit-output
output=TestResults/nunit-output/console.log
dotnet build src/SharpAssert.NUnitOutputTest -c Release -warnaserror
status=0
dotnet test src/SharpAssert.NUnitOutputTest -c Release --no-build --blame-hang-timeout 30s --logger 'console;verbosity=normal' > "$output" 2>&1 || status=$?
[[ "$status" == 1 ]]
grep -q 'Failed: *1' "$output"
grep -q 'Primary diagnostics stay visible' "$output"
grep -Eq 'Left: +1' "$output"
grep -q 'Right: 2' "$output"
[[ "$(grep -c 'Result:' "$output")" == 1 ]]
grep -Eq '^[[:space:]]*Result: Failed[[:space:]]*$' "$output"
! grep -Eq 'AssertionEvaluationResult \{|Context =|ExprNode =|LogicalEvaluationResult \{' "$output"
! grep -Eq 'Left: left|Right: right|&&:|InvalidOperationException' "$output"
[[ "$(grep -c 'Assertion failed:' "$output")" == 1 ]]
echo 'NUnit output shows the failed condition and a bounded Result line'
