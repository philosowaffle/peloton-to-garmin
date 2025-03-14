# Contributing

Enhancements and fixes are always welcome. Feel free to contribute to any of the Issues not already assigned to another person.

## 🐛 Bug Fixes

Feel free to dive right in and help fix bugs when you find them.  Just make sure you've thoroughly tested the fix and be sure to read about [creating a good pull request](#creating-a-good-pull-request).

## ✨ New Features, Refactors, and Enhancements

I really appreciate anyone willing to dive into the deep end to tackle more complex changes in P2G.  Here are some steps I ask you take before embarking on this journey.

1. If an [Issue](https://github.com/philosowaffle/peloton-to-garmin/issues) doesn't already exist, please create one.
1. Ensure the Issue describes in detail what the change is and why its needed
1. Use the Issue to propose an implementation plan
1. See [creating a good pull request](#creating-a-good-pull-request)

Please also keep the following in mind:

1. P2G should never introduce any breaking or non-backwards compatible changes unless we are targeting a Major Version Release
1. P2G supports many [flavors of installl](install/index.md), all enhancements need to be implemented and tested on all flavors, or explicitly documented as not supported.

!!! danger "[What if you never merge my PR?](#what-if-you-never-merge-my-pr)"

## Creating a good Pull Request

Ensure that your code:

1. Compiles
1. Follows established conventions of the codebase (check your white spaces)
1. Keeps changes focused and minimizes scope creep
1. Works for all flavors of P2G
1. Unit tests have been added and pass
1. Documentation Site has been updated
1. Example Config Files and Docker files have been updated (when appropriate)
1. Update the [Release Notes file](https://github.com/philosowaffle/peloton-to-garmin/blob/master/vNextReleaseNotes.md)

## Contribute to this site

This documentation site is maintained from the repository, you can find and edit it [here.](https://github.com/philosowaffle/peloton-to-garmin/tree/master/mkdocs)

## 👿 What if you never merge my PR?

First, I'm sorry, I really do value that you've taken time to work through the code and give back to the community. Seriously, so few people actually do this, so thank you.

There will be times where I choose not to accept what may otherwise be seen as a perfectly reasonable change in yours or others minds.

I reserve this right as the **primary maintainer** and **first line support** for this project.  There will be changes that I may feel (*subjectively*) will make it harder for me to do my job and I will likely not merge these.

Always, my motiviations are:

1. The stability of P2G for all users
1. Long term maintainability (*for me*)
1. Ease of providing first line support to others
1. Ease of future enhancements (*for me*)
1. Ease of rollback, or hotfixing (*for me*)
1. Providing the most functionality while minimizing the barrier of entry
