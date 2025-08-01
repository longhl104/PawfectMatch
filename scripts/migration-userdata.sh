#!/bin/bash

# User data script to set up the migration instance
# This runs when the EC2 instance boots up

#!/bin/bash
yum update -y

# Install .NET 9
rpm --import https://packages.microsoft.com/keys/microsoft.asc
wget -O /etc/yum.repos.d/microsoft-prod.repo https://packages.microsoft.com/config/rhel/9/prod.repo
yum install -y dotnet-sdk-9.0

# Install git
yum install -y git

# Create user directory and clone repository
cd /home/ec2-user
git clone https://github.com/longhl104/PawfectMatch.git
chown -R ec2-user:ec2-user /home/ec2-user/PawfectMatch

# Install EF Core tools as ec2-user
sudo -u ec2-user bash << 'EOF'
export HOME=/home/ec2-user
cd /home/ec2-user
dotnet tool install --global dotnet-ef
echo 'export PATH="$PATH:/home/ec2-user/.dotnet/tools"' >> ~/.bashrc
EOF

# Build the project
cd /home/ec2-user/PawfectMatch/ShelterHub/Longhl104.ShelterHub
sudo -u ec2-user dotnet restore
sudo -u ec2-user dotnet build

# Signal that setup is complete
echo "Migration instance setup complete" > /home/ec2-user/setup-complete.txt
chown ec2-user:ec2-user /home/ec2-user/setup-complete.txt
