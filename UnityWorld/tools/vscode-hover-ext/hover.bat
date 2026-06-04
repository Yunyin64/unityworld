@echo off
chcp 65001 >nul
python -u "%~dp0hover_cli.py" %*
