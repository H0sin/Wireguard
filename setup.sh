#!/bin/bash

# متوقف کردن اسکریپت در صورت بروز خطا
set -e

# متغیرها
REPO_URL="https://github.com/username/repository.git" # لینک مخزن خصوصی گیت‌هاب
PROJECT_DIR="wireguard-project" # نام پوشه پروژه پس از کلون کردن

# درخواست توکن گیت‌هاب از کاربر
read -p "لطفاً توکن گیت‌هاب خود را وارد کنید: " GITHUB_TOKEN

# بررسی نصب Docker
echo "بررسی نصب Docker..."
if ! [ -x "$(command -v docker)" ]; then
  echo "Docker یافت نشد. در حال نصب Docker..."
  sudo apt update
  sudo apt install -y docker.io
else
  echo "Docker از قبل نصب شده است."
fi

# بررسی نصب Docker Compose
echo "بررسی نصب Docker Compose..."
if ! [ -x "$(command -v docker compose)" ]; then
  echo "Docker Compose یافت نشد. در حال نصب Docker Compose..."
  sudo apt update
  sudo apt install -y docker-compose-plugin
else
  echo "Docker Compose از قبل نصب شده است."
fi

# کلون کردن پروژه از گیت‌هاب
if [ ! -d "$PROJECT_DIR" ]; then
  echo "کلون کردن پروژه از مخزن خصوصی گیت‌هاب..."
  git clone https://$GITHUB_TOKEN@github.com/username/repository.git $PROJECT_DIR
else
  echo "پروژه از قبل وجود دارد."
fi

# ورود به پوشه پروژه
cd $PROJECT_DIR

# شناسایی آی‌پی سرور
echo "شناسایی آی‌پی سرور..."
SERVER_IP=$(hostname -I | awk '{print $1}')
echo "آی‌پی سرور: $SERVER_IP"

# جایگزینی آی‌پی در فایل‌های تنظیمات
echo "جایگزینی آی‌پی سرور در فایل‌های تنظیمات..."
sed -i "s/__SERVER_IP__/$SERVER_IP/g" Src/Services/Api/Wireguard.Api/appsettings.json
sed -i "s/__SERVER_IP__/$SERVER_IP/g" Src/Services/Api/Wireguard.Api/appsettings.Development.json
sed -i "s/__SERVER_IP__/$SERVER_IP/g" docker-compose.override.yml

# ساخت تصاویر و بالا آوردن کانتینرها
echo "ساخت تصاویر و بالا آوردن کانتینرها با Docker Compose..."
docker compose -f docker-compose.yml -f docker-compose.override.yml up --build -d

echo "پروژه با موفقیت اجرا شد!"
echo "برای مشاهده وضعیت کانتینرها، از دستور زیر استفاده کنید:"
echo "docker compose ps"
