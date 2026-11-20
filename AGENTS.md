# AlMithaq package cleanup preference

The user requires removing older APK, AAB, APKS and APK signature-sidecar artifacts after each new verified release. Keep only the newest deliverable. This applies to build output/intermediate copies and the chat's outputs/work folders as well. Preserve source code, signing keys, dependencies, and other projects. Do not remove the user's copies outside these project folders.

Use `C:\Users\ADMIN\Documents\Codex\2026-11-03\new-chat-2\work\clean-almithaq-packages.ps1 -KeepPath <absolute verified new package path>` after verifying a new release and before handing it to the user. The user already authorized this ongoing cleanup preference; no repeated confirmation is needed.
