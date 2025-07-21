#!/bin/bash

# Install QueryPush as a systemd service on Linux

SERVICE_NAME="querypush"
SERVICE_USER="querypush"
INSTALL_DIR="/opt/querypush"
SERVICE_FILE="/etc/systemd/system/${SERVICE_NAME}.service"

echo "Installing QueryPush as systemd service..."

# Create service user
sudo useradd -r -s /bin/false $SERVICE_USER

# Create install directory
sudo mkdir -p $INSTALL_DIR
sudo chown $SERVICE_USER:$SERVICE_USER $INSTALL_DIR

# Copy application files
sudo cp -r * $INSTALL_DIR/
sudo chown -R $SERVICE_USER:$SERVICE_USER $INSTALL_DIR

# Create systemd service file
sudo tee $SERVICE_FILE > /dev/null <<EOF
[Unit]
Description=QueryPush Database Query Scheduler
After=network.target

[Service]
Type=notify
ExecStart=$INSTALL_DIR/QueryPush --service
Restart=always
RestartSec=5
User=$SERVICE_USER
Group=$SERVICE_USER
WorkingDirectory=$INSTALL_DIR
SyslogIdentifier=querypush

[Install]
WantedBy=multi-user.target
EOF

# Enable and start service
sudo systemctl daemon-reload
sudo systemctl enable $SERVICE_NAME
sudo systemctl start $SERVICE_NAME

echo "QueryPush service installed and started"
echo "Use 'sudo systemctl status querypush' to check status"
