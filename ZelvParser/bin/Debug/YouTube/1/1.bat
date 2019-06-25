@echo off
echo Starting task... (-names "Zelv_Poster")
"C:\Program Files (x86)\ZennoLab\RU\ZennoPoster Pro\5.16.2.0\Progs\TasksRunner.exe" -o StartTask -names "Zelv_Poster"
timeout /t 1
echo Set tries count to 99... (-names "Zelv_Poster")
"C:\Program Files (x86)\ZennoLab\RU\ZennoPoster Pro\5.16.2.0\Progs\TasksRunner.exe" -o SetTries 99 -names "Zelv_Poster"