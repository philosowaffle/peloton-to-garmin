
# Migrating from V5 to V6

Version 6 introduces a new way of authenticating with Peloton and removes a few different capabilities that were added while investigating solutions.

First, identify your [install method](../install/index.md#-start-here-to-explore-install-options).  Then, use the table below to determine if you need to take any special action.  A ✔️ means there is something for you to do.  Follow the hyperlink to see what steps to take.

| Breaking Change | Build From Source | Docker Headless | Docker WebUI | GitHubAction | Windows Exe |
|:----------------|:------------------|:----------------|:-------------|:-------------|:------------|
| [SessionId Removed](#sessionid-removed) | ✔️ |  ✔️  |  ✔️  |  ✔️  |  ✔️  |
| [New Peloton Authentication](#new-peloton-authentication) | ✔️ |  ✔️  |  ✔️  | ❌  |  ✔️  |


## Breaking Changes

### SessionId Removed

While investigating a solution for the [Peloton Auth Errors](https://github.com/philosowaffle/peloton-to-garmin/issues/795) a temporary solution was introduced that allowed  you to [authenticate via SessionId](https://philosowaffle.github.io/peloton-to-garmin/v5.1.0/configuration/peloton/#peloton-session-id).  This option stopped working somewhere along the way and is officially removed in v6.

If you edited your configuration or settings to specify SessionId, you may delete it entirely.

### New Peloton Authentication

Some users have reported needing to delete their `data/` directory before this new authentication scheme would work.  I'm not exactly sure why that is, it wasn't intentional, but worth noting.  This is inconvenient as it will require you to re-configure all of your settings again after the fact.  So if possible, see if P2G works first without deleting anything, if not, then try deleting the `data/` directory.

To locate your configuration/settings/data start [here](../configuration/index.md).

