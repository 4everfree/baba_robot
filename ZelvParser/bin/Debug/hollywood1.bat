@echo off
echo Starting task... (-names "Hollywood")
"C:\Program Files (x86)\ZennoLab\RU\ZennoPoster Standard\5.16.2.0\Progs\TasksRunner.exe" -o StartTask -names "Hollywood"
timeout /t 1
echo Set tries count to 99... (-names "Hollywood")
"C:\Program Files (x86)\ZennoLab\RU\ZennoPoster Standard\5.16.2.0\Progs\TasksRunner.exe" -o SetTries 8 -names "Hollywood"