@echo off
powershell -ExecutionPolicy Bypass -Command "& '%~dp0compile_and_test_inventory.ps1' -BaseUrl 'http://localhost:5556' -Username 'dellams' -Password 'test!'"
