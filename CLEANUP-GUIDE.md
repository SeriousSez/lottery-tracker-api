# Clean Git History - Step by Step Guide

## ⚠️ CRITICAL FIRST STEP
**Change your database password immediately before proceeding!**
- Host: mysql18.unoeuro.com
- Database: sezginsahin_dk_db_tys
- Username: sezginsahin_dk
- Old Password: Opel4500 (CHANGE THIS NOW!)

## Method: Using git filter-branch (Windows PowerShell)

Run these commands in order:

### Step 1: Backup your repository
```powershell
cd c:\Repos
Copy-Item -Path "LotteryTracker.API" -Destination "LotteryTracker.API.backup" -Recurse
cd LotteryTracker.API
```

### Step 2: Commit current changes (security files)
```powershell
git add .gitignore SECURITY-SETUP.md appsettings.Example.json
git add README.md
git commit -m "Add security configuration and gitignore"
```

### Step 3: Remove sensitive data from Git history
```powershell
git filter-branch --force --index-filter "git rm --cached --ignore-unmatch appsettings.json" --prune-empty --tag-name-filter cat -- --all
```

### Step 4: Re-add the cleaned appsettings.json
```powershell
git add appsettings.json
git commit -m "Add appsettings template without credentials"
```

### Step 5: Clean up Git references
```powershell
git for-each-ref --format="delete %(refname)" refs/original | git update-ref --stdin
git reflog expire --expire=now --all
git gc --prune=now --aggressive
```

### Step 6: Force push to remote (⚠️ This rewrites history!)
```powershell
# Check your remote first
git remote -v

# Force push
git push origin --force --all
git push origin --force --tags
```

### Step 7: Set up User Secrets for local development
```powershell
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=mysql18.unoeuro.com;Port=3306;Database=sezginsahin_dk_db_tys;Uid=sezginsahin_dk;Pwd=YOUR_NEW_PASSWORD_HERE;"
```

### Step 8: Verify it works
```powershell
dotnet run
```

## ⚠️ Important Notes

1. **All team members must re-clone** the repository after you force push
2. **Any existing forks** will still have the old history
3. **GitHub/GitLab cached views** may show old commits for a while
4. **Consider making the repo private** if it's currently public

## If Something Goes Wrong

You have a backup at `c:\Repos\LotteryTracker.API.backup`

To restore:
```powershell
cd c:\Repos
Remove-Item -Path "LotteryTracker.API" -Recurse -Force
Copy-Item -Path "LotteryTracker.API.backup" -Destination "LotteryTracker.API" -Recurse
```

## Alternative: Start Fresh Repository

If you prefer to start with a clean slate:

1. Create a new repository on GitHub/GitLab
2. Remove the old remote: `git remote remove origin`
3. Add new remote: `git remote add origin <new-repo-url>`
4. Make this your first commit: `git push -u origin master`

This ensures NO trace of credentials in history.
