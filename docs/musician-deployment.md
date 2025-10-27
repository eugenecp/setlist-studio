# Musician Deployment Guide 🚀

## Get Your Band Online - Choose Your Setup

Whether you're a solo artist or managing a full band, we have deployment options that match your technical comfort level and budget.

## 🎯 Quick Decision Guide

### Solo Artist / Small Band (2-5 people)
**Recommendation**: Local Docker setup or simple cloud deployment
**Budget**: Free - $20/month
**Technical Level**: Beginner

### Professional Band (5-15 members)  
**Recommendation**: Cloud deployment with PostgreSQL
**Budget**: $50-100/month
**Technical Level**: Intermediate

### Music Organization / Multi-Band Management
**Recommendation**: Load-balanced cloud deployment  
**Budget**: $100-300/month
**Technical Level**: Advanced

---

## 🏠 Local Setup (Free)

### Perfect For:
- Personal use
- Small bands practicing together  
- Testing before cloud deployment
- Venues with poor internet

### One-Command Setup
```bash
# Clone and run (works on Windows, Mac, Linux)
git clone https://github.com/eugenecp/setlist-studio.git
cd setlist-studio
docker-compose up -d

# Access at http://localhost:5000
```

### What You Get:
- ✅ Full Setlist Studio functionality
- ✅ SQLite database (handles 100+ songs easily)
- ✅ Offline capability 
- ✅ No monthly costs
- ❌ Only accessible on your computer
- ❌ No remote access for band members

---

## ☁️ Cloud Deployment Options

### Option 1: Basic Cloud Setup ($10-20/month)

**Best For**: Small bands wanting remote access

**Providers**: DigitalOcean, Linode, AWS Lightsail

```bash
# On your cloud server
git clone https://github.com/eugenecp/setlist-studio.git
cd setlist-studio

# Set up for internet access
cp .env.example .env
# Edit .env with your domain and OAuth secrets

# Deploy with SSL
docker-compose -f docker-compose.prod.yml up -d
```

**What You Get**:
- ✅ Access from anywhere
- ✅ Band member collaboration
- ✅ Automatic backups
- ✅ Professional SSL certificates
- ⚠️ Requires basic server management

### Option 2: Managed Platform ($0-50/month)

**Best For**: Musicians who want zero server management

**Providers**: Railway, Render, Fly.io, Heroku

**Railway Deployment** (Recommended):
```bash
# Install Railway CLI
npm install -g @railway/cli

# Deploy directly from GitHub
railway login
railway project new
railway service add --github eugenecp/setlist-studio
railway up
```

**What You Get**:
- ✅ Zero server management
- ✅ Automatic scaling
- ✅ Built-in SSL certificates
- ✅ Database backups included
- ✅ Free tier available

---

## 🎸 Band Collaboration Setup

### Step 1: Choose Authentication Method

**Option A: Google OAuth (Recommended)**
```bash
# Best for bands already using Gmail/Google Drive
# 1. Go to Google Cloud Console
# 2. Create project: "YourBand Setlist Studio"
# 3. Enable Google+ API
# 4. Create OAuth credentials
# 5. Add your domain to authorized URIs
```

**Option B: Microsoft OAuth**
```bash
# Best for bands using Office 365
# 1. Go to Azure Portal  
# 2. Register app: "YourBand Setlists"
# 3. Add redirect URIs for your domain
# 4. Generate client secret
```

**Option C: Facebook OAuth**
```bash
# Best for social media focused bands
# 1. Go to Facebook Developers
# 2. Create app for your band
# 3. Set up Facebook Login
# 4. Configure OAuth redirect URIs
```

### Step 2: Set Up User Roles

```yaml
Band Leader (Admin):
  - Create and edit all setlists
  - Manage band member access
  - Export setlists for venues
  - Delete songs and setlists

Band Member (Editor):  
  - Create personal setlists
  - Edit shared band setlists
  - Add songs to library
  - Comment on setlists

Sound Engineer (Viewer):
  - View setlists and technical notes
  - Export technical requirements
  - Cannot edit song library

Venue Manager (Viewer):
  - View performance schedule
  - See timing requirements  
  - Cannot access song details
```

---

## 💎 Professional Setup (PostgreSQL)

### When to Upgrade from SQLite:
- More than 10 active users
- Database file approaching 50MB
- Need for concurrent editing
- Professional backup requirements

### Migration Process:
```bash
# 1. Export your current data
cd setlist-studio
./scripts/export-data.sh

# 2. Deploy PostgreSQL version
docker-compose -f docker-compose.postgresql.yml up -d

# 3. Import your data
./scripts/import-data.sh your-backup-file.json

# 4. Verify everything works
# 5. Update DNS to point to new deployment
```

**PostgreSQL Benefits**:
- ✅ Supports 100+ concurrent users
- ✅ Professional backup and recovery
- ✅ Better performance for large libraries
- ✅ Advanced analytics capabilities
- ⚠️ Requires more server resources ($50+/month)

---

## 🏗️ Architecture Examples

### Solo Artist Setup
```yaml
Architecture: Single Docker Container
Cost: $10/month (DigitalOcean Droplet)
Users: 1 artist + occasional collaborators
Database: SQLite (sufficient for personal use)
Backup: Weekly automated database exports
```

### Wedding Band Setup  
```yaml
Architecture: Docker Compose + PostgreSQL
Cost: $50/month (Managed database + app server)
Users: 5 band members + sound engineer access
Database: PostgreSQL (handles concurrent editing)
Features: OAuth login, role-based access, PDF exports
Backup: Daily database backups + file storage
```

### Music Agency Setup
```yaml
Architecture: Load Balanced + Redis Cache
Cost: $200/month (Multiple servers + managed services)  
Users: 50+ musicians across multiple bands
Database: PostgreSQL with read replicas
Features: Multi-tenant, advanced analytics, API access
Backup: Real-time replication + disaster recovery
```

---

## 🔧 Maintenance & Updates

### Automated Updates (Recommended)
```bash
# Set up automatic updates (runs weekly)
crontab -e

# Add this line for Sunday 3 AM updates:
0 3 * * 0 cd /path/to/setlist-studio && git pull && docker-compose up -d --build
```

### Manual Updates
```bash
# Monthly update process
cd setlist-studio
git pull origin main
docker-compose down
docker-compose up -d --build

# Verify everything works
curl http://your-domain.com/health
```

### Backup Strategy
```yaml
Critical Data to Backup:
  Database: All songs, setlists, user accounts
  Configuration: OAuth secrets, environment variables
  User Uploads: Audio files, images (if added)

Backup Schedule:
  Daily: Database dump to cloud storage
  Weekly: Full system backup 
  Monthly: Test disaster recovery process

Recovery Testing:
  Quarterly: Restore from backup to test environment
  Annually: Full disaster recovery drill
```

---

## 🚨 Troubleshooting Common Issues

### "Can't Access from Phone/Tablet"
**Problem**: Firewall blocking external access
**Solution**: Configure cloud server firewall to allow ports 80 and 443

### "OAuth Login Not Working"
**Problem**: Redirect URIs don't match deployment URL
**Solution**: Update OAuth app settings with your actual domain

### "Setlists Load Slowly"
**Problem**: SQLite performance with large libraries
**Solution**: Migrate to PostgreSQL or add database indexes

### "Lost Connection During Performance"
**Problem**: Venue internet is unreliable
**Solution**: Enable offline mode before the show starts

### "Band Members Can't Edit"
**Problem**: Incorrect user role assignments  
**Solution**: Check user permissions in admin panel

---

## 📈 Scaling Your Setup

### From Local to Cloud
```bash
# 1. Export local data
./scripts/backup-local.sh

# 2. Deploy cloud version  
# 3. Import data to cloud
# 4. Test with band
# 5. Switch everyone to cloud URL
```

### From Basic to Professional
```bash
# 1. Set up PostgreSQL database
# 2. Configure load balancer
# 3. Add Redis caching
# 4. Migrate data
# 5. Update DNS gradually
```

### Multi-Band Organizations
```bash
# 1. Enable multi-tenant features
# 2. Set up organization hierarchy
# 3. Configure advanced user roles
# 4. Add analytics and reporting
# 5. Implement API access for integrations
```

---

## 💡 Pro Tips from Band Leaders

### Start Small, Scale Smart
*"We started with the local Docker setup for rehearsals, then moved to cloud when we got our first touring gigs. The data export/import made the transition seamless."* - Rock Band Manager

### OAuth Makes Everything Easier
*"Setting up Google OAuth took 10 minutes and eliminated all password management headaches. Band members just click 'Sign in with Google' and they're in."* - Jazz Ensemble Leader

### Offline Mode Saves Shows
*"During a festival gig, the venue's WiFi died. Because we had offline mode enabled, we could still access our setlists and saved the show."* - Folk Duo

### Backup Your Backup
*"We learned the hard way - always test your backups. We do a monthly restore test to make sure everything works."* - Wedding Band Leader

---

**Ready to deploy your band's Setlist Studio?** 🎵

*Choose the setup that matches your needs and budget. You can always start simple and scale up as your band grows!*