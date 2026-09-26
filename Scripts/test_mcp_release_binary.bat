@echo off
python "%~dp0test_mcp_release_binary.py" %*
exit /b %errorlevel%
