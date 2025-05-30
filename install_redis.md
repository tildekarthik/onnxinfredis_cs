# Install Redis on Ubuntu
sudo apt-get install lsb-release curl gpg
curl -fsSL https://packages.redis.io/gpg | sudo gpg --dearmor -o /usr/share/keyrings/redis-archive-keyring.gpg
sudo chmod 644 /usr/share/keyrings/redis-archive-keyring.gpg
echo "deb [signed-by=/usr/share/keyrings/redis-archive-keyring.gpg] https://packages.redis.io/deb $(lsb_release -cs) main" | sudo tee /etc/apt/sources.list.d/redis.list
sudo apt-get update
sudo apt-get install redis

# Enable redis to start on boot
sudo systemctl enable redis-server
sudo systemctl start redis-server

# Change firewall settings first for port 6379
then add the following to the redis.conf file at /etc/redis/redis.conf
bind 0.0.0.0
# Set in the redis.conf file or run the following command in the redis-cli
CONFIG SET protected-mode no

Add to enable password authentication

requirepass xieWozu0 


# Restart the redis server
sudo systemctl restart redis-server

# Check server
redis-cli -a xieWozu0 -h 172.232.106.190

type PING and get PONG