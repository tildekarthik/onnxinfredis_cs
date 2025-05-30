dotnet publish -r linux-x64 -c Release --self-contained
aws s3 sync bin/Release/net9.0/linux-x64/publish/ s3://aicueapps/server/onxinfredis_cs/publish/
ssh karthik@$1 'cd ~/onnxinfredis_cs;git fetch --all && git reset --hard origin/main'
ssh karthik@$1 'mkdir -p ~/onnxinfredis_cs/bin/Release/net9.0/linux-x64/publish'
ssh karthik@$1 'sudo systemctl stop onnxinfredis_cs@1.service'
ssh karthik@$1 'sudo systemctl stop onnxinfredis_cs@2.service'
ssh karthik@$1 'aws s3 sync s3://aicueapps/server/onxinfredis_cs/publish/ ~/onnxinfredis_cs/bin/Release/net9.0/linux-x64/publish/'
scp .env karthik@$1:/home/karthik/onnxinfredis_cs/bin/Release/net9.0/linux-x64/publish/
# scp -r bin/Release/net9.0/linux-x64/publish/* karthik@$1:/home/karthik/onnxinfredis_cs/bin/Release/net9.0/linux-x64/publish/;
ssh karthik@$1 'cd ~/onnxinfredis_cs/bin/Release/net9.0/linux-x64/publish;chmod +x onnxinfredis_cs;'
ssh karthik@$1 'sudo systemctl start onnxinfredis_cs@1.service'
ssh karthik@$1 'sudo systemctl start onnxinfredis_cs@2.service'
