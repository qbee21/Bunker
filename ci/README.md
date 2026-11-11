# iOS signing inputs for GitHub Actions

The default manual run (`sign_ipa: false`) exports the Unity Xcode project and
builds it for the iOS Simulator without Apple credentials. This validates the
iOS export, but cannot create an installable IPA.

Set `sign_ipa: true` only after the signing secrets below have been configured.

Do not place certificates, profiles, passwords, API keys, or tokens in this repository.

The manual `iOS IPA` workflow expects these GitHub Actions repository secrets:

| Secret | Value |
| --- | --- |
| `UNITY_LICENSE` | Activated Unity `.ulf` license content for the CI build account. |
| `UNITY_EMAIL` and `UNITY_PASSWORD` | Unity account used for a Personal license; store both only as GitHub Secrets. |
| `UNITY_SERIAL` | Unity Pro serial, only when using Pro instead of Personal. |
| `APPLE_TEAM_ID` | Apple Developer Team ID. |
| `IOS_DISTRIBUTION_P12_BASE64` | Base64 of the Apple Distribution `.p12`. |
| `IOS_DISTRIBUTION_P12_PASSWORD` | Password for that `.p12`. |
| `IOS_APPSTORE_PROFILE_BASE64` | Base64 of the App Store provisioning profile for `com.starodymov.bunker`. |
| `KEYCHAIN_PASSWORD` | Newly generated disposable password used only for the temporary CI keychain. |

Prepare Base64 values locally without printing their contents, then paste the result directly into the GitHub secret UI. Use a short-lived local file or clipboard and remove it afterward. Do not add the generated strings to workflow logs.

The workflow intentionally does not upload to TestFlight or App Store Connect. A default run retains the unsigned Xcode project; a signed run additionally retains an `.xcarchive` and signed `.ipa`.

The workflow reads the provisioning-profile display name from the installed profile; do not create a separate secret for it.
