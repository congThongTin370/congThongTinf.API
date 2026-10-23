@echo off
cd /d "%~dp0"

docker network inspect congthongtinf >nul 2>&1 || docker network create congthongtinf
if errorlevel 1 exit /b 1

docker volume inspect congthongtinf_seaweeddata >nul 2>&1 || docker volume create congthongtinf_seaweeddata
if errorlevel 1 exit /b 1

docker compose -f compose.storage.yml up -d --wait
if errorlevel 1 exit /b 1

docker compose -f compose.yml up -d --build --remove-orphans
