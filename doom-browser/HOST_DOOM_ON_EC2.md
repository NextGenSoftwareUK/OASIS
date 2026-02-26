# Host Doom on Existing EC2 (Option 1)

Run doom-browser on the same EC2 as OASIS/STAR. Traffic flows: **Portal → CloudFront → ALB → nginx → doom-browser:8765**.

---

## Prerequisites

- SSH access to EC2 (54.146.197.32 or your instance)
- Node.js 18+ on EC2 (install if missing)
- doom-browser code on EC2

---

## Step 1: SSH into EC2

```bash
ssh -i /path/to/your-key.pem ec2-user@54.146.197.32
# Or: ssh -i /path/to/your-key.pem ubuntu@54.146.197.32
```

---

## Step 2: Install Node.js (if needed)

```bash
# Check if Node is installed
node -v

# If not, install (Amazon Linux 2):
curl -fsSL https://rpm.nodesource.com/setup_20.x | sudo bash -
sudo yum install -y nodejs

# Or on Ubuntu:
# curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
# sudo apt-get install -y nodejs
```

---

## Step 3: Copy doom-browser to EC2

From your **local machine** (not EC2):

```bash
cd /Users/maxgershfield/OASIS_CLEAN
scp -i /path/to/your-key.pem -r doom-browser ec2-user@54.146.197.32:~/
```

Or clone from your repo if it's in git:

```bash
# On EC2
cd ~
git clone <your-repo-url> OASIS_CLEAN
cd OASIS_CLEAN/doom-browser
```

---

## Step 4: Install dependencies and test

On EC2:

```bash
cd ~/doom-browser   # or ~/OASIS_CLEAN/doom-browser
npm install
PORT=8765 npm start
```

Leave this running in one terminal. In another terminal or from your browser, test locally on the EC2:

```bash
curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:8765/
# Should return 200
```

Press Ctrl+C to stop. Next we'll make it run persistently.

---

## Step 5: Run doom-browser with PM2 (persistent)

Install PM2 and start doom-browser:

```bash
sudo npm install -g pm2
cd ~/doom-browser
PORT=8765 pm2 start server.js --name doom-browser
pm2 save
pm2 startup   # Follow the command it prints to enable on boot
```

---

## Step 6: Add nginx proxy for /games/doom

On EC2, edit your nginx config. The exact file may vary; common paths:

- `/etc/nginx/sites-available/starapi`
- `/etc/nginx/conf.d/starapi.conf`

**Reference:** The repo has an updated config at `docker/starapi-oasisweb4-one-80.conf` with the Doom blocks. You can copy the relevant blocks or replace your config.

Add these blocks **before** the `location /` block in **both** the `listen 80` and `listen 443` server blocks:

```nginx
    # DOOM game at /games/doom
    location = /games/doom {
        return 301 /games/doom/;
    }
    location /games/doom/ {
        proxy_pass http://127.0.0.1:8765/;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 300;
        proxy_connect_timeout 300;
        proxy_send_timeout 300;
    }
```

Then:

```bash
sudo nginx -t
sudo systemctl reload nginx
```

---

## Step 7: Rebuild and redeploy the portal

The portal code now auto-detects oasisweb4.one and uses `https://oasisweb4.one/games/doom`. Rebuild and upload:

```bash
cd /Users/maxgershfield/OASIS_CLEAN/oportal-repo
npm run build
aws s3 sync dist/ s3://oasisweb4-one-portal-1771789414/portal/ --delete --region us-east-1
# Update the /portal object with base tag (see DEPLOY_PORTAL_OAISWEB4_ONE_STEP_BY_STEP.md Step 5)
aws cloudfront create-invalidation --distribution-id E1A7FEKEUMFPTI --paths "/portal" "/portal/*"
```

---

## Step 8: Verify

1. Visit **https://oasisweb4.one/portal**
2. Go to **Games** tab
3. Click **Play** on Doom
4. You should see the Doom launcher at **https://oasisweb4.one/games/doom/**

---

## Troubleshooting

| Issue | Check |
|-------|-------|
| 502 Bad Gateway | `pm2 status` – is doom-browser running? `curl http://127.0.0.1:8765/` |
| 404 | nginx config – did you add the location blocks? `sudo nginx -t` |
| Assets not loading | Ensure URL has trailing slash: `/games/doom/` |
| Portal still opens localhost | Clear cache; ensure `GAMES_DOOM_BASE_URL` is set and portal is redeployed |
