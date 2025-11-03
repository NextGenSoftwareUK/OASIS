# Android Studio Setup Guide for Mac

**Date:** October 20, 2025  
**Your System:** macOS Monterey 12.6.0  
**Status:** 🔵 Not Installed - Ready to Install

---

## 📥 Download & Install (Just Opened in Your Browser)

I've opened the Android Studio download page for you. Here's what to do:

### Step 1: Download
1. The page should show: **"Download Android Studio Hedgehog"** (or newer)
2. Click the big green **"Download Android Studio"** button
3. Accept the terms and conditions
4. The `.dmg` file will download (~1.1 GB)
   - Location: Your Downloads folder
   - Name: `android-studio-{version}-mac.dmg`

### Step 2: Install
1. **Open the downloaded `.dmg` file**
2. **Drag "Android Studio"** icon to the Applications folder
3. **Eject the disk image** (right-click the mounted disk and select Eject)

### Step 3: First Launch
1. Open **Finder → Applications**
2. Find **Android Studio** and double-click
3. macOS will ask: *"Android Studio is an app downloaded from the internet. Are you sure you want to open it?"*
   - Click **"Open"**

### Step 4: Setup Wizard
Android Studio will launch a setup wizard:

1. **Import Settings?**
   - Choose: **"Do not import settings"** (first time install)
   - Click OK

2. **Data Sharing**
   - Choose your preference (I recommend "Don't send")
   - Click Next

3. **Install Type**
   - Choose: **"Standard"**
   - Click Next

4. **UI Theme**
   - Choose: **Darcula** (dark) or **Light**
   - Click Next

5. **Verify Settings**
   - Review what will be installed
   - SDK Platform: Android 11.0+
   - Android SDK Build-Tools
   - Android Emulator
   - Total download: ~2-3 GB
   - Click **"Finish"**

6. **Downloading Components** (15-30 minutes depending on internet)
   - Android Studio will download:
     - Android SDK
     - Build tools
     - Platform tools
     - Emulator
   - ☕ Get coffee - this takes a while!

### Step 5: Complete!
Once downloads finish:
- Click **"Finish"**
- Android Studio welcome screen appears
- You're ready to open the TimoRides project!

---

## 🚀 Opening TimoRides Project

1. From Android Studio welcome screen, click **"Open"**
2. Navigate to: `/Volumes/Storage 1/OASIS_CLEAN/TimoRides/CC-UberNextGen-Android`
3. Click **"Open"**
4. Wait for Gradle sync (2-5 minutes first time)
5. You're ready to build!

---

## 💾 Disk Space Requirements

- **Android Studio:** ~1.1 GB
- **Android SDK & Tools:** ~2-3 GB
- **Build Cache:** ~500 MB - 1 GB (over time)
- **Total:** ~4-5 GB

Make sure you have enough space on your Mac!

---

## ⚙️ System Requirements (Your Mac)

✅ **Operating System:** macOS 10.14+ (You have 12.6.0)  
✅ **RAM:** 8 GB minimum (16 GB recommended)  
✅ **Disk Space:** 8 GB minimum  
✅ **Screen Resolution:** 1280 x 800 minimum

Your system should handle it fine!

---

## 🐛 Common Setup Issues

### Issue 1: "Android Studio is damaged and can't be opened"
**Solution:** macOS Gatekeeper blocking the app
```bash
# Run in Terminal:
sudo xattr -cr /Applications/Android\ Studio.app
```

### Issue 2: "Unable to access Android SDK add-on list"
**Solution:** Network/proxy issue
- Close setup wizard
- Open System Preferences → Network
- Check internet connection
- Relaunch Android Studio

### Issue 3: Java JDK not found
**Solution:** Android Studio includes its own JDK, but if needed:
```bash
# Check if Java is installed
java -version

# If not, Android Studio will prompt to download
```

### Issue 4: Emulator won't start
**Solution:** Intel HAXM or hypervisor issue
- macOS already has Hypervisor.framework
- If on Apple Silicon (M1/M2), emulator runs natively
- If on Intel Mac, Android Studio will install HAXM during setup

---

## 🎯 Alternative: Build Without Android Studio

If you prefer command-line only, you can install just the SDK:

```bash
# Install Android SDK via Homebrew
brew install --cask android-sdk
brew install --cask android-platform-tools

# Set environment variables
echo 'export ANDROID_HOME=/usr/local/share/android-sdk' >> ~/.zshrc
echo 'export PATH=$PATH:$ANDROID_HOME/tools:$ANDROID_HOME/platform-tools' >> ~/.zshrc
source ~/.zshrc

# Then build with gradlew
cd "/Volumes/Storage 1/OASIS_CLEAN/TimoRides/CC-UberNextGen-Android"
./gradlew assembleDebug
```

**But I recommend Android Studio** for easier development, debugging, and UI design!

---

## 📱 After Installation - Next Steps

1. **Open TimoRides project** (see above)
2. **Connect Android device** or **create emulator**
3. **Run the app** (Shift + F10)
4. **Start customizing!** (See TIMO_ANDROID_IMPROVEMENTS.md)

---

## 🔍 Verify Installation

After setup completes, verify everything is ready:

```bash
# Check Android Studio is installed
ls -la /Applications/Android\ Studio.app

# Check SDK is installed  
ls -la ~/Library/Android/sdk

# Check platform tools
~/Library/Android/sdk/platform-tools/adb --version
```

All should exist if installation succeeded!

---

## ⏱️ Installation Timeline

- **Download:** 5-10 minutes (1.1 GB)
- **Install:** 2 minutes
- **First Launch & Setup:** 2 minutes
- **SDK Download:** 15-30 minutes (2-3 GB)
- **Total:** ~25-45 minutes

Plan accordingly!

---

## 💡 Pro Tips

1. **Don't close setup wizard** while downloading SDKs
2. **Use wired internet** for faster downloads
3. **Disable VPN** if downloads are slow
4. **Keep Android Studio updated** via Help → Check for Updates
5. **Install Emulator** during setup (useful for testing without device)

---

## 📞 Need Help?

If you encounter issues:

1. **Check macOS version:** System Preferences → About This Mac
2. **Check disk space:** Apple menu → About This Mac → Storage
3. **Check error messages** in setup wizard
4. **Google the error** - Android Studio has great community support

Common search: "Android Studio macOS [your error message]"

---

## ✅ Checklist

Use this to track your progress:

- [ ] Downloaded Android Studio .dmg file
- [ ] Installed to Applications folder
- [ ] Launched Android Studio for first time
- [ ] Completed setup wizard
- [ ] Downloaded SDK components
- [ ] Opened TimoRides project
- [ ] Gradle sync completed
- [ ] Ready to build!

---

## 🎉 Once Installed

Come back and let me know when installation is complete!

I can then help you:
1. Open the TimoRides project
2. Set up an Android emulator
3. Build and run the app
4. Start customizing for Timo branding

---

**Installation Status:** 🔄 In Progress  
**Estimated Time:** 25-45 minutes  
**Next Step:** Wait for downloads to complete



