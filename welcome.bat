@echo off
title FACADE LAUNCHER
echo **** YOU SHOULD ALREADY HAVE THE ANACONDA ENVIRONMENT ALREADY SET UP AT THIS POINT ****
pause
set @myvar="C:\Users\agath\Anaconda3\Scripts\"
echo %@myvar%
start Facade.exe
call %@myvar%\activate.bat cv2
cd ".\Assets\StreamingAssets\FERModel\"
call python video_emotion_color_demo.py
pause