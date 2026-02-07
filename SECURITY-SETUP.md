# Security Setup Guide

## Removing Sensitive Data from Git History

Your database credentials were committed to Git. Follow these steps to remove them:

### Option 1: Using git filter-repo (Recommended)

1. **Install git-filter-repo:**
   ```powershell
   pip install git-filter-repo
   ```

2. **Remove the sensitive file from history:**
   ```powershell
   git filter-repo --path appsettings.json --invert-paths
   ```

3. **Re-add the cleaned appsettings.json:**
   ```powershell
   git add appsettings.json
   git commit -m "Add appsettings template without credentials"
   ```

4. **Force push to remote:**
   ```powershell
   git push origin --force --all
   ```

### Option 2: Using BFG Repo-Cleaner

1. **Download BFG:** https://rtyley.github.io/bfg-repo-cleaner/

2. **Create a file with text to replace:**
   Create `passwords.txt` with your actual password on each line

3. **Run BFG:**
   ```powershell
   java -jar bfg.jar --replace-text passwords.txt
   ```

4. **Clean up and push:**
   ```powershell
   git reflog expire --expire=now --all
   git gc --prune=now --aggressive
   git push origin --force --all
   ```

### Option 3: Manual Git Filter-Branch

```powershell
git filter-branch --force --index-filter `
  "git rm --cached --ignore-unmatch appsettings.json" `
  --prune-empty --tag-name-filter cat -- --all

git push origin --force --all
```

## ⚠️ CRITICAL: After Cleaning Git History

1. **Change your database password immediately** at your MySQL host
2. **Notify your team** that they need to re-clone the repository
3. **Any forks** will still have the old history - contact fork owners

## Setting Up Secure Configuration

### Option A: User Secrets (Development)

1. **Initialize user secrets:**
   ```powershell
   dotnet user-secrets init
   ```

2. **Set your connection string:**
   ```powershell
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=mysql18.unoeuro.com;Port=3306;Database=sezginsahin_dk_db_tys;Uid=sezginsahin_dk;Pwd=YOUR_NEW_PASSWORD;"
   ```

3. **Verify:**
   ```powershell
   dotnet user-secrets list
   ```

### Option B: Environment Variables (Production)

1. **Windows (PowerShell):**
   ```powershell
   $env:ConnectionStrings__DefaultConnection="Server=mysql18.unoeuro.com;Port=3306;Database=sezginsahin_dk_db_tys;Uid=sezginsahin_dk;Pwd=YOUR_NEW_PASSWORD;"
   ```

2. **Or set in launchSettings.json for development:**
   ```json
   {
     "profiles": {
       "LotteryTracker.API": {
         "environmentVariables": {
           "ConnectionStrings__DefaultConnection": "your-connection-string-here"
         }
       }
     }
   }
   ```

### Option C: appsettings.Development.json (Development Only)

1. **Create appsettings.Development.json** (already in .gitignore):
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=mysql18.unoeuro.com;Port=3306;Database=sezginsahin_dk_db_tys;Uid=sezginsahin_dk;Pwd=YOUR_NEW_PASSWORD;"
     }
   }
   ```

2. **This file will NOT be committed** to Git

## Next Steps

1. ✅ .gitignore created - protects future commits
2. ✅ appsettings.Example.json created - template for new developers
3. ⚠️ Choose one of the Git history cleaning methods above
4. ⚠️ **CHANGE YOUR DATABASE PASSWORD**
5. ⚠️ Set up secure configuration using one of the methods above
6. ⚠️ Force push the cleaned repository
7. ⚠️ Have all team members re-clone the repository

## Testing

After setup, verify your connection string is loaded:
```powershell
dotnet run
```

The app should connect to the database without the connection string being in appsettings.json.
