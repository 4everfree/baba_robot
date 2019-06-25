@echo off
echo Starting task... (-ids ca19d6c5-864d-4ee6-a98b-0296039100d7)
"C:\Program Files (x86)\ZennoLab\RU\ZennoPoster Pro\5.16.2.0\Progs\TasksRunner.exe" -o StartTask -names "Zelv_Poster"
timeout /t 1
echo Set tries count to 99... (-ids ca19d6c5-864d-4ee6-a98b-0296039100d7)
"C:\Program Files (x86)\ZennoLab\RU\ZennoPoster Pro\5.16.2.0\Progs\TasksRunner.exe" -o SetTries 5 -names "Zelv_Poster"
timeout /t 1
echo Set max fails count to 3... (-ids ca19d6c5-864d-4ee6-a98b-0296039100d7)
"C:\Program Files (x86)\ZennoLab\RU\ZennoPoster Pro\5.16.2.0\Progs\TasksRunner.exe" -o SetMaxFails 3 -names "Zelv_Poster"