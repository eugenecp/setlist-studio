# Offline Performance Testing Guide

**For Musicians Using Setlist Studio During Live Performances**

## Quick Offline Test (2 minutes)

### Before Your Performance
1. **Connect to Wi-Fi** and open Setlist Studio
2. **Visit Your Setlists** - Navigate to `/setlists` to load and cache them
3. **Cache Important Setlists** - Click "Cache for Offline" on each performance setlist
4. **Test Offline Mode** - Turn off Wi-Fi or enable airplane mode
5. **Verify Access** - Ensure you can still view your cached setlists

### Performance Mode Indicators
- **Orange Badge**: "🎵 Performance Mode" appears when offline
- **Connection Status**: Shows "Performance Mode Active" with cached data info
- **Cached Content**: Look for 📱 icons indicating offline availability

## Comprehensive Offline Testing Scenarios

### Scenario 1: Complete Internet Outage
**Situation**: Venue has no internet or Wi-Fi fails during performance

**Test Steps**:
1. Disconnect from all networks (airplane mode)
2. Open Setlist Studio
3. Navigate to `/setlists`
4. Verify you can see previously cached setlists
5. Click on individual setlists to view song details
6. Test search functionality with cached data

**Expected Results**:
- Orange "Performance Mode" badge visible
- Cached setlists display normally
- Search works on cached data
- Smooth navigation between setlists

### Scenario 2: Intermittent Connection
**Situation**: Venue has unreliable Wi-Fi that drops frequently

**Test Steps**:
1. Cache setlists while connected
2. Alternate between online/offline every 30 seconds
3. Navigate between different pages
4. Test search and setlist viewing during connection drops

**Expected Results**:
- Seamless transition between online/offline modes
- No data loss during connection drops
- Connection status updates automatically
- Cached data available immediately when offline

### Scenario 3: Limited Bandwidth
**Situation**: Venue has slow internet that makes loading difficult

**Test Steps**:
1. Use network throttling (slow 3G simulation)
2. Navigate to setlists page
3. Observe cache loading vs network loading
4. Test responsiveness during slow connections

**Expected Results**:
- Cached content loads instantly
- Network requests timeout gracefully
- App remains responsive
- Fallback to cache when network is too slow

### Scenario 4: First-Time Offline Use
**Situation**: Musician forgets to cache setlists before going offline

**Test Steps**:
1. Go offline without caching any content
2. Navigate to `/setlists`
3. Observe empty state messaging
4. Go back online and cache content
5. Test offline again

**Expected Results**:
- Clear messaging about Performance Mode with no cached data
- Helpful instructions on how to cache for next time
- Graceful handling of empty cache state
- Easy process to cache content once online

## Mobile Device Testing

### Touch Interface Tests
- **Tap Responsiveness**: All buttons work reliably with touch
- **Scroll Performance**: Smooth scrolling through setlist grids
- **Zoom Compatibility**: Content remains accessible when zoomed
- **Screen Rotation**: Layout adapts properly to portrait/landscape

### Battery Impact Tests
- **Service Worker Efficiency**: Monitor battery usage during offline mode
- **Background Processing**: Minimal impact when app is backgrounded
- **Cache Size**: Reasonable storage usage for cached content

### Performance Metrics
- **Load Times**: Cached setlists load in <100ms
- **Memory Usage**: App maintains responsive performance
- **Storage Efficiency**: Cache uses reasonable device storage

## Real-World Performance Scenarios

### Pre-Show Preparation (15 minutes before show)
1. **Connection Check**: Verify venue Wi-Fi or use mobile data
2. **Cache Critical Setlists**: Download main setlist and 1-2 backup setlists
3. **Test Offline Mode**: Briefly go offline to verify cache works
4. **Battery Check**: Ensure device has sufficient charge

### During Performance (Live Testing)
1. **Quick Access Test**: Navigate to setlist in <5 seconds
2. **Song Navigation**: Smoothly move between songs in setlist
3. **Search Test**: Find specific songs quickly using cached search
4. **Backup Plan**: Have printed setlist as ultimate fallback

### Post-Show Validation
1. **Sync Check**: When connection returns, verify data syncs properly
2. **Cache Management**: Clear old cached data if needed
3. **Performance Review**: Note any issues for improvement

## Troubleshooting Common Issues

### Cache Not Working
- **Check Browser**: Ensure modern browser with Service Worker support
- **Storage Space**: Verify device has available storage
- **Clear Cache**: Try clearing browser cache and re-caching content

### Performance Mode Not Activating
- **Connection Status**: Manually check `navigator.onLine` in browser console
- **Service Worker**: Verify Service Worker is registered in dev tools
- **Page Refresh**: Try refreshing the page to re-initialize

### Slow Cache Loading
- **Content Size**: Large setlists may take longer to cache
- **Network Speed**: Initial caching depends on internet connection
- **Background Processing**: Allow time for caching to complete

## Success Criteria

✅ **Cached setlists accessible within 3 seconds when offline**  
✅ **Search functionality works with cached data**  
✅ **Clear visual indicators of offline mode**  
✅ **Smooth transition between online/offline states**  
✅ **No data loss during connection interruptions**  
✅ **Responsive touch interface on mobile devices**  
✅ **Reasonable battery impact during extended use**  
✅ **Graceful handling of uncached content**

## Emergency Fallback Plan

If offline mode fails during a performance:
1. **Use Mobile Data**: Switch to cellular if available
2. **Printed Backup**: Always have a printed setlist as final fallback
3. **Memory Performance**: Rely on memorized song order as last resort
4. **Improvisation**: Be prepared to adjust setlist based on audience

---

**Remember**: The show must go on! Technology enhances performance but should never be a single point of failure. Always have backup plans for critical performances.

**Support**: If offline mode isn't working as expected, report issues with specific device, browser, and scenario details for rapid improvement.