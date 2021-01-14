#!/usr/bin/env bash

# To get your api access token go to quantconnect.com/account

docker run \
 --rm \
 --mount type=bind,source="$LOCAL_CONFIG",target=/Lean/Launcher/config.json,readonly \
 --mount type=bind,source="$LOCAL_DATA",target=/Data \
 --mount type=bind,source="$LOCAL_RESULTS",target=/Results \
 --mount type=bind,source="$LOCAL_ALGO_DLL",target=/Lean/Launcher/bin/Debug/QuantConnect.Algorithm.CSharp.dll,readonly \
 --name LeanEngine-1 \
 --entrypoint /bin/bash \
 quantconnect/lean:10291 \
 -c "mono QuantConnect.Lean.Launcher.exe --data-folder /Data --results-destination-folder /Results --config /Lean/Launcher/config.json --api-access-token $API_ACCESS_TOKEN --job-user-id $JOB_USER_ID < /dev/null"
 