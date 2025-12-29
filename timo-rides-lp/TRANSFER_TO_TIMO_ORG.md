# Transfer Project to timo-org Vercel Team

## Steps to Transfer

1. **Get access to timo-org Vercel team**
   - Ask a team admin to add you to the `timo-org` Vercel team
   - Or have them deploy from the team account

2. **Switch to timo-org team in Vercel CLI**
   ```bash
   vercel teams switch timo-org
   ```

3. **Link project to team**
   ```bash
   cd /Volumes/Storage/OASIS_CLEAN/timo-rides-lp
   vercel link --scope timo-org
   ```

4. **Redeploy to team account**
   ```bash
   vercel --prod
   ```

5. **Add domain in team account**
   - Go to Vercel Dashboard → timo-org team → Your project
   - Settings → Domains → Add `timorides.com`
   - Since DNS already points to Vercel, it should work automatically

## Alternative: Deploy via Dashboard

1. Log into Vercel with account that has access to `timo-org` team
2. Import project: `maxgershfield/timo-rides-pp-lp`
3. Make sure it's in the `timo-org` team (not personal)
4. Add domain `timorides.com` in project settings
5. Deploy




