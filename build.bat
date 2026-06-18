@echo off
chcp 65001 >nul
echo ============================================
echo   DY01 - Windows EXE 打包工具
echo ============================================
echo.

:: 检查 Python
python --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] 未找到 Python，请先安装 Python 3.10+
    echo 下载地址: https://www.python.org/downloads/
    pause
    exit /b 1
)

echo [1/3] 安装依赖...
pip install pygame pyinstaller --quiet
if %errorlevel% neq 0 (
    echo [ERROR] 依赖安装失败，请检查网络连接
    pause
    exit /b 1
)

echo [2/3] 打包为独立 EXE...
python -m PyInstaller --onefile --name DY01 --noconsole --clean main.py
if %errorlevel% neq 0 (
    echo [ERROR] 打包失败
    pause
    exit /b 1
)

echo [3/3] 完成!
echo.
echo ============================================
echo   成功! EXE 文件位于: dist\DY01.exe
echo   直接双击运行即可，无需安装任何依赖!
echo ============================================
echo.
pause