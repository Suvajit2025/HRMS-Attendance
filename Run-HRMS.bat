@echo off
title HRMS - Workforce Management System
echo ===================================================
echo     HRMS Workforce Management System (.NET 8)
echo ===================================================
echo.
echo Starting HRMS Server...
echo   * Local access:    http://localhost:5055
echo   * Default Login:   Username: admin  ^|  Password: 123
echo.
echo Opening browser in 2 seconds...
echo (Keep this window open while using the application)
echo ===================================================
echo.

start "" "http://localhost:5055"
HRMS.exe --urls=http://0.0.0.0:5055
pause
