'use strict';
const fs = require('node:fs');
const path = require('node:path');
const portraitBrowserMode = process.env.OASIS_PORTRAIT_BROWSER_ENABLED === 'true';
const hyperDriveSettingsMode = process.env.OASIS_HYPERDRIVE_SETTINGS_BROWSER_ENABLED === 'true';
const holonStoragePolicyMode = process.env.OASIS_HOLON_STORAGE_POLICY_BROWSER_ENABLED === 'true';
const profileHydrationMode = process.env.OASIS_PROFILE_HYDRATION_BROWSER_ENABLED === 'true';
const browserMode = process.env.OASIS_PROFILE_BROWSER_ENABLED === 'true';
const popupBreadthMode = process.env.OASIS_PORTAL_POPUP_BREADTH_BROWSER_ENABLED === 'true';
const subscriptionMode = process.env.OASIS_SUBSCRIPTION_BROWSER_ENABLED === 'true';
const usageMode = process.env.OASIS_SUBSCRIPTION_USAGE_BROWSER_ENABLED === 'true';
const notificationMode = process.env.OASIS_SUBSCRIPTION_NOTIFICATIONS_BROWSER_ENABLED === 'true';
const appOwnerMode = process.env.OASIS_SUBSCRIPTION_APP_OWNER_BROWSER_ENABLED === 'true';
const appUsageMode = process.env.OASIS_SUBSCRIPTION_APP_USAGE_BROWSER_ENABLED === 'true';
const appBillingReadMode = process.env.OASIS_SUBSCRIPTION_APP_BILLING_READ_BROWSER_ENABLED === 'true';
const personalBillingMode = process.env.OASIS_SUBSCRIPTION_PERSONAL_BILLING_BROWSER_ENABLED === 'true';
const personalConsentReviewMode = process.env.OASIS_SUBSCRIPTION_PERSONAL_CONSENT_REVIEW_BROWSER_ENABLED === 'true';
const subscriptionAdminReadMode = process.env.OASIS_SUBSCRIPTION_ADMIN_READ_BROWSER_ENABLED === 'true';
const subscriptionAdminGrantMode = process.env.OASIS_SUBSCRIPTION_ADMIN_GRANT_BROWSER_ENABLED === 'true';
const subscriptionAdminAccountsMode = process.env.OASIS_SUBSCRIPTION_ADMIN_ACCOUNTS_BROWSER_ENABLED === 'true';
const subscriptionAdminOutboxMode = process.env.OASIS_SUBSCRIPTION_ADMIN_OUTBOX_BROWSER_ENABLED === 'true';
const subscriptionAdminCatalogueMode = process.env.OASIS_SUBSCRIPTION_ADMIN_CATALOGUE_BROWSER_ENABLED === 'true';
const dashboardMode = process.env.OASIS_DASHBOARD_BROWSER_ENABLED === 'true';
const subscriptionAdminOwnersMode = process.env.OASIS_SUBSCRIPTION_ADMIN_OWNERS_BROWSER_ENABLED === 'true';
const subscriptionAdminUsageMode = process.env.OASIS_SUBSCRIPTION_ADMIN_USAGE_BROWSER_ENABLED === 'true';
const appFreshBillingMode = process.env.OASIS_SUBSCRIPTION_APP_FRESH_BILLING_BROWSER_ENABLED === 'true';
const appPayNowMode = process.env.OASIS_SUBSCRIPTION_APP_PAY_NOW_BROWSER_ENABLED === 'true';
const appBillingControlMode = process.env.OASIS_SUBSCRIPTION_APP_BILLING_CONTROL_BROWSER_ENABLED === 'true';
if (personalBillingMode && !subscriptionMode) throw new Error('Personal billing requires the actual subscription browser host.');
if (personalConsentReviewMode && !subscriptionMode) throw new Error('Personal consent review requires the actual subscription browser host.');
if (subscriptionAdminReadMode && !subscriptionMode) throw new Error('Subscription admin reads require the owned subscription browser host.');
if (subscriptionAdminGrantMode && !subscriptionMode) throw new Error('Subscription admin grants require the owned subscription browser host.');
if (subscriptionAdminAccountsMode && !subscriptionMode) throw new Error('Subscription admin accounts require the owned subscription browser host.');
if (subscriptionAdminOutboxMode && !subscriptionMode) throw new Error('Subscription admin outbox requires the owned subscription browser host.');
if (subscriptionAdminCatalogueMode && !subscriptionMode) throw new Error('Subscription catalogue requires the owned subscription browser host.');
if (profileHydrationMode && !subscriptionMode) throw new Error('Profile hydration requires the owned single-case browser host.');
if (dashboardMode && !subscriptionMode) throw new Error('Dashboard verification requires the owned subscription browser host.');
if (subscriptionAdminOwnersMode && !subscriptionMode) throw new Error('App owner administration requires the owned subscription browser host.');
if (subscriptionAdminUsageMode && !subscriptionMode) throw new Error('Administrator usage requires the owned subscription browser host.');
if (appFreshBillingMode && !appPayNowMode) throw new Error('Fresh app billing requires the actual app payment browser host.');
if (appPayNowMode && !appBillingReadMode) throw new Error('App payments require the billing read contract.');
if (appBillingControlMode && !appBillingReadMode) throw new Error('App billing controls require the billing read contract.');
if (appBillingReadMode && !appOwnerMode) throw new Error('App billing read verification requires the app owner controller.');
if (appUsageMode && !appOwnerMode) throw new Error('App usage verification requires the app owner controller.');
if (appOwnerMode && !notificationMode) throw new Error('App owner verification requires the notification controller.');
if (notificationMode && !usageMode) throw new Error('Notification verification requires usage browser mode.');
if (usageMode && !subscriptionMode) throw new Error('Usage verification requires subscription browser mode.');
if (subscriptionMode && !browserMode) throw new Error('Subscription browser verification requires the owned browser runtime.');
if (portraitBrowserMode && (!browserMode || subscriptionMode)) throw new Error('Portrait verification requires the three-provider profile browser host.');
const portraitHandoffs = Number(process.env.OASIS_PORTRAIT_BROWSER_HANDOFF_COUNT || '3');
if (portraitBrowserMode && ![3, 6].includes(portraitHandoffs)) throw new Error('Select exactly three or six owned portrait handoffs.');
if (hyperDriveSettingsMode && (!browserMode || subscriptionMode || portraitBrowserMode))
  throw new Error('HyperDrive settings requires its three-provider browser host.');
if (popupBreadthMode && (!browserMode || subscriptionMode || portraitBrowserMode || hyperDriveSettingsMode))
  throw new Error('Popup breadth requires its single-case actual browser host.');
const expectedCases = popupBreadthMode ? 1 : hyperDriveSettingsMode ? 3 : portraitBrowserMode ? portraitHandoffs : subscriptionMode ? 1 : 6;
if (holonStoragePolicyMode && !hyperDriveSettingsMode) throw new Error('Storage policy requires the actual HyperDrive browser host.');
const { verify } = require(popupBreadthMode ? './files-notifications-browser-native-http.cjs' : holonStoragePolicyMode ? './holon-storage-policy-browser-native-http.cjs' : hyperDriveSettingsMode ? './hyperdrive-settings-browser-native-http.cjs' : portraitBrowserMode ? './portrait-browser-native-http.cjs' : profileHydrationMode ? './profile-hydration-browser-native-http.cjs' : subscriptionAdminUsageMode ? './subscription-admin-usage-browser-native-http.cjs' : subscriptionAdminOwnersMode ? './subscription-admin-owners-browser-native-http.cjs' : dashboardMode ? './dashboard-browser-native-http.cjs' : subscriptionAdminCatalogueMode ? './subscription-admin-catalogue-browser-native-http.cjs' : subscriptionAdminOutboxMode ? './subscription-admin-outbox-browser-native-http.cjs' : subscriptionAdminAccountsMode ? './subscription-admin-accounts-browser-native-http.cjs' : subscriptionAdminGrantMode ? './subscription-admin-grant-browser-native-http.cjs' : subscriptionAdminReadMode ? './subscription-admin-read-browser-native-http.cjs' : personalConsentReviewMode ? './subscription-personal-consent-review-browser-native-http.cjs' : personalBillingMode ? './subscription-personal-billing-browser-native-http.cjs' : appFreshBillingMode ? './subscription-app-fresh-billing-browser-native-http.cjs' : appPayNowMode ? './subscription-app-pay-now-browser-native-http.cjs' : appBillingControlMode ? './subscription-app-billing-control-browser-native-http.cjs' : appBillingReadMode ? './subscription-app-billing-read-browser-native-http.cjs' : appUsageMode ? './subscription-app-usage-browser-native-http.cjs' : appOwnerMode ? './subscription-app-owner-browser-native-http.cjs' : notificationMode ? './subscription-notifications-browser-native-http.cjs' : usageMode ? './subscription-usage-browser-native-http.cjs' : subscriptionMode ? './subscription-browser-native-http.cjs' :
  browserMode ? './profile-browser-native-http.cjs' : './profile-sdk-native-http.cjs');
const root = fs.realpathSync(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT);
const parent = fs.realpathSync(process.env.RUNNER_TEMP);
if (!root.startsWith(parent + path.sep) || !path.basename(root).startsWith('oasis-profile-sdk-'))
  throw new Error('An owned CI handoff directory is required.');
const claimed = new Set();
let completed = 0;
let failed = false;
const deadline = setTimeout(() => {
  console.error('SDK handoff timed out before all ' + expectedCases + ' owned cases completed.');
  process.exitCode = 1; watcher.close(); process.exit(1);
}, 240000);
async function consume(name) {
  if (!/^[0-9a-f]{32}\.request\.json$/.test(name) || claimed.has(name)) return;
  claimed.add(name);
  const stem = name.slice(0, -'.request.json'.length);
  const response = path.join(root, stem + '.response.json');
  let result;
  try {
    const input = fs.readFileSync(path.join(root, name), 'utf8');
    if (Buffer.byteLength(input) > 8192) throw new Error('Bounded SDK handoff input required.');
    result = await verify(JSON.parse(input));
    if (popupBreadthMode || hyperDriveSettingsMode) console.log(JSON.stringify(result));
    else if (portraitBrowserMode) console.log(JSON.stringify({
      portraitBrowserVerified: result.portraitBrowserVerified, providerType: result.providerType,
      uploadRequests: result.uploadRequests, actualFragmentVerified: result.actualFragmentVerified,
      actualSdkHttpVerified: result.actualSdkHttpVerified, reloadWithoutCachedImageVerified: result.reloadWithoutCachedImageVerified,
      accountSwitchVerified: result.accountSwitchVerified, queuedOriginalUploadVerified: result.queuedOriginalUploadVerified,
      oldAvatarUiPublicationSuppressedVerified: result.oldAvatarUiPublicationSuppressedVerified,
      editAfterPortraitVerified: result.editAfterPortraitVerified, profilePatchRequests: result.profilePatchRequests,
      sameBearerClientReplacementVerified: result.sameBearerClientReplacementVerified,
      concurrentNativeUpdateVerified: result.concurrentNativeUpdateVerified,
      productionStartupVerified: result.productionStartupVerified
    }));
    else if (subscriptionMode) console.log(JSON.stringify(result));
    else if (browserMode) console.log(JSON.stringify({
      profileBrowserVerified: result.browserVerified, openMs: result.openMs,
      saveMs: result.saveMs, profileWrites: result.profileWrites
    }));
  } catch (error) {
    failed = true; console.error(error.message);
    result = { isError: true, message: 'The actual SDK/native HTTP check failed.' };
  }
  fs.writeFileSync(response + '.tmp', JSON.stringify(result), { flag: 'wx', mode: 0o600 });
  fs.renameSync(response + '.tmp', response);
  completed++;
  if (completed === expectedCases) {
    clearTimeout(deadline); watcher.close();
    process.exitCode = failed ? 1 : 0;
    console.log(JSON.stringify({ completed, failed }));
  }
}
const watcher = fs.watch(root, (_, filename) => {
  if (filename) consume(String(filename)).catch(error => {
    console.error(error.message); process.exitCode = 1; clearTimeout(deadline); watcher.close();
  });
});
for (const name of fs.readdirSync(root))
  consume(name).catch(error => {
    console.error(error.message); process.exitCode = 1; clearTimeout(deadline); watcher.close();
  });
