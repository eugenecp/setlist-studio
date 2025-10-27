# Musician Onboarding Guide 🎵

Welcome to Setlist Studio - the professional setlist management app built by musicians, for musicians.

## 🎯 Why Setlist Studio?

**Built for Real Performers**: Whether you're a solo acoustic artist, part of a rock band, or a wedding DJ, Setlist Studio adapts to your workflow.

### What Makes Us Different
- **Performance-First Design**: Built for backstage environments with poor lighting and quick access needs
- **Offline Reliability**: Critical features work without internet during performances
- **Musician-Friendly**: Uses authentic musical terminology (BPM ranges 40-250, standard keys, real genres)
- **Professional Output**: Export formats suitable for venues, sound engineers, and collaborators

## 🚀 Get Started in Under 5 Minutes

### For Individual Musicians

**Option A: Try It Right Now (No Setup)**
```bash
git clone https://github.com/eugenecp/setlist-studio.git
cd setlist-studio
docker-compose up -d
```
Visit [http://localhost:5000](http://localhost:5000) - You're ready to rock! 🎸

**Option B: Personal Cloud Deployment**
1. Deploy to your preferred cloud provider
2. Set up OAuth with Google/Microsoft/Facebook for secure login
3. Invite bandmates with secure user accounts

### For Bands & Music Organizations

**Professional Setup (Recommended)**
```bash
# Clone and configure for your band
git clone https://github.com/eugenecp/setlist-studio.git
cd setlist-studio

# Set up band-specific configuration
cp .env.example .env
# Edit .env with your band's OAuth credentials

# Deploy with PostgreSQL for better multi-user performance
docker-compose -f docker-compose.postgresql.yml up -d
```

## 🎼 Your First 15 Minutes

### Step 1: Add Your Songs (5 minutes)
```
1. Click "Songs" → "Add New Song"
2. Fill in the basics:
   - Title: "Sweet Child O' Mine"
   - Artist: "Guns N' Roses" 
   - BPM: 125
   - Key: D Major
   - Genre: Classic Rock
   - Duration: 5:03
3. Add Performance Notes: "Guitar solo at 2:45, crowd singalong on chorus"
4. Set Difficulty: 3/5 (Medium)
5. Add Tags: "crowd-pleaser", "guitar-heavy", "long-song"
```

### Step 2: Create Your First Setlist (5 minutes)
```
1. Click "Setlists" → "Create New"
2. Name it: "Rock Night at The Venue - Oct 27, 2025"
3. Set Performance Details:
   - Venue: "The Local Music Venue"
   - Date: October 27, 2025
   - Expected Duration: 90 minutes
4. Drag songs from your library
5. Reorder by dragging (or use keyboard navigation)
6. Add transition notes between songs
```

### Step 3: Performance Planning (5 minutes)
```
1. Mark opener: High-energy crowd pleaser
2. Set encore songs: Fan favorites
3. Add backup songs: In case you finish early
4. Export for sharing: PDF for sound engineer, JSON for backup
5. Test offline access: Turn off internet, verify it still works
```

## 🎯 Real-World Scenarios

### Wedding Gig Setup
```yaml
Setlist: "Johnson Wedding - October 2025"
Venue: "Garden Pavilion"
Duration: "3 hours (dinner + dancing)"

Songs by Phase:
  Dinner (60 min): Jazz standards, acoustic covers
  First Dance: "At Last" - Etta James
  Dancing (90 min): Mix of decades, high-energy
  Last Call: "Don't Stop Believin'" - Journey
  
Notes: 
  - Sound check at 4 PM
  - Microphone needed for first dance
  - Volume restrictions after 11 PM
```

### Rock Concert Template
```yaml
Setlist: "Opening for [Headliner] - Tour 2025"
Venue: "TBD (Tour venues)"
Duration: "45 minutes (opening slot)"

Structure:
  Opener: High-energy, hook audience immediately
  Middle: Mix of originals and covers
  Closer: Biggest crowd-pleaser, leave them wanting more
  
Encore Policy: "No encore for opening slots"
Backup Songs: "If crowd is really into it, extend with..."
```

### Jazz Club Night
```yaml
Setlist: "Jazz Standards Evening"
Venue: "Blue Note Club"
Duration: "2 sets × 45 minutes"

Set 1: Warm-up standards
  - Easy listening, establish vibe
  - "Take Five", "Blue Moon", "Autumn Leaves"
  
Set 2: Feature pieces
  - Showcase solos, complex arrangements
  - "Giant Steps", "All The Things You Are"
  
Notes: "15-minute intermission, piano tuning during break"
```

## 📱 Performance Day Workflow

### Before the Gig
- [ ] Download offline backup of setlist
- [ ] Share setlist with band via export/email
- [ ] Print backup copy for sound engineer
- [ ] Load app on phone/tablet for stage reference

### During Performance
- [ ] Use large-button mobile interface
- [ ] Quick song lookup if needed
- [ ] Mark completed songs as you go
- [ ] Adjust on-the-fly if crowd requests

### After the Show
- [ ] Mark actual songs played
- [ ] Add notes about crowd favorites
- [ ] Save as template for similar venues
- [ ] Update song ratings based on performance

## 🎸 Pro Tips from Musicians

### Song Organization
```
✅ DO: Use consistent naming
   - "Hotel California - Eagles"
   - "Wonderwall - Oasis"
   
❌ DON'T: Inconsistent formats
   - "eagles hotel california"
   - "WONDERWALL by Oasis"
```

### BPM Guidelines
```
Slow Ballads: 60-80 BPM
  - "The Night We Met", "Hallelujah"
  
Medium Tempo: 90-120 BPM  
  - "Thinking Out Loud", "Perfect"
  
Up-tempo: 130-160 BPM
  - "Uptown Funk", "Can't Stop the Feeling"
  
Fast/Dance: 170+ BPM
  - "I Gotta Feeling", "Shut Up and Dance"
```

### Key Selection for Covers
```
Guitar-Friendly Keys: E, A, D, G, C
  - Open chords, easier fingering
  
Vocal-Friendly Keys: F, Bb, Eb, Ab  
  - Better for most vocal ranges
  
Minor Mood: Am, Em, Bm, F#m, Dm
  - Emotional ballads, introspective songs
```

## 🚨 Troubleshooting Common Issues

### "I Can't Access My Setlists"
**Solution**: Check you're logged in with the same OAuth account (Google/Microsoft/Facebook) you used to create them.

### "App Won't Load During Performance"
**Solution**: Use offline mode - download your setlists before the gig when you have good internet.

### "Sharing Setlists with Band"
**Solutions**: 
- Export as PDF for easy sharing
- Email JSON backup for importing 
- Use same OAuth login for all band members

### "Moving from Paper Setlists"
**Migration Tip**: Start with one gig at a time. Don't try to digitize your entire catalog immediately.

## 🔗 Next Steps

### Growing Your Setup
1. **[Load Balancing Guide](load-balancing-guide.md)** - Scale for larger bands/organizations
2. **[PostgreSQL Migration](PostgreSQL-Migration-Guide.md)** - Upgrade from SQLite for better performance
3. **[Security Enhancements](security-enhancements.md)** - Professional-grade security setup

### Advanced Features
1. **Collaboration**: Multiple band members managing the same setlists
2. **Analytics**: Track which songs work best at different venues
3. **Integration**: Connect with other music software
4. **Mobile App**: Native iOS/Android apps (coming soon)

## 💬 Community & Support

### Get Help
- **Quick Questions**: [GitHub Discussions](https://github.com/eugenecp/setlist-studio/discussions)
- **Bug Reports**: [GitHub Issues](https://github.com/eugenecp/setlist-studio/issues)
- **Feature Requests**: Join the community discussion

### Share Your Setup
- Show us your creative setlist organization
- Share templates that work for your genre
- Help other musicians get started

---

**Ready to revolutionize your performances?** 🎤

*Made with ❤️ by musicians, for musicians. Let's make great music together!*