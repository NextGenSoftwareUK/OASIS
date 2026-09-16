@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0seed_our_world_tree_quest.ps1" %*
exit /b %errorlevel%
