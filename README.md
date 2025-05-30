# Redis queued inference jobs

The new system is based on Redis and Python for inference.
Moving to onnx inference for models for 3 benefits:
1. Speed on CPU
2. Common pipeline for all models including tensorflow
3. MIT licence unlike the new YOLO AGPL licence

# Add to reboot for the workers using the start_yoloinfredis.sh script
```
@reboot /usr/bin/bash /home/karthik/yoloinfredis_python/start_yoloinfredis.sh


# Ensure swap is added to the system
https://www.digitalocean.com/community/tutorials/how-to-add-swap-space-on-ubuntu-20-04
follow this including setting the swappiness to prevent undue usage on server



# uv and other installs
uv sync
sudo apt install libgl1

# !!!! Change the redis host in the .env file 


ONNX Conversion Parameters

    model_name = "best.pt"
    imgsz = 1920
    device = "cpu"
    nms = True
    simplify = True
    opset=16


# Systemd

Create the file in  /etc/systemd/system/onnxinfredis_cs@.service from the service file given in the repo




sudo systemctl daemon-reload

sudo systemctl enable --now onnxinfredis_cs@1.service
sudo systemctl enable --now onnxinfredis_cs@2.service

systemctl status onnxinfredis_cs@*.service

sudo journalctl -u onnxinfredis_cs@1.service -f
